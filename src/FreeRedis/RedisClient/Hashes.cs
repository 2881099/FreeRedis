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
        public Task<long> HDelAsync(string key, params string[] fields) => CallAsync("HDEL".Input(key).Input(fields).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public Task<bool> HExistsAsync(string key, string field) => CallAsync("HEXISTS".Input(key, field).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public Task<string> HGetAsync(string key, string field) => CallAsync("HGET".Input(key, field).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public Task<T> HGetAsync<T>(string key, string field) => CallAsync("HGET".Input(key, field).FlagKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public Task<Dictionary<string, string>> HGetAllAsync(string key) => CallAsync("HGETALL".Input(key).FlagKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
        public Task<Dictionary<string, T>> HGetAllAsync<T>(string key) => CallAsync("HGETALL".Input(key).FlagKey(key).FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) =>
            {
                for (var x = 0; x < a.Length; x += 2) a[x + 1] = DeserializeRedisValue<T>(a[x + 1].ConvertTo<byte[]>(), rt.Encoding);
                return a.MapToHash<T>(rt.Encoding);
            }));

        public Task<long> HIncrByAsync(string key, string field, long increment) => CallAsync("HINCRBY".Input(key, field, increment).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public Task<decimal> HIncrByFloatAsync(string key, string field, decimal increment) => CallAsync("HINCRBYFLOAT".Input(key, field, increment).FlagKey(key), rt => rt.ThrowOrValue<decimal>());
        public Task<string[]> HKeysAsync(string key) => CallAsync("HKEYS".Input(key).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<long> HLenAsync(string key) => CallAsync("HLEN".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public Task<string[]> HMGetAsync(string key, params string[] fields) => CallAsync("HMGET".Input(key).Input(fields).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> HMGetAsync<T>(string key, params string[] fields) => HReadArrayAsync<T>("HMGET".Input(key).Input(fields).FlagKey(key));
        public Task HMSetAsync<T>(string key, string field, T value, params object[] fieldValues) => HSetAsync(false, key, field, value, fieldValues);
        public Task HMSetAsync<T>(string key, Dictionary<string, T> keyValues) => CallAsync("HMSET".Input(key).InputKv(keyValues, SerializeRedisValue).FlagKey(key), rt => rt.ThrowOrValue<string>());

        public Task<ScanResult<string>> HScanAsync(string key, long cursor, string pattern, long count) => CallAsync("HSCAN"
            .Input(key, cursor)
            .InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
            .InputIf(count != 0, "COUNT", count)
            .FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => new ScanResult<string>(a[0].ConvertTo<long>(), a[1].ConvertTo<string[]>())));

        public Task<long> HSetAsync<T>(string key, string field, T value, params object[] fieldValues) => HSetAsync(false, key, field, value, fieldValues);
        public Task<long> HSetAsync<T>(string key, Dictionary<string, T> keyValues) => CallAsync("HSET".Input(key).InputKv(keyValues, SerializeRedisValue).FlagKey(key), rt => rt.ThrowOrValue<long>());
        Task<long> HSetAsync<T>(bool hmset, string key, string field, T value, params object[] fieldValues)
        {
            if (fieldValues?.Any() == true)
                return CallAsync((hmset ? "HMSET" : "HSET").SubCommand(null).InputRaw(key)
                    .InputRaw(field).InputRaw(SerializeRedisValue(value))
                    .InputKv(fieldValues, SerializeRedisValue)
                    .FlagKey(key), rt => rt.ThrowOrValue<long>());
            return CallAsync((hmset ? "HMSET" : "HSET").SubCommand(null).Input(key, field).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue<long>());
        }

        public Task<bool> HSetNxAsync<T>(string key, string field, T value) => CallAsync("HSETNX".Input(key, field).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public Task<long> HStrLenAsync(string key, string field) => CallAsync("HSTRLEN".Input(key, field).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public Task<string[]> HValsAsync(string key) => CallAsync("HVALS".Input(key).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> HValsAsync<T>(string key) => HReadArrayAsync<T>("HVALS".Input(key).FlagKey(key));

        Task<T[]> HReadArrayAsync<T>(CommandPacket cb) => CallAsync(cb.FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) => a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));
        #endregion
#endif

        public long HDel(string key, params string[] fields) => Call("HDEL".Input(key).Input(fields).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public bool HExists(string key, string field) => Call("HEXISTS".Input(key, field).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public string HGet(string key, string field) => Call("HGET".Input(key, field).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public T HGet<T>(string key, string field) => Call("HGET".Input(key, field).FlagKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public Dictionary<string, string> HGetAll(string key) => Call("HGETALL".Input(key).FlagKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
        public Dictionary<string, T> HGetAll<T>(string key) => Call("HGETALL".Input(key).FlagKey(key).FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) =>
            {
                for (var x = 0; x < a.Length; x += 2) a[x + 1] = DeserializeRedisValue<T>(a[x + 1].ConvertTo<byte[]>(), rt.Encoding);
                return a.MapToHash<T>(rt.Encoding);
            }));

        public long HIncrBy(string key, string field, long increment) => Call("HINCRBY".Input(key, field, increment).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public decimal HIncrByFloat(string key, string field, decimal increment) => Call("HINCRBYFLOAT".Input(key, field, increment).FlagKey(key), rt => rt.ThrowOrValue<decimal>());
        public string[] HKeys(string key) => Call("HKEYS".Input(key).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public long HLen(string key) => Call("HLEN".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public string[] HMGet(string key, params string[] fields) => Call("HMGET".Input(key).Input(fields).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public T[] HMGet<T>(string key, params string[] fields) => HReadArray<T>("HMGET".Input(key).Input(fields).FlagKey(key));
        public void HMSet<T>(string key, string field, T value, params object[] fieldValues) => HSet(false, key, field, value, fieldValues);
        public void HMSet<T>(string key, Dictionary<string, T> keyValues) => Call("HMSET".Input(key).InputKv(keyValues, SerializeRedisValue).FlagKey(key), rt => rt.ThrowOrValue<string>());

        public ScanResult<string> HScan(string key, long cursor, string pattern, long count) => Call("HSCAN"
            .Input(key, cursor)
            .InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
            .InputIf(count != 0, "COUNT", count)
            .FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => new ScanResult<string>(a[0].ConvertTo<long>(), a[1].ConvertTo<string[]>())));

        public long HSet<T>(string key, string field, T value, params object[] fieldValues) => HSet(false, key, field, value, fieldValues);
        public long HSet<T>(string key, Dictionary<string, T> keyValues) => Call("HSET".Input(key).InputKv(keyValues, SerializeRedisValue).FlagKey(key), rt => rt.ThrowOrValue<long>());
        long HSet<T>(bool hmset, string key, string field, T value, params object[] fieldValues)
        {
            if (fieldValues?.Any() == true)
                return Call((hmset ? "HMSET" : "HSET").SubCommand(null).InputRaw(key)
                    .InputRaw(field).InputRaw(SerializeRedisValue(value))
                    .InputKv(fieldValues, SerializeRedisValue)
                    .FlagKey(key), rt => rt.ThrowOrValue<long>());
            return Call((hmset ? "HMSET" : "HSET").SubCommand(null).Input(key, field).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue<long>());
        }

        public bool HSetNx<T>(string key, string field, T value) => Call("HSETNX".Input(key, field).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public long HStrLen(string key, string field) => Call("HSTRLEN".Input(key, field).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public string[] HVals(string key) => Call("HVALS".Input(key).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public T[] HVals<T>(string key) => HReadArray<T>("HVALS".Input(key).FlagKey(key));

        T[] HReadArray<T>(CommandPacket cb) => Call(cb.FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) => a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));
    }
}
