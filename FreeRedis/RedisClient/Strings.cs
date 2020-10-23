using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FreeRedis
{
	partial class RedisClient
	{
		public long Append(string key, object value) => Call<long>("APPEND".Input(key).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue());
		public long BitCount(string key, long start, long end) => Call<long>("BITCOUNT".Input(key, start, end).FlagKey(key), rt => rt.ThrowOrValue());
		//BITFIELD key [GET type offset] [SET type offset value] [INCRBY type offset increment] [OVERFLOW WRAP|SAT|FAIL]
		public long BitOp(BitOpOperation operation, string destkey, params string[] keys) => Call<long>("BITOP".SubCommand(null).Input(operation, destkey, keys).FlagKey(destkey).FlagKey(keys), rt => rt.ThrowOrValue());
		public long BitPos(string key, bool bit, long? start = null, long? end = null) => Call<long>("BITPOS"
			.Input(key)
			.InputRaw(bit ? "1": "0")
			.InputIf(start != null, start)
			.InputIf(end != null, start)
			.FlagKey(key), rt => rt.ThrowOrValue());
		public long Decr(string key) => Call<long>("DECR".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public long DecrBy(string key, long decrement) => Call<long>("DECRBY".Input(key, decrement).FlagKey(key), rt => rt.ThrowOrValue());
		public string Get(string key) => Call<string>("GET".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public T Get<T>(string key) => Call<byte[], T>("GET".Input(key).FlagKey(key), rt => rt.NewValue(a => DeserializeRedisValue<T>(a, rt.Encoding)).ThrowOrValue());
		public void Get(string key, Stream destination, int bufferSize = 1024)
		{
			var cmd = "GET".Input(key).FlagKey(key);
			LogCall(cmd, () =>
			{
				using (var rds = Adapter.GetRedisSocket(cmd))
				{
					rds.Write(cmd);
					RespHelper.ReadChunk(rds.Stream, destination, bufferSize);
				}
				return default(string);
			});
		}
		public bool GetBit(string key, long offset) => Call<bool>("GETBIT".Input(key, offset).FlagKey(key), rt => rt.ThrowOrValue());
		public string GetRange(string key, long start, long end) => Call<string>("GETRANGE".Input(key, start, end).FlagKey(key), rt => rt.ThrowOrValue());
		public T GetRange<T>(string key, long start, long end) => Call<byte[], T>("GETRANGE".Input(key, start, end).FlagKey(key), rt => rt.NewValue(a => DeserializeRedisValue<T>(a, rt.Encoding)).ThrowOrValue());

		public string GetSet(string key, object value) => Call<string>("GETSET".Input(key).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue());
		public long Incr(string key) => Call<long>("INCR".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public long IncrBy(string key, long increment) => Call<long>("INCRBY".Input(key, increment).FlagKey(key), rt => rt.ThrowOrValue());
		public decimal IncrByFloat(string key, decimal increment) => Call<decimal>("INCRBYFLOAT".Input(key, increment).FlagKey(key), rt => rt.ThrowOrValue());

		public string[] MGet(params string[] keys) => Call<string[]>("MGET".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue());
		public T[] MGet<T>(params string[] keys) => Call<object, T[]>("MGET".Input(keys).FlagKey(keys), rt => rt
			.NewValue(a => a.ConvertTo<byte[][]>().Select(b => DeserializeRedisValue<T>(b, rt.Encoding)).ToArray())
			.ThrowOrValue());

		public void MSet(Dictionary<string, object> keyValues) => Call<string>("MSET".SubCommand(null).InputKv(keyValues, SerializeRedisValue).FlagKey(keyValues.Keys), rt => rt.ThrowOrValue());
		public void MSet(params object[] keyValues) => MSet(keyValues.MapToHash<object>(Encoding.UTF8));
		public bool MSetNx(Dictionary<string, object> keyValues) => Call<bool>("MSETNX".SubCommand(null).InputKv(keyValues, SerializeRedisValue).FlagKey(keyValues.Keys), rt => rt.ThrowOrValue());
		public bool MSetNx(params object[] keyValues) => MSetNx(keyValues.MapToHash<object>(Encoding.UTF8));
		public void PSetEx(string key, long milliseconds, object value) => Call<string>("PSETEX".Input(key, milliseconds).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue());

		public void Set(string key, object value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, false);
		public void Set(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, keepTtl, false, false);
		public void SetNx(string key, object value, int timeoutSeconds) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, true, false);
		public void SetNx(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, keepTtl, true, false);
		public void SetXx(string key, object value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, true);
		public void SetXx(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, keepTtl, false, true);
		string Set(string key, object value, TimeSpan timeout, bool keepTtl, bool nx, bool xx) => Call<string>("SET"
			.Input(key)
			.InputRaw(SerializeRedisValue(value))
			.InputIf(timeout.TotalSeconds >= 1, "EX", (long)timeout.TotalSeconds)
			.InputIf(timeout.TotalSeconds < 1 && timeout.TotalMilliseconds >= 1, "PX", (long)timeout.TotalMilliseconds)
			.InputIf(keepTtl, "KEEPTTL")
			.InputIf(nx, "NX")
			.InputIf(xx, "XX")
			.FlagKey(key), rt => rt.ThrowOrValue());

		public long SetBit(string key, long offset, bool value) => Call<long>("SETBIT".Input(key, offset).InputRaw(value ? "1" : "0").FlagKey(key), rt => rt.ThrowOrValue());
		public void SetEx(string key, int seconds, object value) => Call<string>("SETEX".Input(key, seconds).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue());
		public bool SetNx(string key, object value) => Call<bool>("SETNX".Input(key).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue());
		public long SetRange(string key, long offset, object value) => Call<long>("SETRANGE".Input(key, offset).InputRaw(SerializeRedisValue(value)).FlagKey(key), rt => rt.ThrowOrValue());
		//STRALGO LCS algo-specific-argument [algo-specific-argument ...]
		public long StrLen(string key) => Call<long>("STRLEN".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
	}
}
