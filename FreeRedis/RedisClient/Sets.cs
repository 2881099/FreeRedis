using FreeRedis.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public long SAdd(string key, params object[] members) => Call<long>("SADD".Input(key).Input(members.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key), rt => rt.ThrowOrValue());
		public long SCard(string key) => Call<long>("SCARD".Input(key).FlagKey(key), rt => rt.ThrowOrValue());

		public string[] SDiff(params string[] keys) => Call<string[]>("SDIFF".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue());
		public T[] SDiff<T>(params string[] keys) => SReadArray<T>("SDIFF".Input(keys).FlagKey(keys));
		public long SDiffStore(string destination, params string[] keys) => Call<long>("SDIFFSTORE".Input(destination).Input(keys).FlagKey(destination).FlagKey(keys), rt => rt.ThrowOrValue());

		public string[] SInter(params string[] keys) => Call<string[]>("SINTER".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue());
		public T[] SInter<T>(params string[] keys) => SReadArray<T>("SINTER".Input(keys).FlagKey(keys));
		public long SInterStore(string destination, params string[] keys) => Call<long>("SINTERSTORE".Input(destination).Input(keys).FlagKey(destination).FlagKey(keys), rt => rt.ThrowOrValue());

		public bool SIsMember(string key, object member) => Call<bool>("SISMEMBER".Input(key).InputRaw(SerializeRedisValue(member)).FlagKey(key), rt => rt.ThrowOrValue());
		public string[] SMeMembers(string key) => Call<string[]>("SMEMBERS".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public T[] SMeMembers<T>(string key) => SReadArray<T>("SMISMEMBER".Input(key).FlagKey(key));

		public bool SMove(string source, string destination, object member) => Call<bool>("SMOVE"
			.Input(source, destination)
			.InputRaw(SerializeRedisValue(member))
			.FlagKey(new[] { source, destination }), rt => rt.ThrowOrValue());

		public string SPop(string key) => Call<string>("SPOP".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public T SPop<T>(string key) => Call<byte[], T>("SPOP".Input(key).FlagKey(key), rt => rt.NewValue(a => DeserializeRedisValue<T>(a, rt.Encoding)).ThrowOrValue());
		public string[] SPop(string key, int count) => Call<string[]>("SPOP".Input(key, count).FlagKey(key), rt => rt.ThrowOrValue());
		public T[] SPop<T>(string key, int count) => SReadArray<T>("SPOP".Input(key, count).FlagKey(key));

		public string SRandMember(string key) => Call<string>("SRANDMEMBER".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public T SRandMember<T>(string key) => Call<byte[], T>("SRANDMEMBER".Input(key).FlagKey(key), rt => rt.NewValue(a => DeserializeRedisValue<T>(a, rt.Encoding)).ThrowOrValue());
		public string[] SRandMember(string key, int count) => Call<string[]>("SRANDMEMBER".Input(key, count).FlagKey(key), rt => rt.ThrowOrValue());
		public T[] SRandMember<T>(string key, int count) => SReadArray<T>("SRANDMEMBER".Input(key, count).FlagKey(key));

		public long SRem(string key, params object[] members) => Call<long>("SREM".Input(key).Input(members.Select(a => SerializeRedisValue(a)).ToArray()).FlagKey(key), rt => rt.ThrowOrValue());
		public ScanResult<string> SScan(string key, long cursor, string pattern, long count) => Call<object, ScanResult<string>>("SSCAN"
			.Input(key, cursor)
			.InputIf(!string.IsNullOrWhiteSpace(pattern), "MATCH", pattern)
			.InputIf(count != 0, "COUNT", count)
			.FlagKey(key), rt => rt.NewValue(a =>
			{
				var arr = a as List<object>;
				return new ScanResult<string>(arr[0].ConvertTo<long>(), arr[1].ConvertTo<string[]>());
			}).ThrowOrValue());

		public string[] SUnion(params string[] keys) => Call<string[]>("SUNION".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue());
		public T[] SUnion<T>(params string[] keys) => SReadArray<T>("SUNION".Input(keys).FlagKey(keys));
		public long SUnionStore(string destination, params string[] keys) => Call<long>("SUNIONSTORE".Input(destination).Input(keys).FlagKey(destination).FlagKey(keys), rt => rt.ThrowOrValue());

		T[] SReadArray<T>(CommandPacket cb) => Call<object, T[]>(cb, rt => rt
			.NewValue(a => a.ConvertTo<byte[][]>().Select(b => DeserializeRedisValue<T>(b, rt.Encoding)).ToArray())
			.ThrowOrValue());
	}
}
