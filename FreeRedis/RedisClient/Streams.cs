using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<long> XAck(string key, string group, params string[] id) => Call<long>("XACK", key, group.AddIf(true, id).ToArray());
		public RedisResult<string> XAdd(string key, long maxLen, string id = "*", params KeyValuePair<string, string>[] fieldValues) => Call<string>("XADD", key, ""
			.AddIf(maxLen > 0, "MAXLEN", maxLen)
			.AddIf(maxLen < 0, "MAXLEN", $"~{Math.Abs(maxLen)}")
			.AddIf(true, fieldValues.ToKvArray())
			.ToArray());
		public RedisResult<object> XClaim(string key, string group, string consumer, long minIdleTime, params string[] id) => Call<object>("XCLAIM", key, group
			.AddIf(true, consumer, minIdleTime, id)
			.ToArray());
		public RedisResult<object> XClaim(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => Call<object>("XCLAIM", key, group
			.AddIf(true, consumer, minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
			.AddIf(force, "FORCE")
			.ToArray());
		public RedisResult<string[]> XClaimJustId(string key, string group, string consumer, long minIdleTime, params string[] id) => Call<string[]>("XCLAIM", key, group
			.AddIf(true, consumer, minIdleTime, id, "JUSTID")
			.ToArray());
		public RedisResult<string[]> XClaimJustId(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => Call<string[]>("XCLAIM", key, group
			.AddIf(true, consumer, minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
			.AddIf(force, "FORCE")
			.AddIf(true, "JUSTID")
			.ToArray());
		public RedisResult<long> XDel(string key, params string[] id) => Call<long>("XDEL", key, id);
		public RedisResult<string> XGroupCreate(string key, string group, string id = "$", bool MkStream = false) => Call<string>("XGROUP", "CREATE", key
			.AddIf(true, group, id)
			.AddIf(MkStream, "MKSTREAM")
			.ToArray());
		public RedisResult<string> XGroupSetId(string key, string group, string id = "$") => Call<string>("XGROUP", "SETID", key, group, id);
		public RedisResult<bool> XGroupDestroy(string key, string group) => Call<bool>("XGROUP", "DESTROY", key, group);
		public RedisResult<bool> XGroupDelConsumer(string key, string group, string consumer) => Call<bool>("XGROUP", "DELCONSUMER", key, group, consumer);
		public RedisResult<object> XInfoStream(string key) => Call<object>("XINFO", "STREAM", key);
		public RedisResult<object> XInfoGroups(string key) => Call<object>("XINFO", "GROUPS", key);
		public RedisResult<object> XInfoConsumers(string key, string group) => Call<object>("XINFO", "CONSUMERS", key, group);
		public RedisResult<long> XLen(string key) => Call<long>("XLEN", key);
		public RedisResult<object> XPending(string key, string group) => Call<object>("XPENDING", key, group);
		public RedisResult<object> XPending(string key, string group, string start, string end, long count, string consumer = null) => Call<object>("XPENDING", key, group
			.AddIf(true, start, end, count)
			.AddIf(!string.IsNullOrWhiteSpace(consumer), consumer)
			.ToArray());
		public RedisResult<object> XRange(string key, string start, string end, long count = 1) => Call<object>("XRANGE", key, start
			.AddIf(true, end)
			.AddIf(count > 0, "COUNT", count)
			.ToArray());
		public RedisResult<object> XRevRange(string key, string end, string start, long count = 1) => Call<object>("XREVRANGE", key, end
			.AddIf(true, start)
			.AddIf(count > 0, "COUNT", count)
			.ToArray());
		public RedisResult<object> XRead(long count, long block, string key, string id) => Call<object>("XREAD", null, ""
			.AddIf(count > 0, "COUNT", count)
			.AddIf(block > 0, "BLOCK", block)
			.AddIf(true, "STREAMS", key, id)
			.ToArray());
		public RedisResult<object> XRead(long count, long block, string[] key, string[] id) => Call<object>("XREAD", null, ""
			.AddIf(count > 0, "COUNT", count)
			.AddIf(block > 0, "BLOCK", block)
			.AddIf(true, "STREAMS", key, id)
			.ToArray());
		public RedisResult<object> XReadGroup(string group, string consumer, long count, long block, string key, string id) => Call<object>("XREADGROUP", null, "GROUP"
			.AddIf(true, group, consumer)
			.AddIf(count > 0, "COUNT", count)
			.AddIf(block > 0, "BLOCK", block)
			.AddIf(true, "STREAMS", key, id)
			.ToArray());
		public RedisResult<object> XReadGroup(string group, string consumer, long count, long block, string[] key, string[] id) => Call<object>("XREADGROUP", null, "GROUP"
			.AddIf(true, group, consumer)
			.AddIf(count > 0, "COUNT", count)
			.AddIf(block > 0, "BLOCK", block)
			.AddIf(true, "STREAMS", key, id)
			.ToArray());
		public RedisResult<long> XTrim(string key, long maxLen) => Call<long>("XTRIM", key, "MAXLEN", maxLen > 0 ? maxLen.ToString() : $"~{Math.Abs(maxLen)}");
    }
}
