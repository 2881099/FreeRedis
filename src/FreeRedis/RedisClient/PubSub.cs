using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeRedis.Internal
{
    public interface IPubSubSubscriber : IDisposable
    {
        RedisClient TopOwner { get; }
        IRedisSocket RedisSocket { get; }
    }
}

namespace FreeRedis
{
    partial class RedisClient
    {
        PubSub _pubsubPriv;
        object _pubsubPrivLock = new object();
        PubSub _pubsub //Sharing top RedisClient PubSub, The cluster can forward and take effect
        {
            get
            {
                if (this != Adapter.TopOwner) return Adapter.TopOwner._pubsub;
                if (_pubsubPriv == null)
                    lock (_pubsubPrivLock)
                        if (_pubsubPriv == null)
                            _pubsubPriv = new PubSub(this);
                return _pubsubPriv;
            }
        }

#if isasync
        #region async (copy from sync)
        public Task<long> PublishAsync(string channel, string message) => CallAsync("PUBLISH".Input(channel, message), rt => rt.ThrowOrValue<long>());
        public Task<string[]> PubSubChannelsAsync(string pattern) => CallAsync("PUBSUB".SubCommand("CHANNELS").Input(pattern), rt => rt.ThrowOrValue<string[]>());
        public Task<long> PubSubNumSubAsync(string channel) => CallAsync("PUBSUB".SubCommand("NUMSUB").Input(channel), rt => rt.ThrowOrValue((a, _) => a.MapToList((x, y) => y.ConvertTo<long>()).FirstOrDefault()));
        public Task<long[]> PubSubNumSubAsync(string[] channels) => CallAsync("PUBSUB".SubCommand("NUMSUB").Input(channels), rt => rt.ThrowOrValue((a, _) => a.MapToList((x, y) => y.ConvertTo<long>()).ToArray()));
        public Task<long> PubSubNumPatAsync(string message) => CallAsync("PUBLISH".SubCommand("NUMPAT").InputRaw(message), rt => rt.ThrowOrValue<long>());
        #endregion
#endif

        public IDisposable PSubscribe(string pattern, Action<string, object> handler)
        {
            if (string.IsNullOrEmpty(pattern)) throw new ArgumentNullException(nameof(pattern));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            return _pubsub.Subscribe(true, new[] { pattern }, (p, k, d) => handler(k, d));
        }
        public IDisposable PSubscribe(string[] pattern, Action<string, object> handler)
        {
            if (pattern?.Any() != true) throw new ArgumentNullException(nameof(pattern));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            return _pubsub.Subscribe(true, pattern, (p, k, d) => handler(k, d));
        }

        public long Publish(string channel, string message) => Call("PUBLISH".Input(channel, message), rt => rt.ThrowOrValue<long>());
        public string[] PubSubChannels(string pattern) => Call("PUBSUB".SubCommand("CHANNELS").Input(pattern), rt => rt.ThrowOrValue<string[]>());
        public long PubSubNumSub(string channel) => Call("PUBSUB".SubCommand("NUMSUB").Input(channel), rt => rt.ThrowOrValue((a, _) => a.MapToList((x, y) => y.ConvertTo<long>()).FirstOrDefault()));
        public long[] PubSubNumSub(string[] channels) => Call("PUBSUB".SubCommand("NUMSUB").Input(channels), rt => rt.ThrowOrValue((a, _) => a.MapToList((x, y) => y.ConvertTo<long>()).ToArray()));
        public long PubSubNumPat(string message) => Call("PUBLISH".SubCommand("NUMPAT").InputRaw(message), rt => rt.ThrowOrValue<long>());


        public void PUnSubscribe(params string[] pattern) => _pubsub.UnSubscribe(true, pattern);
        public IDisposable Subscribe(string[] channels, Action<string, object> handler)
        {
            if (channels?.Any() != true) throw new ArgumentNullException(nameof(channels));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            return _pubsub.Subscribe(false, channels, (p, k, d) => handler(k, d));
        }
        public IDisposable Subscribe(string channel, Action<string, object> handler)
        {
            if (string.IsNullOrEmpty(channel)) throw new ArgumentNullException(nameof(channel));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            return _pubsub.Subscribe(false, new[] { channel }, (p, k, d) => handler(k, d));
        }
        public void UnSubscribe(params string[] channels) => _pubsub.UnSubscribe(false, channels);


        class PubSubSubscribeDisposable : IPubSubSubscriber
        {
            PubSub _pubsub;
            public RedisClient TopOwner => _pubsub._topOwner;
            public IRedisSocket RedisSocket => _pubsub._redisSocket;
            Action _release;
            public PubSubSubscribeDisposable(PubSub pubsub, Action release)
            {
                _pubsub = pubsub;
                _release = release;
            }

            public void Dispose() => _release?.Invoke();
        }

        //ERR only (P)SUBSCRIBE / (P)UNSUBSCRIBE / PING / QUIT allowed in this context
        class PubSub : IDisposable
        {
            bool _stoped = false;
            object _lock = new object();
            internal RedisClient _topOwner;
            internal IRedisSocket _redisSocket;
            TimeSpan _redisSocketReceiveTimeoutOld;
            internal bool IsSubscribed { get; private set; }
            ConcurrentDictionary<Guid, string[]> _cancels = new ConcurrentDictionary<Guid, string[]>();
            ConcurrentDictionary<string, ConcurrentDictionary<Guid, RegisterInfo>> _registers = new ConcurrentDictionary<string, ConcurrentDictionary<Guid, RegisterInfo>>();
            const string _psub_regkey_prefix = "PSubscribe__ |";
            internal class RegisterInfo
            {
                public Guid Id { get; }
                public Action<string, string, object> Handler { get; }
                public DateTime RegTime { get; }
                public RegisterInfo(Guid id, Action<string, string, object> handler, DateTime time)
                {
                    this.Id = id;
                    this.Handler = handler;
                    this.RegTime = time;
                }
            }
            internal PubSub(RedisClient topOwner)
            {
                _topOwner = topOwner;
            }

            public void Dispose()
            {
                try
                {
                    Cancel(_cancels.Keys.ToArray());
                }
                finally
                {
                    _stoped = true;
                }
            }

            internal void Cancel(params Guid[] ids)
            {
                if (ids == null) return;
                var readyUnsubInterKeys = new List<string>();
                foreach (var id in ids)
                {
                    if (_cancels.TryRemove(id, out var oldkeys))
                        foreach (var oldkey in oldkeys)
                        {
                            if (_registers.TryGetValue(oldkey, out var oldrecvs) &&
                                oldrecvs.TryRemove(id, out var oldrecv) &&
                                oldrecvs.Any() == false)
                                readyUnsubInterKeys.Add(oldkey);
                        }
                }
                var unsub = readyUnsubInterKeys.Where(a => !a.StartsWith(_psub_regkey_prefix)).ToArray();
                var punsub = readyUnsubInterKeys.Where(a => a.StartsWith(_psub_regkey_prefix)).Select(a => a.Replace(_psub_regkey_prefix, "")).ToArray();
                if (unsub.Any()) Call("UNSUBSCRIBE".Input(unsub));
                if (punsub.Any()) Call("PUNSUBSCRIBE".Input(punsub));

                if (!_cancels.Any())
                    lock (_lock)
                        if (!_cancels.Any())
                            _redisSocket.ReleaseSocket();
            }
            internal void UnSubscribe(bool punsub, string[] channels)
            {
                channels = channels?.Distinct().Select(a => punsub ? $"{_psub_regkey_prefix}{a}" : a).ToArray();
                if (channels.Any() != true) return;
                var ids = channels.Select(a => _registers.TryGetValue(a, out var tryval) ? tryval : null).Where(a => a != null).SelectMany(a => a.Keys).Distinct().ToArray();
                Cancel(ids);
            }
            internal IDisposable Subscribe(bool psub, string[] channels, Action<string, string, object> handler)
            {
                if (_stoped) return new PubSubSubscribeDisposable(this, null);
                channels = channels?.Distinct().Where(a => !string.IsNullOrEmpty(a)).ToArray(); //In case of external modification
                if (channels?.Any() != true) return new PubSubSubscribeDisposable(this, null);

                var id = Guid.NewGuid();
                var time = DateTime.Now;
                var regkeys = channels.Select(a => psub ? $"{_psub_regkey_prefix}{a}" : a).ToArray();
                for (var a = 0; a < regkeys.Length; a++)
                {
                    ConcurrentDictionary<Guid, RegisterInfo> dict = null;
                    lock (_lock) dict = _registers.GetOrAdd(regkeys[0], k1 => new ConcurrentDictionary<Guid, RegisterInfo>());
                    dict.TryAdd(id, new RegisterInfo(id, handler, time));
                }
                lock (_lock)
                    _cancels.TryAdd(id, regkeys);
                var isnew = false;
                if (IsSubscribed == false)
                {
                    lock (_lock)
                    {
                        if (IsSubscribed == false)
                        {
                            _redisSocket = _topOwner.Adapter.GetRedisSocket(null);
                            IsSubscribed = isnew = true;
                        }
                    }
                }
                if (isnew)
                {
                    new Thread(() =>
                    {
                        _redisSocketReceiveTimeoutOld = _redisSocket.ReceiveTimeout;
                        _redisSocket.ReceiveTimeout = TimeSpan.Zero;
                        var timer = new Timer(state =>
                        {
                            _topOwner.Adapter.Refersh(_redisSocket); //防止 IdleBus 超时回收
                            try { _redisSocket.Write("PING"); } catch { }
                        }, null, 10000, 10000);
                        var readCmd = "PubSubRead".SubCommand(null).FlagReadbytes(false);
                        while (_stoped == false)
                        {
                            RedisResult rt = null;
                            try
                            {
                                rt = _redisSocket.Read(readCmd);
                            }
                            catch
                            {
                                Thread.CurrentThread.Join(100);
                                if (_cancels.Any()) continue;
                                break;
                            }
                            var val = rt.Value as object[];
                            if (val == null) continue; //special case

                            var val1 = val[0].ConvertTo<string>();
                            switch (val1)
                            {
                                case "pong":
                                case "punsubscribe":
                                case "unsubscribe":
                                    continue;
                                case "pmessage":
                                    OnData(val[1].ConvertTo<string>(), val[2].ConvertTo<string>(), val[3]);
                                    continue;
                                case "message":
                                    OnData(null, val[1].ConvertTo<string>(), val[2]);
                                    continue;
                            }
                        }
                        timer.Dispose();
                        lock (_lock)
                        {
                            IsSubscribed = false;
                            _redisSocket.ReceiveTimeout = _redisSocketReceiveTimeoutOld;
                            _redisSocket.ReleaseSocket();
                            _redisSocket.Dispose();
                            _redisSocket = null;
                        }
                    }).Start();
                }
                Call((psub ? "PSUBSCRIBE" : "SUBSCRIBE").Input(channels));
                return new PubSubSubscribeDisposable(this, () => Cancel(id));
            }
            void OnData(string pattern, string key, object data)
            {
                var regkey = pattern == null ? key : $"{_psub_regkey_prefix}{pattern}";
                if (_registers.TryGetValue(regkey, out var tryval) == false) return;
                var multirecvs = tryval.Values.OrderBy(a => a.RegTime).ToArray(); //Execute in order
                foreach (var recv in multirecvs)
                    recv.Handler(pattern, key, data);
            }
            internal void Call(CommandPacket cmd)
            {
                _topOwner.LogCall<object>(cmd, () =>
                {
                    if (IsSubscribed == false)
                        throw new RedisClientException($"Subscription not opened, unable to execute");
                    lock (_lock)
                        _redisSocket.Write(cmd);
                    return null;
                });
            }
        }
    }
}