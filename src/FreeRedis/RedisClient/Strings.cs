using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FreeRedis
{
    partial class RedisClient
    {
        public long Append<T>(string key, T value) => Call("APPEND".Input(key).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public long BitCount(string key, long start, long end) => Call("BITCOUNT".Input(key, start, end).FlagKey(key), rt => rt.ThrowOrValue<long>());
        //BITFIELD key [GET type offset] [SET type offset value] [INCRBY type offset increment] [OVERFLOW WRAP|SAT|FAIL]
        public long BitOp(BitOpOperation operation, string destkey, params string[] keys) => Call("BITOP".SubCommand(null).Input(operation, destkey, keys).FlagKey(destkey).FlagKey(keys), rt => rt.ThrowOrValue<long>());
        public long BitPos(string key, bool bit, long? start = null, long? end = null) => Call("BITPOS"
            .Input(key)
            .InputRaw(bit ? "1": "0")
            .InputIf(start != null, start)
            .InputIf(end != null, start)
            .FlagKey(key), rt => rt.ThrowOrValue<long>());
        public long Decr(string key) => Call("DECR".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public long DecrBy(string key, long decrement) => Call("DECRBY".Input(key, decrement).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public string Get(string key) => Call("GET".Input(key).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public T Get<T>(string key) => Call("GET".Input(key).FlagKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));
        public void Get(string key, Stream destination, int bufferSize = 1024)
        {
            var cmd = "GET".Input(key).FlagKey(key);
            Adapter.TopOwner.LogCall(cmd, () =>
            {
                using (var rds = Adapter.GetRedisSocket(cmd))
                {
                    rds.Write(cmd);
                    rds.ReadChunk(destination, bufferSize);
                }
                return default(string);
            });
        }
        public bool GetBit(string key, long offset) => Call("GETBIT".Input(key, offset).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public string GetRange(string key, long start, long end) => Call("GETRANGE".Input(key, start, end).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public T GetRange<T>(string key, long start, long end) => Call("GETRANGE".Input(key, start, end).FlagKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public string GetSet<T>(string key, T value) => Call("GETSET".Input(key).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public long Incr(string key) => Call("INCR".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public long IncrBy(string key, long increment) => Call("INCRBY".Input(key, increment).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public decimal IncrByFloat(string key, decimal increment) => Call("INCRBYFLOAT".Input(key, increment).FlagKey(key), rt => rt.ThrowOrValue<decimal>());

        public string[] MGet(params string[] keys) => Call("MGET".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue<string[]>());
        public T[] MGet<T>(params string[] keys) => Call("MGET".Input(keys).FlagKey(keys).FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) => a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));

        public void MSet(string key, object value, params object[] keyValues) => MSet<bool>(false, key, value, keyValues);
        public void MSet<T>(Dictionary<string, T> keyValues) => Call("MSET".SubCommand(null).InputKv(keyValues, SerializeRedisValue).FlagKey(keyValues.Keys), rt => rt.ThrowOrValue<string>());
        public bool MSetNx(string key, object value, params object[] keyValues) => MSet<bool>(true, key, value, keyValues);
        public bool MSetNx<T>(Dictionary<string, T> keyValues) => Call("MSETNX".SubCommand(null).InputKv(keyValues, SerializeRedisValue).FlagKey(keyValues.Keys), rt => rt.ThrowOrValue<bool>());
        T MSet<T>(bool nx, string key, object value, params object[] keyValues)
        {
            if (keyValues?.Any() == true)
                return Call((nx ? "MSETNX" : "MSET").SubCommand(null)
                    .InputRaw(key).InputRaw(SerializeRedisValue(value))
                    .InputKv(keyValues, SerializeRedisValue)
                    .FlagKey(key)
                    .FlagKey(keyValues.Where((a, b) => b % 2 == 0).Select(a => a?.ConvertTo<string>()).ToArray()), rt => rt.ThrowOrValue<T>());
            return Call((nx ? "MSETNX" : "MSET").SubCommand(null).InputRaw(key).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue<T>());
        }

        public void PSetEx<T>(string key, long milliseconds, T value) => Call("PSETEX".Input(key, milliseconds).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrNothing());

        public void Set<T>(string key, T value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, false);
        public void Set<T>(string key, T value, bool keepTtl) => Set(key, value, TimeSpan.Zero, keepTtl, false, false);
        public bool SetNx<T>(string key, T value, int timeoutSeconds) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, true, false) == "OK";
        public bool SetXx<T>(string key, T value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, true) == "OK";
        public bool SetXx<T>(string key, T value, bool keepTtl) => Set(key, value, TimeSpan.Zero, keepTtl, false, true) == "OK";
        string Set<T>(string key, T value, TimeSpan timeout, bool keepTtl, bool nx, bool xx) => Call("SET"
            .Input(key)
            .InputRaw(SerializeRedisValue(value))
            .InputIf(timeout.TotalSeconds >= 1, "EX", (long)timeout.TotalSeconds)
            .InputIf(timeout.TotalSeconds < 1 && timeout.TotalMilliseconds >= 1, "PX", (long)timeout.TotalMilliseconds)
            .InputIf(keepTtl, "KEEPTTL")
            .InputIf(nx, "NX")
            .InputIf(xx, "XX")
            .FlagKey(key), rt => rt.ThrowOrValue<string>());

        public long SetBit(string key, long offset, bool value) => Call("SETBIT".Input(key, offset).InputRaw(value ? "1" : "0").FlagKey(key), rt => rt.ThrowOrValue<long>());
        public void SetEx<T>(string key, int seconds, T value) => Call("SETEX".Input(key, seconds).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrNothing());
        public bool SetNx<T>(string key, T value) => Call("SETNX".Input(key).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public long SetRange<T>(string key, long offset, T value) => Call("SETRANGE".Input(key, offset).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue<long>());
        //STRALGO LCS algo-specific-argument [algo-specific-argument ...]
        public long StrLen(string key) => Call("STRLEN".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());
    }
}
