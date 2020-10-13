using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public long HDel(string key, params string[] fields) => Call<long>("HDEL".Input(key).Input(fields).FlagKey(key), rt => rt.ThrowOrValue());
		public bool HExists(string key, string field) => Call<bool>("HEXISTS".Input(key, field).FlagKey(key), rt => rt.ThrowOrValue());
		public string HGet(string key, string field) => Call<string>("HGET".Input(key, field).FlagKey(key), rt => rt.ThrowOrValue());
		public Dictionary<string, string> HGetAll(string key) => Call<string[], Dictionary<string, string>>("HGETALL".Input(key).FlagKey(key), rt => rt.NewValue(a => a.MapToHash<string>(Encoding)).ThrowOrValue());

		public long IncrBy(string key, string field, long increment) => Call<long>("HINCRBY".Input(key, field, increment).FlagKey(key), rt => rt.ThrowOrValue());
		public decimal IncrByFloat(string key, string field, decimal increment) => Call<decimal>("HINCRBYFLOAT".Input(key, field, increment).FlagKey(key), rt => rt.ThrowOrValue());
		public string[] HKeys(string key) => Call<string[]>("HKEYS".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public long HLen(string key) => Call<long>("HLEN".Input(key).FlagKey(key), rt => rt.ThrowOrValue());

		public string[] HMGet(string key, params string[] fields) => Call<string[]>("HMGET".Input(key).Input(fields).FlagKey(key), rt => rt.ThrowOrValue());
		public void HMSet(string key, Dictionary<string, string> keyValues) => Call<string>("HMSET".Input(key).InputKv(keyValues).FlagKey(key), rt => rt.ThrowOrValue());
		public ScanValue<string> HScan(string key, long cursor, string pattern, long count) => Call<object, ScanValue<string>>("HSCAN"
			.Input(key, cursor)
			.InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
			.InputIf(count != 0, "COUNT", count)
			.FlagKey(key), rt => rt
			.NewValue(a =>
			{
				var arr = a as List<object>;
				return new ScanValue<string>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<string[]>());
			}).ThrowOrValue());
		public bool HSet(string key, string field, string value) => Call<bool>("HSET".Input(key, field, value).FlagKey(key), rt => rt.ThrowOrValue());
		public long HSet(string key, Dictionary<string, string> keyValues) => Call<long>("HSET".Input(key).InputKv(keyValues).FlagKey(key), rt => rt.ThrowOrValue());
		public bool HSetNx(string key, string field, string value) => Call<bool>("HSET".Input(key, field, value).FlagKey(key), rt => rt.ThrowOrValue());
		public long HStrLen(string key, string field) => Call<long>("HSTRLEN".Input(key, field).FlagKey(key), rt => rt.ThrowOrValue());
		public string[] HVals(string key) => Call<string[]>("HVALS".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
	}
}
