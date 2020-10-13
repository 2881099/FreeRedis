﻿using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
	{
		public string BLPop(string key, int timeoutSeconds) => Call<string[]>("BLPOP".Input(key, timeoutSeconds).FlagKey(key)).NewValue(a => a.LastOrDefault()).ThrowOrValue();
		public T BLPop<T>(string key, int timeoutSeconds)
        {
            var kv = BLRPop<T>("BLPOP", new[] { key }, timeoutSeconds);
            if (kv == null) return default(T);
            return kv.Value;
        }
		public KeyValue<string> BLPop(string[] keys, int timeoutSeconds) => BLRPop<string>("BLPOP", keys, timeoutSeconds);
		public KeyValue<T> BLPop<T>(string[] keys, int timeoutSeconds) => BLRPop<T>("BLPOP", keys, timeoutSeconds);
		public string BRPop(string key, int timeoutSeconds) => Call<string[]>("BRPOP".Input(key, timeoutSeconds).FlagKey(key)).NewValue(a => a.LastOrDefault()).ThrowOrValue();
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
			CallWriteOnly(cmd.SubCommand(null).Input(keys).InputRaw(timeoutSeconds).FlagKey(keys));
			var value = Resp3Helper.Read<object>(Stream).ThrowOrValue();
			var list = value.ConvertTo<byte[][]>();
			if (list?.Length != 2) return null;
			return new KeyValue<T>(Encoding.GetString(list.FirstOrDefault()), DeserializeRedisValue<T>(list.LastOrDefault()));
		}

		public string BRPopLPush(string source, string destination, int timeoutSeconds) => Call<string>("BRPOPLPUSH"
			.Input(source, destination, timeoutSeconds)
			.FlagKey(source, destination)).ThrowOrValue();
		public T BRPopLPush<T>(string source, string destination, int timeoutSeconds) => Call<byte[]>("BRPOPLPUSH"
			.Input(source, destination, timeoutSeconds)
			.FlagKey(source, destination)).NewValue(a => DeserializeRedisValue<T>(a)).ThrowOrValue();

		public string LIndex(string key, long index) => Call<string>("LINDEX".Input(key, index).FlagKey(key)).ThrowOrValue();
		public T LIndex<T>(string key, long index) => Call<byte[]>("LINDEX".Input(key, index).FlagKey(key)).NewValue(a => DeserializeRedisValue<T>(a)).ThrowOrValue();

		public long LInsert(string key, InsertDirection direction, object pivot, object element) => Call<long>("LINSERT"
			.Input(key)
			.InputRaw(direction)
			.InputRaw(SerializeRedisValue(pivot))
			.InputRaw(SerializeRedisValue(element))
			.FlagKey(key)).ThrowOrValue();
		public long LLen(string key) => Call<long>("LLEN".Input(key).FlagKey(key)).ThrowOrValue();
		public string LPop(string key) => Call<string>("LPOP".Input(key).FlagKey(key)).ThrowOrValue();
		public T LPop<T>(string key) => Call<byte[]>("LPOP".Input(key).FlagKey(key)).NewValue(a => DeserializeRedisValue<T>(a)).ThrowOrValue();

		public long LPos(string key, object element, int rank = 0) => Call<long>("LPOS"
			.Input(key)
			.InputRaw(SerializeRedisValue(element))
			.InputIf(rank != 0, "RANK", rank)
			.FlagKey(key)).ThrowOrValue();
		public long[] LPos(string key, object element, int rank, int count, int maxLen) => Call<long[]>("LPOS"
			.Input(key)
			.InputRaw(SerializeRedisValue(element))
			.InputIf(rank != 0, "RANK", rank)
			.Input("COUNT", count)
			.InputIf(maxLen != 0, "MAXLEN ", maxLen)
			.FlagKey(key)).ThrowOrValue();
		public long LPush(string key, params object[] elements) => Call<long>("LPUSH".Input(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key)).ThrowOrValue();
		public long LPushX(string key, params object[] elements) => Call<long>("LPUSHX".Input(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key)).ThrowOrValue();
		public string[] LRange(string key, long start, long stop) => Call<string[]>("LRANGE".Input(key, start, stop).FlagKey(key)).ThrowOrValue();
		public T[] LRange<T>(string key, long start, long stop) => Call<string[]>("LRANGE".Input(key, start, stop).FlagKey(key)).NewValue(a => a.ConvertTo<byte[][]>().Select(b => DeserializeRedisValue<T>(b)).ToArray()).ThrowOrValue();

		public long LRem(string key, long count, object element) => Call<long>("LREM".Input(key, count).InputRaw(SerializeRedisValue(element)).FlagKey(key)).ThrowOrValue();
		public void LSet(string key, long index, object element) => Call<string>("LSET".Input(key, index).InputRaw(SerializeRedisValue(element)).FlagKey()).ThrowOrValue();
		public void LTrim(string key, long start, long stop) => Call<string>("LTRIM".Input(key, start, stop).FlagKey(key)).ThrowOrValue();
		public string RPop(string key) => Call<string>("RPOP".Input(key).FlagKey(key)).ThrowOrValue();
		public T RPop<T>(string key) => Call<byte[]>("RPOP".Input(key).FlagKey(key)).NewValue(a => DeserializeRedisValue<T>(a)).ThrowOrValue();

		public string RPopLPush(string source, string destination) => Call<string>("RPOPLPUSH".Input(source, destination).FlagKey(source, destination)).ThrowOrValue();
		public T RPopLPush<T>(string source, string destination) => Call<byte[]>("RPOPLPUSH".Input(source, destination).FlagKey(source, destination)).NewValue(a => DeserializeRedisValue<T>(a)).ThrowOrValue();

		public long RPush(string key, params object[] elements) => Call<long>("RPUSH".Input(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key)).ThrowOrValue();
		public long RPushX(string key, params object[] elements) => Call<long>("RPUSHX".Input(key).Input(elements.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key)).ThrowOrValue();
	}
}
