using FreeRedis.Internal;
using FreeRedis.Internal.ObjectPool;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;

namespace FreeRedis
{
    public partial class RedisClient : IDisposable
    {
        internal BaseAdapter Adapter { get; }
        public event EventHandler<NoticeEventArgs> Notice;

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
        }

        /// <summary>
        /// Cluster RedisClient
        /// </summary>
        public RedisClient(ConnectionStringBuilder[] clusterConnectionStrings)
        {
            Adapter = new ClusterAdapter(this, clusterConnectionStrings);
        }

        /// <summary>
        /// Sentinel RedisClient
        /// </summary>
        public RedisClient(ConnectionStringBuilder sentinelConnectionString, string[] sentinels, bool rw_splitting)
        {
            Adapter = new SentinelAdapter(this, sentinelConnectionString, sentinels, rw_splitting);
        }

        /// <summary>
        /// Single inside RedisClient
        /// </summary>
        protected internal RedisClient(RedisClient topOwner, string host, bool ssl, TimeSpan connectTimeout, TimeSpan receiveTimeout, TimeSpan sendTimeout, Action<RedisClient> connected)
        {
            Adapter = new SingleInsideAdapter(topOwner ?? this, this, host, ssl, connectTimeout, receiveTimeout, sendTimeout, connected);
        }

        ~RedisClient() => this.Dispose();
        int _disposeCounter;
        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCounter) != 1) return;
            Adapter.Dispose();
        }

        protected void CheckUseTypeOrThrow(params UseType[] useTypes)
        {
            if (useTypes?.Contains(Adapter.UseType) == true) return;
            throw new RedisServerException($"RedisClient: Method cannot be used in {Adapter.UseType} mode.");
        }

        internal bool _isThrowRedisSimpleError { get; set; } = true;
        protected internal RedisServerException RedisSimpleError { get; private set; }
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

        public object Call(CommandPacket cmd) => Adapter.AdapterCall(cmd, rt => rt.ThrowOrValue());
        protected TValue Call<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse) => Adapter.AdapterCall(cmd, parse);

        internal T LogCall<T>(CommandPacket cmd, Func<T> func)
        {
            if (this.Notice == null) return func();
            Exception exception = null;
            Stopwatch sw = new Stopwatch();
            T ret = default(T);
            sw.Start();
            try
            {
                ret = func();
                return ret;
            }
            catch (Exception ex)
            {
                exception = ex;
                throw ex;
            }
            finally
            {
                sw.Stop();
                LogCallFinally(cmd, ret, sw, exception);
            }
        }
        void LogCallFinally<T>(CommandPacket cmd, T result, Stopwatch sw, Exception exception)
        {
            if (exception == null && _isThrowRedisSimpleError) exception = this.RedisSimpleError;
            string log;
            if (exception != null) log = $"{exception.Message}";
            else if (result is Array array)
            {
                var sb = new StringBuilder().Append("[");
                var itemindex = 0;
                foreach (var item in array)
                {
                    if (itemindex++ > 0) sb.Append(", ");
                    sb.Append(item.ToInvariantCultureToString());
                }
                log = sb.Append("]").ToString();
                sb.Clear();
            }
            else
                log = $"{result.ToInvariantCultureToString()}";
            this.OnNotice(new NoticeEventArgs(
                NoticeType.Call,
                exception ?? this.RedisSimpleError,
                $"{(cmd.WriteHost ?? "Not connected")} ({sw.ElapsedMilliseconds}ms) > {cmd}\r\n{log}",
                result));
        }
        public class NoticeEventArgs : EventArgs
        {
            public NoticeType NoticeType { get; }
            public Exception Exception { get; }
            public string Log { get; }
            public object Tag { get; }

            public NoticeEventArgs(NoticeType noticeType, Exception exception, string log, object tag)
            {
                this.NoticeType = noticeType;
                this.Exception = exception;
                this.Log = log;
                this.Tag = tag;
            }
        }
        public enum NoticeType
        {
            Call, Info
        }
        internal bool OnNotice(NoticeEventArgs e)
        {
            this.Notice?.Invoke(this, e);
            return this.Notice != null;
        }

        #region 序列化写入，反序列化
        public Func<object, string> Serialize;
        public Func<string, Type, object> Deserialize;

        internal object SerializeRedisValue(object value)
        {
            switch (value)
            {
                case null: return null;
                case string _:
                case byte[] _:
                case char _:
                    return value;
                case bool b: return b ? "1" : "0";
                case DateTime time: return time.ToString("yyyy-MM-ddTHH:mm:sszzzz", System.Globalization.DateTimeFormatInfo.InvariantInfo);
                case TimeSpan span: return span.Ticks;
                case DateTimeOffset _:
                case Guid _:
                    return value.ToString();
                default:
                    var type = value.GetType();
                    if (type.IsPrimitive && type.IsValueType) return value.ToString();
                    return Adapter.TopOwner.Serialize?.Invoke(value) ?? value.ConvertTo<string>();
            }
        }

        internal T DeserializeRedisValue<T>(byte[] value, Encoding encoding)
        {
            if (value == null) return default;

            var type = typeof(T);
            if (type == typeof(byte[])) return (T)Convert.ChangeType(value, type);
            if (type == typeof(string)) return (T)Convert.ChangeType(encoding.GetString(value), type);
            if (type == typeof(bool[])) return (T)Convert.ChangeType(value.Select(a => a == 49).ToArray(), type);
            
            var valueStr = encoding.GetString(value);
            if (string.IsNullOrEmpty(valueStr)) return default;

            var isNullable = type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (isNullable) type = type.GetGenericArguments().First();

            if (type == typeof(bool)) return (T)(object)(valueStr == "1");
            if (type == typeof(char)) return valueStr.Length > 0 ? (T)(object)valueStr[0] : default;
            if (type == typeof(TimeSpan))
            {
                if (long.TryParse(valueStr, out var i64Result)) return (T)(object)new TimeSpan(i64Result);
                return default;
            }

            var parse = type.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), type.MakeByRefType() }, null);
            if (parse != null)
            {
                var parameters = new object[] { valueStr, null };
                var succeeded = (bool)parse.Invoke(null, parameters);
                if (succeeded) return (T)parameters[1];
                return default;
            }

            return Adapter.TopOwner.Deserialize != null ? (T)Adapter.TopOwner.Deserialize.Invoke(valueStr, typeof(T)) : valueStr.ConvertTo<T>();
        }
        #endregion
    }

    public class RedisClientException : Exception
    {
        public RedisClientException(string message) : base(message) { }
    }

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

    /// <summary>
    /// redis version >=6.2: Added the GT and LT options.
    /// </summary>
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
