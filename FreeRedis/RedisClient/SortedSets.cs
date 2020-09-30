using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<SortedSetMember<string>> BZPopMax(string key, int timeoutSeconds) => BZPopMaxMin("BZPOPMAX", key, timeoutSeconds);
		public RedisResult<SortedSetMember<string>[]> BZPopMax(string[] keys, int timeoutSeconds) => BZPopMaxMin("BZPOPMAX", keys, timeoutSeconds);
		public RedisResult<SortedSetMember<string>> BZPopMin(string key, int timeoutSeconds) => BZPopMaxMin("BZPOPMIN", key, timeoutSeconds);
		public RedisResult<SortedSetMember<string>[]> BZPopMin(string[] keys, int timeoutSeconds) => BZPopMaxMin("BZPOPMIN", keys, timeoutSeconds);
		RedisResult<SortedSetMember<string>> BZPopMaxMin(string command, string key, int timeoutSeconds) => Call<string[]>(command, key, timeoutSeconds).NewValue(a => a == null ? null : new SortedSetMember<string>(a[1], a[2].ConvertTo<decimal>()));
		/// <summary>
		/// 弹出多个 keys 有序集合值，返回 [] 的下标与之对应
		/// </summary>
		/// <param name="keys"></param>
		/// <param name="timeoutSeconds"></param>
		/// <returns></returns>
		RedisResult<SortedSetMember<string>[]> BZPopMaxMin(string command, string[] keys, int timeoutSeconds)
		{
			return Call<string[]>(command, null, "".AddIf(true, keys, timeoutSeconds).ToArray())
				.NewValue(a =>
				{
					if (a == null) return null;
					var result = new SortedSetMember<string>[keys.Length];
					var oldkeys = keys.ToList();
					for (var z = 0; z < a.Length; z += 3)
					{
						var oldkeysIdx = oldkeys.FindIndex(x => x == a[z]);
						result[oldkeysIdx] = new SortedSetMember<string>(a[z + 1], a[z + 2].ConvertTo<decimal>());
						oldkeys[oldkeysIdx] = null;
					}
					return result;
				});
		}
		public RedisResult<long> ZAdd(string key, decimal score, string member) => ZAdd<long>(key, new[] { new SortedSetMember<string>(member, score) }, false, false, false, false);
		public RedisResult<long> ZAdd(string key, SortedSetMember<string>[] memberScores) => ZAdd<long>(key, memberScores, false, false, false, false);
		public RedisResult<long> ZAdd(string key, SortedSetMember<string>[] memberScores, bool nx, bool xx, bool ch) => ZAdd<long>(key, memberScores, nx, xx, ch, false);
		public RedisResult<string[]> ZAddIncr(string key, SortedSetMember<string>[] memberScores, bool nx, bool xx, bool ch) => ZAdd<string[]>(key, memberScores, nx, xx, ch, true);
		RedisResult<TReturn> ZAdd<TReturn>(string key, SortedSetMember<string>[] memberScores, bool nx, bool xx, bool ch, bool incr) => Call<TReturn>("ZADD", key, ""
			.AddIf(nx, "NX")
			.AddIf(xx, "XX")
			.AddIf(ch, "CH")
			.AddIf(incr, "INCR")
			.AddIf(true, memberScores.Select(a => new object[] { a.Score, a.Member }).SelectMany(a => a).ToArray())
			.ToArray());
		public RedisResult<long> ZCard(string key) => Call<long>("ZCARD", key);
		public RedisResult<long> ZCount(string key, decimal min, decimal max) => Call<long>("ZCOUNT", key, min, max);
		public RedisResult<long> ZCount(string key, string min, string max) => Call<long>("ZCOUNT", key, min, max);
		public RedisResult<decimal> ZIncrBy(string key, decimal increment, string member) => Call<decimal>("ZINCRBY", key, increment, member);
		//ZINTERSTORE destination numkeys key [key ...] [WEIGHTS weight [weight ...]] [AGGREGATE SUM|MIN|MAX]
		public RedisResult<long> ZLexCount(string key, string min, string max) => Call<long>("ZLEXCOUNT", key, min, max);
		public RedisResult<SortedSetMember<string>> ZPopMin(string key) => ZPopMaxMin("ZPOPMIN", key);
		public RedisResult<SortedSetMember<string>[]> ZPopMin(string key, int count) => ZPopMaxMin("ZPOPMIN", key, count);
		public RedisResult<SortedSetMember<string>> ZPopMax(string key) => ZPopMaxMin("ZPOPMAX", key);
		public RedisResult<SortedSetMember<string>[]> ZPopMax(string key, int count) => ZPopMaxMin("ZPOPMAX", key, count);
		RedisResult<SortedSetMember<string>> ZPopMaxMin(string command, string key) => Call<string[]>(command, key).NewValue(a => a == null ? null : new SortedSetMember<string>(a[1], a[2].ConvertTo<decimal>()));
		RedisResult<SortedSetMember<string>[]> ZPopMaxMin(string command, string key, int count) => Call<string[]>(command, key, count).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<string[]> ZRange(string key, decimal start, decimal stop) => Call<string[]>("ZRANGE", key, start, stop);
		public RedisResult<SortedSetMember<string>[]> ZRangeWithScores(string key, decimal start, decimal stop) => Call<string[]>("ZRANGE", key, start, stop, "WITHSCORES").NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<string[]> ZRangeByLex(string key, decimal min, decimal max, int offset = 0, int count = 0) => offset > 0 || count > 0 ? Call<string[]>("ZRANGEBYLEX", key, min, max, "LIMIT", offset, count) : Call<string[]>("ZRANGEBYLEX", key, min, max);
		public RedisResult<string[]> ZRangeByLex(string key, string min, string max, int offset = 0, int count = 0) => offset > 0 || count > 0 ? Call<string[]>("ZRANGEBYLEX", key, min, max, "LIMIT", offset, count) : Call<string[]>("ZRANGEBYLEX", key, min, max);
		public RedisResult<string[]> ZRangeByScore(string key, decimal min, decimal max, int offset = 0, int count = 0) => offset > 0 || count > 0 ? Call<string[]>("ZRANGEBYSCORE", key, min, max, "LIMIT", offset, count) : Call<string[]>("ZRANGEBYSCORE", key, min, max);
		public RedisResult<string[]> ZRangeByScore(string key, string min, string max, int offset = 0, int count = 0) => offset > 0 || count > 0 ? Call<string[]>("ZRANGEBYSCORE", key, min, max, "LIMIT", offset, count) : Call<string[]>("ZRANGEBYSCORE", key, min, max);
		public RedisResult<SortedSetMember<string>[]> ZRangeByScoreWithScores(string key, decimal min, decimal max, int offset = 0, int count = 0) => (offset > 0 || count > 0 ? Call<string[]>("ZRANGEBYSCORE", key, min, max, "LIMIT", offset, count) : Call<string[]>("ZRANGEBYSCORE", key, min, max)).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<SortedSetMember<string>[]> ZRangeByScoreWithScores(string key, string min, string max, int offset = 0, int count = 0) => (offset > 0 || count > 0 ? Call<string[]>("ZRANGEBYSCORE", key, min, max, "LIMIT", offset, count) : Call<string[]>("ZRANGEBYSCORE", key, min, max)).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<long> ZRank(string key, string member) => Call<long>("ZRANK", key, member);
		public RedisResult<long> ZRem(string key, params string[] members) => Call<long>("ZREM", key, members);
		public RedisResult<long> ZRemRangeByLex(string key, string min, string max) => Call<long>("ZREMRANGEBYLEX", key, min, max);
		public RedisResult<long> ZRemRangeByRank(string key, long start, long stop) => Call<long>("ZREMRANGEBYRANK", key, start, stop);
		public RedisResult<long> ZRemRangeByScore(string key, decimal min, decimal max) => Call<long>("ZREMRANGEBYSCORE", key, min, max);
		public RedisResult<long> ZRemRangeByScore(string key, string min, string max) => Call<long>("ZREMRANGEBYSCORE", key, min, max);
		public RedisResult<string[]> ZRevRange(string key, decimal start, decimal stop) => Call<string[]>("ZREVRANGE", key, start, stop);
		public RedisResult<SortedSetMember<string>[]> ZRevRangeWithScores(string key, decimal start, decimal stop) => Call<string[]>("ZREVRANGE", key, start, stop, "WITHSCORES").NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<string[]> ZRevRangeByLex(string key, decimal max, decimal min, int offset = 0, int count = 0) => offset > 0 || count > 0 ? Call<string[]>("ZREVRANGEBYLEX", key, max, min, "LIMIT", offset, count) : Call<string[]>("ZREVRANGEBYLEX", key, max, min);
		public RedisResult<string[]> ZRevRangeByLex(string key, string max, string min, int offset = 0, int count = 0) => offset > 0 || count > 0 ? Call<string[]>("ZREVRANGEBYLEX", key, max, min, "LIMIT", offset, count) : Call<string[]>("ZREVRANGEBYLEX", key, max, min);
		public RedisResult<string[]> ZRevRangeByScore(string key, decimal max, decimal min, int offset = 0, int count = 0) => offset > 0 || count > 0 ? Call<string[]>("ZREVRANGEBYSCORE", key, max, min, "LIMIT", offset, count) : Call<string[]>("ZREVRANGEBYSCORE", key, max, min);
		public RedisResult<string[]> ZRevRangeByScore(string key, string max, string min, int offset = 0, int count = 0) => offset > 0 || count > 0 ? Call<string[]>("ZREVRANGEBYSCORE", key, max, min, "LIMIT", offset, count) : Call<string[]>("ZREVRANGEBYSCORE", key, max, min);
		public RedisResult<SortedSetMember<string>[]> ZRevRangeByScoreWithScores(string key, decimal max, decimal min, int offset = 0, int count = 0) => (offset > 0 || count > 0 ? Call<string[]>("ZREVRANGEBYSCORE", key, max, min, "LIMIT", offset, count) : Call<string[]>("ZREVRANGEBYSCORE", key, max, min)).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<SortedSetMember<string>[]> ZRevRangeByScoreWithScores(string key, string max, string min, int offset = 0, int count = 0) => (offset > 0 || count > 0 ? Call<string[]>("ZREVRANGEBYSCORE", key, max, min, "LIMIT", offset, count) : Call<string[]>("ZREVRANGEBYSCORE", key, max, min)).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray());
		public RedisResult<long> ZRevRank(string key, string member) => Call<long>("ZREVRANK", key, member);
		//ZSCAN key cursor [MATCH pattern] [COUNT count]
		public RedisResult<decimal> ZScore(string key, string member) => Call<decimal>("ZSCORE", key, member);
		//ZUNIONSTORE destination numkeys key [key ...] [WEIGHTS weight [weight ...]] [AGGREGATE SUM|MIN|MAX]
    }
}
