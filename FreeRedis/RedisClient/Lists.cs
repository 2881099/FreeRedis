using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<string> BLPop(string key, int timeoutSeconds) => Call<string[]>("BLPOP", key, timeoutSeconds).NewValue(a => a.FirstOrDefault());
		public RedisResult<string[]> BLPop(string[] keys, int timeoutSeconds) => Call<string[]>("BLPOP", null, "".AddIf(true, keys, timeoutSeconds));
		public RedisResult<string> BRPop(string key, int timeoutSeconds) => Call<string[]>("BRPOP", key, timeoutSeconds).NewValue(a => a.FirstOrDefault());
		public RedisResult<string[]> BRPop(string[] keys, int timeoutSeconds) => Call<string[]>("BRPOP", null, "".AddIf(true, keys, timeoutSeconds));
		public RedisResult<string[]> BRPopLPush(string source, string destination, int timeoutSeconds) => Call<string[]>("BRPOPLPUSH", source, destination, timeoutSeconds);
		public RedisResult<string> LIndex(string key, long index) => Call<string>("LINDEX", key, index);
		public RedisResult<long> LInsert(string key, InsertDirection direction, string pivot, string element) => Call<long>("LINSERT", key, direction, pivot, element);
		public RedisResult<long> LLen(string key) => Call<long>("LLEN", key);
		public RedisResult<string> LPop(string key) => Call<string>("LPOP", key);
		public RedisResult<long> LPos(string key, string element, int rank = 0) => Call<long>("LPOS", key, element.AddIf(rank != 0, "RANK", rank).ToArray());
		public RedisResult<long[]> LPos(string key, string element, int rank, int count, int maxLen) => Call<long[]>("LPOS", key, element
			.AddIf(rank != 0, "RANK", rank)
			.AddIf(true, "COUNT", count)
			.AddIf(maxLen != 0, "MAXLEN ", maxLen)
			.ToArray());
		public RedisResult<long> LPush(string key, params string[] elements) => Call<long>("LPUSH", key, elements);
		public RedisResult<long> LPushX(string key, params string[] elements) => Call<long>("LPUSHX", key, elements);
		public RedisResult<string[]> LRange(string key, long start, long stop) => Call<string[]>("LRANGE", key, start, stop);
		public RedisResult<long> LRem(string key, long count, string element) => Call<long>("LREM", key, count, element);
		public RedisResult<string> LSet(string key, long index, string element) => Call<string>("LSET", key, index, element);
		public RedisResult<string[]> LTrim(string key, long start, long stop) => Call<string[]>("LTRIM", key, start, stop);
		public RedisResult<string> RPop(string key) => Call<string>("RPOP", key);
		public RedisResult<string[]> RPopLPush(string source, string destination) => Call<string[]>("RPOPLPUSH", source, destination);
		public RedisResult<long> RPush(string key, params string[] elements) => Call<long>("RPUSH", key, elements);
		public RedisResult<long> RPushX(string key, params string[] elements) => Call<long>("RPUSHX", key, elements);
	}
}
