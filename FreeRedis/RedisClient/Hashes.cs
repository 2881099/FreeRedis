using FreeRedis.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FreeRedis
{
    partial class RedisClient
    {
        public long HDel(string key, params string[] fields) => Call("HDEL".Input(key).Input(fields).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public bool HExists(string key, string field) => Call("HEXISTS".Input(key, field).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public string HGet(string key, string field) => Call("HGET".Input(key, field).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public T HGet<T>(string key, string field) => Call<byte[], T>("HGET".Input(key, field).FlagKey(key), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public Dictionary<string, string> HGetAll(string key) => Call("HGETALL".Input(key).FlagKey(key), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));
        public Dictionary<string, T> HGetAll<T>(string key) => Call<byte[], Dictionary<string, T>>("HGETALL".Input(key).FlagKey(key), rt => rt
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
        public void HMSet(string key, Dictionary<string, string> keyValues) => Call("HMSET".Input(key).InputKv(keyValues).FlagKey(key), rt => rt.ThrowOrValue<string>());

        public ScanResult<string> HScan(string key, long cursor, string pattern, long count) => Call("HSCAN"
            .Input(key, cursor)
            .InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
            .InputIf(count != 0, "COUNT", count)
            .FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => new ScanResult<string>(a[0].ConvertTo<long>(), a[1].ConvertTo<string[]>())));

        public long HSet<T>(string key, string field, T value, params object[] fieldValues) => HSet(false, key, field, value, fieldValues);
        public long HSet(string key, Dictionary<string, string> keyValues) => Call("HSET".Input(key).InputKv(keyValues).FlagKey(key), rt => rt.ThrowOrValue<long>());
        long HSet<T>(bool hmset, string key, string field, T value, params object[] fieldValues)
        {
            if (fieldValues?.Any() == true)
            {
                var kvs = fieldValues.MapToKvList<object>(Encoding.UTF8);
                kvs.Insert(0, new KeyValuePair<string, object>(field, SerializeRedisValue(value)));
                return Call((hmset ? "HMSET" : "HSET").SubCommand(null).InputRaw(key).InputKv(kvs, SerializeRedisValue).FlagKey(kvs.Select(a => a.Key).ToArray()), rt => rt.ThrowOrValue<long>());
            }
            return Call((hmset ? "HMSET" : "HSET").SubCommand(null).Input(key, field).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue<long>());
        }

        public bool HSetNx<T>(string key, string field, T value) => Call("HSETNX".Input(key, field).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public long HStrLen(string key, string field) => Call("HSTRLEN".Input(key, field).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public string[] HVals(string key) => Call("HVALS".Input(key).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public T[] HVals<T>(string key) => HReadArray<T>("HVALS".Input(key).FlagKey(key));

        T[] HReadArray<T>(CommandPacket cb) => Call<byte[], T[]>(cb, rt => rt
            .ThrowOrValue((a, _) => a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));
    }
}
