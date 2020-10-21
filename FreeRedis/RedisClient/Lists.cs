using System.Collections.Generic;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
	{
		public string BLPop(string key, int timeoutSeconds) => Call<string[], string>("BLPOP".Input(key, timeoutSeconds).FlagKey(key), rt => rt.NewValue(a => a.LastOrDefault()).ThrowOrValue());
		public T BLPop<T>(string key, int timeoutSeconds)
        {
            var kv = BLRPop<T>("BLPOP", new[] { key }, timeoutSeconds);
            if (kv == null) return default(T);
            return kv.Value;
        }
		public KeyValue<string> BLPop(string[] keys, int timeoutSeconds) => BLRPop<string>("BLPOP", keys, timeoutSeconds);
		public KeyValue<T> BLPop<T>(string[] keys, int timeoutSeconds) => BLRPop<T>("BLPOP", keys, timeoutSeconds);
		public string BRPop(string key, int timeoutSeconds) => Call<string[], string>("BRPOP".Input(key, timeoutSeconds).FlagKey(key), rt => rt.NewValue(a => a.LastOrDefault()).ThrowOrValue());
		public T BRPop<T>(string key, int timeoutSeconds)
		{
			var kv = BLRPop<T>("BRPOP", new[] { key }, timeoutSeconds);
			if (kv == null) return default(T);
			return kv.Value;
		}
		public KeyValue<string> BRPop(string[] keys, int timeoutSeconds) => BLRPop<string>("BRPOP", keys, timeoutSeconds);
		public KeyValue<T> BRPop<T>(string[] keys, int timeoutSeconds) => BLRPop<T>("BRPOP", keys, timeoutSeconds);
		KeyValue<T> BLRPop<T>(string cmd, string[] keys, int timeoutSeconds)
		{
			var cb = cmd.SubCommand(null).Input(keys).InputRaw(timeoutSeconds).FlagKey(keys);
			using (var rds = _adapter.GetRedisSocket(cb))
			{
				rds.Write(cb);
				var value = cb.Read<object>();
				var list = value.ConvertTo<byte[][]>();
				if (list?.Length != 2) return null;
				return new KeyValue<T>(rds.Encoding.GetString(list.FirstOrDefault()), DeserializeRedisValue<T>(list.LastOrDefault(), rds.Encoding));
			}
		}

		public string BRPopLPush(string source, string destination, int timeoutSeconds) => Call<string>("BRPOPLPUSH"
			.Input(source, destination, timeoutSeconds)
			.FlagKey(source, destination), rt => rt.ThrowOrValue());
		public T BRPopLPush<T>(string source, string destination, int timeoutSeconds) => Call<byte[], T>("BRPOPLPUSH"
			.Input(source, destination, timeoutSeconds)
			.FlagKey(source, destination), rt => rt.NewValue(a => DeserializeRedisValue<T>(a, rt.Encoding)).ThrowOrValue());

		public string LIndex(string key, long index) => Call<string>("LINDEX".Input(key, index).FlagKey(key), rt => rt.ThrowOrValue());
		public T LIndex<T>(string key, long index) => Call<byte[], T>("LINDEX".Input(key, index).FlagKey(key), rt => rt.NewValue(a => DeserializeRedisValue<T>(a, rt.Encoding)).ThrowOrValue());

		public long LInsert(string key, InsertDirection direction, object pivot, object element) => Call<long>("LINSERT"
			.Input(key)
			.InputRaw(direction)
			.InputRaw(SerializeRedisValue(pivot))
			.InputRaw(SerializeRedisValue(element))
			.FlagKey(key), rt => rt.ThrowOrValue());
		public long LLen(string key) => Call<long>("LLEN".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public string LPop(string key) => Call<string>("LPOP".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public T LPop<T>(string key) => Call<byte[], T>("LPOP".Input(key).FlagKey(key), rt => rt.NewValue(a => DeserializeRedisValue<T>(a, rt.Encoding)).ThrowOrValue());

		public long LPos(string key, object element, int rank = 0) => Call<long>("LPOS"
			.Input(key)
			.InputRaw(SerializeRedisValue(element))
			.InputIf(rank != 0, "RANK", rank)
			.FlagKey(key), rt => rt.ThrowOrValue());
		public long[] LPos(string key, object element, int rank, int count, int maxLen) => Call<long[]>("LPOS"
			.Input(key)
			.InputRaw(SerializeRedisValue(element))
			.InputIf(rank != 0, "RANK", rank)
			.Input("COUNT", count)
			.InputIf(maxLen != 0, "MAXLEN ", maxLen)
			.FlagKey(key), rt => rt.ThrowOrValue());
		public long LPush(string key, params object[] elements) => Call<long>("LPUSH".Input(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key), rt => rt.ThrowOrValue());
		public long LPushX(string key, params object[] elements) => Call<long>("LPUSHX".Input(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key), rt => rt.ThrowOrValue());
		public string[] LRange(string key, long start, long stop) => Call<string[]>("LRANGE".Input(key, start, stop).FlagKey(key), rt => rt.ThrowOrValue());
		public T[] LRange<T>(string key, long start, long stop) => Call<object, T[]>("LRANGE".Input(key, start, stop).FlagKey(key), rt => rt.NewValue(a => a.ConvertTo<List<byte[]>>().Select(b => DeserializeRedisValue<T>(b, rt.Encoding)).ToArray()).ThrowOrValue());

		public long LRem(string key, long count, object element) => Call<long>("LREM".Input(key, count).InputRaw(SerializeRedisValue(element)).FlagKey(key), rt => rt.ThrowOrValue());
		public void LSet(string key, long index, object element) => Call<string>("LSET".Input(key, index).InputRaw(SerializeRedisValue(element)).FlagKey(), rt => rt.ThrowOrValue());
		public void LTrim(string key, long start, long stop) => Call<string>("LTRIM".Input(key, start, stop).FlagKey(key), rt => rt.ThrowOrValue());
		public string RPop(string key) => Call<string>("RPOP".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public T RPop<T>(string key) => Call<byte[], T>("RPOP".Input(key).FlagKey(key), rt => rt.NewValue(a => DeserializeRedisValue<T>(a, rt.Encoding)).ThrowOrValue());

		public string RPopLPush(string source, string destination) => Call<string>("RPOPLPUSH".Input(source, destination).FlagKey(source, destination), rt => rt.ThrowOrValue());
		public T RPopLPush<T>(string source, string destination) => Call<byte[], T>("RPOPLPUSH".Input(source, destination).FlagKey(source, destination), rt => rt.NewValue(a => DeserializeRedisValue<T>(a, rt.Encoding)).ThrowOrValue());

		public long RPush(string key, params object[] elements) => Call<long>("RPUSH".Input(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key), rt => rt.ThrowOrValue());
		public long RPushX(string key, params object[] elements) => Call<long>("RPUSHX".Input(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key), rt => rt.ThrowOrValue());
	}
}
