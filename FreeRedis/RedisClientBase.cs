using FreeRedis.Internal.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FreeRedis
{
    //Pipelining: Learn how to send multiple commands at once, saving on round trip time.
    //Redis transactions: It is possible to group commands together so that they are executed as a single transaction.
    //Client side caching: Starting with version 6 Redis supports server assisted client side caching. This document describes how to use it.
    //Distributed locks: Implementing a distributed lock manager with Redis.
    //Redis keyspace notifications: Get notifications of keyspace events via Pub/Sub (Redis 2.8 or greater).
    //High Availability: Redis Sentinel is the official high availability solution for Redis.

    public abstract class RedisClientBase
    {
        protected ClientStatus _state;
        protected Queue<Func<object>> _pipeParses = new Queue<Func<object>>();

        bool _isThrowRedisSimpleError { get; set; } = true;
        protected internal RedisException RedisSimpleError { get; private set; }
        class NoneRedisSimpleErrorScopeImpl : IDisposable
        {
            public Action Release;
            public void Dispose() => Release?.Invoke();
        }
        protected internal IDisposable NoneRedisSimpleError()
        {
            _isThrowRedisSimpleError = false;
            return new NoneRedisSimpleErrorScopeImpl { Release = () => _isThrowRedisSimpleError = true };
        }

        protected abstract IRedisSocket GetRedisSocket();

        protected T2 Call<T2>(CommandBuilder cmd, Func<RedisResult<T2>, T2> parse) => Call<T2, T2>(cmd, parse);
        protected T2 Call<T1, T2>(CommandBuilder cmd, Func<RedisResult<T1>, T2> parse)
        {
            if (_isThrowRedisSimpleError == false) RedisSimpleError = null;
            RedisResult<T1> result = null;
            using (var rds = GetRedisSocket())
            {
                rds.Write(cmd);
                switch (_state)
                {
                    case ClientStatus.ClientReplyOff:
                    case ClientStatus.ClientReplySkip: //CLIENT REPLY ON|OFF|SKIP
                        return default(T2);
                    case ClientStatus.Pipeline:
                        _pipeParses.Enqueue(() =>
                        {
                            var rt = rds.Read<T1>();
                            return parse(rt);
                        });
                        return default(T2);
                    case ClientStatus.ReadWhile:
                        return default(T2);
                    case ClientStatus.Transaction:
                        return default(T2);
                }
                result = rds.Read<T1>();
                result.Encoding = rds.Encoding;
            }
            if (_isThrowRedisSimpleError == false)
            {
                if (!string.IsNullOrEmpty(result.SimpleError))
                    RedisSimpleError = new RedisException(result.SimpleError);
                result.IsErrorThrow = false;
            }
            return parse(result);
        }
        protected IRedisSocket CallReadWhile(Action<object> ondata, Func<bool> next, CommandBuilder cmd)
        {
            var rds = GetRedisSocket();
            var cli = rds.Client ?? this;
            rds.Write(cmd);

            new Thread(() =>
            {
                cli._state = ClientStatus.ReadWhile;
                var oldRecieveTimeout = rds.Socket.ReceiveTimeout;
                rds.Socket.ReceiveTimeout = 0;
                try
                {
                    do
                    {
                        try
                        {
                            var data = rds.Read<object>().Value;
                            ondata?.Invoke(data);
                        }
                        catch (IOException ex)
                        {
                            Console.WriteLine(ex.Message);
                            if (rds.IsConnected) throw;
                            break;
                        }
                    } while (next());
                }
                finally
                {
                    rds.Socket.ReceiveTimeout = oldRecieveTimeout;
                    cli._state = ClientStatus.Normal;
                }
            }).Start();

            return rds;
        }

        #region Commands Pub/Sub
		public RedisClientBase PSubscribe(string pattern, Action<object> onData)
		{
            var cb = "PSUBSCRIBE".Input(pattern);
            if (_state == ClientStatus.Normal)
            {
                IRedisSocket rds = null;
                rds = CallReadWhile(onData, () => rds.Client._state == ClientStatus.ReadWhile && rds.IsConnected, cb);
                return rds.Client;
            }
            else GetRedisSocket().Write(cb);
            return this;
		}
		public RedisClientBase PSubscribe(string[] pattern, Action<object> onData)
		{
            var cb = "PSUBSCRIBE".Input(pattern);
            if (_state == ClientStatus.Normal)
            {
                IRedisSocket rds = null;
                rds = CallReadWhile(onData, () => rds.Client._state == ClientStatus.ReadWhile && rds.IsConnected, cb);
                return rds.Client;
            }
            else GetRedisSocket().Write(cb);
            return this;
        }
		public long Publish(string channel, string message) => Call<long>("PUBLISH".Input(channel, message).FlagKey(channel), rt => rt.ThrowOrValue());
		public string[] PubSubChannels(string pattern) => Call<string[]>("PUBSUB".SubCommand("CHANNELS").Input(pattern), rt => rt.ThrowOrValue());
        public string[] PubSubNumSub(params string[] channels) => Call<string[]>("PUBSUB".SubCommand("NUMSUB").Input(channels).FlagKey(channels), rt => rt.ThrowOrValue());
        public long PubSubNumPat() => Call<long>("PUBLISH".SubCommand("NUMPAT"), rt => rt.ThrowOrValue());
        public void PUnSubscribe(params string[] pattern)
        {
            GetRedisSocket().Write("PUNSUBSCRIBE".Input(pattern));
            _state = ClientStatus.Normal;
        }
		public RedisClientBase Subscribe(string channel, Action<object> onData)
		{
            var cb = "SUBSCRIBE".Input(channel).FlagKey(channel);
            if (_state == ClientStatus.Normal)
            {
                IRedisSocket rds = null;
                rds = CallReadWhile(onData, () => rds.Client._state == ClientStatus.ReadWhile && rds.IsConnected, cb);
                return rds.Client;
            }
            else GetRedisSocket().Write(cb);
            return this;
        }
		public RedisClientBase Subscribe(string[] channels, Action<object> onData)
		{
            var cb = "SUBSCRIBE".Input(channels).FlagKey(channels);
            if (_state == ClientStatus.Normal)
            {
                IRedisSocket rds = null;
                rds = CallReadWhile(onData, () => rds.Client._state == ClientStatus.ReadWhile && rds.IsConnected, cb);
                return rds.Client;
            }
            else GetRedisSocket().Write(cb);
            return this;
        }
        public void UnSubscribe(params string[] channels)
        {
            GetRedisSocket().Write("UNSUBSCRIBE".Input(channels).FlagKey(channels));
            _state = ClientStatus.Normal;
        }
		#endregion

        public void Release()
        {
            _state = ClientStatus.Normal;
            _pipeParses.Clear();
        }
    }
}
