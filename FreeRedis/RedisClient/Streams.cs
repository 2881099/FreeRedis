using FreeRedis.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FreeRedis
{
    partial class RedisClient
    {
        public long XAck(string key, string group, params string[] id) => Call("XACK".Input(key, group).Input(id).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public string XAdd<T>(string key, string field, T value, params object[] fieldValues) => XAdd(key, 0, "*", field, value, fieldValues);
        public string XAdd<T>(string key, long maxlen, string id, string field, T value, params object[] fieldValues) => Call("XADD"
            .Input(key)
            .InputIf(maxlen > 0, "MAXLEN", maxlen)
            .InputIf(maxlen < 0, "MAXLEN", "~", Math.Abs(maxlen))
            .Input(string.IsNullOrEmpty(id) ? "*" : id)
            .InputRaw(field).InputRaw(SerializeRedisValue(value))
            .InputKv(fieldValues, SerializeRedisValue)
            .FlagKey(key), rt => rt.ThrowOrValue<string>());
        public string XAdd<T>(string key, Dictionary<string, T> fieldValues) => XAdd(key, 0, "*", fieldValues);
        public string XAdd<T>(string key, long maxlen, string id, Dictionary<string, T> fieldValues) => Call("XADD"
            .Input(key)
            .InputIf(maxlen > 0, "MAXLEN", maxlen)
            .InputIf(maxlen < 0, "MAXLEN", "~", Math.Abs(maxlen))
            .Input(string.IsNullOrEmpty(id) ? "*" : id)
            .InputKv(fieldValues, SerializeRedisValue)
            .FlagKey(key), rt => rt.ThrowOrValue<string>());
        

        public StreamsEntry[] XClaim(string key, string group, string consumer, long minIdleTime, params string[] id) => Call("XCLAIM"
            .Input(key, group, consumer)
            .Input(minIdleTime, id)
            .FlagKey(key), rt => rt.ThrowOrValueToStreamsEntryArray());
        public StreamsEntry[] XClaim(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => Call("XCLAIM"
            .Input(key, group, consumer)
            .Input(minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
            .InputIf(force, "FORCE")
            .FlagKey(key), rt => rt.ThrowOrValueToStreamsEntryArray());

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

        public void XGroupCreate(string key, string group, string id = "$", bool MkStream = false) => Call("XGROUP"
            .SubCommand("CREATE")
            .Input(key, group, id)
            .InputIf(MkStream, "MKSTREAM")
            .FlagKey(key), rt => rt.ThrowOrNothing());
        public void XGroupSetId(string key, string group, string id = "$") => Call("XGROUP".SubCommand("SETID").Input(key, group, id).FlagKey(key), rt => rt.ThrowOrNothing());
        public bool XGroupDestroy(string key, string group) => Call("XGROUP".SubCommand("DESTROY").Input(key, group).FlagKey(key), rt => rt.ThrowOrValue<bool>());
        public void XGroupCreateConsumer(string key, string group, string consumer) => Call("XGROUP".SubCommand("CREATECONSUMER").Input(key, group, consumer).FlagKey(key), rt => rt.ThrowOrNothing());
        public long XGroupDelConsumer(string key, string group, string consumer) => Call("XGROUP".SubCommand("DELCONSUMER").Input(key, group, consumer).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public StreamsXInfoStreamResult XInfoStream(string key) => Call("XINFO".SubCommand("STREAM").Input(key).FlagKey(key), rt => rt.ThrowOrValueToXInfoStream());
        public StreamsXInfoStreamFullResult XInfoStreamFull(string key, long count = 10) => Call("XINFO"
            .SubCommand("STREAM").Input(key).Input("FULL")
            .InputIf(count != 10, "COUNT", count)
            .FlagKey(key), rt => rt.ThrowOrValueToXInfoStreamFullResult());
        public StreamsXInfoGroupsResult[] XInfoGroups(string key) => Call("XINFO".SubCommand("GROUPS").Input(key).FlagKey(key), rt => rt.ThrowOrValue((a, _) => a.Select(x => (x as object[]).MapToClass<StreamsXInfoGroupsResult>(rt.Encoding)).ToArray()));
        public StreamsXInfoConsumersResult[] XInfoConsumers(string key, string group) => Call("XINFO".SubCommand("CONSUMERS").Input(key, group).FlagKey(key), rt => rt.ThrowOrValue((a, _) => a.Select(x => (x as object[]).MapToClass<StreamsXInfoConsumersResult>(rt.Encoding)).ToArray()));

        public long XLen(string key) => Call("XLEN".Input(key).FlagKey(key), rt => rt.ThrowOrValue<long>());

        public StreamsXPendingResult XPending(string key, string group) => Call("XPENDING".Input(key, group).FlagKey(key), rt => rt.ThrowOrValueToXPending());
        public StreamsXPendingConsumerResult[] XPending(string key, string group, string start, string end, long count, string consumer = null) => Call("XPENDING"
            .Input(key, group)
            .Input(start, end, count)
            .InputIf(!string.IsNullOrWhiteSpace(consumer), consumer)
            .FlagKey(key), rt => rt.ThrowOrValueToXPendingConsumer());

        public StreamsEntry[] XRange(string key, string start, string end, long count = -1) => Call("XRANGE"
            .Input(key, string.IsNullOrEmpty(start) ? "-" : start, string.IsNullOrEmpty(end) ? "+" : end)
            .InputIf(count > 0, "COUNT", count)
            .FlagKey(key), rt => rt.ThrowOrValueToStreamsEntryArray());
        public StreamsEntry[] XRevRange(string key, string end, string start, long count = -1) => Call("XREVRANGE"
            .Input(key, string.IsNullOrEmpty(end) ? "+" : end, string.IsNullOrEmpty(start) ? "-" : start)
            .InputIf(count > 0, "COUNT", count)
            .FlagKey(key), rt => rt.ThrowOrValueToStreamsEntryArray());

        public StreamsEntry XRead(long block, string key, string id) => XRead(1, block, key, id)?.FirstOrDefault()?.entries?.FirstOrDefault();
        public StreamsEntryResult[] XRead(long count, long block, string key, string id, params string[] keyIds)
        {
            var kis = keyIds.MapToHash<string>(Encoding.UTF8);
            var kikeys = kis.Keys.ToArray();
            return Call("XREAD".SubCommand(null)
                .InputIf(count != 0, "COUNT", count)
                .InputIf(block > 0, "BLOCK", block)
                .Input("STREAMS", key)
                .InputIf(kikeys.Any(), kikeys)
                .Input(id)
                .InputIf(kikeys.Any(), kis.Values.ToArray())
                .FlagKey(key).FlagKey(kikeys), rt => rt.ThrowOrValueToXRead());
        }
        public StreamsEntryResult[] XRead(long count, long block, Dictionary<string, string> keyIds)
        {
            var kikeys = keyIds.Keys.ToArray();
            return Call("XREAD".SubCommand(null)
                .InputIf(count != 0, "COUNT", count)
                .InputIf(block > 0, "BLOCK", block)
                .InputRaw("STREAMS")
                .Input(kikeys)
                .Input(keyIds.Values.ToArray())
                .FlagKey(kikeys), rt => rt.ThrowOrValueToXRead());
        }

        public StreamsEntry XReadGroup(string group, string consumer, long block, string key, string id) => XReadGroup(group, consumer, 1, block, false, key, id)?.FirstOrDefault()?.entries?.First();
        public StreamsEntryResult[] XReadGroup(string group, string consumer, long count, long block, bool noack, string key, string id, params string[] keyIds) {
            var kis = keyIds.MapToHash<string>(Encoding.UTF8);
            var kikeys = kis.Keys.ToArray();
            return Call("XREADGROUP"
                .Input("GROUP", group, consumer)
                .InputIf(count != 0, "COUNT", count)
                .InputIf(block > 0, "BLOCK", block)
                .InputIf(noack, "NOACK")
                .Input("STREAMS", key)
                .InputIf(kikeys.Any(), kikeys)
                .Input(id)
                .InputIf(kikeys.Any(), kis.Values.ToArray())
                .FlagKey(key).FlagKey(kikeys), rt => rt.ThrowOrValueToXRead());
        }
        public StreamsEntryResult[] XReadGroup(string group, string consumer, long count, long block, bool noack, Dictionary<string, string> keyIds)
        {
            var kikeys = keyIds.Keys.ToArray();
            return Call("XREADGROUP"
                .Input("GROUP", group, consumer)
                .InputIf(count != 0, "COUNT", count)
                .InputIf(block > 0, "BLOCK", block)
                .InputIf(noack, "NOACK")
                .InputRaw("STREAMS")
                .Input(kikeys)
                .Input(keyIds.Values.ToArray())
                .FlagKey(kikeys), rt => rt.ThrowOrValueToXRead());
        }

        public long XTrim(string key, long count) => Call("XTRIM"
            .Input(key)
            .InputIf(count > 0, "MAXLEN", count)
            .InputIf(count < 0, "MAXLEN", "~", Math.Abs(count))
            .FlagKey(key), rt => rt.ThrowOrValue<long>());
    }
}
