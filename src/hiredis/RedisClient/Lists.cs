using System.Collections.Generic;
using System.Linq;

namespace hiredis
{
    partial class RedisClient
    {
        public string BLPop(string key, int timeoutSeconds) => Call("BLPOP".Input(key, timeoutSeconds).FlagKey(key), rt => rt.ThrowOrValue((a, _) => a.LastOrDefault().ConvertTo<string>()));
        public T BLPop<T>(string key, int timeoutSeconds)
        {
            var kv = BLRPop<T>("BLPOP", new[] { key }, timeoutSeconds);
            if (kv == null) return default(T);
            return kv.value;
        }
        public KeyValue<string> BLPop(string[] keys, int timeoutSeconds) => BLRPop<string>("BLPOP", keys, timeoutSeconds);
        public KeyValue<T> BLPop<T>(string[] keys, int timeoutSeconds) => BLRPop<T>("BLPOP", keys, timeoutSeconds);
        public string BRPop(string key, int timeoutSeconds) => Call("BRPOP".Input(key, timeoutSeconds).FlagKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue((a, _) => a.LastOrDefault().ConvertTo<string>()));
        public T BRPop<T>(string key, int timeoutSeconds)
        {
            var kv = BLRPop<T>("BRPOP", new[] { key }, timeoutSeconds);
            if (kv == null) return default(T);
            return kv.value;
        }
        public KeyValue<string> BRPop(string[] keys, int timeoutSeconds) => BLRPop<string>("BRPOP", keys, timeoutSeconds);
        public KeyValue<T> BRPop<T>(string[] keys, int timeoutSeconds) => BLRPop<T>("BRPOP", keys, timeoutSeconds);
        KeyValue<T> BLRPop<T>(string cmd, string[] keys, int timeoutSeconds) => Call(cmd.SubCommand(null).Input(keys).InputRaw(timeoutSeconds).FlagKey(keys).FlagReadbytes(true), rt => rt.ThrowOrValue((a, _) =>
                a?.Length != 2 ? null : new KeyValue<T>(rt.Encoding.GetString(a.FirstOrDefault().ConvertTo<byte[]>()), DeserializeRedisValue<T>(a.LastOrDefault().ConvertTo<byte[]>(), rt.Encoding))));

        public string BRPopLPush(string source, string destination, int timeoutSeconds) => Call("BRPOPLPUSH"
            .Input(source, destination, timeoutSeconds)
            .FlagKey(source, destination), rt => rt.ThrowOrValue<string>());
        public T BRPopLPush<T>(string source, string destination, int timeoutSeconds) => Call("BRPOPLPUSH"
            .Input(source, destination, timeoutSeconds)
            .FlagKey(source, destination).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public string LIndex(string key, long index) => Call("LINDEX".Input(key, index).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public T LIndex<T>(string key, long index) => Call("LINDEX".Input(key, index).FlagKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public long LInsert(string key, InsertDirection direction, object pivot, object element) => Call("LINSERT"
            .Input(key)
            .InputRaw(direction)
            .InputRaw(SerializeRedisValue(pivot))
            .InputRaw(SerializeRedisValue(element))
            .FlagKey(key), rt => rt.ThrowOrValue<long>());
        public long LLen(string key) => Call("LLEN".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public string LPop(string key) => Call("LPOP".Input(key).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public T LPop<T>(string key) => Call("LPOP".Input(key).FlagKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public long LPos<T>(string key, T element, int rank = 0) => Call("LPOS"
            .Input(key)
            .InputRaw(SerializeRedisValue(element))
            .InputIf(rank != 0, "RANK", rank)
            .FlagKey(key), rt => rt.ThrowOrValue<long>());
        public long[] LPos<T>(string key, T element, int rank, int count, int maxLen) => Call("LPOS"
            .Input(key)
            .InputRaw(SerializeRedisValue(element))
            .InputIf(rank != 0, "RANK", rank)
            .Input("COUNT", count)
            .InputIf(maxLen != 0, "MAXLEN ", maxLen)
            .FlagKey(key), rt => rt.ThrowOrValue<long[]>());

        public long LPush(string key, params object[] elements) => Call("LPUSH".Input(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public long LPushX(string key, params object[] elements) => Call("LPUSHX".Input(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public string[] LRange(string key, long start, long stop) => Call("LRANGE".Input(key, start, stop).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public T[] LRange<T>(string key, long start, long stop) => Call("LRANGE".Input(key, start, stop).FlagKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue((a, _) => a.Select(b => DeserializeRedisValue<T>(b.ConvertTo<byte[]>(), rt.Encoding)).ToArray()));

        public long LRem<T>(string key, long count, T element) => Call("LREM".Input(key, count).InputRaw(SerializeRedisValue(element)).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public void LSet<T>(string key, long index, T element) => Call("LSET".Input(key, index).InputRaw(SerializeRedisValue(element)).FlagKey(), rt => rt.ThrowOrValue<string>());
        public void LTrim(string key, long start, long stop) => Call("LTRIM".Input(key, start, stop).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public string RPop(string key) => Call("RPOP".Input(key).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public T RPop<T>(string key) => Call("RPOP".Input(key).FlagKey(key).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public string RPopLPush(string source, string destination) => Call("RPOPLPUSH".Input(source, destination).FlagKey(source, destination), rt => rt.ThrowOrValue<string>());
        public T RPopLPush<T>(string source, string destination) => Call("RPOPLPUSH".Input(source, destination).FlagKey(source, destination).FlagReadbytes(true), rt => rt.ThrowOrValue(a => DeserializeRedisValue<T>(a.ConvertTo<byte[]>(), rt.Encoding)));

        public long RPush(string key, params object[] elements) => Call("RPUSH".Input(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public long RPushX(string key, params object[] elements) => Call("RPUSHX".Input(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key), rt => rt.ThrowOrValue<long>());
    }
}
