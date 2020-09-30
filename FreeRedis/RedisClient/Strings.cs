using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<long> Append(string key, object value) => Call<long>("APPEND", key, value);
		public RedisResult<long> BitCount(string key, long start, long end) => Call<long>("BITCOUNT", key, start, end);
		//BITFIELD key [GET type offset] [SET type offset value] [INCRBY type offset increment] [OVERFLOW WRAP|SAT|FAIL]
		public RedisResult<long> BitOp(BitOpOperation operation, string destkey, params string[] keys) => Call<long>("BITOP", null, "".AddIf(true, operation, destkey, keys).ToArray());
		public RedisResult<long> BitPos(string key, object bit, long start = 0, long end = 0) => start > 0 && end > 0 ? Call<long>("BITPOS", key, new object[] { bit, start, end }) :
			(start > 0 ? Call<long>("BITPOS", key, new object[] { bit, start }) : Call<long>("BITPOS", key, bit));
		public RedisResult<long> Decr(string key) => Call<long>("DECR", key);
		public RedisResult<long> DecrBy(string key, long decrement) => Call<long>("DECRBY", key, decrement);
		public RedisResult<string> Get(string key) => Call<string>("GET", key);
		public RedisResult<long> GetBit(string key, long offset) => Call<long>("GETBIT", key, offset);
		public RedisResult<string> GetRange(string key, long start, long end) => Call<string>("GETRANGE", key, start, end);
		public RedisResult<string> GetSet(string key, object value) => Call<string>("GETSET", key, value);
		public RedisResult<long> Incr(string key) => Call<long>("INCR", key);
		public RedisResult<long> IncrBy(string key, long increment) => Call<long>("INCRBY", key, increment);
		public RedisResult<decimal> IncrByFloat(string key, decimal increment) => Call<decimal>("INCRBYFLOAT", key, increment);
		public RedisResult<string[]> MGet(params string[] keys) => Call<string[]>("MGET", null, keys);
		public RedisResult<string> MSet(Dictionary<string, object> keyValues) => Call<string>("MSET", null, keyValues.ToKvArray());
		public RedisResult<long> MSetNx(Dictionary<string, object> keyValues) => Call<long>("MSETNX", null, keyValues.ToKvArray());
		public RedisResult<string> PSetNx(string key, long milliseconds, object value) => Call<string>("PSETEX", key, milliseconds, value);
		public RedisResult<string> Set(string key, object value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, false);
		public RedisResult<string> Set(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, true, false, false);
		public RedisResult<string> SetNx(string key, object value, int timeoutSeconds) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, true, false);
		public RedisResult<string> SetNx(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, true, true, false);
		public RedisResult<string> SetXx(string key, object value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, true);
		public RedisResult<string> SetXx(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, true, false, true);
		RedisResult<string> Set(string key, object value, TimeSpan timeout, bool keepTtl, bool nx, bool xx) => Call<string>("SET", key, ""
			.AddIf(true, value)
			.AddIf(timeout.TotalSeconds >= 1, "EX", (long)timeout.TotalSeconds)
			.AddIf(timeout.TotalSeconds < 1 && timeout.TotalMilliseconds >= 1, "PX", (long)timeout.TotalMilliseconds)
			.AddIf(keepTtl, "KEEPTTL")
			.AddIf(nx, "NX")
			.AddIf(xx, "XX").ToArray());
		public RedisResult<long> SetBit(string key, long offset, object value) => Call<long>("SETBIT", key, offset, value);
		public RedisResult<string> SetEx(string key, int seconds, object value) => Call<string>("SETEX", key, seconds, value);
		public RedisResult<bool> SetNx(string key, object value) => Call<bool>("SETNX", key, value);
		public RedisResult<long> SetRange(string key, long offset, object value) => Call<long>("SETRANGE", key, offset, value);
		//STRALGO LCS algo-specific-argument [algo-specific-argument ...]
		public RedisResult<long> StrLen(string key) => Call<long>("STRLEN", key);
    }
}
