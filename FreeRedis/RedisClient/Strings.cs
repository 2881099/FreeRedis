using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public long Append(string key, object value) => Call<long>("APPEND", key, SerializeRedisValue(value)).ThrowOrValue();
		public long BitCount(string key, long start, long end) => Call<long>("BITCOUNT", key, start, end).ThrowOrValue();
		//BITFIELD key [GET type offset] [SET type offset value] [INCRBY type offset increment] [OVERFLOW WRAP|SAT|FAIL]
		public long BitOp(BitOpOperation operation, string destkey, params string[] keys) => Call<long>("BITOP", null, "".AddIf(true, operation, destkey, keys).ToArray()).ThrowOrValue();
		public long BitPos(string key, bool bit, long? start = null, long? end = null) => Call<long>("BITPOS", key, ""
			.AddIf(true, bit ? "1": "0")
			.AddIf(start != null, start)
			.AddIf(end != null, start)
			.ToArray()).ThrowOrValue();
		public long Decr(string key) => Call<long>("DECR", key).ThrowOrValue();
		public long DecrBy(string key, long decrement) => Call<long>("DECRBY", key, decrement).ThrowOrValue();
		public string Get(string key) => Call<string>("GET", key).ThrowOrValue();
		public T Get<T>(string key) => Call<byte[]>("GET", key).NewValue(a => DeserializeRedisValue<T>(a)).ThrowOrValue();
		public void Get(string key, Stream destination, int bufferSize = 1024)
		{
			CallWriteOnly("GET", key);
			Resp3Helper.ReadChunk(Stream, destination, bufferSize);
		}
		public bool GetBit(string key, long offset) => Call<bool>("GETBIT", key, offset).ThrowOrValue();
		public string GetRange(string key, long start, long end) => Call<string>("GETRANGE", key, start, end).ThrowOrValue();
		public T GetRange<T>(string key, long start, long end) => Call<byte[]>("GETRANGE", key, start, end).NewValue(a => DeserializeRedisValue<T>(a)).ThrowOrValue();

		public string GetSet(string key, object value) => Call<string>("GETSET", key, SerializeRedisValue(value)).ThrowOrValue();
		public long Incr(string key) => Call<long>("INCR", key).ThrowOrValue();
		public long IncrBy(string key, long increment) => Call<long>("INCRBY", key, increment).ThrowOrValue();
		public decimal IncrByFloat(string key, decimal increment) => Call<decimal>("INCRBYFLOAT", key, increment).ThrowOrValue();

		public string[] MGet(params string[] keys) => Call<string[]>("MGET", null, keys).ThrowOrValue();
		public string MSet(Dictionary<string, object> keyValues) => Call<string>("MSET", null, keyValues.ToKvArray(SerializeRedisValue)).ThrowOrValue();
		public long MSetNx(Dictionary<string, object> keyValues) => Call<long>("MSETNX", null, keyValues.ToKvArray(SerializeRedisValue)).ThrowOrValue();
		public string PSetNx(string key, long milliseconds, object value) => Call<string>("PSETEX", key, milliseconds, SerializeRedisValue(value)).ThrowOrValue();

		public void Set(string key, object value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, false).ThrowOrValue();
		public RedisResult<string> Set(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, keepTtl, false, false);
		public RedisResult<string> SetNx(string key, object value, int timeoutSeconds) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, true, false);
		public RedisResult<string> SetNx(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, keepTtl, true, false);
		public RedisResult<string> SetXx(string key, object value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, true);
		public RedisResult<string> SetXx(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, keepTtl, false, true);
		RedisResult<string> Set(string key, object value, TimeSpan timeout, bool keepTtl, bool nx, bool xx) => Call<string>("SET", key, ""
			.AddRaw(SerializeRedisValue(value))
			.AddIf(timeout.TotalSeconds >= 1, "EX", (long)timeout.TotalSeconds)
			.AddIf(timeout.TotalSeconds < 1 && timeout.TotalMilliseconds >= 1, "PX", (long)timeout.TotalMilliseconds)
			.AddIf(keepTtl, "KEEPTTL")
			.AddIf(nx, "NX")
			.AddIf(xx, "XX").ToArray());

		public long SetBit(string key, long offset, bool value) => Call<long>("SETBIT", key, offset, value ? "1" : "0").ThrowOrValue();
		public RedisResult<string> SetEx(string key, int seconds, object value) => Call<string>("SETEX", key, seconds, SerializeRedisValue(value));
		public RedisResult<bool> SetNx(string key, object value) => Call<bool>("SETNX", key, SerializeRedisValue(value));
		public RedisResult<long> SetRange(string key, long offset, string value) => Call<long>("SETRANGE", key, offset, value);
		//STRALGO LCS algo-specific-argument [algo-specific-argument ...]
		public RedisResult<long> StrLen(string key) => Call<long>("STRLEN", key);
    }
}
