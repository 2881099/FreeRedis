using FreeRedis.Internal;
using FreeRedis.Internal.ObjectPool;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace FreeRedis
{
	public partial class RedisClient : IDisposable
    {
        static ThreadLocal<Random> _rnd = new ThreadLocal<Random>();
        internal int _exclusived;
        internal RedisClientPool _pooltemp; //template flag, using Return to pool
        internal Object<RedisClient> _pooltempItem;
        IdleBus<RedisClientPool> _ib;

        readonly UseType _usetype;
        public enum UseType { Pooling, Cluster, Sentinel, SingleInsideSocket, OutsideSocket }

        #region Pooling
        readonly PoolingBag _poolingBag;
        /// <summary>
        /// Pooling RedisClient
        /// </summary>
        public RedisClient(ConnectionStringBuilder connectionString, params ConnectionStringBuilder[] slaveConnectionStrings)
        {
            _usetype = UseType.Pooling;
            _poolingBag = new PoolingBag
            {
                masterHost = connectionString.Host,
                rw_plitting = slaveConnectionStrings?.Any() == true
            };
            _ib = new IdleBus<RedisClientPool>();
            _ib.Register(_poolingBag.masterHost, () => new RedisClientPool(connectionString, null));
            if (_poolingBag.rw_plitting)
                foreach (var slave in slaveConnectionStrings)
                    _ib.TryRegister($"slave_{slave.Host}", () => new RedisClientPool(slave, null));
        }
        class PoolingBag
        {
            public string masterHost;
            public bool rw_plitting;
        }
        #endregion

        #region Cluster
        /// <summary>
        /// Cluster RedisClient
        /// </summary>
        public RedisClient(ConnectionStringBuilder[] clusterConnectionStrings)
        {
            _usetype = UseType.Cluster;
            _ib = new IdleBus<RedisClientPool>();
        }
        #endregion

        #region Sentinel
        readonly SentinelBag _sentinelBag;
        /// <summary>
        /// Sentinel RedisClient
        /// </summary>
        public RedisClient(ConnectionStringBuilder sentinelConnectionString, string[] sentinels, bool rw_splitting = false)
        {
            _usetype = UseType.Sentinel;
            _sentinelBag = new SentinelBag
            {
                connectionString = sentinelConnectionString,
                sentinels = new LinkedList<string>(sentinels?.Select(a => a.ToLower()).Distinct() ?? new string[0]),
                rw_splitting = rw_splitting,
            };
            if (_sentinelBag.sentinels.Any() == false) throw new ArgumentNullException(nameof(sentinels));
            _ib = new IdleBus<RedisClientPool>();
            SentinelReset();
        }
        class SentinelBag
        {
            public string masterHost;
            public ConnectionStringBuilder connectionString;
            public LinkedList<string> sentinels;
            public bool rw_splitting;
            public int resetFlag = 0;
        }
        internal void SentinelReset()
        {
            if (_sentinelBag.resetFlag != 0) return;
            if (Interlocked.Increment(ref _sentinelBag.resetFlag) != 1)
            {
                Interlocked.Decrement(ref _sentinelBag.resetFlag);
                return;
            }
            string masterhostEnd = null;
            var allkeys = _ib.GetKeys().ToList();

            for (int i = 0; i < _sentinelBag.sentinels.Count; i++)
            {
                if (i > 0)
                {
                    var first = _sentinelBag.sentinels.First;
                    _sentinelBag.sentinels.RemoveFirst();
                    _sentinelBag.sentinels.AddLast(first.Value);
                }

                try
                {
                    using (var sentinelcli = new RedisSentinelClient(_sentinelBag.sentinels.First.Value))
                    {
                        var masterhost = sentinelcli.GetMasterAddrByName(_sentinelBag.connectionString.Host);
                        var masterConnectionString = localTestHost(masterhost, Model.RoleType.Master);
                        if (masterConnectionString == null) continue;
                        masterhostEnd = masterhost;

                        if (_sentinelBag.rw_splitting)
                        {
                            foreach (var slave in sentinelcli.Salves(_sentinelBag.connectionString.Host))
                            {
                                ConnectionStringBuilder slaveConnectionString = localTestHost($"{slave.ip}:{slave.port}", Model.RoleType.Slave);
                                if (slaveConnectionString == null) continue;
                            }
                        }

                        foreach (var sentinel in sentinelcli.Sentinels(_sentinelBag.connectionString.Host))
                        {
                            var remoteSentinelHost = $"{sentinel.ip}:{sentinel.port}";
                            if (_sentinelBag.sentinels.Contains(remoteSentinelHost)) continue;
                            _sentinelBag.sentinels.AddLast(remoteSentinelHost);
                        }
                        return;
                    }
                }
                catch
                {
                }
            }

            foreach (var spkey in allkeys) _ib.TryRemove(spkey);
            Interlocked.Exchange(ref _sentinelBag.masterHost, masterhostEnd);
            Interlocked.Decrement(ref _sentinelBag.resetFlag);

            ConnectionStringBuilder localTestHost(string host, Model.RoleType role)
            {
                ConnectionStringBuilder connectionString = _sentinelBag.connectionString.ToString();
                connectionString.Host = host;
                connectionString.MinPoolSize = 1;
                connectionString.MaxPoolSize = 1;
                using (var cli = new RedisClient(connectionString))
                {
                    if (cli.Role().role != role)
                        return null;

                    if (role == Model.RoleType.Master)
                    {
                        //test set/get
                    }
                }
                connectionString.MinPoolSize = _sentinelBag.connectionString.MinPoolSize;
                connectionString.MaxPoolSize = _sentinelBag.connectionString.MaxPoolSize;

                _ib.TryRegister(host, () => new RedisClientPool(connectionString, null));
                allkeys.Remove(host);

                return connectionString;
            }
        }
        #endregion

        #region InsideSocket、OutsideSocket
        internal IRedisSocket _singleRedisSocket;
        /// <summary>
        /// Single socket RedisClient
        /// </summary>
        protected internal RedisClient(string host, bool ssl, TimeSpan connectTimeout, TimeSpan receiveTimeout, TimeSpan sendTimeout, Action<RedisClient> connected)
        {
            _usetype = UseType.SingleInsideSocket;
            var rds = new DefaultRedisSocket(host, ssl);
            rds.Connected += (s, e) => connected(this);
            rds.Client = this;
            rds.ConnectTimeout = connectTimeout;
            rds.ReceiveTimeout = receiveTimeout;
            rds.SendTimeout = sendTimeout;
            _singleRedisSocket = rds;
        }
        IRedisSocket _outsiteRedisSocket;
        /// <summary>
        /// Outesite socket RedisClient
        /// </summary>
        protected internal RedisClient(IRedisSocket redisSocket)
        {
            _usetype = UseType.OutsideSocket;
            _outsiteRedisSocket = redisSocket;
        }
        #endregion

        public RedisClient GetExclusive()
        {
            string masterHost = null;
            switch (_usetype)
            {
                case UseType.Pooling:
                    masterHost = _poolingBag.masterHost;
                    break;
                case UseType.Sentinel:
                    masterHost = _sentinelBag.masterHost;
                    break;
            }
            if (masterHost != null)
            {
                var pool = _ib.Get(masterHost);
                var cli = pool.Get();
                cli.Value._exclusived++;
                cli.Value._pooltemp = pool;
                cli.Value._pooltempItem = cli;
                return cli.Value;
            }
            _exclusived++;
            return this;
        }

        protected IRedisSocket GetRedisSocket(CommandBuilder cmd)
        {
            switch (_usetype)
            {
                case UseType.Pooling:
                    if (_poolingBag.rw_plitting)
                    {
                        var cmdcfg = CommandConfig.Get(cmd._command);
                        if (cmdcfg != null)
                        {
                            if (
                                (cmdcfg.Tag | CommandTag.read) == CommandTag.read &&
                                (cmdcfg.Flag | CommandFlag.@readonly) == CommandFlag.@readonly)
                            {
                                var rndkeys = _ib.GetKeys(v => v == null || v.IsAvailable && v._policy._connectionStringBuilder.Host != _poolingBag.masterHost);
                                if (rndkeys.Any())
                                {
                                    var rndkey = rndkeys[_rnd.Value.Next(0, rndkeys.Length)];
                                    var rndpool = _ib.Get(rndkey);
                                    var rndcli = rndpool.Get();
                                    return new RedisClientPool.RedisSocketScope(rndcli, rndpool);
                                }
                            }
                        }
                    }
                    var pool = _ib.Get(_poolingBag.masterHost);
                    var cli = pool.Get();
                    return new RedisClientPool.RedisSocketScope(cli, pool);

                case UseType.Cluster:
                    return null;

                case UseType.Sentinel:
                    if (_sentinelBag.rw_splitting)
                    {
                        var cmdcfg = CommandConfig.Get(cmd._command);
                        if (cmdcfg != null)
                        {
                            if (
                                (cmdcfg.Tag | CommandTag.read) == CommandTag.read &&
                                (cmdcfg.Flag | CommandFlag.@readonly) == CommandFlag.@readonly)
                            {
                                var rndkeys = _ib.GetKeys(v => v == null || v.IsAvailable && v._policy._connectionStringBuilder.Host != _sentinelBag.masterHost);
                                if (rndkeys.Any())
                                {
                                    var rndkey = rndkeys[_rnd.Value.Next(0, rndkeys.Length)];
                                    var rndpool = _ib.Get(rndkey);
                                    var rndcli = rndpool.Get();
                                    return new RedisClientPool.RedisSocketScope(rndcli, rndpool);
                                }
                            }
                        }
                    }
                    var sentinelMaster = _sentinelBag.masterHost;
                    if (string.IsNullOrWhiteSpace(sentinelMaster))
                        throw new Exception("RedisClient.GetRedisSocket() Redis Sentinel Master is switching");
                    var senpool = _ib.Get(sentinelMaster);
                    var sencli = senpool.Get();
                    return new RedisClientPool.RedisSocketScope(sencli, senpool);

                case UseType.SingleInsideSocket:
                    return _singleRedisSocket;

                case UseType.OutsideSocket:
                    return _outsiteRedisSocket;
            }
            throw new Exception("RedisClient.GetRedisSocket() cannot return null");
        }

        public void Dispose()
        {
            if (_exclusived > 0 && --_exclusived == 0)
            {
                if (_pooltemp != null)
                {
                    _pooltemp.Return(_pooltempItem);
                    _pooltemp = null;
                    _pooltempItem = null;
                }
            }
            else
            {
                switch (_usetype)
                {
                    case UseType.Pooling:
                        _ib.Dispose();
                        break;

                    case UseType.Cluster:
                        _ib.Dispose();
                        break;

                    case UseType.Sentinel:
                        _ib.Dispose();
                        break;

                    case UseType.SingleInsideSocket:
                        _ib.Dispose();
                        _singleRedisSocket.Dispose();
                        break;

                    case UseType.OutsideSocket:
                        _ib.Dispose();
                        break;
                }
            }
            Release();
        }

        public void Release()
        {
            _state = ClientStatus.Normal;
            _pipeParses.Clear();
        }
        protected ClientStatus _state;
        protected Queue<Func<object>> _pipeParses = new Queue<Func<object>>();

        bool _isThrowRedisSimpleError { get; set; } = true;
        protected internal RedisException RedisSimpleError { get; private set; }
        protected internal IDisposable NoneRedisSimpleError()
        {
            var old_isThrowRedisSimpleError = _isThrowRedisSimpleError;
            _isThrowRedisSimpleError = false;
            return new TempDisposable(() =>
            {
                _isThrowRedisSimpleError = old_isThrowRedisSimpleError;
                RedisSimpleError = null;
            });
        }

        protected T2 Call<T2>(CommandBuilder cmd, Func<RedisResult<T2>, T2> parse) => Call<T2, T2>(cmd, parse);
        protected T2 Call<T1, T2>(CommandBuilder cmd, Func<RedisResult<T1>, T2> parse)
        {
            if (_isThrowRedisSimpleError == false) RedisSimpleError = null;
            RedisResult<T1> result = null;
            using (var rds = GetRedisSocket(cmd))
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
            var rds = GetRedisSocket(cmd);
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
        public RedisClient PSubscribe(string pattern, Action<object> onData)
        {
            var cb = "PSUBSCRIBE".Input(pattern);
            if (_state == ClientStatus.Normal)
            {
                IRedisSocket rds = null;
                rds = CallReadWhile(onData, () => rds.Client._state == ClientStatus.ReadWhile && rds.IsConnected, cb);
                return rds.Client;
            }
            else GetRedisSocket(cb).Write(cb);
            return this;
        }
        public RedisClient PSubscribe(string[] pattern, Action<object> onData)
        {
            var cb = "PSUBSCRIBE".Input(pattern);
            if (_state == ClientStatus.Normal)
            {
                IRedisSocket rds = null;
                rds = CallReadWhile(onData, () => rds.Client._state == ClientStatus.ReadWhile && rds.IsConnected, cb);
                return rds.Client;
            }
            else GetRedisSocket(cb).Write(cb);
            return this;
        }
        public long Publish(string channel, string message) => Call<long>("PUBLISH".Input(channel, message).FlagKey(channel), rt => rt.ThrowOrValue());
        public string[] PubSubChannels(string pattern) => Call<string[]>("PUBSUB".SubCommand("CHANNELS").Input(pattern), rt => rt.ThrowOrValue());
        public string[] PubSubNumSub(params string[] channels) => Call<string[]>("PUBSUB".SubCommand("NUMSUB").Input(channels).FlagKey(channels), rt => rt.ThrowOrValue());
        public long PubSubNumPat() => Call<long>("PUBLISH".SubCommand("NUMPAT"), rt => rt.ThrowOrValue());
        public void PUnSubscribe(params string[] pattern)
        {
            var cb = "PUNSUBSCRIBE".Input(pattern);
            GetRedisSocket(cb).Write(cb);
            _state = ClientStatus.Normal;
        }
        public RedisClient Subscribe(string channel, Action<object> onData)
        {
            var cb = "SUBSCRIBE".Input(channel).FlagKey(channel);
            if (_state == ClientStatus.Normal)
            {
                IRedisSocket rds = null;
                rds = CallReadWhile(onData, () => rds.Client._state == ClientStatus.ReadWhile && rds.IsConnected, cb);
                return rds.Client;
            }
            else GetRedisSocket(cb).Write(cb);
            return this;
        }
        public RedisClient Subscribe(string[] channels, Action<object> onData)
        {
            var cb = "SUBSCRIBE".Input(channels).FlagKey(channels);
            if (_state == ClientStatus.Normal)
            {
                IRedisSocket rds = null;
                rds = CallReadWhile(onData, () => rds.Client._state == ClientStatus.ReadWhile && rds.IsConnected, cb);
                return rds.Client;
            }
            else GetRedisSocket(cb).Write(cb);
            return this;
        }
        public void UnSubscribe(params string[] channels)
        {
            var cb = "UNSUBSCRIBE".Input(channels).FlagKey(channels);
            GetRedisSocket(cb).Write(cb);
            _state = ClientStatus.Normal;
        }
        #endregion

        #region 序列化写入，反序列化
        public Func<object, string> Serialize;
        public Func<string, Type, object> Deserialize;

        internal object SerializeRedisValue(object value)
        {
            if (value == null) return null;
            var type = value.GetType();
            var typename = type.ToString().TrimEnd(']');
            if (typename == "System.Byte[" ||
                typename == "System.String") return value;

            if (type.IsValueType)
            {
                bool isNullable = typename.StartsWith("System.Nullable`1[");
                var basename = isNullable ? typename.Substring(18) : typename;

                switch (basename)
                {
                    case "System.Boolean": return value.ToString() == "True" ? "1" : "0";
                    case "System.Byte": return value.ToString();
                    case "System.Char": return value.ToString()[0];
                    case "System.Decimal":
                    case "System.Double":
                    case "System.Single":
                    case "System.Int32":
                    case "System.Int64":
                    case "System.SByte":
                    case "System.Int16":
                    case "System.UInt32":
                    case "System.UInt64":
                    case "System.UInt16": return value.ToString();
                    case "System.DateTime": return ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:sszzzz", System.Globalization.DateTimeFormatInfo.InvariantInfo);
                    case "System.DateTimeOffset": return value.ToString();
                    case "System.TimeSpan": return ((TimeSpan)value).Ticks;
                    case "System.Guid": return value.ToString();
                }
            }

            if (Serialize != null) return Serialize(value);
            return value.ConvertTo<string>();
        }
        internal T DeserializeRedisValue<T>(byte[] value, Encoding encoding)
        {
            if (value == null) return default(T);
            var type = typeof(T);
            var typename = type.ToString().TrimEnd(']');
            if (typename == "System.Byte[") return (T)Convert.ChangeType(value, type);
            if (typename == "System.String") return (T)Convert.ChangeType(encoding.GetString(value), type);
            if (typename == "System.Boolean[") return (T)Convert.ChangeType(value.Select(a => a == 49).ToArray(), type);

            var valueStr = encoding.GetString(value);
            if (string.IsNullOrEmpty(valueStr)) return default(T);
            if (type.IsValueType)
            {
                bool isNullable = typename.StartsWith("System.Nullable`1[");
                var basename = isNullable ? typename.Substring(18) : typename;

                bool isElse = false;
                object obj = null;
                switch (basename)
                {
                    case "System.Boolean":
                        if (valueStr == "1") obj = true;
                        else if (valueStr == "0") obj = false;
                        break;
                    case "System.Byte":
                        if (byte.TryParse(valueStr, out var trybyte)) obj = trybyte;
                        break;
                    case "System.Char":
                        if (valueStr.Length > 0) obj = valueStr[0];
                        break;
                    case "System.Decimal":
                        if (Decimal.TryParse(valueStr, out var trydec)) obj = trydec;
                        break;
                    case "System.Double":
                        if (Double.TryParse(valueStr, out var trydb)) obj = trydb;
                        break;
                    case "System.Single":
                        if (Single.TryParse(valueStr, out var trysg)) obj = trysg;
                        break;
                    case "System.Int32":
                        if (Int32.TryParse(valueStr, out var tryint32)) obj = tryint32;
                        break;
                    case "System.Int64":
                        if (Int64.TryParse(valueStr, out var tryint64)) obj = tryint64;
                        break;
                    case "System.SByte":
                        if (SByte.TryParse(valueStr, out var trysb)) obj = trysb;
                        break;
                    case "System.Int16":
                        if (Int16.TryParse(valueStr, out var tryint16)) obj = tryint16;
                        break;
                    case "System.UInt32":
                        if (UInt32.TryParse(valueStr, out var tryuint32)) obj = tryuint32;
                        break;
                    case "System.UInt64":
                        if (UInt64.TryParse(valueStr, out var tryuint64)) obj = tryuint64;
                        break;
                    case "System.UInt16":
                        if (UInt16.TryParse(valueStr, out var tryuint16)) obj = tryuint16;
                        break;
                    case "System.DateTime":
                        if (DateTime.TryParse(valueStr, out var trydt)) obj = trydt;
                        break;
                    case "System.DateTimeOffset":
                        if (DateTimeOffset.TryParse(valueStr, out var trydtos)) obj = trydtos;
                        break;
                    case "System.TimeSpan":
                        if (Int64.TryParse(valueStr, out tryint64)) obj = new TimeSpan(tryint64);
                        break;
                    case "System.Guid":
                        if (Guid.TryParse(valueStr, out var tryguid)) obj = tryguid;
                        break;
                    default:
                        isElse = true;
                        break;
                }

                if (isElse == false)
                {
                    if (obj == null) return default(T);
                    return (T)obj;
                    //return (T)Convert.ChangeType(obj, typeof(T));
                }
            }

            if (Deserialize != null) return (T)Deserialize(valueStr, typeof(T));
            return valueStr.ConvertTo<T>();
        }
        #endregion
    }

    public enum ClientStatus { Normal, Transaction, Pipeline, ReadWhile, ClientReplyOff, ClientReplySkip }
    public enum ClusterSetSlotType { Importing, Migrating, Stable, Node }
	public enum ClusterResetType { Hard, Soft }
	public enum ClusterFailOverType { Force, TakeOver }
	public enum ClientUnBlockType { Timeout, Error }
	public enum ClientReplyType { On, Off, Skip }
	public enum ClientType { Normal, Master, Slave, PubSub }
	public enum Confirm { Yes, No }
	public enum GeoUnit { M, KM, MI, FT }
	public enum Collation { Asc, Desc }
	public enum InsertDirection { Before, After }
	public enum ScriptDebugOption { Yes, Sync, No }
	public enum BitOpOperation { And, Or, Xor, Not }
	public class GeoMember
	{
		public decimal Longitude { get; set; }
		public decimal Latitude { get; set; }
		public string Member { get; set; }

		public GeoMember(decimal longitude, decimal latitude, string member) { Longitude = longitude; Latitude = latitude; Member = member; }
	}
	public class ScanValue<T>
	{
		public long Cursor { get; set; }
		public T[] Items { get; set; }
        public long Length => Items.LongLength;
		public ScanValue(long cursor, T[] items) { Cursor = cursor; Items = items; }
	}
	public class SortedSetMember<T>
	{
		public T Member { get; set; }
		public decimal Score { get; set; }
		public SortedSetMember(T member, decimal score) { this.Member = member; this.Score = score; }
	}
    public class KeyValue<T>
    {
        public string Key { get; set; }
        public T Value { get; set; }
        public KeyValue(string key, T value) { this.Key = key; this.Value = value; }
    }
}
