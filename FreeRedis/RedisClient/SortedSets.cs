using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public SortedSetMember<string> BZPopMin(string key, int timeoutSeconds) => BZPopMaxMin(false, key, timeoutSeconds);
		public SortedSetMember<string>[] BZPopMin(string[] keys, int timeoutSeconds) => BZPopMaxMin(false, keys, timeoutSeconds);
		public SortedSetMember<string> BZPopMax(string key, int timeoutSeconds) => BZPopMaxMin(true, key, timeoutSeconds);
		public SortedSetMember<string>[] BZPopMax(string[] keys, int timeoutSeconds) => BZPopMaxMin(true, keys, timeoutSeconds);
		SortedSetMember<string> BZPopMaxMin(bool ismax, string key, int timeoutSeconds) => Call<string[], SortedSetMember<string>>((ismax ? "BZPOPMAX" : "BZPOPMIN")
			.Input(key, timeoutSeconds)
			.FlagKey(key), rt => rt
			.NewValue(a => a == null ? null : new SortedSetMember<string>(a[1], a[2].ConvertTo<decimal>())).ThrowOrValue());
		/// <summary>
		/// 弹出多个 keys 有序集合值，返回 [] 的下标与之对应
		/// </summary>
		/// <param name="keys"></param>
		/// <param name="timeoutSeconds"></param>
		/// <returns></returns>
		SortedSetMember<string>[] BZPopMaxMin(bool ismax, string[] keys, int timeoutSeconds)
		{
			return Call<string[], SortedSetMember<string>[]>((ismax ? "BZPOPMAX" : "BZPOPMIN").SubCommand(null)
				.Input(keys)
				.InputRaw(timeoutSeconds)
				.FlagKey(keys), rt => rt
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
				}).ThrowOrValue());
		}
		public long ZAdd(string key, decimal score, string member) => ZAdd<long>(key, new[] { new SortedSetMember<string>(member, score) }, false, false, false, false);
		public long ZAdd(string key, SortedSetMember<string>[] memberScores) => ZAdd<long>(key, memberScores, false, false, false, false);
		public long ZAdd(string key, SortedSetMember<string>[] memberScores, bool nx, bool xx, bool ch) => ZAdd<long>(key, memberScores, nx, xx, ch, false);
		public string[] ZAddIncr(string key, SortedSetMember<string>[] memberScores, bool nx, bool xx, bool ch) => ZAdd<string[]>(key, memberScores, nx, xx, ch, true);
		TReturn ZAdd<TReturn>(string key, SortedSetMember<string>[] memberScores, bool nx, bool xx, bool ch, bool incr) => Call<TReturn>("ZADD"
			.Input(key)
			.InputIf(nx, "NX")
			.InputIf(xx, "XX")
			.InputIf(ch, "CH")
			.InputIf(incr, "INCR")
			.InputIf(true, memberScores.Select(a => new object[] { a.Score, a.Member }).SelectMany(a => a).ToArray())
			.FlagKey(key), rt => rt.ThrowOrValue());

		public long ZCard(string key) => Call<long>("ZCARD".Input(key).FlagKey(key), rt => rt.ThrowOrValue());
		public long ZCount(string key, decimal min, decimal max) => Call<long>("ZCOUNT".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue());
		public long ZCount(string key, string min, string max) => Call<long>("ZCOUNT".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue());
		public decimal ZIncrBy(string key, decimal increment, string member) => Call<decimal>("ZINCRBY".Input(key, increment, member).FlagKey(key), rt => rt.ThrowOrValue());
		//ZINTERSTORE destination numkeys key [key ...] [WEIGHTS weight [weight ...]] [AGGREGATE SUM|MIN|MAX]
		public long ZLexCount(string key, string min, string max) => Call<long>("ZLEXCOUNT".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue());

		public SortedSetMember<string> ZPopMin(string key) => ZPopMaxMin(false, key);
		public SortedSetMember<string>[] ZPopMin(string key, int count) => ZPopMaxMin(false, key, count);
		public SortedSetMember<string> ZPopMax(string key) => ZPopMaxMin(true, key);
		public SortedSetMember<string>[] ZPopMax(string key, int count) => ZPopMaxMin(true, key, count);
		SortedSetMember<string> ZPopMaxMin(bool ismax, string key) => Call<string[], SortedSetMember<string>>((ismax ? "ZPOPMAX" : "ZPOPMIN").Input(key).FlagKey(key), rt => rt
			.NewValue(a => a == null ? null : new SortedSetMember<string>(a[1], a[2].ConvertTo<decimal>())).ThrowOrValue());
		SortedSetMember<string>[] ZPopMaxMin(bool ismax, string key, int count) => Call<string[], SortedSetMember<string>[]>((ismax ? "ZPOPMAX" : "ZPOPMIN").Input(key, count).FlagKey(key), rt => rt
			.NewValue(a => a == null ? null : a.MapToHash<decimal>(rt.Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray()).ThrowOrValue());

		public string[] ZRange(string key, decimal start, decimal stop) => Call<string[]>("ZRANGE".Input(key, start, stop).FlagKey(key), rt => rt.ThrowOrValue());
		public SortedSetMember<string>[] ZRangeWithScores(string key, decimal start, decimal stop) => Call<string[], SortedSetMember<string>[]>("ZRANGE"
			.Input(key, start, stop)
			.Input("WITHSCORES")
			.FlagKey(key), rt => rt
			.NewValue(a => a == null ? null : a.MapToHash<decimal>(rt.Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray()).ThrowOrValue());
		public string[] ZRangeByLex(string key, decimal min, decimal max, int offset = 0, int count = 0) => Call<string[]>("ZRANGEBYLEX"
			.Input(key, min, max)
			.InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
			.FlagKey(key), rt => rt.ThrowOrValue());
		public string[] ZRangeByLex(string key, string min, string max, int offset = 0, int count = 0) => Call<string[]>("ZRANGEBYLEX"
			.Input(key, min, max)
			.InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
			.FlagKey(key), rt => rt.ThrowOrValue());
		public string[] ZRangeByScore(string key, decimal min, decimal max, int offset = 0, int count = 0) => Call<string[]>("ZRANGEBYSCORE"
			.Input(key, min, max)
			.InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
			.FlagKey(key), rt => rt.ThrowOrValue());
		public string[] ZRangeByScore(string key, string min, string max, int offset = 0, int count = 0) => Call<string[]>("ZRANGEBYSCORE"
			.Input(key, min, max)
			.InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
			.FlagKey(key), rt => rt.ThrowOrValue());
		public SortedSetMember<string>[] ZRangeByScoreWithScores(string key, decimal min, decimal max, int offset = 0, int count = 0) => Call<string[], SortedSetMember<string>[]>("ZRANGEBYSCORE"
			.Input(key, min, max)
			.InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
			.FlagKey(key), rt => rt
			.NewValue(a => a == null ? null : a.MapToHash<decimal>(rt.Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray()).ThrowOrValue());
		public SortedSetMember<string>[] ZRangeByScoreWithScores(string key, string min, string max, int offset = 0, int count = 0) => Call<string[], SortedSetMember<string>[]>("ZRANGEBYSCORE"
			.Input(key, min, max)
			.InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
			.FlagKey(key), rt => rt
			.NewValue(a => a == null ? null : a.MapToHash<decimal>(rt.Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray()).ThrowOrValue());

		public long ZRank(string key, string member) => Call<long>("ZRANK".Input(key, member).FlagKey(key), rt => rt.ThrowOrValue());
		public long ZRem(string key, params string[] members) => Call<long>("ZREM".Input(key).Input(members).FlagKey(key), rt => rt.ThrowOrValue());
		public long ZRemRangeByLex(string key, string min, string max) => Call<long>("ZREMRANGEBYLEX".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue());
		public long ZRemRangeByRank(string key, long start, long stop) => Call<long>("ZREMRANGEBYRANK".Input(key, start, stop).FlagKey(key), rt => rt.ThrowOrValue());
		public long ZRemRangeByScore(string key, decimal min, decimal max) => Call<long>("ZREMRANGEBYSCORE".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue());
		public long ZRemRangeByScore(string key, string min, string max) => Call<long>("ZREMRANGEBYSCORE".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue());
		public string[] ZRevRange(string key, decimal start, decimal stop) => Call<string[]>("ZREVRANGE".Input(key, start, stop).FlagKey(key), rt => rt.ThrowOrValue());
		public SortedSetMember<string>[] ZRevRangeWithScores(string key, decimal start, decimal stop) => Call<string[], SortedSetMember<string>[]>("ZREVRANGE"
			.Input(key, start, stop)
			.Input("WITHSCORES")
			.FlagKey(key), rt => rt
			.NewValue(a => a == null ? null : a.MapToHash<decimal>(rt.Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray()).ThrowOrValue());
		public string[] ZRevRangeByLex(string key, decimal max, decimal min, int offset = 0, int count = 0) => Call<string[]>("ZREVRANGEBYLEX"
			.Input(key, max, min)
			.InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
			.FlagKey(key), rt => rt.ThrowOrValue());
		public string[] ZRevRangeByLex(string key, string max, string min, int offset = 0, int count = 0) => Call<string[]>("ZREVRANGEBYLEX"
			.Input(key, max, min)
			.InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
			.FlagKey(key), rt => rt.ThrowOrValue());
		public string[] ZRevRangeByScore(string key, decimal max, decimal min, int offset = 0, int count = 0) => Call<string[]>("ZREVRANGEBYSCORE"
			.Input(key, max, min)
			.InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
			.FlagKey(key), rt => rt.ThrowOrValue());
		public string[] ZRevRangeByScore(string key, string max, string min, int offset = 0, int count = 0) => Call<string[]>("ZREVRANGEBYSCORE"
			.Input(key, max, min)
			.InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
			.FlagKey(key), rt => rt.ThrowOrValue());
		public SortedSetMember<string>[] ZRevRangeByScoreWithScores(string key, decimal max, decimal min, int offset = 0, int count = 0) => Call<string[], SortedSetMember<string>[]>("ZREVRANGEBYSCORE"
			.Input(key, max, min)
			.InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
			.FlagKey(key), rt => rt
			.NewValue(a => a == null ? null : a.MapToHash<decimal>(rt.Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray()).ThrowOrValue());
		public SortedSetMember<string>[] ZRevRangeByScoreWithScores(string key, string max, string min, int offset = 0, int count = 0) => Call<string[], SortedSetMember<string>[]>("ZREVRANGEBYSCORE"
			.Input(key, max, min)
			.InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
			.FlagKey(key), rt => rt
			.NewValue(a => a == null ? null : a.MapToHash<decimal>(rt.Encoding).Select(b => new SortedSetMember<string>(b.Key, b.Value)).ToArray()).ThrowOrValue());
		public long ZRevRank(string key, string member) => Call<long>("ZREVRANK".Input(key, member).FlagKey(key), rt => rt.ThrowOrValue());
		//ZSCAN key cursor [MATCH pattern] [COUNT count]
		public decimal ZScore(string key, string member) => Call<decimal>("ZSCORE".Input(key, member).FlagKey(key), rt => rt.ThrowOrValue());
		//ZUNIONSTORE destination numkeys key [key ...] [WEIGHTS weight [weight ...]] [AGGREGATE SUM|MIN|MAX]
	}
}
