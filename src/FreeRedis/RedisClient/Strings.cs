using FreeRedis.Internal;
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
        public Task<long> AppendAsync<T>(string key, T value) => CallAsync("APPEND".InputKey(key).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<long>());
        public Task<long> BitCountAsync(string key, long start, long end) => CallAsync("BITCOUNT".InputKey(key, start, end), rt => rt.ThrowOrValue<long>());
        public Task<long> BitOpAsync(BitOpOperation operation, string destkey, params string[] keys) => CallAsync("BITOP".InputRaw(operation).InputKey(destkey).InputKey(keys), rt => rt.ThrowOrValue<long>());
        public Task<long> BitPosAsync(string key, bool bit, long? start = null, long? end = null) => CallAsync("BITPOS"
            .InputKey(key, bit ? "1" : "0")
            .InputIf(start != null, start)
            .InputIf(end != null, start), rt => rt.ThrowOrValue<long>());
        public Task<long> DecrAsync(string key) => CallAsync("DECR".InputKey(key), rt => rt.ThrowOrValue<long>());
        public Task<long> DecrByAsync(string key, long decrement) => CallAsync("DECRBY".InputKey(key, decrement), rt => rt.ThrowOrValue<long>());
        public Task<string> GetAsync(string key) => CallAsync("GET".InputKey(key), rt => rt.ThrowOrValue<string>());
        public Task<T> GetAsync<T>(string key) => CallAsync("GET".InputKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));
        public Task<bool> GetBitAsync(string key, long offset) => CallAsync("GETBIT".InputKey(key, offset), rt => rt.ThrowOrValue<bool>());
        public Task<string> GetRangeAsync(string key, long start, long end) => CallAsync("GETRANGE".InputKey(key, start, end), rt => rt.ThrowOrValue<string>());
        public Task<T> GetRangeAsync<T>(string key, long start, long end) => CallAsync("GETRANGE".InputKey(key, start, end).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public Task<string> GetSetAsync<T>(string key, T value) => CallAsync("GETSET".InputKey(key).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<string>());
        public Task<long> IncrAsync(string key) => CallAsync("INCR".InputKey(key), rt => rt.ThrowOrValue<long>());
        public Task<long> IncrByAsync(string key, long increment) => CallAsync("INCRBY".InputKey(key, increment), rt => rt.ThrowOrValue<long>());
        public Task<decimal> IncrByFloatAsync(string key, decimal increment) => CallAsync("INCRBYFLOAT".InputKey(key, increment), rt => rt.ThrowOrValue<decimal>());

        public Task<string[]> MGetAsync(params string[] keys) => CallAsync("MGET".InputKey(keys), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> MGetAsync<T>(params string[] keys) => CallAsync("MGET".InputKey(keys).FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) => a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));

        public Task MSetAsync(string key, object value, params object[] keyValues) => MSetAsync<bool>(false, key, value, keyValues);
        public Task MSetAsync<T>(Dictionary<string, T> keyValues) => CallAsync("MSET".SubCommand(null).InputKv(keyValues, true, SerializeRedisValue), rt => rt.ThrowOrValue<string>());
        public Task<bool> MSetNxAsync(string key, object value, params object[] keyValues) => MSetAsync<bool>(true, key, value, keyValues);
        public Task<bool> MSetNxAsync<T>(Dictionary<string, T> keyValues) => CallAsync("MSETNX".SubCommand(null).InputKv(keyValues, true, SerializeRedisValue), rt => rt.ThrowOrValue<bool>());
        Task<T> MSetAsync<T>(bool nx, string key, object value, params object[] keyValues)
        {
            if (keyValues?.Any() == true)
                return CallAsync((nx ? "MSETNX" : "MSET")
                    .InputKey(key).InputRaw(SerializeRedisValue(value))
                    .InputKv(keyValues, true, SerializeRedisValue), rt => rt.ThrowOrValue<T>());
            return CallAsync((nx ? "MSETNX" : "MSET").InputKey(key).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<T>());
        }

        public Task PSetExAsync<T>(string key, long milliseconds, T value) => CallAsync("PSETEX".InputKey(key, milliseconds).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrNothing());

        public Task SetAsync<T>(string key, T value, int timeoutSeconds = 0) => SetAsync(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, false);
        public Task SetAsync<T>(string key, T value, bool keepTtl) => SetAsync(key, value, TimeSpan.Zero, keepTtl, false, false);
        async public Task<bool> SetNxAsync<T>(string key, T value, int timeoutSeconds) => (await SetAsync(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, true, false)) == "OK";
        async public Task<bool> SetXxAsync<T>(string key, T value, int timeoutSeconds = 0) => (await SetAsync(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, true)) == "OK";
        async public Task<bool> SetXxAsync<T>(string key, T value, bool keepTtl) => (await SetAsync(key, value, TimeSpan.Zero, keepTtl, false, true)) == "OK";
        Task<string> SetAsync<T>(string key, T value, TimeSpan timeout, bool keepTtl, bool nx, bool xx) => CallAsync("SET"
            .InputKey(key)
            .InputRaw(SerializeRedisValue(value))
            .InputIf(timeout.TotalSeconds >= 1, "EX", (long)timeout.TotalSeconds)
            .InputIf(timeout.TotalSeconds < 1 && timeout.TotalMilliseconds >= 1, "PX", (long)timeout.TotalMilliseconds)
            .InputIf(keepTtl, "KEEPTTL")
            .InputIf(nx, "NX")
            .InputIf(xx, "XX"), rt => rt.ThrowOrValue<string>());

        public Task<long> SetBitAsync(string key, long offset, bool value) => CallAsync("SETBIT".InputKey(key, offset, value ? "1" : "0"), rt => rt.ThrowOrValue<long>());
        public Task SetExAsync<T>(string key, int seconds, T value) => CallAsync("SETEX".InputKey(key, seconds).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrNothing());
        public Task<bool> SetNxAsync<T>(string key, T value) => CallAsync("SETNX".InputKey(key).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<bool>());
        public Task<long> SetRangeAsync<T>(string key, long offset, T value) => CallAsync("SETRANGE".InputKey(key, offset).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<long>());
        //STRALGO LCS algo-specific-argument [algo-specific-argument ...]
        public Task<long> StrLenAsync(string key) => CallAsync("STRLEN".InputKey(key), rt => rt.ThrowOrValue<long>());
        #endregion
#endif

        public long Append<T>(string key, T value) => Call("APPEND".InputKey(key).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<long>());
        public long BitCount(string key, long start, long end) => Call("BITCOUNT".InputKey(key, start, end), rt => rt.ThrowOrValue<long>());
        public long BitOp(BitOpOperation operation, string destkey, params string[] keys) => Call("BITOP".InputRaw(operation).InputKey(destkey).InputKey(keys), rt => rt.ThrowOrValue<long>());
        public long BitPos(string key, bool bit, long? start = null, long? end = null) => Call("BITPOS"
            .InputKey(key, bit ? "1": "0")
            .InputIf(start != null, start)
            .InputIf(end != null, start), rt => rt.ThrowOrValue<long>());
        public long Decr(string key) => Call("DECR".InputKey(key), rt => rt.ThrowOrValue<long>());
        public long DecrBy(string key, long decrement) => Call("DECRBY".InputKey(key, decrement), rt => rt.ThrowOrValue<long>());
        public string Get(string key) => Call("GET".InputKey(key), rt => rt.ThrowOrValue<string>());
        public T Get<T>(string key) => Call("GET".InputKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));
        public void Get(string key, Stream destination, int bufferSize = 1024)
        {
            var cmd = "GET".InputKey(key);
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
        public bool GetBit(string key, long offset) => Call("GETBIT".InputKey(key, offset), rt => rt.ThrowOrValue<bool>());
        public string GetRange(string key, long start, long end) => Call("GETRANGE".InputKey(key, start, end), rt => rt.ThrowOrValue<string>());
        public T GetRange<T>(string key, long start, long end) => Call("GETRANGE".InputKey(key, start, end).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public string GetSet<T>(string key, T value) => Call("GETSET".InputKey(key).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<string>());
        public long Incr(string key) => Call("INCR".InputKey(key), rt => rt.ThrowOrValue<long>());
        public long IncrBy(string key, long increment) => Call("INCRBY".InputKey(key, increment), rt => rt.ThrowOrValue<long>());
        public decimal IncrByFloat(string key, decimal increment) => Call("INCRBYFLOAT".InputKey(key, increment), rt => rt.ThrowOrValue<decimal>());

        public string[] MGet(params string[] keys) => Call("MGET".InputKey(keys), rt => rt.ThrowOrValue<string[]>());
        public T[] MGet<T>(params string[] keys) => Call("MGET".InputKey(keys).FlagReadbytes(true), rt => rt
            .ThrowOrValue((a, _) => a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));

        public void MSet(string key, object value, params object[] keyValues) => MSet<bool>(false, key, value, keyValues);
        public void MSet<T>(Dictionary<string, T> keyValues) => Call("MSET".SubCommand(null).InputKv(keyValues, true, SerializeRedisValue), rt => rt.ThrowOrValue<string>());
        public bool MSetNx(string key, object value, params object[] keyValues) => MSet<bool>(true, key, value, keyValues);
        public bool MSetNx<T>(Dictionary<string, T> keyValues) => Call("MSETNX".SubCommand(null).InputKv(keyValues, true, SerializeRedisValue), rt => rt.ThrowOrValue<bool>());
        T MSet<T>(bool nx, string key, object value, params object[] keyValues)
        {
            if (keyValues?.Any() == true)
                return Call((nx ? "MSETNX" : "MSET")
                    .InputKey(key).InputRaw(SerializeRedisValue(value))
                    .InputKv(keyValues, true, SerializeRedisValue), rt => rt.ThrowOrValue<T>());
            return Call((nx ? "MSETNX" : "MSET").InputKey(key).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<T>());
        }

        public void PSetEx<T>(string key, long milliseconds, T value) => Call("PSETEX".InputKey(key, milliseconds).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrNothing());

        public void Set<T>(string key, T value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, false);
        public void Set<T>(string key, T value, bool keepTtl) => Set(key, value, TimeSpan.Zero, keepTtl, false, false);
        public bool SetNx<T>(string key, T value, int timeoutSeconds) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, true, false) == "OK";
        public bool SetXx<T>(string key, T value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, true) == "OK";
        public bool SetXx<T>(string key, T value, bool keepTtl) => Set(key, value, TimeSpan.Zero, keepTtl, false, true) == "OK";
        string Set<T>(string key, T value, TimeSpan timeout, bool keepTtl, bool nx, bool xx) => Call("SET"
            .InputKey(key)
            .InputRaw(SerializeRedisValue(value))
            .InputIf(timeout.TotalSeconds >= 1, "EX", (long)timeout.TotalSeconds)
            .InputIf(timeout.TotalSeconds < 1 && timeout.TotalMilliseconds >= 1, "PX", (long)timeout.TotalMilliseconds)
            .InputIf(keepTtl, "KEEPTTL")
            .InputIf(nx, "NX")
            .InputIf(xx, "XX"), rt => rt.ThrowOrValue<string>());

        public long SetBit(string key, long offset, bool value) => Call("SETBIT".InputKey(key, offset, value ? "1" : "0"), rt => rt.ThrowOrValue<long>());
        public void SetEx<T>(string key, int seconds, T value) => Call("SETEX".InputKey(key, seconds).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrNothing());
        public bool SetNx<T>(string key, T value) => Call("SETNX".InputKey(key).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<bool>());
        public long SetRange<T>(string key, long offset, T value) => Call("SETRANGE".InputKey(key, offset).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<long>());
        //STRALGO LCS algo-specific-argument [algo-specific-argument ...]
        public long StrLen(string key) => Call("STRLEN".InputKey(key), rt => rt.ThrowOrValue<long>());
    }
}
