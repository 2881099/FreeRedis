using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
#if isasync
        #region async (copy from sync)
        public Task<long> HDelAsync(string key, params string[] fields) => CallAsync("HDEL".InputKey(key, fields), rt => rt.ThrowOrValue<long>());
        public Task<bool> HExistsAsync(string key, string field) => CallAsync("HEXISTS".InputKey(key, field), rt => rt.ThrowOrValue<bool>());
        public Task<string> HGetAsync(string key, string field) => CallAsync("HGET".InputKey(key, field), rt => rt.ThrowOrValue<string>());
        public Task<T> HGetAsync<T>(string key, string field) => CallAsync("HGET".InputKey(key, field).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public Task<Dictionary<string, string>> HGetAllAsync(string key) => CallAsync("HGETALL".InputKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
        public Task<Dictionary<string, T>> HGetAllAsync<T>(string key) => CallAsync("HGETALL".InputKey(key).FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) =>
            {
                for (var x = 0; x < a.Length; x += 2)
                {
                    a[x] = rt.Encoding.GetString(a[x].ConvertTo<byte[]>());
                    a[x + 1] = DeserializeRedisValue<T>(a[x + 1].ConvertTo<byte[]>(), rt.Encoding);
                }
                return a.MapToHash<T>(rt.Encoding);
            }));

        public Task<long> HIncrByAsync(string key, string field, long increment) => CallAsync("HINCRBY".InputKey(key, field, increment), rt => rt.ThrowOrValue<long>());
        public Task<decimal> HIncrByFloatAsync(string key, string field, decimal increment) => CallAsync("HINCRBYFLOAT".InputKey(key, field, increment), rt => rt.ThrowOrValue<decimal>());
        public Task<string[]> HKeysAsync(string key) => CallAsync("HKEYS".InputKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<long> HLenAsync(string key) => CallAsync("HLEN".InputKey(key), rt => rt.ThrowOrValue<long>());

        public Task<string[]> HMGetAsync(string key, params string[] fields) => CallAsync("HMGET".InputKey(key, fields), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> HMGetAsync<T>(string key, params string[] fields) => HReadArrayAsync<T>("HMGET".InputKey(key, fields));
        public Task HMSetAsync<T>(string key, string field, T value, params object[] fieldValues) => HSetAsync(false, key, field, value, fieldValues);
        public Task HMSetAsync<T>(string key, Dictionary<string, T> keyValues) => CallAsync("HMSET".InputKey(key).InputKv(keyValues, false, SerializeRedisValue), rt => rt.ThrowOrValue<string>());

        public Task<ScanResult<string>> HScanAsync(string key, long cursor, string pattern, long count) => CallAsync("HSCAN"
            .InputKey(key, cursor)
            .InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
            .InputIf(count != 0, "COUNT", count), rt => rt.ThrowOrValue((a, _) => new ScanResult<string>(a[0].ConvertTo<long>(), a[1].ConvertTo<string[]>())));

        public Task<long> HSetAsync<T>(string key, string field, T value, params object[] fieldValues) => HSetAsync(false, key, field, value, fieldValues);
        public Task<long> HSetAsync<T>(string key, Dictionary<string, T> keyValues) => CallAsync("HSET".InputKey(key).InputKv(keyValues, false, SerializeRedisValue), rt => rt.ThrowOrValue<long>());
        Task<long> HSetAsync<T>(bool hmset, string key, string field, T value, params object[] fieldValues)
        {
            if (fieldValues?.Any() == true)
                return CallAsync((hmset ? "HMSET" : "HSET").InputKey(key, field).InputRaw(SerializeRedisValue(value))
                    .InputKv(fieldValues, false, SerializeRedisValue), rt => rt.ThrowOrValue<long>());
            return CallAsync((hmset ? "HMSET" : "HSET").InputKey(key, field).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<long>());
        }

        public Task<bool> HSetNxAsync<T>(string key, string field, T value) => CallAsync("HSETNX".InputKey(key, field).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<bool>());
        public Task<long> HStrLenAsync(string key, string field) => CallAsync("HSTRLEN".InputKey(key, field), rt => rt.ThrowOrValue<long>());

        public Task<string[]> HValsAsync(string key) => CallAsync("HVALS".InputKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> HValsAsync<T>(string key) => HReadArrayAsync<T>("HVALS".InputKey(key));

        Task<T[]> HReadArrayAsync<T>(CommandPacket cb) => CallAsync(cb.FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) => a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));
        #endregion
#endif

        public long HDel(string key, params string[] fields) => Call("HDEL".InputKey(key, fields), rt => rt.ThrowOrValue<long>());
        public bool HExists(string key, string field) => Call("HEXISTS".InputKey(key, field), rt => rt.ThrowOrValue<bool>());
        public string HGet(string key, string field) => Call("HGET".InputKey(key, field), rt => rt.ThrowOrValue<string>());
        public T HGet<T>(string key, string field) => Call("HGET".InputKey(key, field).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public Dictionary<string, string> HGetAll(string key) => Call("HGETALL".InputKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
        public Dictionary<string, T> HGetAll<T>(string key) => Call("HGETALL".InputKey(key).FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) =>
            {
                for (var x = 0; x < a.Length; x += 2)
                {
                    a[x] = rt.Encoding.GetString(a[x].ConvertTo<byte[]>());
                    a[x + 1] = DeserializeRedisValue<T>(a[x + 1].ConvertTo<byte[]>(), rt.Encoding);
                }
                return a.MapToHash<T>(rt.Encoding);
            }));

        public long HIncrBy(string key, string field, long increment) => Call("HINCRBY".InputKey(key, field, increment), rt => rt.ThrowOrValue<long>());
        public decimal HIncrByFloat(string key, string field, decimal increment) => Call("HINCRBYFLOAT".InputKey(key, field, increment), rt => rt.ThrowOrValue<decimal>());
        public string[] HKeys(string key) => Call("HKEYS".InputKey(key), rt => rt.ThrowOrValue<string[]>());
        public long HLen(string key) => Call("HLEN".InputKey(key), rt => rt.ThrowOrValue<long>());

        public string[] HMGet(string key, params string[] fields) => Call("HMGET".InputKey(key, fields), rt => rt.ThrowOrValue<string[]>());
        public T[] HMGet<T>(string key, params string[] fields) => HReadArray<T>("HMGET".InputKey(key, fields));
        public void HMSet<T>(string key, string field, T value, params object[] fieldValues) => HSet(true, key, field, value, fieldValues);
        public void HMSet<T>(string key, Dictionary<string, T> keyValues) => Call("HMSET".InputKey(key).InputKv(keyValues, false, SerializeRedisValue), rt => rt.ThrowOrValue<string>());

        public ScanResult<string> HScan(string key, long cursor, string pattern, long count) => Call("HSCAN"
            .InputKey(key, cursor)
            .InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
            .InputIf(count != 0, "COUNT", count), rt => rt.ThrowOrValue((a, _) => new ScanResult<string>(a[0].ConvertTo<long>(), a[1].ConvertTo<string[]>())));

        public long HSet<T>(string key, string field, T value, params object[] fieldValues) => HSet(false, key, field, value, fieldValues);
        public long HSet<T>(string key, Dictionary<string, T> keyValues) => Call("HSET".InputKey(key).InputKv(keyValues, false, SerializeRedisValue), rt => rt.ThrowOrValue<long>());
        long HSet<T>(bool hmset, string key, string field, T value, params object[] fieldValues)
        {
            if (fieldValues?.Any() == true)
                return Call((hmset ? "HMSET" : "HSET").InputKey(key, field).InputRaw(SerializeRedisValue(value))
                    .InputKv(fieldValues, false, SerializeRedisValue), rt => rt.ThrowOrValue<long>());
            return Call((hmset ? "HMSET" : "HSET").InputKey(key, field).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<long>());
        }

        public bool HSetNx<T>(string key, string field, T value) => Call("HSETNX".InputKey(key, field).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<bool>());
        public long HStrLen(string key, string field) => Call("HSTRLEN".InputKey(key, field), rt => rt.ThrowOrValue<long>());

        public string[] HVals(string key) => Call("HVALS".InputKey(key), rt => rt.ThrowOrValue<string[]>());
        public T[] HVals<T>(string key) => HReadArray<T>("HVALS".InputKey(key));

        T[] HReadArray<T>(CommandPacket cb) => Call(cb.FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) => a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));
    }
}
