using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Text;

namespace FreeRedis
{
    public partial class RedisClient : IDisposable, IRedisClient
    {
        internal protected BaseAdapter Adapter { get; }
        internal protected string Prefix { get; }
        internal protected ConnectionStringBuilder ConnectionString { get; }
        public List<Func<IInterceptor>> Interceptors { get; } = new List<Func<IInterceptor>>();
        public event EventHandler<NoticeEventArgs> Notice;
        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<UnavailableEventArgs> Unavailable;

        protected RedisClient(BaseAdapter adapter)
        {
            Adapter = adapter;
        }

        /// <summary>
        /// Pooling RedisClient
        /// </summary>
        public RedisClient(ConnectionStringBuilder connectionString, params ConnectionStringBuilder[] slaveConnectionStrings)
        {
            Adapter = new PoolingAdapter(this, connectionString, slaveConnectionStrings);
            Prefix = connectionString.Prefix;
            ConnectionString = connectionString;
        }

        /// <summary>
        /// Cluster RedisClient
        /// </summary>
        public RedisClient(ConnectionStringBuilder[] clusterConnectionStrings)
        {
            Adapter = new ClusterAdapter(this, clusterConnectionStrings);
            Prefix = clusterConnectionStrings[0].Prefix;
            ConnectionString = clusterConnectionStrings[0];
        }
        /// <summary>
        /// Norman RedisClient
        /// </summary>
        public RedisClient(ConnectionStringBuilder[] connectionStrings, Func<string, string> redirectRule)
        {
            Adapter = new NormanAdapter(this, connectionStrings, redirectRule);
            Prefix = connectionStrings[0].Prefix;
            ConnectionString = connectionStrings[0];
        }

        /// <summary>
        /// Sentinel RedisClient
        /// </summary>
        public RedisClient(ConnectionStringBuilder sentinelConnectionString, string[] sentinels, bool rw_splitting)
        {
            Adapter = new SentinelAdapter(this, sentinelConnectionString, sentinels, rw_splitting);
            Prefix = sentinelConnectionString.Prefix;
            ConnectionString = sentinelConnectionString;
        }

        /// <summary>
        /// Single inside RedisClient
        /// </summary>
        protected internal RedisClient(RedisClient topOwner, string host, 
            bool ssl, RemoteCertificateValidationCallback certificateValidation, LocalCertificateSelectionCallback certificateSelection,
            TimeSpan connectTimeout, TimeSpan receiveTimeout, TimeSpan sendTimeout, 
            Action<RedisClient> connected, Action<RedisClient> disconnected)
        {
            Adapter = new SingleInsideAdapter(topOwner ?? this, this, host, ssl, certificateValidation, certificateSelection,
                connectTimeout, receiveTimeout, sendTimeout, connected, disconnected);
            Prefix = topOwner.Prefix;
            ConnectionString = topOwner.ConnectionString;
        }

        public void Dispose()
        {
            Adapter.Dispose();
            _pubsubPriv?.Dispose();
        }

        protected void CheckUseTypeOrThrow(params UseType[] useTypes)
        {
            if (useTypes?.Contains(Adapter.UseType) == true) return;
            throw new RedisClientException($"Method cannot be used in {Adapter.UseType} mode.");
        }

        public object Call(CommandPacket cmd) => Adapter.AdapterCall(cmd, rt => rt.ThrowOrValue());
        public TValue Call<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse) => Adapter.AdapterCall(cmd, parse);

        internal protected virtual T LogCall<T>(CommandPacket cmd, Func<T> func) => LogCallCtrl(cmd, func, true, true);
        internal protected virtual T LogCallCtrl<T>(CommandPacket cmd, Func<T> func, bool aopBefore, bool aopAfter)
        {
            if (aopBefore) cmd.Prefix(Prefix);
            var isnotice = this.Notice != null;
            if (isnotice == false && this.Interceptors.Any() == false) return func();
            if (cmd.IsIgnoreAop) return func();
            Exception exception = null;

            T ret = default(T);
            var isaopval = false;
            IInterceptor[] aops = new IInterceptor[this.Interceptors.Count + (isnotice ? 1 : 0)];
            Stopwatch[] aopsws = new Stopwatch[aops.Length];
            for (var idx = 0; idx < aops.Length; idx++)
            {
                aopsws[idx] = new Stopwatch();
                aopsws[idx].Start();
                if (aopBefore == false && aopAfter == false) continue;
                aops[idx] = isnotice && idx == aops.Length - 1 ? new NoticeCallInterceptor(this) : this.Interceptors[idx]?.Invoke();
                if (aopBefore == false) continue;
                var args = new InterceptorBeforeEventArgs(this, cmd, typeof(T));
                aops[idx].Before(args);
                if (args.ValueIsChanged && args.Value is T argsValue)
                {
                    isaopval = true;
                    ret = argsValue;
                }
            }
            try
            {
                if (isaopval == false) ret = func();
                return ret;
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                if (aopAfter)
                {
                    for (var idx = 0; idx < aops.Length; idx++)
                    {
                        aopsws[idx].Stop();
                        var args = new InterceptorAfterEventArgs(this, cmd, typeof(T), ret, exception, aopsws[idx].ElapsedMilliseconds);
                        aops[idx].After(args);
                    }
                }
            }
        }
        internal bool OnNotice(RedisClient cli, NoticeEventArgs e)
        {
            var topOwner = Adapter?.TopOwner ?? cli;
            if (topOwner?.Notice == null) return false;
            topOwner.Notice(topOwner, e);
            return true;
        }
        internal bool OnConnected(RedisClient cli, ConnectedEventArgs e)
        {
            var topOwner = Adapter?.TopOwner ?? cli;
            if (topOwner?.Connected == null) return false;
            topOwner.Connected(topOwner, e);
            return true;
        }
        internal bool OnDisconnected(RedisClient cli, DisconnectedEventArgs e)
        {
            var topOwner = Adapter?.TopOwner ?? cli;
            if (topOwner?.Disconnected == null) return false;
            topOwner.Disconnected(topOwner, e);
            return true;
        }
        internal bool OnUnavailable(RedisClient cli, UnavailableEventArgs e)
        {
            var topOwner = Adapter?.TopOwner ?? cli;
            if (topOwner?.Unavailable == null) return false;
            topOwner.Unavailable(topOwner, e);
            return true;
        }

        #region 序列化写入，反序列化
        public Func<object, object> Serialize;
        public Func<string, Type, object> Deserialize;
        public Func<byte[], Type, object> DeserializeRaw;

        internal object SerializeRedisValue(object value)
        {
            switch (value)
            {
                case null: return null;
                case string _:
                case byte[] _: return value;

                case bool b: return b ? "1" : "0";
                case char c: return value;
                case decimal _:
                case double _:
                case float _:
                case int _:
                case long _:
                case sbyte _:
                case short _:
                case uint _:
                case ulong _:
                case ushort _: return value.ToInvariantCultureToString();

                case DateTime time: return time.ToString("yyyy-MM-ddTHH:mm:sszzzz", System.Globalization.DateTimeFormatInfo.InvariantInfo);
                case DateTimeOffset _: return value.ToString();
                case TimeSpan span: return span.Ticks;
                case Guid _: return value.ToString();
                default:
                    if (Adapter.TopOwner.Serialize != null) return Adapter.TopOwner.Serialize(value);
                    return value.ConvertTo<string>();
            }
        }
        internal T DeserializeRedisValue<T>(byte[] valueRaw, Encoding encoding)
        {
            if (valueRaw == null) return default(T);
            var type = typeof(T);
            var typename = type.ToString().TrimEnd(']');
            if (typename == "System.Byte[") return (T)Convert.ChangeType(valueRaw, type);
            if (typename == "System.String") return (T)Convert.ChangeType(encoding.GetString(valueRaw), type);
            if (typename == "System.Boolean[") return (T)Convert.ChangeType(valueRaw.Select(a => a == 49).ToArray(), type);
            if (valueRaw.Length == 0) return default(T);

            string valueStr = null;
            if (type.IsValueType)
            {
                valueStr = encoding.GetString(valueRaw);
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
                }
            }

            if (Adapter.TopOwner.DeserializeRaw != null) return (T)Adapter.TopOwner.DeserializeRaw(valueRaw, typeof(T));

            if (valueStr == null) valueStr = encoding.GetString(valueRaw);
            if (Adapter.TopOwner.Deserialize != null) return (T)Adapter.TopOwner.Deserialize(valueStr, typeof(T));
            return valueStr.ConvertTo<T>();
        }
        #endregion
    }

    public class RedisClientException : Exception
    {
        public RedisClientException(string message) : base(message) { }
    }

    /// <summary>
    /// redis version >=6.2: Added the GT and LT options.
    /// </summary>
    public enum ZAddThan { gt, lt }
    public enum BitOpOperation { and, or, xor, not }
    public enum ClusterSetSlotType { importing, migrating, stable, node }
    public enum ClusterResetType { hard, soft }
    public enum ClusterFailOverType { force, takeover }
    public enum ClientUnBlockType { timeout, error }
    public enum ClientReplyType { on, off, skip }
    public enum ClientType { normal, master, slave, pubsub }
    public enum Collation { asc, desc }
    public enum Confirm { yes, no }
    public enum GeoUnit { m, km, mi, ft }
    public enum InsertDirection { before, after }
    public enum KeyType { none, @string, list, set, zset, hash, stream }
    public enum RoleType { Master, Slave, Sentinel }

    public class KeyValue<T>
    {
        public readonly string key;
        public readonly T value;
        public KeyValue(string key, T value) { this.key = key; this.value = value; }
    }
    public class ScanResult<T>
    {
        public readonly long cursor;
        public readonly T[] items;
        public readonly long length;
        public ScanResult(long cursor, T[] items) { this.cursor = cursor; this.items = items; this.length = items.LongLength; }
    }

}
