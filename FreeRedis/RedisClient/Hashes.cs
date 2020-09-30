using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<long> HDel(string key, params string[] fields) => Call<long>("HDEL", key, "".AddIf(fields?.Any() == true, fields).ToArray());
		public RedisResult<bool> HExists(string key, string field) => Call<bool>("HEXISTS", key, field);
		public RedisResult<string> HGet(string key, string field) => Call<string>("HGET", key, field);
		public RedisResult<string[]> HGetAll(string key) => Call<string[]>("HGETALL", key);
		public RedisResult<long> IncrBy(string key, string field, long increment) => Call<long>("HINCRBY", key, field, increment);
		public RedisResult<decimal> IncrByFloat(string key, string field, decimal increment) => Call<decimal>("HINCRBYFLOAT", key, field, increment);
		public RedisResult<string[]> HKeys(string key) => Call<string[]>("HKEYS", key);
		public RedisResult<long> HLen(string key) => Call<long>("HLEN", key);
		public RedisResult<string[]> HMGet(string key, params string[] fields) => Call<string[]>("HMGET", key, "".AddIf(fields?.Any() == true, fields).ToArray());
		public RedisResult<string> HMSet(string key, Dictionary<string, string> keyValues) => Call<string>("HMSET", key, keyValues.ToKvArray());
		public RedisResult<ScanValue<string>> HScan(string key, long cursor, string pattern, long count, string type) => Call<object>("HSCAN", key, ""
			.AddIf(true, cursor)
			.AddIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
			.AddIf(count != 0, "COUNT", count)
			.ToArray()).NewValue(a =>
			{
				var arr = a as object[];
				return new ScanValue<string>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<string[]>());
			});
		public RedisResult<long> HSet(string key, string field, string value) => Call<long>("HSET", key, field, value);
		public RedisResult<long> HSet(string key, Dictionary<string, string> keyValues) => Call<long>("HSET", key, keyValues.ToKvArray());
		public RedisResult<bool> HSetNx(string key, string field, string value) => Call<bool>("HSET", key, field, value);
		public RedisResult<long> HStrLen(string key, string field) => Call<long>("HSTRLEN", key, field);
		public RedisResult<string[]> HVals(string key) => Call<string[]>("HVALS", key);
    }
}
