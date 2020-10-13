using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<long> XAck(string key, string group, params string[] id) => Call<long>("XACK".Input(key, group).Input(id).FlagKey(key));
		public RedisResult<string> XAdd(string key, long maxLen, string id = "*", params KeyValuePair<string, string>[] fieldValues) => Call<string>("XADD"
			.Input(key, id)
			.InputIf(maxLen > 0, "MAXLEN", maxLen)
			.InputIf(maxLen < 0, "MAXLEN", $"~{Math.Abs(maxLen)}")
			.InputKv(fieldValues)
			.FlagKey(key));
		public RedisResult<object> XClaim(string key, string group, string consumer, long minIdleTime, params string[] id) => Call<object>("XCLAIM"
			.Input(key, group, consumer)
			.Input(minIdleTime, id)
			.FlagKey(key));
		public RedisResult<object> XClaim(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => Call<object>("XCLAIM"
			.Input(key, group, consumer)
			.Input(minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
			.InputIf(force, "FORCE")
			.FlagKey(key));
		public RedisResult<string[]> XClaimJustId(string key, string group, string consumer, long minIdleTime, params string[] id) => Call<string[]>("XCLAIM"
			.Input(key, group, consumer)
			.Input(minIdleTime, id, "JUSTID")
			.FlagKey(key));
		public RedisResult<string[]> XClaimJustId(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => Call<string[]>("XCLAIM"
			.Input(key, group, consumer)
			.Input(minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
			.InputIf(force, "FORCE")
			.Input("JUSTID")
			.FlagKey(key));
		public RedisResult<long> XDel(string key, params string[] id) => Call<long>("XDEL".Input(key).Input(id).FlagKey(key));
		public RedisResult<string> XGroupCreate(string key, string group, string id = "$", bool MkStream = false) => Call<string>("XGROUP"
			.SubCommand("CREATE")
			.Input(key, group, id)
			.InputIf(MkStream, "MKSTREAM")
			.FlagKey(key));
		public RedisResult<string> XGroupSetId(string key, string group, string id = "$") => Call<string>("XGROUP".SubCommand("SETID").Input(key, group, id).FlagKey(key));
		public RedisResult<bool> XGroupDestroy(string key, string group) => Call<bool>("XGROUP".SubCommand("DESTROY").Input(key, group).FlagKey(key));
		public RedisResult<bool> XGroupDelConsumer(string key, string group, string consumer) => Call<bool>("XGROUP".SubCommand("DELCONSUMER").Input(key, group, consumer).FlagKey(key));
		public RedisResult<object> XInfoStream(string key) => Call<object>("XINFO".SubCommand("STREAM").Input(key).FlagKey(key));
		public RedisResult<object> XInfoGroups(string key) => Call<object>("XINFO".SubCommand( "GROUPS").Input(key).FlagKey(key));
		public RedisResult<object> XInfoConsumers(string key, string group) => Call<object>("XINFO".SubCommand("CONSUMERS").Input(key, group).FlagKey(key));
		public RedisResult<long> XLen(string key) => Call<long>("XLEN".Input(key).FlagKey(key));
		public RedisResult<object> XPending(string key, string group) => Call<object>("XPENDING".Input(key, group).FlagKey(key));
		public RedisResult<object> XPending(string key, string group, string start, string end, long count, string consumer = null) => Call<object>("XPENDING"
			.Input(key, group)
			.Input(start, end, count)
			.InputIf(!string.IsNullOrWhiteSpace(consumer), consumer)
			.FlagKey(key));
		public RedisResult<object> XRange(string key, string start, string end, long count = 1) => Call<object>("XRANGE"
			.Input(key, start, end)
			.InputIf(count > 0, "COUNT", count)
			.FlagKey(key));
		public RedisResult<object> XRevRange(string key, string end, string start, long count = 1) => Call<object>("XREVRANGE"
			.Input(key, end, start)
			.InputIf(count > 0, "COUNT", count)
			.FlagKey(key));
		public RedisResult<object> XRead(long count, long block, string key, string id) => Call<object>("XREAD".SubCommand(null)
			.InputIf(count > 0, "COUNT", count)
			.InputIf(block > 0, "BLOCK", block)
			.Input(new[] { "STREAMS", key, id })
			.FlagKey(key));
		public RedisResult<object> XRead(long count, long block, string[] key, string[] id) => Call<object>("XREAD".SubCommand(null)
			.InputIf(count > 0, "COUNT", count)
			.InputIf(block > 0, "BLOCK", block)
			.InputRaw("STREAMS")
			.Input(key)
			.Input(id)
			.FlagKey(key));
		public RedisResult<object> XReadGroup(string group, string consumer, long count, long block, string key, string id) => Call<object>("XREADGROUP"
			.Input("GROUP", group, consumer)
			.InputIf(count > 0, "COUNT", count)
			.InputIf(block > 0, "BLOCK", block)
			.Input(new[] { "STREAMS", key, id })
			.FlagKey(key));
		public RedisResult<object> XReadGroup(string group, string consumer, long count, long block, string[] key, string[] id) => Call<object>("XREADGROUP"
			.Input("GROUP", group, consumer)
			.InputIf(count > 0, "COUNT", count)
			.InputIf(block > 0, "BLOCK", block)
			.InputRaw("STREAMS")
			.Input(key)
			.Input(id)
			.FlagKey(key));
		public RedisResult<long> XTrim(string key, long maxLen) => Call<long>("XTRIM"
			.Input(key)
			.InputIf(maxLen > 0, "MAXLEN", maxLen)
			.InputIf(maxLen < 0, "MAXLEN", $"~{Math.Abs(maxLen)}")
			.FlagKey(key));
    }
}
