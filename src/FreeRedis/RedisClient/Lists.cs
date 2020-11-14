using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
#if isasync
        #region async (copy from sync)
        public Task<string> BLPopAsync(string key, int timeoutSeconds) => CallAsync("BLPOP".InputKey(key, timeoutSeconds), rt => rt.ThrowOrValue((a, _) => a.LastOrDefault().ConvertTo<string>()));
        async public Task<T> BLPopAsync<T>(string key, int timeoutSeconds)
        {
            var kv = await BLRPopAsync<T>("BLPOP", new[] { key }, timeoutSeconds);
            if (kv == null) return default(T);
            return kv.value;
        }
        public Task<KeyValue<string>> BLPopAsync(string[] keys, int timeoutSeconds) => BLRPopAsync<string>("BLPOP", keys, timeoutSeconds);
        public Task<KeyValue<T>> BLPopAsync<T>(string[] keys, int timeoutSeconds) => BLRPopAsync<T>("BLPOP", keys, timeoutSeconds);
        public Task<string> BRPopAsync(string key, int timeoutSeconds) => CallAsync("BRPOP".InputKey(key, timeoutSeconds), rt => rt.ThrowOrValue((a, _) => a.LastOrDefault().ConvertTo<string>()));
        async public Task<T> BRPopAsync<T>(string key, int timeoutSeconds)
        {
            var kv = await BLRPopAsync<T>("BRPOP", new[] { key }, timeoutSeconds);
            if (kv == null) return default(T);
            return kv.value;
        }
        public Task<KeyValue<string>> BRPopAsync(string[] keys, int timeoutSeconds) => BLRPopAsync<string>("BRPOP", keys, timeoutSeconds);
        public Task<KeyValue<T>> BRPopAsync<T>(string[] keys, int timeoutSeconds) => BLRPopAsync<T>("BRPOP", keys, timeoutSeconds);
        Task<KeyValue<T>> BLRPopAsync<T>(string cmd, string[] keys, int timeoutSeconds) => CallAsync(cmd.InputKey(keys, timeoutSeconds).FlagReadbytes(true), rt => rt.ThrowOrValue((a, _) =>
                a?.Length != 2 ? null : new KeyValue<T>(rt.Encoding.GetString(a.FirstOrDefault().ConvertTo<byte[]>()), DeserializeRedisValue<T>(a.LastOrDefault().ConvertTo<byte[]>(), rt.Encoding))));

        public Task<string> BRPopLPushAsync(string source, string destination, int timeoutSeconds) => CallAsync("BRPOPLPUSH"
            .InputKey(source).InputKey(destination)
            .InputRaw(timeoutSeconds), rt => rt.ThrowOrValue<string>());
        public Task<T> BRPopLPushAsync<T>(string source, string destination, int timeoutSeconds) => CallAsync("BRPOPLPUSH"
            .InputKey(source).InputKey(destination)
            .InputRaw(timeoutSeconds)
            .FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public Task<string> LIndexAsync(string key, long index) => CallAsync("LINDEX".InputKey(key, index), rt => rt.ThrowOrValue<string>());
        public Task<T> LIndexAsync<T>(string key, long index) => CallAsync("LINDEX".InputKey(key, index).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public Task<long> LInsertAsync(string key, InsertDirection direction, object pivot, object element) => CallAsync("LINSERT"
            .InputKey(key)
            .InputRaw(direction)
            .InputRaw(SerializeRedisValue(pivot))
            .InputRaw(SerializeRedisValue(element)), rt => rt.ThrowOrValue<long>());
        public Task<long> LLenAsync(string key) => CallAsync("LLEN".InputKey(key), rt => rt.ThrowOrValue<long>());
        public Task<string> LPopAsync(string key) => CallAsync("LPOP".InputKey(key), rt => rt.ThrowOrValue<string>());
        public Task<T> LPopAsync<T>(string key) => CallAsync("LPOP".InputKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public Task<long> LPosAsync<T>(string key, T element, int rank = 0) => CallAsync("LPOS"
            .InputKey(key)
            .InputRaw(SerializeRedisValue(element))
            .InputIf(rank != 0, "RANK", rank), rt => rt.ThrowOrValue<long>());
        public Task<long[]> LPosAsync<T>(string key, T element, int rank, int count, int maxLen) => CallAsync("LPOS"
            .InputKey(key)
            .InputRaw(SerializeRedisValue(element))
            .InputIf(rank != 0, "RANK", rank)
            .Input("COUNT", count)
            .InputIf(maxLen != 0, "MAXLEN ", maxLen), rt => rt.ThrowOrValue<long[]>());

        public Task<long> LPushAsync(string key, params object[] elements) => CallAsync("LPUSH".InputKey(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()), rt => rt.ThrowOrValue<long>());
        public Task<long> LPushXAsync(string key, params object[] elements) => CallAsync("LPUSHX".InputKey(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()), rt => rt.ThrowOrValue<long>());
        public Task<string[]> LRangeAsync(string key, long start, long stop) => CallAsync("LRANGE".InputKey(key, start, stop), rt => rt.ThrowOrValue<string[]>());
        public Task<T[]> LRangeAsync<T>(string key, long start, long stop) => CallAsync("LRANGE".InputKey(key, start, stop).FlagReadbytes(true), rt => rt.ThrowOrValue((a, _) => a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));

        public Task<long> LRemAsync<T>(string key, long count, T element) => CallAsync("LREM".InputKey(key, count).InputRaw(SerializeRedisValue(element)), rt => rt.ThrowOrValue<long>());
        public Task LSetAsync<T>(string key, long index, T element) => CallAsync("LSET".InputKey(key, index).InputRaw(SerializeRedisValue(element)), rt => rt.ThrowOrValue<string>());
        public Task LTrimAsync(string key, long start, long stop) => CallAsync("LTRIM".InputKey(key, start, stop), rt => rt.ThrowOrValue<string>());
        public Task<string> RPopAsync(string key) => CallAsync("RPOP".InputKey(key), rt => rt.ThrowOrValue<string>());
        public Task<T> RPopAsync<T>(string key) => CallAsync("RPOP".InputKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public Task<string> RPopLPushAsync(string source, string destination) => CallAsync("RPOPLPUSH".InputKey(source).InputKey(destination), rt => rt.ThrowOrValue<string>());
        public Task<T> RPopLPushAsync<T>(string source, string destination) => CallAsync("RPOPLPUSH".InputKey(source).InputKey(destination).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public Task<long> RPushAsync(string key, params object[] elements) => CallAsync("RPUSH".InputKey(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()), rt => rt.ThrowOrValue<long>());
        public Task<long> RPushXAsync(string key, params object[] elements) => CallAsync("RPUSHX".InputKey(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()), rt => rt.ThrowOrValue<long>());
        #endregion
#endif

        public string BLPop(string key, int timeoutSeconds) => Call("BLPOP".InputKey(key, timeoutSeconds), rt => rt.ThrowOrValue((a, _) => a.LastOrDefault().ConvertTo<string>()));
        public T BLPop<T>(string key, int timeoutSeconds)
        {
            var kv = BLRPop<T>("BLPOP", new[] { key }, timeoutSeconds);
            if (kv == null) return default(T);
            return kv.value;
        }
        public KeyValue<string> BLPop(string[] keys, int timeoutSeconds) => BLRPop<string>("BLPOP", keys, timeoutSeconds);
        public KeyValue<T> BLPop<T>(string[] keys, int timeoutSeconds) => BLRPop<T>("BLPOP", keys, timeoutSeconds);
        public string BRPop(string key, int timeoutSeconds) => Call("BRPOP".InputKey(key, timeoutSeconds), rt => rt.ThrowOrValue((a, _) => a.LastOrDefault().ConvertTo<string>()));
        public T BRPop<T>(string key, int timeoutSeconds)
        {
            var kv = BLRPop<T>("BRPOP", new[] { key }, timeoutSeconds);
            if (kv == null) return default(T);
            return kv.value;
        }
        public KeyValue<string> BRPop(string[] keys, int timeoutSeconds) => BLRPop<string>("BRPOP", keys, timeoutSeconds);
        public KeyValue<T> BRPop<T>(string[] keys, int timeoutSeconds) => BLRPop<T>("BRPOP", keys, timeoutSeconds);
        KeyValue<T> BLRPop<T>(string cmd, string[] keys, int timeoutSeconds) => Call(cmd.InputKey(keys, timeoutSeconds).FlagReadbytes(true), rt => rt.ThrowOrValue((a, _) =>
                a?.Length != 2 ? null : new KeyValue<T>(rt.Encoding.GetString(a.FirstOrDefault().ConvertTo<byte[]>()), DeserializeRedisValue<T>(a.LastOrDefault().ConvertTo<byte[]>(), rt.Encoding))));

        public string BRPopLPush(string source, string destination, int timeoutSeconds) => Call("BRPOPLPUSH"
            .InputKey(source).InputKey(destination)
            .InputRaw(timeoutSeconds), rt => rt.ThrowOrValue<string>());
        public T BRPopLPush<T>(string source, string destination, int timeoutSeconds) => Call("BRPOPLPUSH"
            .InputKey(source).InputKey(destination)
            .InputRaw(timeoutSeconds)
            .FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public string LIndex(string key, long index) => Call("LINDEX".InputKey(key, index), rt => rt.ThrowOrValue<string>());
        public T LIndex<T>(string key, long index) => Call("LINDEX".InputKey(key, index).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public long LInsert(string key, InsertDirection direction, object pivot, object element) => Call("LINSERT"
            .InputKey(key)
            .InputRaw(direction)
            .InputRaw(SerializeRedisValue(pivot))
            .InputRaw(SerializeRedisValue(element)), rt => rt.ThrowOrValue<long>());
        public long LLen(string key) => Call("LLEN".InputKey(key), rt => rt.ThrowOrValue<long>());
        public string LPop(string key) => Call("LPOP".InputKey(key), rt => rt.ThrowOrValue<string>());
        public T LPop<T>(string key) => Call("LPOP".InputKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public long LPos<T>(string key, T element, int rank = 0) => Call("LPOS"
            .InputKey(key)
            .InputRaw(SerializeRedisValue(element))
            .InputIf(rank != 0, "RANK", rank), rt => rt.ThrowOrValue<long>());
        public long[] LPos<T>(string key, T element, int rank, int count, int maxLen) => Call("LPOS"
            .InputKey(key)
            .InputRaw(SerializeRedisValue(element))
            .InputIf(rank != 0, "RANK", rank)
            .Input("COUNT", count)
            .InputIf(maxLen != 0, "MAXLEN ", maxLen), rt => rt.ThrowOrValue<long[]>());

        public long LPush(string key, params object[] elements) => Call("LPUSH".InputKey(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()), rt => rt.ThrowOrValue<long>());
        public long LPushX(string key, params object[] elements) => Call("LPUSHX".InputKey(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()), rt => rt.ThrowOrValue<long>());
        public string[] LRange(string key, long start, long stop) => Call("LRANGE".InputKey(key, start, stop), rt => rt.ThrowOrValue<string[]>());
        public T[] LRange<T>(string key, long start, long stop) => Call("LRANGE".InputKey(key, start, stop).FlagReadbytes(true), rt => rt.ThrowOrValue((a, _) => a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));

        public long LRem<T>(string key, long count, T element) => Call("LREM".InputKey(key, count).InputRaw(SerializeRedisValue(element)), rt => rt.ThrowOrValue<long>());
        public void LSet<T>(string key, long index, T element) => Call("LSET".InputKey(key, index).InputRaw(SerializeRedisValue(element)), rt => rt.ThrowOrValue<string>());
        public void LTrim(string key, long start, long stop) => Call("LTRIM".InputKey(key, start, stop), rt => rt.ThrowOrValue<string>());
        public string RPop(string key) => Call("RPOP".InputKey(key), rt => rt.ThrowOrValue<string>());
        public T RPop<T>(string key) => Call("RPOP".InputKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public string RPopLPush(string source, string destination) => Call("RPOPLPUSH".InputKey(source).InputKey(destination), rt => rt.ThrowOrValue<string>());
        public T RPopLPush<T>(string source, string destination) => Call("RPOPLPUSH".InputKey(source).InputKey(destination).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public long RPush(string key, params object[] elements) => Call("RPUSH".InputKey(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()), rt => rt.ThrowOrValue<long>());
        public long RPushX(string key, params object[] elements) => Call("RPUSHX".InputKey(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()), rt => rt.ThrowOrValue<long>());
    }
}
