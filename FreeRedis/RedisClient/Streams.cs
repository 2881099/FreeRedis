using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
    {
        public long XAck(string key, string group, params string[] id) => Call("XACK".Input(key, group).Input(id).FlagKey(key), rt => rt.ThrowOrValue<long>());
        public string XAdd(string key, long maxLen, string id = "*", params KeyValuePair<string, string>[] fieldValues) => Call("XADD"
            .Input(key, id)
            .InputIf(maxLen > 0, "MAXLEN", maxLen)
            .InputIf(maxLen < 0, "MAXLEN", $"~{Math.Abs(maxLen)}")
            .InputKv(fieldValues)
            .FlagKey(key), rt => rt.ThrowOrValue<string>());

        public object XClaim(string key, string group, string consumer, long minIdleTime, params string[] id) => Call("XCLAIM"
            .Input(key, group, consumer)
            .Input(minIdleTime, id)
            .FlagKey(key), rt => rt.ThrowOrValue());
        public object XClaim(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => Call("XCLAIM"
            .Input(key, group, consumer)
            .Input(minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
            .InputIf(force, "FORCE")
            .FlagKey(key), rt => rt.ThrowOrValue());

        public string[] XClaimJustId(string key, string group, string consumer, long minIdleTime, params string[] id) => Call("XCLAIM"
            .Input(key, group, consumer)
            .Input(minIdleTime, id, "JUSTID")
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());
        public string[] XClaimJustId(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => Call("XCLAIM"
            .Input(key, group, consumer)
            .Input(minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
            .InputIf(force, "FORCE")
            .Input("JUSTID")
            .FlagKey(key), rt => rt.ThrowOrValue<string[]>());

        public long XDel(string key, params string[] id) => Call("XDEL".Input(key).Input(id).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public string XGroupCreate(string key, string group, string id = "$", bool MkStream = false) => Call("XGROUP"
            .SubCommand("CREATE")
            .Input(key, group, id)
            .InputIf(MkStream, "MKSTREAM")
            .FlagKey(key), rt => rt.ThrowOrValue<string>());
        public string XGroupSetId(string key, string group, string id = "$") => Call("XGROUP".SubCommand("SETID").Input(key, group, id).FlagKey(key), rt => rt.ThrowOrValue<string>());
        public bool XGroupDestroy(string key, string group) => Call("XGROUP".SubCommand("DESTROY").Input(key, group).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public bool XGroupDelConsumer(string key, string group, string consumer) => Call("XGROUP".SubCommand("DELCONSUMER").Input(key, group, consumer).FlagKey(key), rt => rt.ThrowOrValue<bool>());

        public object XInfoStream(string key) => Call("XINFO".SubCommand("STREAM").Input(key).FlagKey(key), rt => rt.ThrowOrValue());
        public object XInfoGroups(string key) => Call("XINFO".SubCommand( "GROUPS").Input(key).FlagKey(key), rt => rt.ThrowOrValue());
        public object XInfoConsumers(string key, string group) => Call("XINFO".SubCommand("CONSUMERS").Input(key, group).FlagKey(key), rt => rt.ThrowOrValue());

        public long XLen(string key) => Call("XLEN".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public object XPending(string key, string group) => Call("XPENDING".Input(key, group).FlagKey(key), rt => rt.ThrowOrValue());
        public object XPending(string key, string group, string start, string end, long count, string consumer = null) => Call("XPENDING"
            .Input(key, group)
            .Input(start, end, count)
            .InputIf(!string.IsNullOrWhiteSpace(consumer), consumer)
            .FlagKey(key), rt => rt.ThrowOrValue());

        public object XRange(string key, string start, string end, long count = 1) => Call("XRANGE"
            .Input(key, start, end)
            .InputIf(count > 0, "COUNT", count)
            .FlagKey(key), rt => rt.ThrowOrValue());
        public object XRevRange(string key, string end, string start, long count = 1) => Call("XREVRANGE"
            .Input(key, end, start)
            .InputIf(count > 0, "COUNT", count)
            .FlagKey(key), rt => rt.ThrowOrValue());

        public object XRead(long count, long block, string key, string id) => Call("XREAD".SubCommand(null)
            .InputIf(count > 0, "COUNT", count)
            .InputIf(block > 0, "BLOCK", block)
            .Input(new[] { "STREAMS", key, id })
            .FlagKey(key), rt => rt.ThrowOrValue());
        public object XRead(long count, long block, string[] key, string[] id) => Call("XREAD".SubCommand(null)
            .InputIf(count > 0, "COUNT", count)
            .InputIf(block > 0, "BLOCK", block)
            .InputRaw("STREAMS")
            .Input(key)
            .Input(id)
            .FlagKey(key), rt => rt.ThrowOrValue());

        public object XReadGroup(string group, string consumer, long count, long block, string key, string id) => Call("XREADGROUP"
            .Input("GROUP", group, consumer)
            .InputIf(count > 0, "COUNT", count)
            .InputIf(block > 0, "BLOCK", block)
            .Input(new[] { "STREAMS", key, id })
            .FlagKey(key), rt => rt.ThrowOrValue());
        public object XReadGroup(string group, string consumer, long count, long block, string[] key, string[] id) => Call("XREADGROUP"
            .Input("GROUP", group, consumer)
            .InputIf(count > 0, "COUNT", count)
            .InputIf(block > 0, "BLOCK", block)
            .InputRaw("STREAMS")
            .Input(key)
            .Input(id)
            .FlagKey(key), rt => rt.ThrowOrValue());

        public long XTrim(string key, long maxLen) => Call("XTRIM"
            .Input(key)
            .InputIf(maxLen > 0, "MAXLEN", maxLen)
            .InputIf(maxLen < 0, "MAXLEN", $"~{Math.Abs(maxLen)}")
            .FlagKey(key), rt => rt.ThrowOrValue<long>());
    }
}
