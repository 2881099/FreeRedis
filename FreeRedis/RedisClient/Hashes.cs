using FreeRedis.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FreeRedis
{
	partial class RedisClient
	{
		public long HDel(string key, params string[] fields) => Call<long>("HDEL".Input(key).Input(fields).FlagKey(key), rt => rt.ThrowOrValue());
		public bool HExists(string key, string field) => Call<bool>("HEXISTS".Input(key, field).FlagKey(key), rt => rt.ThrowOrValue());
		public string HGet(string key, string field) => Call<string>("HGET".Input(key, field).FlagKey(key), rt => rt.ThrowOrValue());
		public T HGet<T>(string key, string field) => Call<byte[], T>("HGET".Input(key, field).FlagKey(key), rt => rt.NewValue(a => DeserializeRedisValue<T>(a, rt.Encoding)).ThrowOrValue());

		public Dictionary<string, string> HGetAll(string key) => Call<string[], Dictionary<string, string>>("HGETALL".Input(key).FlagKey(key), rt => rt.NewValue(a => a.MapToHash<string>(rt.Encoding)).ThrowOrValue());
		public Dictionary<string, T> HGetAll<T>(string key) => Call<object, Dictionary<string, T>>("HGETALL".Input(key).FlagKey(key), rt => rt
			.NewValue(a =>
			{
				var objs = a as object[];
				for (var x = 0; x < objs.Length; x += 2) objs[x + 1] = DeserializeRedisValue<T>(objs[x + 1] as byte[], rt.Encoding);
				return objs.MapToHash<T>(rt.Encoding);
			}).ThrowOrValue());

		public long HIncrBy(string key, string field, long increment) => Call<long>("HINCRBY".Input(key, field, increment).FlagKey(key), rt => rt.ThrowOrValue());
		public decimal HIncrByFloat(string key, string field, decimal increment) => Call<decimal>("HINCRBYFLOAT".Input(key, field, increment).FlagKey(key), rt => rt.ThrowOrValue());
		public string[] HKeys(string key) => Call<string[]>("HKEYS".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public long HLen(string key) => Call<long>("HLEN".Input(key).FlagKey(key), rt => rt.ThrowOrValue());

		public string[] HMGet(string key, params string[] fields) => Call<string[]>("HMGET".Input(key).Input(fields).FlagKey(key), rt => rt.ThrowOrValue());
		public T[] HMGet<T>(string key, params string[] fields) => HReadArray<T>("HMGET".Input(key).Input(fields).FlagKey(key));
		public void HMSet(string key, string field, object value, params object[] fieldValues) => HSet(false, key, field, value, fieldValues);
		public void HMSet(string key, Dictionary<string, string> keyValues) => Call<string>("HMSET".Input(key).InputKv(keyValues).FlagKey(key), rt => rt.ThrowOrValue());

		public ScanResult<string> HScan(string key, long cursor, string pattern, long count) => Call<object, ScanResult<string>>("HSCAN"
			.Input(key, cursor)
			.InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
			.InputIf(count != 0, "COUNT", count)
			.FlagKey(key), rt => rt
			.NewValue(a =>
			{
				var arr = a as object[];
				return new ScanResult<string>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<string[]>());
			}).ThrowOrValue());

		public long HSet(string key, string field, object value, params object[] fieldValues) => HSet(false, key, field, value, fieldValues);
		public long HSet(string key, Dictionary<string, string> keyValues) => Call<long>("HSET".Input(key).InputKv(keyValues).FlagKey(key), rt => rt.ThrowOrValue());
		long HSet(bool hmset, string key, string field, object value, params object[] fieldValues)
		{
			if (fieldValues?.Any() == true)
			{
				var kvs = fieldValues.MapToKvList<object>(Encoding.UTF8);
				kvs.Insert(0, new KeyValuePair<string, object>(field, SerializeRedisValue(value)));
				return Call<long>((hmset ? "HMSET" : "HSET").SubCommand(null).InputRaw(key).InputKv(kvs, SerializeRedisValue).FlagKey(kvs.Select(a => a.Key).ToArray()), rt => rt.ThrowOrValue());
			}
			return Call<long>((hmset ? "HMSET" : "HSET").SubCommand(null).Input(key, field).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue());
		}

		public bool HSetNx(string key, string field, object value) => Call<bool>("HSETNX".Input(key, field).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue());
		public long HStrLen(string key, string field) => Call<long>("HSTRLEN".Input(key, field).FlagKey(key), rt => rt.ThrowOrValue());

		public string[] HVals(string key) => Call<string[]>("HVALS".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public T[] HVals<T>(string key) => HReadArray<T>("HVALS".Input(key).FlagKey(key));

		T[] HReadArray<T>(CommandPacket cb) => Call<object, T[]>(cb, rt => rt
			.NewValue(a => a.ConvertTo<byte[][]>().Select(b => DeserializeRedisValue<T>(b, rt.Encoding)).ToArray())
			.ThrowOrValue());
	}
}
