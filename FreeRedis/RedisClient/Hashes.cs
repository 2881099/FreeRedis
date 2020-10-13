using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<long> HDel(string key, params string[] fields) => Call<long>("HDEL".Input(key).Input(fields).FlagKey(key));
		public RedisResult<bool> HExists(string key, string field) => Call<bool>("HEXISTS".Input(key, field).FlagKey(key));
		public RedisResult<string> HGet(string key, string field) => Call<string>("HGET".Input(key, field).FlagKey(key));
		public RedisResult<string[]> HGetAll(string key) => Call<string[]>("HGETALL".Input(key).FlagKey(key));
		public RedisResult<long> IncrBy(string key, string field, long increment) => Call<long>("HINCRBY".Input(key, field, increment).FlagKey(key));
		public RedisResult<decimal> IncrByFloat(string key, string field, decimal increment) => Call<decimal>("HINCRBYFLOAT".Input(key, field, increment).FlagKey(key));
		public RedisResult<string[]> HKeys(string key) => Call<string[]>("HKEYS".Input(key).FlagKey(key));
		public RedisResult<long> HLen(string key) => Call<long>("HLEN".Input(key).FlagKey(key));
		public RedisResult<string[]> HMGet(string key, params string[] fields) => Call<string[]>("HMGET".Input(key).Input(fields).FlagKey(key));
		public RedisResult<string> HMSet(string key, Dictionary<string, string> keyValues) => Call<string>("HMSET".Input(key).InputKv(keyValues).FlagKey(key));
		public RedisResult<ScanValue<string>> HScan(string key, long cursor, string pattern, long count, string type) => Call<object>("HSCAN"
			.Input(key, cursor)
			.InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
			.InputIf(count != 0, "COUNT", count)
			.FlagKey(key)).NewValue(a =>
			{
				var arr = a as object[];
				return new ScanValue<string>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<string[]>());
			});
		public RedisResult<long> HSet(string key, string field, string value) => Call<long>("HSET".Input(key, field, value).FlagKey(key));
		public RedisResult<long> HSet(string key, Dictionary<string, string> keyValues) => Call<long>("HSET".Input(key).InputKv(keyValues).FlagKey(key));
		public RedisResult<bool> HSetNx(string key, string field, string value) => Call<bool>("HSET".Input(key, field, value).FlagKey(key));
		public RedisResult<long> HStrLen(string key, string field) => Call<long>("HSTRLEN".Input(key, field).FlagKey(key));
		public RedisResult<string[]> HVals(string key) => Call<string[]>("HVALS".Input(key).FlagKey(key));
    }
}
