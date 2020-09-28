using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
    {
        public RedisResult<T> SendCommand<T>(string cmd, string subcmd = null, params object[] parms)
        {
            var args = PrepareCommand(cmd, subcmd, parms);
            Resp3Helper.Write(Stream, Encoding, args, true);
            var result = Resp3Helper.Read<T>(Stream, Encoding);
            return result;
		}
		public void SendCommandListen(Action<string> ondata, Func<bool> next, string command, string subcommand = null, params object[] parms)
		{
			var args = PrepareCommand(command, subcommand, parms);
			Resp3Helper.Write(Stream, args, true);
			_listeningCommand = string.Join(" ", args);
			do
			{
				try
				{
					var data = Resp3Helper.Read<string>(Stream).Value;
					ondata?.Invoke(data);
				}
				catch (IOException ex)
				{
					Console.WriteLine(ex.Message);
					if (IsConnected) throw;
					break;
				}
			} while (next());
			_listeningCommand = null;
		}

        #region Commands Server
        public RedisResult<string[]> AclCat(string categoryname = null) => string.IsNullOrWhiteSpace(categoryname) ? SendCommand<string[]>("ACL", "CAT") : SendCommand<string[]>("ACL", "CAT", categoryname);
		public RedisResult<int> AclDelUser(params string[] username) => username?.Any() == true ? SendCommand<int>("ACL", "DELUSER", username) : throw new ArgumentException(nameof(username));
		public RedisResult<string> AclGenPass(int bits = 0) => bits <= 0 ? SendCommand<string>("ACL", "GENPASS") : SendCommand<string>("ACL", "GENPASS", bits);
		public RedisResult<object> AclGetUser(string username = "default") => SendCommand<object>("ACL", "GETUSER", username);
		public RedisResult<object> AclHelp() => SendCommand<object>("ACL", "HELP");
		public RedisResult<string[]> AclList() => SendCommand<string[]>("ACL", "LIST");
		public RedisResult<string> AclLoad() => SendCommand<string>("ACL", "LOAD");
		public RedisResult<LogInfo[]> AclLog(long count = 0) => (count <= 0 ? SendCommand<object[][]>("ACL", "LOG") : SendCommand<object[][]>("ACL", "LOG", count)).NewValue(x => x.Select(a => a.MapToClass<LogInfo>(Encoding)).ToArray());
		public class LogInfo { public long Count { get; } public string Reason { get; } public string Context { get; } public string Object { get; } public string Username { get; } public decimal AgeSeconds { get; } public string ClientInfo { get; } }
		public RedisResult<string> AclSave() => SendCommand<string>("ACL", "SAVE");
		public RedisResult<string> AclSetUser(params string[] rule) => rule?.Any() == true ? SendCommand<string>("ACL", "SETUSER", rule) : throw new ArgumentException(nameof(rule));
		public RedisResult<string[]> AclUsers() => SendCommand<string[]>("ACL", "USERS");
		public RedisResult<string> AclWhoami() => SendCommand<string>("ACL", "WHOAMI");
		public RedisResult<string> BgRewriteAof() => SendCommand<string>("BGREWRITEAOF");
		public RedisResult<string> BgSave(string schedule = null) => SendCommand<string>("BGSAVE", schedule);
		public RedisResult<object[]> Command() => SendCommand<object[]>("COMMAND");
		public RedisResult<int> CommandCount() => SendCommand<int>("COMMAND", "COUNT");
		public RedisResult<string[]> CommandGetKeys(params string[] command) => command?.Any() == true ? SendCommand<string[]>("COMMAND", "GETKEYS", command) : throw new ArgumentException(nameof(command));
		public RedisResult<string[]> CommandInfo(params string[] command) => command?.Any() == true ? SendCommand<string[]>("COMMAND", "INFO", command) : throw new ArgumentException(nameof(command));
		public RedisResult<Dictionary<string, string>> ConfigGet(string parameter) => SendCommand<string[]>("CONFIG", "GET", parameter).NewValue(a => a.MapToHash<string>(Encoding));
		public RedisResult<string> ConfigResetStat() => SendCommand<string>("CONFIG", "RESETSTAT");
		public RedisResult<string> ConfigRewrite() => SendCommand<string>("CONFIG", "REWRITE");
		public RedisResult<string> ConfigSet(string parameter, object value) => SendCommand<string>("CONFIG", "SET", parameter, value);
		public RedisResult<long> DbSize() => SendCommand<long>("DBSIZE");
		public RedisResult<string> DebugObject(string key) => SendCommand<string>("DEBUG", "OBJECT", key);
		public RedisResult<string> DebugSegfault() => SendCommand<string>("DEBUG", "SEGFAULT");
		public RedisResult<string> FlushAll(bool isasync = false) => SendCommand<string>("FLUSHALL", isasync ? "ASYNC" : null);
		public RedisResult<string> FlushDb(bool isasync = false) => SendCommand<string>("FLUSHDB", isasync ? "ASYNC" : null);
		public RedisResult<string> Info(string section = null) => SendCommand<string>("INFO", section);
		public RedisResult<long> LastSave() => SendCommand<long>("LASTSAVE");
		public RedisResult<string> LatencyDoctor() => SendCommand<string>("LATENCY", "DOCTOR");
		public RedisResult<string> LatencyGraph(string @event) => SendCommand<string>("LATENCY", "GRAPH", @event);
		public RedisResult<string[]> LatencyHelp() => SendCommand<string[]>("LATENCY", "HELP");
		public RedisResult<string[][]> LatencyHistory(string @event) => SendCommand<string[][]>("HISTORY", "HELP", @event);
		public RedisResult<string[][]> LatencyLatest() => SendCommand<string[][]>("HISTORY", "LATEST");
		public RedisResult<long> LatencyReset(string @event) => SendCommand<long>("LASTSAVE", "RESET", @event);
		public RedisResult<string> Lolwut(string version) => SendCommand<string>("LATENCY", string.IsNullOrWhiteSpace(version) ? null : $"VERSION {version}");
		public RedisResult<string> MemoryDoctor() => SendCommand<string>("MEMORY", "DOCTOR");
		public RedisResult<string[]> MemoryHelp() => SendCommand<string[]>("MEMORY", "HELP");
		public RedisResult<string> MemoryMallocStats() => SendCommand<string>("MEMORY", "MALLOC-STATS");
		public RedisResult<string> MemoryPurge() => SendCommand<string>("MEMORY", "PURGE");
		public RedisResult<Dictionary<string, string>> MemoryStats() => SendCommand<string[]>("MEMORY", "STATS").NewValue(a => a.MapToHash<string>(Encoding));
		public RedisResult<long> MemoryUsage(string key, long count = 0) => count <= 0 ? SendCommand<long>("MEMORY ", "USAGE", key) : SendCommand<long>("MEMORY ", "USAGE", key, "SAMPLES", count);
		public RedisResult<string[][]> ModuleList() => SendCommand<string[][]>("MODULE", "LIST");
		public RedisResult<string> ModuleLoad(string path, params string[] args) => SendCommand<string>("MODULE", "LOAD", path.AddIf(args?.Any() == true, args).ToArray());
		public RedisResult<string> ModuleUnload(string name) => SendCommand<string>("MODULE", "UNLOAD", name);
		public void Monitor(Action<string> onData) => SendCommandListen(onData, () => IsConnected, "MONITOR");
		//public void PSync(string replicationid, string offset, Action<string> onData) => SendCommandListen(onData, "PSYNC", replicationid, offset);
		public RedisResult<string> ReplicaOf(string host, int port) => SendCommand<string>("REPLICAOF", host, port);
		public RedisResult<object> Role() => SendCommand<object>("ROLE");
		public RedisResult<string> Save() => SendCommand<string>("SAVE");
		public RedisResult<string> Shutdown(bool save) => SendCommand<string>("SHUTDOWN", save ? "SAVE" : "NOSAVE");
		public RedisResult<string> SlaveOf(string host, int port) => SendCommand<string>("SLAVEOF", host, port);
		public RedisResult<object> SlowLog(string subcommand, params string[] argument) => SendCommand<object>("SLOWLOG", subcommand, argument);
		public RedisResult<string> SwapDb(int index1, int index2) => SendCommand<string>("SWAPDB", null, index1, index2);
		//public void Sync(Action<string> onData) => SendCommandListen(onData, "SYNC");
		public RedisResult<DateTime> Time() => SendCommand<long[]>("TIME").NewValue(a => new DateTime(1970, 0, 0).AddSeconds(a[0]).AddTicks(a[1] * 10));
		#endregion

		#region Commands Sets
		public RedisResult<long> SAdd(string key, params string[] members) => SendCommand<long>("SADD", key, members);
		public RedisResult<long> SCard(string key) => SendCommand<long>("SCARD", key);
		public RedisResult<string[]> SDiff(params string[] keys) => SendCommand<string[]>("SDIFF", null, keys);
		public RedisResult<long> SDiffStore(string destination, params string[] keys) => SendCommand<long>("SDIFFSTORE", destination, keys);
		public RedisResult<string[]> SInter(params string[] keys) => SendCommand<string[]>("SINTER", null, keys);
		public RedisResult<long> SInterStore(string destination, params string[] keys) => SendCommand<long>("SINTERSTORE", destination, keys);
		public RedisResult<bool> SIsMember(string key, string member) => SendCommand<bool>("SISMEMBER", key, member);
		public RedisResult<string[]> SMeMembers(string key) => SendCommand<string[]>("SMEMBERS", key);
		public RedisResult<bool> SMove(string source, string destination, string member) => SendCommand<bool>("SMOVE", source, destination, member);
		public RedisResult<string> SPop(string key) => SendCommand<string>("SPOP", key);
		public RedisResult<string[]> SPop(string key, int count) => SendCommand<string[]>("SPOP", key, count);
		public RedisResult<string> SRandMember(string key) => SendCommand<string>("SRANDMEMBER", key);
		public RedisResult<string[]> SRandMember(string key, int count) => SendCommand<string[]>("SRANDMEMBER", key, count);
		public RedisResult<long> SRem(string key, params string[] members) => SendCommand<long>("SREM", key, members);
		//SSCAN key cursor [MATCH pattern] [COUNT count]
		public RedisResult<string[]> SUnion(params string[] keys) => SendCommand<string[]>("SUNION", null, keys);
		public RedisResult<long> SUnionStore(string destination, params string[] keys) => SendCommand<long>("SUNIONSTORE", destination, keys);
		#endregion

		#region Commands Sorted Sets
		public RedisResult<RedisSortedSetItem<string>> BZPopMax(string key, int timeoutSeconds) => BZPopMaxMin("BZPOPMAX", key, timeoutSeconds);
		public RedisResult<RedisSortedSetItem<string>[]> BZPopMax(string[] keys, int timeoutSeconds) => BZPopMaxMin("BZPOPMAX", keys, timeoutSeconds);
		public RedisResult<RedisSortedSetItem<string>> BZPopMin(string key, int timeoutSeconds) => BZPopMaxMin("BZPOPMIN", key, timeoutSeconds);
		public RedisResult<RedisSortedSetItem<string>[]> BZPopMin(string[] keys, int timeoutSeconds) => BZPopMaxMin("BZPOPMIN", keys, timeoutSeconds);
		RedisResult<RedisSortedSetItem<string>> BZPopMaxMin(string command, string key, int timeoutSeconds) => SendCommand<string[]>(command, key, timeoutSeconds).NewValue(a => a == null ? null : new RedisSortedSetItem<string>(a[1], a[2].ConvertTo<decimal>()));
		/// <summary>
		/// 弹出多个 keys 有序集合值，返回 [] 的下标与之对应
		/// </summary>
		/// <param name="keys"></param>
		/// <param name="timeoutSeconds"></param>
		/// <returns></returns>
		RedisResult<RedisSortedSetItem<string>[]> BZPopMaxMin(string command, string[] keys, int timeoutSeconds)
		{
			return SendCommand<string[]>(command, null, "".AddIf(true, keys, timeoutSeconds).ToArray())
				.NewValue(a =>
				{
					if (a == null) return null;
					var result = new RedisSortedSetItem<string>[keys.Length];
					var oldkeys = keys.ToList();
					for (var z = 0; z < a.Length; z += 3)
					{
						var oldkeysIdx = oldkeys.FindIndex(x => x == a[z]);
						result[oldkeysIdx] = new RedisSortedSetItem<string>(a[z + 1], a[z + 2].ConvertTo<decimal>());
						oldkeys[oldkeysIdx] = null;
					}
					return result;
				});
		}
		public RedisResult<long> ZAdd(string key, decimal score, string member) => ZAdd<long>(key, new[] { new RedisSortedSetItem<string>(member, score) }, false, false, false, false);
		public RedisResult<long> ZAdd(string key, RedisSortedSetItem<string>[] memberScores) => ZAdd<long>(key, memberScores, false, false, false, false);
		public RedisResult<long> ZAdd(string key, RedisSortedSetItem<string>[] memberScores, bool nx, bool xx, bool ch) => ZAdd<long>(key, memberScores, nx, xx, ch, false);
		public RedisResult<string[]> ZAddIncr(string key, RedisSortedSetItem<string>[] memberScores, bool nx, bool xx, bool ch) => ZAdd<string[]>(key, memberScores, nx, xx, ch, true);
		RedisResult<TReturn> ZAdd<TReturn>(string key, RedisSortedSetItem<string>[] memberScores, bool nx, bool xx, bool ch, bool incr) => SendCommand<TReturn>("ZADD", key, ""
			.AddIf(nx, "NX")
			.AddIf(xx, "XX")
			.AddIf(ch, "CH")
			.AddIf(incr, "INCR")
			.AddIf(true, memberScores.Select(a => new object[] { a.Score, a.Member }).SelectMany(a => a).ToArray())
			.ToArray());
		public RedisResult<long> ZCard(string key) => SendCommand<long>("ZCARD", key);
		public RedisResult<long> ZCount(string key, decimal min, decimal max) => SendCommand<long>("ZCOUNT", key, min, max);
		public RedisResult<long> ZCount(string key, string min, string max) => SendCommand<long>("ZCOUNT", key, min, max);
		public RedisResult<decimal> ZIncrBy(string key, decimal increment, string member) => SendCommand<decimal>("ZINCRBY", key, increment, member);
		//ZINTERSTORE destination numkeys key [key ...] [WEIGHTS weight [weight ...]] [AGGREGATE SUM|MIN|MAX]
		public RedisResult<long> ZLexCount(string key, string min, string max) => SendCommand<long>("ZLEXCOUNT", key, min, max);
		public RedisResult<RedisSortedSetItem<string>> ZPopMin(string key) => ZPopMaxMin("ZPOPMIN", key);
		public RedisResult<RedisSortedSetItem<string>[]> ZPopMin(string key, int count) => ZPopMaxMin("ZPOPMIN", key, count);
		public RedisResult<RedisSortedSetItem<string>> ZPopMax(string key) => ZPopMaxMin("ZPOPMAX", key);
		public RedisResult<RedisSortedSetItem<string>[]> ZPopMax(string key, int count) => ZPopMaxMin("ZPOPMAX", key, count);
		RedisResult<RedisSortedSetItem<string>> ZPopMaxMin(string command, string key) => SendCommand<string[]>(command, key).NewValue(a => a == null ? null : new RedisSortedSetItem<string>(a[1], a[2].ConvertTo<decimal>()));
		RedisResult<RedisSortedSetItem<string>[]> ZPopMaxMin(string command, string key, int count) => SendCommand<string[]>(command, key, count).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new RedisSortedSetItem<string>(b.Key, b.Value)).ToArray());
		public RedisResult<string[]> ZRange(string key, decimal start, decimal stop) => SendCommand<string[]>("ZRANGE", key, start, stop);
		public RedisResult<RedisSortedSetItem<string>[]> ZRangeWithScores(string key, decimal start, decimal stop) => SendCommand<string[]>("ZRANGE", key, start, stop, "WITHSCORES").NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new RedisSortedSetItem<string>(b.Key, b.Value)).ToArray());
		public RedisResult<string[]> ZRangeByLex(string key, decimal min, decimal max, int offset = 0, int count = 0) => offset > 0 || count > 0 ? SendCommand<string[]>("ZRANGEBYLEX", key, min, max, "LIMIT", offset, count) : SendCommand<string[]>("ZRANGEBYLEX", key, min, max);
		public RedisResult<string[]> ZRangeByLex(string key, string min, string max, int offset = 0, int count = 0) => offset > 0 || count > 0 ? SendCommand<string[]>("ZRANGEBYLEX", key, min, max, "LIMIT", offset, count) : SendCommand<string[]>("ZRANGEBYLEX", key, min, max);
		public RedisResult<string[]> ZRangeByScore(string key, decimal min, decimal max, int offset = 0, int count = 0) => offset > 0 || count > 0 ? SendCommand<string[]>("ZRANGEBYSCORE", key, min, max, "LIMIT", offset, count) : SendCommand<string[]>("ZRANGEBYSCORE", key, min, max);
		public RedisResult<string[]> ZRangeByScore(string key, string min, string max, int offset = 0, int count = 0) => offset > 0 || count > 0 ? SendCommand<string[]>("ZRANGEBYSCORE", key, min, max, "LIMIT", offset, count) : SendCommand<string[]>("ZRANGEBYSCORE", key, min, max);
		public RedisResult<RedisSortedSetItem<string>[]> ZRangeByScoreWithScores(string key, decimal min, decimal max, int offset = 0, int count = 0) => (offset > 0 || count > 0 ? SendCommand<string[]>("ZRANGEBYSCORE", key, min, max, "LIMIT", offset, count) : SendCommand<string[]>("ZRANGEBYSCORE", key, min, max)).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new RedisSortedSetItem<string>(b.Key, b.Value)).ToArray());
		public RedisResult<RedisSortedSetItem<string>[]> ZRangeByScoreWithScores(string key, string min, string max, int offset = 0, int count = 0) => (offset > 0 || count > 0 ? SendCommand<string[]>("ZRANGEBYSCORE", key, min, max, "LIMIT", offset, count) : SendCommand<string[]>("ZRANGEBYSCORE", key, min, max)).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new RedisSortedSetItem<string>(b.Key, b.Value)).ToArray());
		public RedisResult<long> ZRank(string key, string member) => SendCommand<long>("ZRANK", key, member);
		public RedisResult<long> ZRem(string key, params string[] members) => SendCommand<long>("ZREM", key, members);
		public RedisResult<long> ZRemRangeByLex(string key, string min, string max) => SendCommand<long>("ZREMRANGEBYLEX", key, min, max);
		public RedisResult<long> ZRemRangeByRank(string key, long start, long stop) => SendCommand<long>("ZREMRANGEBYRANK", key, start, stop);
		public RedisResult<long> ZRemRangeByScore(string key, decimal min, decimal max) => SendCommand<long>("ZREMRANGEBYSCORE", key, min, max);
		public RedisResult<long> ZRemRangeByScore(string key, string min, string max) => SendCommand<long>("ZREMRANGEBYSCORE", key, min, max);
		public RedisResult<string[]> ZRevRange(string key, decimal start, decimal stop) => SendCommand<string[]>("ZREVRANGE", key, start, stop);
		public RedisResult<RedisSortedSetItem<string>[]> ZRevRangeWithScores(string key, decimal start, decimal stop) => SendCommand<string[]>("ZREVRANGE", key, start, stop, "WITHSCORES").NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new RedisSortedSetItem<string>(b.Key, b.Value)).ToArray());
		public RedisResult<string[]> ZRevRangeByLex(string key, decimal max, decimal min, int offset = 0, int count = 0) => offset > 0 || count > 0 ? SendCommand<string[]>("ZREVRANGEBYLEX", key, max, min, "LIMIT", offset, count) : SendCommand<string[]>("ZREVRANGEBYLEX", key, max, min);
		public RedisResult<string[]> ZRevRangeByLex(string key, string max, string min, int offset = 0, int count = 0) => offset > 0 || count > 0 ? SendCommand<string[]>("ZREVRANGEBYLEX", key, max, min, "LIMIT", offset, count) : SendCommand<string[]>("ZREVRANGEBYLEX", key, max, min);
		public RedisResult<string[]> ZRevRangeByScore(string key, decimal max, decimal min, int offset = 0, int count = 0) => offset > 0 || count > 0 ? SendCommand<string[]>("ZREVRANGEBYSCORE", key, max, min, "LIMIT", offset, count) : SendCommand<string[]>("ZREVRANGEBYSCORE", key, max, min);
		public RedisResult<string[]> ZRevRangeByScore(string key, string max, string min, int offset = 0, int count = 0) => offset > 0 || count > 0 ? SendCommand<string[]>("ZREVRANGEBYSCORE", key, max, min, "LIMIT", offset, count) : SendCommand<string[]>("ZREVRANGEBYSCORE", key, max, min);
		public RedisResult<RedisSortedSetItem<string>[]> ZRevRangeByScoreWithScores(string key, decimal max, decimal min, int offset = 0, int count = 0) => (offset > 0 || count > 0 ? SendCommand<string[]>("ZREVRANGEBYSCORE", key, max, min, "LIMIT", offset, count) : SendCommand<string[]>("ZREVRANGEBYSCORE", key, max, min)).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new RedisSortedSetItem<string>(b.Key, b.Value)).ToArray());
		public RedisResult<RedisSortedSetItem<string>[]> ZRevRangeByScoreWithScores(string key, string max, string min, int offset = 0, int count = 0) => (offset > 0 || count > 0 ? SendCommand<string[]>("ZREVRANGEBYSCORE", key, max, min, "LIMIT", offset, count) : SendCommand<string[]>("ZREVRANGEBYSCORE", key, max, min)).NewValue(a => a == null ? null : a.MapToHash<decimal>(Encoding).Select(b => new RedisSortedSetItem<string>(b.Key, b.Value)).ToArray());
		public RedisResult<long> ZRevRank(string key, string member) => SendCommand<long>("ZREVRANK", key, member);
		//ZSCAN key cursor [MATCH pattern] [COUNT count]
		public RedisResult<decimal> ZScore(string key, string member) => SendCommand<decimal>("ZSCORE", key, member);
		//ZUNIONSTORE destination numkeys key [key ...] [WEIGHTS weight [weight ...]] [AGGREGATE SUM|MIN|MAX]
		#endregion

		#region Commands Streams
		public RedisResult<long> XAck(string key, string group, params string[] id) => SendCommand<long>("XACK", key, group.AddIf(true, id).ToArray());
		public RedisResult<string> XAdd(string key, long maxLen, string id = "*", params KeyValuePair<string, string>[] fieldValues) => SendCommand<string>("XADD", key, ""
			.AddIf(maxLen > 0, "MAXLEN", maxLen)
			.AddIf(maxLen < 0, "MAXLEN", $"~{Math.Abs(maxLen)}")
			.AddIf(true, fieldValues.ToKvArray())
			.ToArray());
		public RedisResult<object> XClaim(string key, string group, string consumer, long minIdleTime, params string[] id) => SendCommand<object>("XCLAIM", key, group
			.AddIf(true, consumer, minIdleTime, id)
			.ToArray());
		public RedisResult<object> XClaim(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => SendCommand<object>("XCLAIM", key, group
			.AddIf(true, consumer, minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
			.AddIf(force, "FORCE")
			.ToArray());
		public RedisResult<string[]> XClaimJustId(string key, string group, string consumer, long minIdleTime, params string[] id) => SendCommand<string[]>("XCLAIM", key, group
			.AddIf(true, consumer, minIdleTime, id, "JUSTID")
			.ToArray());
		public RedisResult<string[]> XClaimJustId(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => SendCommand<string[]>("XCLAIM", key, group
			.AddIf(true, consumer, minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
			.AddIf(force, "FORCE")
			.AddIf(true, "JUSTID")
			.ToArray());
		public RedisResult<long> XDel(string key, params string[] id) => SendCommand<long>("XDEL", key, id);
		public RedisResult<string> XGroupCreate(string key, string group, string id = "$", bool MkStream = false) => SendCommand<string>("XGROUP", "CREATE", key
			.AddIf(true, group, id)
			.AddIf(MkStream, "MKSTREAM")
			.ToArray()); 
		public RedisResult<string> XGroupSetId(string key, string group, string id = "$") => SendCommand<string>("XGROUP", "SETID", key, group, id);
		public RedisResult<bool> XGroupDestroy(string key, string group) => SendCommand<bool>("XGROUP", "DESTROY", key, group);
		public RedisResult<bool> XGroupDelConsumer(string key, string group, string consumer) => SendCommand<bool>("XGROUP", "DELCONSUMER", key, group, consumer);
		public RedisResult<object> XInfoStream(string key) => SendCommand<object>("XINFO", "STREAM", key);
		public RedisResult<object> XInfoGroups(string key) => SendCommand<object>("XINFO", "GROUPS", key);
		public RedisResult<object> XInfoConsumers(string key, string group) => SendCommand<object>("XINFO", "CONSUMERS", key, group);
		public RedisResult<long> XLen(string key) => SendCommand<long>("XLEN", key);
		public RedisResult<object> XPending(string key, string group) => SendCommand<object>("XPENDING", key, group);
		public RedisResult<object> XPending(string key, string group, string start, string end, long count, string consumer = null) => SendCommand<object>("XPENDING", key, group
			.AddIf(true, start, end, count)
			.AddIf(!string.IsNullOrWhiteSpace(consumer), consumer)
			.ToArray());
		public RedisResult<object> XRange(string key, string start, string end, long count = 1) => SendCommand<object>("XRANGE", key, start
			.AddIf(true, end)
			.AddIf(count > 0, "COUNT", count)
			.ToArray());
		public RedisResult<object> XRevRange(string key, string end, string start, long count = 1) => SendCommand<object>("XREVRANGE", key, end
			.AddIf(true, start)
			.AddIf(count > 0, "COUNT", count)
			.ToArray());
		public RedisResult<object> XRead(long count, long block, string key, string id) => SendCommand<object>("XREAD", null, ""
			.AddIf(count > 0, "COUNT", count)
			.AddIf(block > 0, "BLOCK", block)
			.AddIf(true, "STREAMS", key, id)
			.ToArray());
		public RedisResult<object> XRead(long count, long block, string[] key, string[] id) => SendCommand<object>("XREAD", null, ""
			.AddIf(count > 0, "COUNT", count)
			.AddIf(block > 0, "BLOCK", block)
			.AddIf(true, "STREAMS", key, id)
			.ToArray());
		public RedisResult<object> XReadGroup(string group, string consumer, long count, long block, string key, string id) => SendCommand<object>("XREADGROUP", null, "GROUP"
			.AddIf(true, group, consumer)
			.AddIf(count > 0, "COUNT", count)
			.AddIf(block > 0, "BLOCK", block)
			.AddIf(true, "STREAMS", key, id)
			.ToArray());
		public RedisResult<object> XReadGroup(string group, string consumer, long count, long block, string[] key, string[] id) => SendCommand<object>("XREADGROUP", null, "GROUP"
			.AddIf(true, group, consumer)
			.AddIf(count > 0, "COUNT", count)
			.AddIf(block > 0, "BLOCK", block)
			.AddIf(true, "STREAMS", key, id)
			.ToArray());
		public RedisResult<long> XTrim(string key, long maxLen) => SendCommand<long>("XTRIM", key, "MAXLEN", maxLen > 0 ? maxLen.ToString() : $"~{Math.Abs(maxLen)}");
		#endregion

		#region Commands Strings
		public RedisResult<long> Append(string key, object value) => SendCommand<long>("APPEND", key, value);
		public RedisResult<long> BitCount(string key, long start, long end) => SendCommand<long>("BITCOUNT", key, start, end);
		//BITFIELD key [GET type offset] [SET type offset value] [INCRBY type offset increment] [OVERFLOW WRAP|SAT|FAIL]
		public RedisResult<long> BitOp(BitOpOperation operation, string destkey, params string[] keys) => SendCommand<long>("BITOP", null, "".AddIf(true, operation, destkey, keys).ToArray());
		public RedisResult<long> BitPos(string key, object bit, long start = 0, long end = 0) => start > 0 && end > 0 ? SendCommand<long>("BITPOS", key, new object[] { bit, start, end }) :
			(start > 0 ? SendCommand<long>("BITPOS", key, new object[] { bit, start }) : SendCommand<long>("BITPOS", key, bit));
		public RedisResult<long> Decr(string key) => SendCommand<long>("DECR", key);
		public RedisResult<long> DecrBy(string key, long decrement) => SendCommand<long>("DECRBY", key, decrement);
		public RedisResult<string> Get(string key) => SendCommand<string>("GET", key);
		public RedisResult<long> GetBit(string key, long offset) => SendCommand<long>("GETBIT", key, offset);
		public RedisResult<string> GetRange(string key, long start, long end) => SendCommand<string>("GETRANGE", key, start, end);
		public RedisResult<string> GetSet(string key, object value) => SendCommand<string>("GETSET", key, value);
		public RedisResult<long> Incr(string key) => SendCommand<long>("INCR", key);
		public RedisResult<long> IncrBy(string key, long decrement) => SendCommand<long>("INCRBY", key, decrement);
		public RedisResult<decimal> IncrByFloat(string key, decimal decrement) => SendCommand<decimal>("INCRBYFLOAT", key, decrement);
		public RedisResult<string[]> MGet(params string[] keys) => SendCommand<string[]>("MGET", null, keys);
		public RedisResult<string> MSet(Dictionary<string, object> keyValues) => SendCommand<string>("MSET", null, keyValues.ToKvArray());
		public RedisResult<long> MSetNx(Dictionary<string, object> keyValues) => SendCommand<long>("MSETNX", null, keyValues.ToKvArray());
		public RedisResult<string> PSetNx(string key, long milliseconds, object value) => SendCommand<string>("PSETEX", key, milliseconds, value);
		public RedisResult<string> Set(string key, object value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, false);
		public RedisResult<string> Set(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, true, false, false);
		public RedisResult<string> SetNx(string key, object value, int timeoutSeconds) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, true, false);
		public RedisResult<string> SetNx(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, true, true, false);
		public RedisResult<string> SetXx(string key, object value, int timeoutSeconds = 0) => Set(key, value, TimeSpan.FromSeconds(timeoutSeconds), false, false, true);
		public RedisResult<string> SetXx(string key, object value, bool keepTtl) => Set(key, value, TimeSpan.Zero, true, false, true);
		RedisResult<string> Set(string key, object value, TimeSpan timeout, bool keepTtl, bool nx, bool xx) => SendCommand<string>("SET", key, ""
			.AddIf(true, value)
			.AddIf(timeout.TotalSeconds >= 1, "EX", (long)timeout.TotalSeconds)
			.AddIf(timeout.TotalSeconds < 1 && timeout.TotalMilliseconds >= 1, "PX", (long)timeout.TotalMilliseconds)
			.AddIf(keepTtl, "KEEPTTL")
			.AddIf(nx, "NX")
			.AddIf(xx, "XX").ToArray());
		public RedisResult<long> SetBit(string key, long offset, object value) => SendCommand<long>("SETBIT", key, offset, value);
		public RedisResult<string> SetEx(string key, int seconds, object value) => SendCommand<string>("SETEX", key, seconds, value);
		public RedisResult<bool> SetNx(string key, object value) => SendCommand<bool>("SETNX", key, value);
		public RedisResult<long> SetRange(string key, long offset, object value) => SendCommand<long>("SETRANGE", key, offset, value);
		//STRALGO LCS algo-specific-argument [algo-specific-argument ...]
		public RedisResult<long> StrLen(string key) => SendCommand<long>("STRLEN", key);
		#endregion

		#region Commands Transactions
		public RedisResult<string> Discard() => SendCommand<string>("DISCARD");
		public RedisResult<object[]> Exec() => SendCommand<object[]>("EXEC");
		public RedisResult<string> Multi() => SendCommand<string>("MULTI");
		public RedisResult<string> UnWatch() => SendCommand<string>("UNWATCH");
		public RedisResult<string> Watch(params string[] keys) => SendCommand<string>("WATCH", null, keys);
		#endregion


    }
    public enum BitOpOperation { And, Or, Xor, Not }
	public class RedisSortedSetItem<T> { public T Member { get; set; } public decimal Score { get; set; } public RedisSortedSetItem(T member, decimal score) { this.Member = member;this.Score = score; } }
}
