using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<long> SAdd(string key, params string[] members) => Call<long>("SADD", key, members);
		public RedisResult<long> SCard(string key) => Call<long>("SCARD", key);
		public RedisResult<string[]> SDiff(params string[] keys) => Call<string[]>("SDIFF", null, keys);
		public RedisResult<long> SDiffStore(string destination, params string[] keys) => Call<long>("SDIFFSTORE", destination, keys);
		public RedisResult<string[]> SInter(params string[] keys) => Call<string[]>("SINTER", null, keys);
		public RedisResult<long> SInterStore(string destination, params string[] keys) => Call<long>("SINTERSTORE", destination, keys);
		public RedisResult<bool> SIsMember(string key, string member) => Call<bool>("SISMEMBER", key, member);
		public RedisResult<string[]> SMeMembers(string key) => Call<string[]>("SMEMBERS", key);
		public RedisResult<bool> SMove(string source, string destination, string member) => Call<bool>("SMOVE", source, destination, member);
		public RedisResult<string> SPop(string key) => Call<string>("SPOP", key);
		public RedisResult<string[]> SPop(string key, int count) => Call<string[]>("SPOP", key, count);
		public RedisResult<string> SRandMember(string key) => Call<string>("SRANDMEMBER", key);
		public RedisResult<string[]> SRandMember(string key, int count) => Call<string[]>("SRANDMEMBER", key, count);
		public RedisResult<long> SRem(string key, params string[] members) => Call<long>("SREM", key, members);
		//SSCAN key cursor [MATCH pattern] [COUNT count]
		public RedisResult<string[]> SUnion(params string[] keys) => Call<string[]>("SUNION", null, keys);
		public RedisResult<long> SUnionStore(string destination, params string[] keys) => Call<long>("SUNIONSTORE", destination, keys);
    }
}
