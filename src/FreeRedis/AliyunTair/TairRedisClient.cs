using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;

namespace FreeRedis.AliyunTair
{
    public class TairRedisClient : RedisClient, ITairRedisClient
    {
        protected TairRedisClient(BaseAdapter adapter) : base(adapter)
        {
        }

        public TairRedisClient(ConnectionStringBuilder connectionString, params ConnectionStringBuilder[] slaveConnectionStrings) : base(connectionString, slaveConnectionStrings)
        {
        }

        public TairRedisClient(ConnectionStringBuilder[] clusterConnectionStrings, Dictionary<string, string> hostMappings = null) : base(clusterConnectionStrings, hostMappings)
        {
        }

        public TairRedisClient(ConnectionStringBuilder[] connectionStrings, Func<string, string> redirectRule) : base(connectionStrings, redirectRule)
        {
        }

        public TairRedisClient(ConnectionStringBuilder sentinelConnectionString, string[] sentinels, bool rw_splitting) : base(sentinelConnectionString, sentinels, rw_splitting)
        {
        }

        protected internal TairRedisClient(RedisClient topOwner, string host, bool ssl, RemoteCertificateValidationCallback certificateValidation, LocalCertificateSelectionCallback certificateSelection, TimeSpan connectTimeout, TimeSpan receiveTimeout, TimeSpan sendTimeout, Action<RedisClient> connected, Action<RedisClient> disconnected) : base(topOwner, host, ssl, certificateValidation, certificateSelection, connectTimeout, receiveTimeout, sendTimeout, connected, disconnected)
        {
        }

        public long ExHSet<T>(string key, string field, T value, TimeSpan? timeout = null)
        {
            var command = "EXHSET"
                .InputKey(key, field)
                .InputRaw(SerializeRedisValue(value));
            if (timeout != null)
            {
                command.InputIf(timeout.Value.TotalSeconds >= 1, "EX", (long)timeout.Value.TotalSeconds)
                    .InputIf(timeout.Value.TotalSeconds < 1 && timeout.Value.TotalMilliseconds >= 1,
                        "PX", (long)timeout.Value.TotalMilliseconds);
            }
            return Call(command, rt => rt.ThrowOrValue<long>());
        }

        public bool ExHMSet<T>(string key, string field, T value, params object[] fieldValues)
        {
            if (fieldValues != null && fieldValues.Any())
            {
                return Call("EXHMSET".InputKey(key, field).InputRaw(SerializeRedisValue(value))
                    .InputKv(fieldValues, false, SerializeRedisValue), rt => rt.ThrowOrValue<string>()) == "OK";
            }
            return Call("EXHMSET".InputKey(key, field).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<string>()) == "OK";
        }
        public bool ExHMSet<T>(string key, Dictionary<string, T> keyValues) => Call("EXHMSET".InputKey(key).InputKv(keyValues, false, SerializeRedisValue), rt => rt.ThrowOrValue<string>()) == "OK";
        public string ExHGet(string key, string field) => Call("EXHGET".InputKey(key, field), rt => rt.ThrowOrValue<string>());
        public T ExHGet<T>(string key, string field) => Call("EXHGET".InputKey(key, field).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));
        public long ExHPTtl(string key, string field) => Call("EXHPTTL".InputKey(key, field), rt => rt.ThrowOrValue<long>());
        public long ExHTtl(string key, string field) => Call("EXHTTL".InputKey(key, field), rt => rt.ThrowOrValue<long>());

        public bool ExHExpireTime(string key, string field, TimeSpan expireTimeSpan, bool isMillisecond = false)
        {
            if (expireTimeSpan.TotalSeconds <= 0)
            {
                return false;
            }
            var expireTime = (long)expireTimeSpan.TotalSeconds;
            //EXHEXPIRE  //精确到秒
            //EXHPEXPIRE //精确到毫秒
            if (isMillisecond)
            {
                expireTime = (long)expireTimeSpan.TotalMilliseconds;
                return Call("EXHPEXPIRE".InputKey(key, field, expireTime), rt => rt.ThrowOrValue<bool>());
            }
            return Call("EXHEXPIRE".InputKey(key, field, expireTime), rt => rt.ThrowOrValue<bool>());
        }

        public bool ExHExpireTime(string key, string field, DateTime expireTime, bool isMillisecond = false)
        { 
            //EXHEXPIREAT  //精确到秒
            //EXHPEXPIREAT //精确到毫秒
             DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
             var timeSpan = (long)expireTime.ToUniversalTime().Subtract(_epoch).TotalSeconds;
            if (isMillisecond)
            {
                timeSpan = (long)expireTime.ToUniversalTime().Subtract(_epoch).TotalMilliseconds;
                return Call("EXHPEXPIREAT".InputKey(key, field, timeSpan), rt => rt.ThrowOrValue<bool>());
            }
            return Call("EXHEXPIREAT".InputKey(key, field, timeSpan), rt => rt.ThrowOrValue<bool>());
        }

        public long ExHIncrBy(string key, string field, long num, TimeSpan? timeout = null)
        {
            var command = "EXHINCRBY".InputKey(key, field, num);
            if (timeout != null)
            {
                command.InputIf(timeout.Value.TotalSeconds >= 1, "EX", (long)timeout.Value.TotalSeconds)
                    .InputIf(timeout.Value.TotalSeconds < 1 && timeout.Value.TotalMilliseconds >= 1,
                        "PX", (long)timeout.Value.TotalMilliseconds);
            }
            return Call(command, rt => rt.ThrowOrValue<long>());
        }

        public decimal ExHIncrByFloat(string key, string field, decimal num, TimeSpan? timeout = null)
        {
            var command = "EXHINCRBYFLOAT".InputKey(key, field, num);
            if (timeout != null)
            {
                command.InputIf(timeout.Value.TotalSeconds >= 1, "EX", (long)timeout.Value.TotalSeconds)
                    .InputIf(timeout.Value.TotalSeconds < 1 && timeout.Value.TotalMilliseconds >= 1,
                        "PX", (long)timeout.Value.TotalMilliseconds);
            }
            return Call(command, rt => rt.ThrowOrValue<decimal>());
        }

        public string[] ExHMGet(string key, params string[] fields) => Call("EXHMGET".InputKey(key, fields), rt => rt.ThrowOrValue<string[]>());

        public T[] ExHMGet<T>(string key, params string[] fields) => HReadArray<T>("EXHMGET".InputKey(key, fields));

        public long ExHLen(string key, bool noExpire = false) => Call("EXHLEN".InputKey(key).InputIf(noExpire, "NOEXP"), rt => rt.ThrowOrValue<long>());
        public bool ExHExists(string key, string field) => Call("EXHEXISTS".InputKey(key, field), rt => rt.ThrowOrValue<bool>());
        public long ExHStrLen(string key, string field) => Call("EXHSTRLEN".InputKey(key, field), rt => rt.ThrowOrValue<long>());
        public string[] ExHKeys(string key) => Call("EXHKEYS".InputKey(key), rt => rt.ThrowOrValue<string[]>());


        public string[] ExHVals(string key) => Call("EXHVALS".InputKey(key), rt => rt.ThrowOrValue<string[]>());
        public T[] ExHVals<T>(string key) => HReadArray<T>("EXHVALS".InputKey(key));
        public Dictionary<string, string> ExHGetAll(string key) => Call("EXHGETALL".InputKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
        public Dictionary<string, T> ExHGetAll<T>(string key) => Call("EXHGETALL".InputKey(key).FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) =>
            {
                for (var x = 0; x < a.Length; x += 2)
                {
                    a[x] = rt.Encoding.GetString(a[x].ConvertTo<byte[]>());
                    a[x + 1] = DeserializeRedisValue<T>(a[x + 1].ConvertTo<byte[]>(), rt.Encoding);
                }
                return a.MapToHash<T>(rt.Encoding);
            }));


        public ScanResult<KeyValuePair<string, string>> ExHScan(string key, long cursor, string pattern, long count) => Call("EXHSCANUNORDER"
            .InputKey(key, cursor)
            .InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
            .InputIf(count != 0, "COUNT", count), rt => rt.ThrowOrValue((a, _) => new ScanResult<KeyValuePair<string, string>>(a[0].ConvertTo<long>(),
            a[1].ConvertTo<string[]>().MapToList((k, v) => new KeyValuePair<string, string>(k.ConvertTo<string>(), v.ConvertTo<string>())).ToArray())));


        public long ExHDel(string key, params string[] fields) => Call("EXHDEL".InputKey(key, fields), rt => rt.ThrowOrValue<long>());

        T[] HReadArray<T>(CommandPacket cb) => Call(cb.FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) => a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));
    }
}
