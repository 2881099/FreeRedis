using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public long SAdd(string key, params object[] members) => Call<long>("SADD", key, members.Select(a => SerializeRedisValue(a)).ToArray()).ThrowOrValue();
		public long SCard(string key) => Call<long>("SCARD", key).ThrowOrValue();

		public string[] SDiff(params string[] keys) => Call<string[]>("SDIFF", null, keys).ThrowOrValue();
		public T[] SDiff<T>(params string[] keys) => SReadArray<T>("SDIFF", keys);
		public long SDiffStore(string destination, params string[] keys) => Call<long>("SDIFFSTORE", destination, keys).ThrowOrValue();

		public string[] SInter(params string[] keys) => Call<string[]>("SINTER", null, keys).ThrowOrValue();
		public T[] SInter<T>(params string[] keys) => SReadArray<T>("SINTER", keys);
		public long SInterStore(string destination, params string[] keys) => Call<long>("SINTERSTORE", destination, keys).ThrowOrValue();

		public bool SIsMember(string key, object member) => Call<bool>("SISMEMBER", key, SerializeRedisValue(member)).ThrowOrValue();
		public string[] SMeMembers(string key) => Call<string[]>("SMEMBERS", key).ThrowOrValue();
		public T[] SMeMembers<T>(string key) => SReadArray<T>("SDIFF", key);

		public bool SMove(string source, string destination, object member) => Call<bool>("SMOVE", source, destination, SerializeRedisValue(member)).ThrowOrValue();

		public string SPop(string key) => Call<string>("SPOP", key).ThrowOrValue();
		public T SPop<T>(string key) => Call<byte[]>("SPOP", key).NewValue(a => DeserializeRedisValue<T>(a)).ThrowOrValue();
		public string[] SPop(string key, int count) => Call<string[]>("SPOP", key, count).ThrowOrValue();
		public T[] SPop<T>(string key, int count) => SReadArray<T>("SPOP", key, count);

		public string SRandMember(string key) => Call<string>("SRANDMEMBER", key).ThrowOrValue();
		public T SRandMember<T>(string key) => Call<byte[]>("SRANDMEMBER", key).NewValue(a => DeserializeRedisValue<T>(a)).ThrowOrValue();
		public string[] SRandMember(string key, int count) => Call<string[]>("SRANDMEMBER", key, count).ThrowOrValue();
		public T[] SRandMember<T>(string key, int count) => SReadArray<T>("SRANDMEMBER", key, count);

		public long SRem(string key, params object[] members) => Call<long>("SREM", key, members.Select(a => SerializeRedisValue(a)).ToArray()).ThrowOrValue();
		//SSCAN key cursor [MATCH pattern] [COUNT count]

		public string[] SUnion(params string[] keys) => Call<string[]>("SUNION", null, keys).ThrowOrValue();
		public T[] SUnion<T>(params string[] keys) => SReadArray<T>("SUNION", keys);
		public long SUnionStore(string destination, params string[] keys) => Call<long>("SUNIONSTORE", destination, keys).ThrowOrValue();

		T[] SReadArray<T>(string cmd, params object[] parms)
		{
			CallWriteOnly(cmd, null, parms);
			var value = Resp3Helper.Read<object>(Stream).ThrowOrValue();
			var list = value.ConvertTo<byte[][]>();
			return list.Select(a => DeserializeRedisValue<T>(a)).ToArray();
		}
	}
}
