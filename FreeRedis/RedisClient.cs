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
        internal readonly BaseAdapter _adapter;

        protected RedisClient(BaseAdapter adapter)
        {
            _adapter = adapter;
        }

        /// <summary>
        /// Pooling RedisClient
        /// </summary>
        public RedisClient(ConnectionStringBuilder connectionString, params ConnectionStringBuilder[] slaveConnectionStrings)
        {
            _adapter = new PoolingAdapter(this, connectionString, slaveConnectionStrings);
        }

        /// <summary>
        /// Cluster RedisClient
        /// </summary>
        public RedisClient(ConnectionStringBuilder[] clusterConnectionStrings)
        {
            throw new NotImplementedException();
            //_adapter = new ClusterAdapter(clusterConnectionStrings);
        }

        /// <summary>
        /// Sentinel RedisClient
        /// </summary>
        public RedisClient(ConnectionStringBuilder sentinelConnectionString, string[] sentinels, bool rw_splitting)
        {
            _adapter = new SentinelAdapter(this, sentinelConnectionString, sentinels, rw_splitting);
        }

        /// <summary>
        /// Single inside RedisClient
        /// </summary>
        protected internal RedisClient(string host, bool ssl, TimeSpan connectTimeout, TimeSpan receiveTimeout, TimeSpan sendTimeout, Action<RedisClient> connected)
        {
            _adapter = new SingleInsideAdapter(this, host, ssl, connectTimeout, receiveTimeout, sendTimeout, connected);
        }

        public void Dispose()
        {
            _adapter.Dispose();
        }

        protected void CheckUseTypeOrThrow(params UseType[] useTypes)
        {
            if (useTypes?.Contains(_adapter.UseType) == true) return;
            throw new RedisException($"RedisClient: Method cannot be used in {_adapter.UseType} mode.");
        }

        internal bool _isThrowRedisSimpleError { get; set; } = true;
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

        protected T2 Call<T2>(CommandPacket cmd, Func<RedisResult<T2>, T2> parse) => Call<T2, T2>(cmd, parse);
        protected T2 Call<T1, T2>(CommandPacket cmd, Func<RedisResult<T1>, T2> parse) => _adapter.Call<T1, T2>(cmd, parse);
        //protected IRedisSocket CallReadWhile(Action<object> ondata, Func<bool> next, CommandBuilder cmd)
        //{
        //    var rds = GetRedisSocket(cmd);
        //    var cli = rds.Client ?? this;
        //    rds.Write(cmd);

        //    new Thread(() =>
        //    {
        //        cli._state = ClientStatus.ReadWhile;
        //        var oldRecieveTimeout = rds.Socket.ReceiveTimeout;
        //        rds.Socket.ReceiveTimeout = 0;
        //        try
        //        {
        //            do
        //            {
        //                try
        //                {
        //                    var data = rds.Read<object>().Value;
        //                    ondata?.Invoke(data);
        //                }
        //                catch (IOException ex)
        //                {
        //                    Console.WriteLine(ex.Message);
        //                    if (rds.IsConnected) throw;
        //                    break;
        //                }
        //            } while (next());
        //        }
        //        finally
        //        {
        //            rds.Socket.ReceiveTimeout = oldRecieveTimeout;
        //            cli._state = ClientStatus.Normal;
        //        }
        //    }).Start();

        //    return rds;
        //}

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
