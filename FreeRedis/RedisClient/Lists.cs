using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
	{
		public string BLPop(string key, int timeoutSeconds) => Call<string[]>("BLPOP", key, timeoutSeconds).NewValue(a => a.LastOrDefault()).ThrowOrValue();
		public T BLPop<T>(string key, int timeoutSeconds)
        {
            var kv = BLRPop<T>("BLPOP", new[] { key }, timeoutSeconds);
            if (kv == null) return default(T);
            return kv.Value;
        }
		public KeyValue<string> BLPop(string[] keys, int timeoutSeconds) => BLRPop<string>("BLPOP", keys, timeoutSeconds);
		public KeyValue<T> BLPop<T>(string[] keys, int timeoutSeconds) => BLRPop<T>("BLPOP", keys, timeoutSeconds);
		public string BRPop(string key, int timeoutSeconds) => Call<string[]>("BRPOP", key, timeoutSeconds).NewValue(a => a.LastOrDefault()).ThrowOrValue();
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
			CallWriteOnly(cmd, null, ""
				.AddIf(true, keys, timeoutSeconds)
				.ToArray());
			var value = Resp3Helper.Read<object>(Stream).ThrowOrValue();
			var list = value.ConvertTo<byte[][]>();
			if (list?.Length != 2) return null;
			return new KeyValue<T>(Encoding.GetString(list.FirstOrDefault()), DeserializeRedisValue<T>(list.LastOrDefault()));
		}

		public string BRPopLPush(string source, string destination, int timeoutSeconds) => Call<string>("BRPOPLPUSH", source, destination, timeoutSeconds).ThrowOrValue();
		public T BRPopLPush<T>(string source, string destination, int timeoutSeconds) => Call<byte[]>("BRPOPLPUSH", source, destination, timeoutSeconds).NewValue(a => DeserializeRedisValue<T>(a)).ThrowOrValue();

		public string LIndex(string key, long index) => Call<string>("LINDEX", key, index).ThrowOrValue();
		public T LIndex<T>(string key, long index) => Call<byte[]>("LINDEX", key, index).NewValue(a => DeserializeRedisValue<T>(a)).ThrowOrValue();

		public long LInsert(string key, InsertDirection direction, object pivot, object element) => Call<long>("LINSERT", key, ""
			.AddIf(true, direction)
			.AddRaw(SerializeRedisValue(pivot))
			.AddRaw(SerializeRedisValue(element))
			.ToArray()).ThrowOrValue();
		public long LLen(string key) => Call<long>("LLEN", key).ThrowOrValue();
		public string LPop(string key) => Call<string>("LPOP", key).ThrowOrValue();
		public T LPop<T>(string key) => Call<byte[]>("LPOP", key).NewValue(a => DeserializeRedisValue<T>(a)).ThrowOrValue();

		public long LPos(string key, object element, int rank = 0) => Call<long>("LPOS", key, ""
			.AddRaw(SerializeRedisValue(element))
			.AddIf(rank != 0, "RANK", rank)
			.ToArray()).ThrowOrValue();
		public long[] LPos(string key, object element, int rank, int count, int maxLen) => Call<long[]>("LPOS", key, ""
			.AddRaw(SerializeRedisValue(element))
			.AddIf(rank != 0, "RANK", rank)
			.AddIf(true, "COUNT", count)
			.AddIf(maxLen != 0, "MAXLEN ", maxLen)
			.ToArray()).ThrowOrValue();
		public long LPush(string key, params object[] elements) => Call<long>("LPUSH", key, elements.Select(a => SerializeRedisValue(a)).ToArray()).ThrowOrValue();
		public long LPushX(string key, params object[] elements) => Call<long>("LPUSHX", key, elements.Select(a => SerializeRedisValue(a)).ToArray()).ThrowOrValue();
		public string[] LRange(string key, long start, long stop) => Call<string[]>("LRANGE", key, start, stop).ThrowOrValue();
		public T[] LRange<T>(string key, long start, long stop)
		{
			CallWriteOnly("LRANGE", key, start, stop);
			var value = Resp3Helper.Read<object>(Stream).ThrowOrValue();
			var list = value.ConvertTo<byte[][]>();
			return list.Select(a => DeserializeRedisValue<T>(a)).ToArray();
		}

		public long LRem(string key, long count, object element) => Call<long>("LREM", key, count, SerializeRedisValue(element)).ThrowOrValue();
		public void LSet(string key, long index, object element) => Call<string>("LSET", key, index, SerializeRedisValue(element)).ThrowOrValue();
		public void LTrim(string key, long start, long stop) => Call<string>("LTRIM", key, start, stop).ThrowOrValue();
		public string RPop(string key) => Call<string>("RPOP", key).ThrowOrValue();
		public T RPop<T>(string key) => Call<byte[]>("RPOP", key).NewValue(a => DeserializeRedisValue<T>(a)).ThrowOrValue();

		public string RPopLPush(string source, string destination) => Call<string>("RPOPLPUSH", source, destination).ThrowOrValue();
		public T RPopLPush<T>(string source, string destination) => Call<byte[]>("RPOPLPUSH", source, destination).NewValue(a => DeserializeRedisValue<T>(a)).ThrowOrValue();

		public long RPush(string key, params object[] elements) => Call<long>("RPUSH", key, elements.Select(a => SerializeRedisValue(a)).ToArray()).ThrowOrValue();
		public long RPushX(string key, params object[] elements) => Call<long>("RPUSHX", key, elements.Select(a => SerializeRedisValue(a)).ToArray()).ThrowOrValue();
	}
}
