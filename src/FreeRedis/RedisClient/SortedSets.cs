using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
#if isasync
        #region async (copy from sync)
        async public Task<ZMember> BZPopMinAsync(string key, int timeoutSeconds) => (await BZPopAsync(false, new[] { key }, timeoutSeconds))?.value;
        public Task<KeyValue<ZMember>> BZPopMinAsync(string[] keys, int timeoutSeconds) => BZPopAsync(false, keys, timeoutSeconds);
        async public Task<ZMember> BZPopMaxAsync(string key, int timeoutSeconds) => (await BZPopAsync(true, new[] { key }, timeoutSeconds))?.value;
        public Task<KeyValue<ZMember>> BZPopMaxAsync(string[] keys, int timeoutSeconds) => BZPopAsync(true, keys, timeoutSeconds);
        Task<KeyValue<ZMember>> BZPopAsync(bool ismax, string[] keys, int timeoutSeconds) => CallAsync((ismax ? "BZPOPMAX" : "BZPOPMIN")
            .Input(keys)
            .InputRaw(timeoutSeconds)
            .FlagKey(keys), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? null : new KeyValue<ZMember>(a[0].ConvertTo<string>(), new ZMember(a[1].ConvertTo<string>(), a[2].ConvertTo<decimal>().ConvertTo<decimal>()))));

        public Task<long> ZAddAsync(string key, decimal score, string member, params object[] scoreMembers) => ZAddAsync<long>(key, false, false, null, false, false, score, member, scoreMembers);
        public Task<long> ZAddAsync(string key, ZMember[] scoreMembers, ZAddThan? than = null, bool ch = false) => ZAddAsync<long>(key, false, false, than, ch, false, scoreMembers);

        public Task<long> ZAddNxAsync(string key, decimal score, string member, params object[] scoreMembers) => ZAddAsync<long>(key, true, false, null, false, false, score, member, scoreMembers);
        public Task<long> ZAddNxAsync(string key, ZMember[] scoreMembers, ZAddThan? than = null, bool ch = false) => ZAddAsync<long>(key, true, false, than, ch, false, scoreMembers);

        public Task<long> ZAddXxAsync(string key, decimal score, string member, params object[] scoreMembers) => ZAddAsync<long>(key, false, true, null, false, false, score, member, scoreMembers);
        public Task<long> ZAddXxAsync(string key, ZMember[] scoreMembers, ZAddThan? than = null, bool ch = false) => ZAddAsync<long>(key, false, true, than, ch, false, scoreMembers);
        Task<T> ZAddAsync<T>(string key, bool nx, bool xx, ZAddThan? than, bool ch, bool incr, decimal score, string member, params object[] scoreMembers)
        {
            if (scoreMembers?.Length > 0)
            {
                var members = scoreMembers.MapToList((sco, mem) => new ZMember(mem.ConvertTo<string>(), sco.ConvertTo<decimal>()));
                members.Insert(0, new ZMember(member, score));
                return ZAddAsync<T>(key, nx, xx, than, ch, incr, members);
            }
            return ZAddAsync<T>(key, nx, xx, than, ch, incr, new[] { new ZMember(member, score) });
        }
        Task<TReturn> ZAddAsync<TReturn>(string key, bool nx, bool xx, ZAddThan? than, bool ch, bool incr, IEnumerable<ZMember> scoreMembers)
        {
            var cmd = "ZADD"
                .Input(key)
                .InputIf(nx, "NX")
                .InputIf(xx, "XX")
                .InputIf(than != null, than)
                .InputIf(ch, "CH")
                .InputIf(incr, "INCR");
            foreach (var sm in scoreMembers) cmd.InputRaw(sm.score).InputRaw(sm.member);
            return CallAsync(cmd.FlagKey(key), rt => rt.ThrowOrValue<TReturn>());
        }

        public Task<long> ZCardAsync(string key) => CallAsync("ZCARD".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public Task<long> ZCountAsync(string key, decimal min, decimal max) => CallAsync("ZCOUNT".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public Task<long> ZCountAsync(string key, string min, string max) => CallAsync("ZCOUNT".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public Task<decimal> ZIncrByAsync(string key, decimal increment, string member) => CallAsync("ZINCRBY".Input(key, increment, member).FlagKey(key), rt => rt.ThrowOrValue<decimal>());
        public Task<long> ZInterStoreAsync(string destination, string[] keys, int[] weights = null, ZAggregate? aggregate = null) => ZStoreAsync(true, destination, keys, weights, aggregate);
        public Task<long> ZLexCountAsync(string key, string min, string max) => CallAsync("ZLEXCOUNT".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public Task<ZMember> ZPopMinAsync(string key) => ZPopAsync(false, key);
        public Task<ZMember[]> ZPopMinAsync(string key, int count) => ZPopAsync(false, key, count);
        public Task<ZMember> ZPopMaxAsync(string key) => ZPopAsync(true, key);
        public Task<ZMember[]> ZPopMaxAsync(string key, int count) => ZPopAsync(true, key, count);
        Task<ZMember> ZPopAsync(bool ismax, string key) => CallAsync((ismax ? "ZPOPMAX" : "ZPOPMIN").Input(key).FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? null : new ZMember(a[0].ConvertTo<string>(), a[1].ConvertTo<decimal>())));
        Task<ZMember[]> ZPopAsync(bool ismax, string key, int count) => CallAsync((ismax ? "ZPOPMAX" : "ZPOPMIN").Input(key, count).FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new ZMember[0] : a.MapToHash<decimal>(rt.Encoding).Select(b => new ZMember(b.Key, b.Value)).ToArray()));

        public Task<string[]> ZRangeAsync(string key, decimal start, decimal stop) => CallAsync("ZRANGE".Input(key, start, stop).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<ZMember[]> ZRangeWithScoresAsync(string key, decimal start, decimal stop) => CallAsync("ZRANGE"
            .Input(key, start, stop)
            .Input("WITHSCORES")
            .FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new ZMember[0] : a.MapToHash<decimal>(rt.Encoding).Select(b => new ZMember(b.Key, b.Value)).ToArray()));

        public Task<string[]> ZRangeByLexAsync(string key, string min, string max, int offset = 0, int count = 0) => CallAsync("ZRANGEBYLEX"
            .Input(key, min, max)
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<string[]> ZRangeByScoreAsync(string key, decimal min, decimal max, int offset = 0, int count = 0) => CallAsync("ZRANGEBYSCORE"
            .Input(key, min, max)
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<string[]> ZRangeByScoreAsync(string key, string min, string max, int offset = 0, int count = 0) => CallAsync("ZRANGEBYSCORE"
            .Input(key, min, max)
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<ZMember[]> ZRangeByScoreWithScoresAsync(string key, decimal min, decimal max, int offset = 0, int count = 0) => CallAsync("ZRANGEBYSCORE"
            .Input(key, min, max)
            .Input("WITHSCORES")
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new ZMember[0] : a.MapToHash<decimal>(rt.Encoding).Select(b => new ZMember(b.Key, b.Value)).ToArray()));
        public Task<ZMember[]> ZRangeByScoreWithScoresAsync(string key, string min, string max, int offset = 0, int count = 0) => CallAsync("ZRANGEBYSCORE"
            .Input(key, min, max)
            .Input("WITHSCORES")
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new ZMember[0] : a.MapToHash<decimal>(rt.Encoding).Select(b => new ZMember(b.Key, b.Value)).ToArray()));

        public Task<long> ZRankAsync(string key, string member) => CallAsync("ZRANK".Input(key, member).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public Task<long> ZRemAsync(string key, params string[] members) => CallAsync("ZREM".Input(key).Input(members).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public Task<long> ZRemRangeByLexAsync(string key, string min, string max) => CallAsync("ZREMRANGEBYLEX".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public Task<long> ZRemRangeByRankAsync(string key, long start, long stop) => CallAsync("ZREMRANGEBYRANK".Input(key, start, stop).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public Task<long> ZRemRangeByScoreAsync(string key, decimal min, decimal max) => CallAsync("ZREMRANGEBYSCORE".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public Task<long> ZRemRangeByScoreAsync(string key, string min, string max) => CallAsync("ZREMRANGEBYSCORE".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public Task<string[]> ZRevRangeAsync(string key, decimal start, decimal stop) => CallAsync("ZREVRANGE".Input(key, start, stop).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<ZMember[]> ZRevRangeWithScoresAsync(string key, decimal start, decimal stop) => CallAsync("ZREVRANGE"
            .Input(key, start, stop)
            .Input("WITHSCORES")
            .FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new ZMember[0] : a.MapToHash<decimal>(rt.Encoding).Select(b => new ZMember(b.Key, b.Value)).ToArray()));
        public Task<string[]> ZRevRangeByLexAsync(string key, decimal max, decimal min, int offset = 0, int count = 0) => CallAsync("ZREVRANGEBYLEX"
            .Input(key, max, min)
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<string[]> ZRevRangeByLexAsync(string key, string max, string min, int offset = 0, int count = 0) => CallAsync("ZREVRANGEBYLEX"
            .Input(key, max, min)
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<string[]> ZRevRangeByScoreAsync(string key, decimal max, decimal min, int offset = 0, int count = 0) => CallAsync("ZREVRANGEBYSCORE"
            .Input(key, max, min)
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<string[]> ZRevRangeByScoreAsync(string key, string max, string min, int offset = 0, int count = 0) => CallAsync("ZREVRANGEBYSCORE"
            .Input(key, max, min)
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public Task<ZMember[]> ZRevRangeByScoreWithScoresAsync(string key, decimal max, decimal min, int offset = 0, int count = 0) => CallAsync("ZREVRANGEBYSCORE"
            .Input(key, max, min)
            .Input("WITHSCORES")
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new ZMember[0] : a.MapToHash<decimal>(rt.Encoding).Select(b => new ZMember(b.Key, b.Value)).ToArray()));
        public Task<ZMember[]> ZRevRangeByScoreWithScoresAsync(string key, string max, string min, int offset = 0, int count = 0) => CallAsync("ZREVRANGEBYSCORE"
            .Input(key, max, min)
            .Input("WITHSCORES")
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new ZMember[0] : a.MapToHash<decimal>(rt.Encoding).Select(b => new ZMember(b.Key, b.Value)).ToArray()));
        public Task<long> ZRevRankAsync(string key, string member) => CallAsync("ZREVRANK".Input(key, member).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public Task<decimal> ZScoreAsync(string key, string member) => CallAsync("ZSCORE".Input(key, member).FlagKey(key), rt => rt.ThrowOrValue<decimal>());
        public Task<long> ZUnionStoreAsync(string destination, string[] keys, int[] weights = null, ZAggregate? aggregate = null) => ZStoreAsync(false, destination, keys, weights, aggregate);
        Task<long> ZStoreAsync(bool inter, string destination, string[] keys, int[] weights, ZAggregate? aggregate) => CallAsync((inter ? "ZINTERSTORE" : "ZUNIONSTORE")
            .Input(destination, keys?.Length ?? 0)
            .InputIf(keys?.Any() == true, keys)
            .InputIf(weights?.Any() == true, weights)
            .InputIf(aggregate != null, "AGGREGATE", aggregate ?? ZAggregate.max)
            .FlagKey(destination).FlagKey(keys), rt => rt
            .ThrowOrValue<long>());
        #endregion
#endif

        public ZMember BZPopMin(string key, int timeoutSeconds) => BZPop(false, new[] { key }, timeoutSeconds)?.value;
        public KeyValue<ZMember> BZPopMin(string[] keys, int timeoutSeconds) => BZPop(false, keys, timeoutSeconds);
        public ZMember BZPopMax(string key, int timeoutSeconds) => BZPop(true, new[] { key }, timeoutSeconds)?.value;
        public KeyValue<ZMember> BZPopMax(string[] keys, int timeoutSeconds) => BZPop(true, keys, timeoutSeconds);
        KeyValue<ZMember> BZPop(bool ismax, string[] keys, int timeoutSeconds) => Call((ismax ? "BZPOPMAX" : "BZPOPMIN")
            .Input(keys)
            .InputRaw(timeoutSeconds)
            .FlagKey(keys), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? null : new KeyValue<ZMember>(a[0].ConvertTo<string>(), new ZMember(a[1].ConvertTo<string>(), a[2].ConvertTo<decimal>().ConvertTo<decimal>()))));

        public long ZAdd(string key, decimal score, string member, params object[] scoreMembers) => ZAdd<long>(key, false, false, null, false, false, score, member, scoreMembers);
        public long ZAdd(string key, ZMember[] scoreMembers, ZAddThan? than = null, bool ch = false) => ZAdd<long>(key, false, false, than, ch, false, scoreMembers);

        public long ZAddNx(string key, decimal score, string member, params object[] scoreMembers) => ZAdd<long>(key, true, false, null, false, false, score, member, scoreMembers);
        public long ZAddNx(string key, ZMember[] scoreMembers, ZAddThan? than = null, bool ch = false) => ZAdd<long>(key, true, false, than, ch, false, scoreMembers);

        public long ZAddXx(string key, decimal score, string member, params object[] scoreMembers) => ZAdd<long>(key, false, true, null, false, false, score, member, scoreMembers);
        public long ZAddXx(string key, ZMember[] scoreMembers, ZAddThan? than = null, bool ch = false) => ZAdd<long>(key, false, true, than, ch, false, scoreMembers);
        T ZAdd<T>(string key, bool nx, bool xx, ZAddThan? than, bool ch, bool incr, decimal score, string member, params object[] scoreMembers)
        {
            if (scoreMembers?.Length > 0)
            {
                var members = scoreMembers.MapToList((sco, mem) => new ZMember(mem.ConvertTo<string>(), sco.ConvertTo<decimal>()));
                members.Insert(0, new ZMember(member, score));
                return ZAdd<T>(key, nx, xx, than, ch, incr, members);
            }
            return ZAdd<T>(key, nx, xx, than, ch, incr, new[] { new ZMember(member, score) });
        }
        TReturn ZAdd<TReturn>(string key, bool nx, bool xx, ZAddThan? than, bool ch, bool incr, IEnumerable<ZMember> scoreMembers)
        {
            var cmd = "ZADD"
                .Input(key)
                .InputIf(nx, "NX")
                .InputIf(xx, "XX")
                .InputIf(than != null, than)
                .InputIf(ch, "CH")
                .InputIf(incr, "INCR");
            foreach (var sm in scoreMembers) cmd.InputRaw(sm.score).InputRaw(sm.member);
            return Call(cmd.FlagKey(key), rt => rt.ThrowOrValue<TReturn>());
        }

        public long ZCard(string key) => Call("ZCARD".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public long ZCount(string key, decimal min, decimal max) => Call("ZCOUNT".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public long ZCount(string key, string min, string max) => Call("ZCOUNT".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public decimal ZIncrBy(string key, decimal increment, string member) => Call("ZINCRBY".Input(key, increment, member).FlagKey(key), rt => rt.ThrowOrValue<decimal>());
        public long ZInterStore(string destination, string[] keys, int[] weights = null, ZAggregate? aggregate = null) => ZStore(true, destination, keys, weights, aggregate);
        public long ZLexCount(string key, string min, string max) => Call("ZLEXCOUNT".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public ZMember ZPopMin(string key) => ZPop(false, key);
        public ZMember[] ZPopMin(string key, int count) => ZPop(false, key, count);
        public ZMember ZPopMax(string key) => ZPop(true, key);
        public ZMember[] ZPopMax(string key, int count) => ZPop(true, key, count);
        ZMember ZPop(bool ismax, string key) => Call((ismax ? "ZPOPMAX" : "ZPOPMIN").Input(key).FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? null : new ZMember(a[0].ConvertTo<string>(), a[1].ConvertTo<decimal>())));
        ZMember[] ZPop(bool ismax, string key, int count) => Call((ismax ? "ZPOPMAX" : "ZPOPMIN").Input(key, count).FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new ZMember[0] : a.MapToHash<decimal>(rt.Encoding).Select(b => new ZMember(b.Key, b.Value)).ToArray()));

        public string[] ZRange(string key, decimal start, decimal stop) => Call("ZRANGE".Input(key, start, stop).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public ZMember[] ZRangeWithScores(string key, decimal start, decimal stop) => Call("ZRANGE"
            .Input(key, start, stop)
            .Input("WITHSCORES")
            .FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new ZMember[0] : a.MapToHash<decimal>(rt.Encoding).Select(b => new ZMember(b.Key, b.Value)).ToArray()));

        public string[] ZRangeByLex(string key, string min, string max, int offset = 0, int count = 0) => Call("ZRANGEBYLEX"
            .Input(key, min, max)
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public string[] ZRangeByScore(string key, decimal min, decimal max, int offset = 0, int count = 0) => Call("ZRANGEBYSCORE"
            .Input(key, min, max)
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public string[] ZRangeByScore(string key, string min, string max, int offset = 0, int count = 0) => Call("ZRANGEBYSCORE"
            .Input(key, min, max)
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public ZMember[] ZRangeByScoreWithScores(string key, decimal min, decimal max, int offset = 0, int count = 0) => Call("ZRANGEBYSCORE"
            .Input(key, min, max)
            .Input("WITHSCORES")
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new ZMember[0] : a.MapToHash<decimal>(rt.Encoding).Select(b => new ZMember(b.Key, b.Value)).ToArray()));
        public ZMember[] ZRangeByScoreWithScores(string key, string min, string max, int offset = 0, int count = 0) => Call("ZRANGEBYSCORE"
            .Input(key, min, max)
            .Input("WITHSCORES")
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new ZMember[0] : a.MapToHash<decimal>(rt.Encoding).Select(b => new ZMember(b.Key, b.Value)).ToArray()));

        public long ZRank(string key, string member) => Call("ZRANK".Input(key, member).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public long ZRem(string key, params string[] members) => Call("ZREM".Input(key).Input(members).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public long ZRemRangeByLex(string key, string min, string max) => Call("ZREMRANGEBYLEX".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public long ZRemRangeByRank(string key, long start, long stop) => Call("ZREMRANGEBYRANK".Input(key, start, stop).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public long ZRemRangeByScore(string key, decimal min, decimal max) => Call("ZREMRANGEBYSCORE".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public long ZRemRangeByScore(string key, string min, string max) => Call("ZREMRANGEBYSCORE".Input(key, min, max).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public string[] ZRevRange(string key, decimal start, decimal stop) => Call("ZREVRANGE".Input(key, start, stop).FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public ZMember[] ZRevRangeWithScores(string key, decimal start, decimal stop) => Call("ZREVRANGE"
            .Input(key, start, stop)
            .Input("WITHSCORES")
            .FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new ZMember[0] : a.MapToHash<decimal>(rt.Encoding).Select(b => new ZMember(b.Key, b.Value)).ToArray()));
        public string[] ZRevRangeByLex(string key, decimal max, decimal min, int offset = 0, int count = 0) => Call("ZREVRANGEBYLEX"
            .Input(key, max, min)
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public string[] ZRevRangeByLex(string key, string max, string min, int offset = 0, int count = 0) => Call("ZREVRANGEBYLEX"
            .Input(key, max, min)
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public string[] ZRevRangeByScore(string key, decimal max, decimal min, int offset = 0, int count = 0) => Call("ZREVRANGEBYSCORE"
            .Input(key, max, min)
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public string[] ZRevRangeByScore(string key, string max, string min, int offset = 0, int count = 0) => Call("ZREVRANGEBYSCORE"
            .Input(key, max, min)
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public ZMember[] ZRevRangeByScoreWithScores(string key, decimal max, decimal min, int offset = 0, int count = 0) => Call("ZREVRANGEBYSCORE"
            .Input(key, max, min)
            .Input("WITHSCORES")
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new ZMember[0] : a.MapToHash<decimal>(rt.Encoding).Select(b => new ZMember(b.Key, b.Value)).ToArray()));
        public ZMember[] ZRevRangeByScoreWithScores(string key, string max, string min, int offset = 0, int count = 0) => Call("ZREVRANGEBYSCORE"
            .Input(key, max, min)
            .Input("WITHSCORES")
            .InputIf(offset > 0 || count > 0, "LIMIT", offset, count)
            .FlagKey(key), rt => rt
            .ThrowOrValue((a, _) => a == null || a.Length == 0 ? new ZMember[0] : a.MapToHash<decimal>(rt.Encoding).Select(b => new ZMember(b.Key, b.Value)).ToArray()));
        public long ZRevRank(string key, string member) => Call("ZREVRANK".Input(key, member).FlagKey(key), rt => rt.ThrowOrValue<long>());

        //ZSCAN key cursor [MATCH pattern] [COUNT count]
        public decimal ZScore(string key, string member) => Call("ZSCORE".Input(key, member).FlagKey(key), rt => rt.ThrowOrValue<decimal>());
        public long ZUnionStore(string destination, string[] keys, int[] weights = null, ZAggregate? aggregate = null) => ZStore(false, destination, keys, weights, aggregate);
        long ZStore(bool inter, string destination, string[] keys, int[] weights, ZAggregate? aggregate) => Call((inter ? "ZINTERSTORE" : "ZUNIONSTORE")
            .Input(destination, keys?.Length ?? 0)
            .InputIf(keys?.Any() == true, keys)
            .InputIf(weights?.Any() == true, weights)
            .InputIf(aggregate != null, "AGGREGATE", aggregate ?? ZAggregate.max)
            .FlagKey(destination).FlagKey(keys), rt => rt
            .ThrowOrValue<long>());
    }

    public class ZMember
    {
        public readonly string member;
        public readonly decimal score;
        public ZMember(string member, decimal score) { this.member = member; this.score = score; }
    }
    public enum ZAggregate { sum, min, max }
}
