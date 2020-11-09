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
        public Task<long> XAckAsync(string key, string group, params string[] id) => CallAsync("XACK".InputKey(key, group).Input(id), rt => rt.ThrowOrValue<long>());

        public Task<string> XAddAsync<T>(string key, string field, T value, params object[] fieldValues) => XAddAsync(key, 0, "*", field, value, fieldValues);
        public Task<string> XAddAsync<T>(string key, long maxlen, string id, string field, T value, params object[] fieldValues) => CallAsync("XADD"
            .InputKey(key)
            .InputIf(maxlen > 0, "MAXLEN", maxlen)
            .InputIf(maxlen < 0, "MAXLEN", "~", Math.Abs(maxlen))
            .Input(string.IsNullOrEmpty(id) ? "*" : id)
            .InputRaw(field).InputRaw(SerializeRedisValue(value))
            .InputKv(fieldValues, false, SerializeRedisValue), rt => rt.ThrowOrValue<string>());
        public Task<string> XAddAsync<T>(string key, Dictionary<string, T> fieldValues) => XAddAsync(key, 0, "*", fieldValues);
        public Task<string> XAddAsync<T>(string key, long maxlen, string id, Dictionary<string, T> fieldValues) => CallAsync("XADD"
            .InputKey(key)
            .InputIf(maxlen > 0, "MAXLEN", maxlen)
            .InputIf(maxlen < 0, "MAXLEN", "~", Math.Abs(maxlen))
            .Input(string.IsNullOrEmpty(id) ? "*" : id)
            .InputKv(fieldValues, false, SerializeRedisValue), rt => rt.ThrowOrValue<string>());

        public Task<StreamsEntry[]> XClaimAsync(string key, string group, string consumer, long minIdleTime, params string[] id) => CallAsync("XCLAIM"
            .InputKey(key, group, consumer)
            .Input(minIdleTime, id), rt => rt.ThrowOrValueToStreamsEntryArray());
        public Task<StreamsEntry[]> XClaimAsync(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => CallAsync("XCLAIM"
            .InputKey(key, group, consumer)
            .Input(minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
            .InputIf(force, "FORCE"), rt => rt.ThrowOrValueToStreamsEntryArray());

        public Task<string[]> XClaimJustIdAsync(string key, string group, string consumer, long minIdleTime, params string[] id) => CallAsync("XCLAIM"
            .InputKey(key, group, consumer)
            .Input(minIdleTime, id, "JUSTID"), rt => rt.ThrowOrValue<string[]>());
        public Task<string[]> XClaimJustIdAsync(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => CallAsync("XCLAIM"
            .InputKey(key, group, consumer)
            .Input(minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
            .InputIf(force, "FORCE")
            .Input("JUSTID"), rt => rt.ThrowOrValue<string[]>());

        public Task<long> XDelAsync(string key, params string[] id) => CallAsync("XDEL".InputKey(key, id), rt => rt.ThrowOrValue<long>());

        public Task XGroupCreateAsync(string key, string group, string id = "$", bool MkStream = false) => CallAsync("XGROUP"
            .SubCommand("CREATE")
            .InputKey(key)
            .Input(group, id)
            .InputIf(MkStream, "MKSTREAM"), rt => rt.ThrowOrNothing());
        public Task XGroupSetIdAsync(string key, string group, string id = "$") => CallAsync("XGROUP".SubCommand("SETID").InputKey(key).Input(group, id), rt => rt.ThrowOrNothing());
        public Task<bool> XGroupDestroyAsync(string key, string group) => CallAsync("XGROUP".SubCommand("DESTROY").InputKey(key).InputRaw(group), rt => rt.ThrowOrValue<bool>());
        public Task XGroupCreateConsumerAsync(string key, string group, string consumer) => CallAsync("XGROUP".SubCommand("CREATECONSUMER").InputKey(key).Input(group, consumer), rt => rt.ThrowOrNothing());
        public Task<long> XGroupDelConsumerAsync(string key, string group, string consumer) => CallAsync("XGROUP".SubCommand("DELCONSUMER").InputKey(key).Input(group, consumer), rt => rt.ThrowOrValue<long>());

        public Task<StreamsXInfoStreamResult> XInfoStreamAsync(string key) => CallAsync("XINFO".SubCommand("STREAM").InputKey(key), rt => rt.ThrowOrValueToXInfoStream());
        public Task<StreamsXInfoStreamFullResult> XInfoStreamFullAsync(string key, long count = 10) => CallAsync("XINFO"
            .SubCommand("STREAM")
            .InputKey(key)
            .Input("FULL")
            .InputIf(count != 10, "COUNT", count), rt => rt.ThrowOrValueToXInfoStreamFullResult());
        public Task<StreamsXInfoGroupsResult[]> XInfoGroupsAsync(string key) => CallAsync("XINFO".SubCommand("GROUPS").InputKey(key), rt => rt.ThrowOrValue((a, _) => a.Select(x => (x as object[]).MapToClass<StreamsXInfoGroupsResult>(rt.Encoding)).ToArray()));
        public Task<StreamsXInfoConsumersResult[]> XInfoConsumersAsync(string key, string group) => CallAsync("XINFO".SubCommand("CONSUMERS").InputKey(key).InputRaw(group), rt => rt.ThrowOrValue((a, _) => a.Select(x => (x as object[]).MapToClass<StreamsXInfoConsumersResult>(rt.Encoding)).ToArray()));

        public Task<long> XLenAsync(string key) => CallAsync("XLEN".InputKey(key), rt => rt.ThrowOrValue<long>());

        public Task<StreamsXPendingResult> XPendingAsync(string key, string group) => CallAsync("XPENDING".InputKey(key, group), rt => rt.ThrowOrValueToXPending());
        public Task<StreamsXPendingConsumerResult[]> XPendingAsync(string key, string group, string start, string end, long count, string consumer = null) => CallAsync("XPENDING"
            .InputKey(key, group)
            .Input(start, end, count)
            .InputIf(!string.IsNullOrWhiteSpace(consumer), consumer), rt => rt.ThrowOrValueToXPendingConsumer());

        public Task<StreamsEntry[]> XRangeAsync(string key, string start, string end, long count = -1) => CallAsync("XRANGE"
            .InputKey(key, string.IsNullOrEmpty(start) ? "-" : start, string.IsNullOrEmpty(end) ? "+" : end)
            .InputIf(count > 0, "COUNT", count), rt => rt.ThrowOrValueToStreamsEntryArray());
        public Task<StreamsEntry[]> XRevRangeAsync(string key, string end, string start, long count = -1) => CallAsync("XREVRANGE"
            .InputKey(key, string.IsNullOrEmpty(end) ? "+" : end, string.IsNullOrEmpty(start) ? "-" : start)
            .InputIf(count > 0, "COUNT", count), rt => rt.ThrowOrValueToStreamsEntryArray());

        async public Task<StreamsEntry> XReadAsync(long block, string key, string id) => (await XReadAsync(1, block, key, id))?.FirstOrDefault()?.entries?.FirstOrDefault();
        public Task<StreamsEntryResult[]> XReadAsync(long count, long block, string key, string id, params string[] keyIds)
        {
            var kis = keyIds.MapToHash<string>(Encoding.UTF8);
            var kikeys = kis.Keys.ToArray();
            return CallAsync("XREAD".SubCommand(null)
                .InputIf(count != 0, "COUNT", count)
                .InputIf(block > 0, "BLOCK", block)
                .InputRaw("STREAMS")
                .InputKey(key)
                .InputKeyIf(kikeys.Any(), kikeys)
                .Input(id)
                .InputIf(kikeys.Any(), kis.Values.ToArray()), rt => rt.ThrowOrValueToXRead());
        }
        public Task<StreamsEntryResult[]> XReadAsync(long count, long block, Dictionary<string, string> keyIds)
        {
            var kikeys = keyIds.Keys.ToArray();
            return CallAsync("XREAD".SubCommand(null)
                .InputIf(count != 0, "COUNT", count)
                .InputIf(block > 0, "BLOCK", block)
                .InputRaw("STREAMS")
                .InputKey(kikeys)
                .Input(keyIds.Values.ToArray()), rt => rt.ThrowOrValueToXRead());
        }

        async public Task<StreamsEntry> XReadGroupAsync(string group, string consumer, long block, string key, string id) => (await XReadGroupAsync(group, consumer, 1, block, false, key, id))?.FirstOrDefault()?.entries?.First();
        public Task<StreamsEntryResult[]> XReadGroupAsync(string group, string consumer, long count, long block, bool noack, string key, string id, params string[] keyIds)
        {
            var kis = keyIds.MapToHash<string>(Encoding.UTF8);
            var kikeys = kis.Keys.ToArray();
            return CallAsync("XREADGROUP"
                .Input("GROUP", group, consumer)
                .InputIf(count != 0, "COUNT", count)
                .InputIf(block > 0, "BLOCK", block)
                .InputIf(noack, "NOACK")
                .InputRaw("STREAMS")
                .InputKey(key)
                .InputKeyIf(kikeys.Any(), kikeys)
                .Input(id)
                .InputIf(kikeys.Any(), kis.Values.ToArray()), rt => rt.ThrowOrValueToXRead());
        }
        public Task<StreamsEntryResult[]> XReadGroupAsync(string group, string consumer, long count, long block, bool noack, Dictionary<string, string> keyIds)
        {
            var kikeys = keyIds.Keys.ToArray();
            return CallAsync("XREADGROUP"
                .Input("GROUP", group, consumer)
                .InputIf(count != 0, "COUNT", count)
                .InputIf(block > 0, "BLOCK", block)
                .InputIf(noack, "NOACK")
                .InputRaw("STREAMS")
                .InputKey(kikeys)
                .Input(keyIds.Values.ToArray()), rt => rt.ThrowOrValueToXRead());
        }

        public Task<long> XTrimAsync(string key, long count) => CallAsync("XTRIM"
            .InputKey(key)
            .InputIf(count > 0, "MAXLEN", count)
            .InputIf(count < 0, "MAXLEN", "~", Math.Abs(count)), rt => rt.ThrowOrValue<long>());
        #endregion
#endif

        public long XAck(string key, string group, params string[] id) => Call("XACK".InputKey(key, group).Input(id), rt => rt.ThrowOrValue<long>());

        public string XAdd<T>(string key, string field, T value, params object[] fieldValues) => XAdd(key, 0, "*", field, value, fieldValues);
        public string XAdd<T>(string key, long maxlen, string id, string field, T value, params object[] fieldValues) => Call("XADD"
            .InputKey(key)
            .InputIf(maxlen > 0, "MAXLEN", maxlen)
            .InputIf(maxlen < 0, "MAXLEN", "~", Math.Abs(maxlen))
            .Input(string.IsNullOrEmpty(id) ? "*" : id)
            .InputRaw(field).InputRaw(SerializeRedisValue(value))
            .InputKv(fieldValues, false, SerializeRedisValue), rt => rt.ThrowOrValue<string>());
        public string XAdd<T>(string key, Dictionary<string, T> fieldValues) => XAdd(key, 0, "*", fieldValues);
        public string XAdd<T>(string key, long maxlen, string id, Dictionary<string, T> fieldValues) => Call("XADD"
            .InputKey(key)
            .InputIf(maxlen > 0, "MAXLEN", maxlen)
            .InputIf(maxlen < 0, "MAXLEN", "~", Math.Abs(maxlen))
            .Input(string.IsNullOrEmpty(id) ? "*" : id)
            .InputKv(fieldValues, false, SerializeRedisValue), rt => rt.ThrowOrValue<string>());

        public StreamsEntry[] XClaim(string key, string group, string consumer, long minIdleTime, params string[] id) => Call("XCLAIM"
            .InputKey(key, group, consumer)
            .Input(minIdleTime, id), rt => rt.ThrowOrValueToStreamsEntryArray());
        public StreamsEntry[] XClaim(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => Call("XCLAIM"
            .InputKey(key, group, consumer)
            .Input(minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
            .InputIf(force, "FORCE"), rt => rt.ThrowOrValueToStreamsEntryArray());

        public string[] XClaimJustId(string key, string group, string consumer, long minIdleTime, params string[] id) => Call("XCLAIM"
            .InputKey(key, group, consumer)
            .Input(minIdleTime, id, "JUSTID"), rt => rt.ThrowOrValue<string[]>());
        public string[] XClaimJustId(string key, string group, string consumer, long minIdleTime, string[] id, long idle, long retryCount, bool force) => Call("XCLAIM"
            .InputKey(key, group, consumer)
            .Input(minIdleTime, id, "IDLE", idle, "RETRYCOUNT", retryCount)
            .InputIf(force, "FORCE")
            .Input("JUSTID"), rt => rt.ThrowOrValue<string[]>());

        public long XDel(string key, params string[] id) => Call("XDEL".InputKey(key, id), rt => rt.ThrowOrValue<long>());

        public void XGroupCreate(string key, string group, string id = "$", bool MkStream = false) => Call("XGROUP"
            .SubCommand("CREATE")
            .InputKey(key)
            .Input(group, id)
            .InputIf(MkStream, "MKSTREAM"), rt => rt.ThrowOrNothing());
        public void XGroupSetId(string key, string group, string id = "$") => Call("XGROUP".SubCommand("SETID").InputKey(key).Input(group, id), rt => rt.ThrowOrNothing());
        public bool XGroupDestroy(string key, string group) => Call("XGROUP".SubCommand("DESTROY").InputKey(key).InputRaw(group), rt => rt.ThrowOrValue<bool>());
        public void XGroupCreateConsumer(string key, string group, string consumer) => Call("XGROUP".SubCommand("CREATECONSUMER").InputKey(key).Input(group, consumer), rt => rt.ThrowOrNothing());
        public long XGroupDelConsumer(string key, string group, string consumer) => Call("XGROUP".SubCommand("DELCONSUMER").InputKey(key).Input(group, consumer), rt => rt.ThrowOrValue<long>());

        public StreamsXInfoStreamResult XInfoStream(string key) => Call("XINFO".SubCommand("STREAM").InputKey(key), rt => rt.ThrowOrValueToXInfoStream());
        public StreamsXInfoStreamFullResult XInfoStreamFull(string key, long count = 10) => Call("XINFO"
            .SubCommand("STREAM")
            .InputKey(key)
            .Input("FULL")
            .InputIf(count != 10, "COUNT", count), rt => rt.ThrowOrValueToXInfoStreamFullResult());
        public StreamsXInfoGroupsResult[] XInfoGroups(string key) => Call("XINFO".SubCommand("GROUPS").InputKey(key), rt => rt.ThrowOrValue((a, _) => a.Select(x => (x as object[]).MapToClass<StreamsXInfoGroupsResult>(rt.Encoding)).ToArray()));
        public StreamsXInfoConsumersResult[] XInfoConsumers(string key, string group) => Call("XINFO".SubCommand("CONSUMERS").InputKey(key).InputRaw(group), rt => rt.ThrowOrValue((a, _) => a.Select(x => (x as object[]).MapToClass<StreamsXInfoConsumersResult>(rt.Encoding)).ToArray()));

        public long XLen(string key) => Call("XLEN".InputKey(key), rt => rt.ThrowOrValue<long>());

        public StreamsXPendingResult XPending(string key, string group) => Call("XPENDING".InputKey(key, group), rt => rt.ThrowOrValueToXPending());
        public StreamsXPendingConsumerResult[] XPending(string key, string group, string start, string end, long count, string consumer = null) => Call("XPENDING"
            .InputKey(key, group)
            .Input(start, end, count)
            .InputIf(!string.IsNullOrWhiteSpace(consumer), consumer), rt => rt.ThrowOrValueToXPendingConsumer());

        public StreamsEntry[] XRange(string key, string start, string end, long count = -1) => Call("XRANGE"
            .InputKey(key, string.IsNullOrEmpty(start) ? "-" : start, string.IsNullOrEmpty(end) ? "+" : end)
            .InputIf(count > 0, "COUNT", count), rt => rt.ThrowOrValueToStreamsEntryArray());
        public StreamsEntry[] XRevRange(string key, string end, string start, long count = -1) => Call("XREVRANGE"
            .InputKey(key, string.IsNullOrEmpty(end) ? "+" : end, string.IsNullOrEmpty(start) ? "-" : start)
            .InputIf(count > 0, "COUNT", count), rt => rt.ThrowOrValueToStreamsEntryArray());

        public StreamsEntry XRead(long block, string key, string id) => XRead(1, block, key, id)?.FirstOrDefault()?.entries?.FirstOrDefault();
        public StreamsEntryResult[] XRead(long count, long block, string key, string id, params string[] keyIds)
        {
            var kis = keyIds.MapToHash<string>(Encoding.UTF8);
            var kikeys = kis.Keys.ToArray();
            return Call("XREAD".SubCommand(null)
                .InputIf(count != 0, "COUNT", count)
                .InputIf(block > 0, "BLOCK", block)
                .InputRaw("STREAMS")
                .InputKey(key)
                .InputKeyIf(kikeys.Any(), kikeys)
                .Input(id)
                .InputIf(kikeys.Any(), kis.Values.ToArray()), rt => rt.ThrowOrValueToXRead());
        }
        public StreamsEntryResult[] XRead(long count, long block, Dictionary<string, string> keyIds)
        {
            var kikeys = keyIds.Keys.ToArray();
            return Call("XREAD".SubCommand(null)
                .InputIf(count != 0, "COUNT", count)
                .InputIf(block > 0, "BLOCK", block)
                .InputRaw("STREAMS")
                .InputKey(kikeys)
                .Input(keyIds.Values.ToArray()), rt => rt.ThrowOrValueToXRead());
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
                .InputRaw("STREAMS")
                .InputKey(key)
                .InputKeyIf(kikeys.Any(), kikeys)
                .Input(id)
                .InputIf(kikeys.Any(), kis.Values.ToArray()), rt => rt.ThrowOrValueToXRead());
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
                .InputKey(kikeys)
                .Input(keyIds.Values.ToArray()), rt => rt.ThrowOrValueToXRead());
        }

        public long XTrim(string key, long count) => Call("XTRIM"
            .InputKey(key)
            .InputIf(count > 0, "MAXLEN", count)
            .InputIf(count < 0, "MAXLEN", "~", Math.Abs(count)), rt => rt.ThrowOrValue<long>());
    }

    #region Model
    static partial class RedisResultThrowOrValueExtensions
    {

        public static StreamsEntry[] ThrowOrValueToStreamsEntryArray(this RedisResult rt) =>
           rt.ThrowOrValue((a, _) =>
           {
               if (a == null) return new StreamsEntry[0];
               var entries = new StreamsEntry[a.Length];
               for (var z = 0; z < a.Length; z++)
               {
                   var objs1 = a[z] as object[];
                   if (objs1 == null) continue;
                   entries[z] = new StreamsEntry { id = objs1[0].ConvertTo<string>(), fieldValues = objs1[1] as object[] };
               }
               return entries;
           });

        public static StreamsXPendingResult ThrowOrValueToXPending(this RedisResult rt) =>
           rt.ThrowOrValue((a, _) =>
           {
               if (a?.Any() != true) return null;
               var ret = new StreamsXPendingResult { count = a[0].ConvertTo<long>(), minId = a[1].ConvertTo<string>(), maxId = a[2].ConvertTo<string>() };
               var objs1 = a[3] as object[];
               ret.consumers = new StreamsXPendingResult.Consumer[objs1.Length];
               for (var z = 0; z < objs1.Length; z++)
               {
                   var objs2 = objs1[z] as object[];
                   ret.consumers[z] = new StreamsXPendingResult.Consumer { consumer = objs2[0].ConvertTo<string>(), count = objs2[1].ConvertTo<long>() };
               }
               return ret;
           });
        public static StreamsXPendingConsumerResult[] ThrowOrValueToXPendingConsumer(this RedisResult rt) =>
           rt.ThrowOrValue((a, _) =>
           {
               if (a == null) return new StreamsXPendingConsumerResult[0];
               var ret = new StreamsXPendingConsumerResult[a.Length];
               for (var z = 0; z < a.Length; z++)
               {
                   var objs1 = a[z] as object[];
                   ret[z] = new StreamsXPendingConsumerResult { id = objs1[0].ConvertTo<string>(), consumer = objs1[1].ConvertTo<string>(), idle = objs1[2].ConvertTo<long>(), deliveredTimes = objs1[3].ConvertTo<long>() };
               }
               return ret;
           });

        public static StreamsXInfoStreamResult ThrowOrValueToXInfoStream(this RedisResult rt) =>
           rt.ThrowOrValue((a, _) =>
           {
               if (a == null) return null;
               var objs1 = a[11] as object[];
               a[11] = objs1 == null ? null : new StreamsEntry { id = objs1[0].ConvertTo<string>(), fieldValues = objs1[1] as object[] };
               var objs2 = a[13] as object[];
               a[13] = objs2 == null ? null : new StreamsEntry { id = objs2[0].ConvertTo<string>(), fieldValues = objs2[1] as object[] };
               return a.MapToClass<StreamsXInfoStreamResult>(rt.Encoding);
           });
        public static StreamsXInfoStreamFullResult ThrowOrValueToXInfoStreamFullResult(this RedisResult rt) =>
           rt.ThrowOrValue((objs_full, _) =>
           {
               if (objs_full == null) return null;
               var objs_entries = objs_full[9] as object[];
               if (objs_entries != null)
               {
                   objs_full[9] = objs_entries.Select(z =>
                   {
                       var objs_entry = z as object[];
                       return objs_entry == null ? null : new StreamsEntry { id = objs_entry[0].ConvertTo<string>(), fieldValues = objs_entry[1] as object[] };
                   }).ToArray();
               }
               var objs_groups = objs_full[11] as object[];
               if (objs_groups != null)
               {
                   objs_full[11] = objs_groups.Select(z =>
                   {
                       var objs_group = z as object[];
                       if (objs_group == null) return null;
                       var objs_pendings = objs_group[7] as object[];
                       if (objs_pendings != null)
                       {
                           objs_group[7] = objs_pendings.Select(y =>
                           {
                               var objs_pending = y as object[];
                               if (objs_pending == null) return null;
                               return new StreamsXInfoStreamFullResult.Group.Pending
                               {
                                   id = objs_pending[0].ConvertTo<string>(),
                                   consumer = objs_pending[1].ConvertTo<string>(),
                                   seen_time = objs_pending[2].ConvertTo<long>(),
                                   pel_count = objs_pending[3].ConvertTo<long>()
                               };
                           }).ToArray();
                       }
                       var objs_consumers = objs_group[9] as object[];
                       if (objs_consumers != null)
                       {
                           objs_group[9] = objs_consumers.Select(y =>
                           {
                               var objs_consumer = y as object[];
                               if (objs_consumer == null) return null;
                               var objs_consumer_pendings = objs_consumer[7] as object[];
                               if (objs_consumer_pendings != null)
                               {
                                   objs_consumer[7] = objs_consumer_pendings.Select(x =>
                                   {
                                       var objs_consumer_pending = x as object[];
                                       if (objs_consumer_pending == null) return null;
                                       return new StreamsXInfoStreamFullResult.Group.Pending
                                       {
                                           id = objs_consumer_pending[0].ConvertTo<string>(),
                                           consumer = objs_consumer[1].ConvertTo<string>(),
                                           seen_time = objs_consumer_pending[1].ConvertTo<long>(),
                                           pel_count = objs_consumer_pending[2].ConvertTo<long>()
                                       };
                                   }).ToArray();
                               }
                               return objs_consumer.MapToClass<StreamsXInfoStreamFullResult.Group.Consumer>(rt.Encoding);
                           }).ToArray();
                       }
                       return objs_group.MapToClass<StreamsXInfoStreamFullResult.Group>(rt.Encoding);
                   }).ToArray();
               }
               return objs_full.MapToClass<StreamsXInfoStreamFullResult>(rt.Encoding);
           });


        public static StreamsEntryResult[] ThrowOrValueToXRead(this RedisResult rt) =>
           rt.ThrowOrValue((a, _) =>
           {
               if (a == null) return new StreamsEntryResult[0];
               var ret = new StreamsEntryResult[a.Length];
               for (var z = 0; z < a.Length; z++)
               {
                   var objs1 = a[z] as object[];
                   var objs2 = objs1[1] as object[];
                   var entries = new StreamsEntry[objs2.Length];
                   ret[z] = new StreamsEntryResult { key = objs1[0].ConvertTo<string>(), entries = entries };
                   for (var y = 0; y < objs2.Length; y++)
                   {
                       var objs3 = objs2[y] as object[];
                       entries[y] = new StreamsEntry { id = objs3[0].ConvertTo<string>(), fieldValues = objs3[1] as object[] };
                   }
               }
               return ret;
           });
    }

    public class StreamsXPendingResult
    {
        public long count;
        public string minId;
        public string maxId;
        public Consumer[] consumers;

        public override string ToString() => $"{count}, {minId}, {maxId}, {string.Join(", ", consumers?.Select(a => a.ToString()))}";

        public class Consumer
        {
            public string consumer;
            public long count;

            public override string ToString() => $"{consumer}({count})";
        }
    }
    public class StreamsXPendingConsumerResult
    {
        public string id;
        public string consumer;
        public long idle;
        public long deliveredTimes;

        public override string ToString() => $"{id}, {consumer}, {idle}, {deliveredTimes}";
    }

    public class StreamsXInfoStreamResult
    {
        public long length;
        public long radix_tree_keys;
        public long radix_tree_nodes;
        public long groups;
        public string last_generated_id;
        public StreamsEntry first_entry;
        public StreamsEntry last_entry;

        public override string ToString() => $"{length}, {radix_tree_keys}, {radix_tree_nodes}, {groups}, {last_generated_id}, {(first_entry?.ToString() ?? "NULL")}, {(last_entry?.ToString() ?? "NULL")}";
    }
    public class StreamsXInfoGroupsResult
    {
        public string name;
        public long consumers;
        public long pending;
        public string last_delivered_id;

        public override string ToString() => $"{name}, {consumers}, {pending}, {last_delivered_id}";
    }
    public class StreamsXInfoConsumersResult
    {
        public string name;
        public long pending;
        public long idle;

        public override string ToString() => $"{name}, {pending}, {idle}";
    }
    public class StreamsXInfoStreamFullResult
    {
        public long length;
        public long radix_tree_keys;
        public long radix_tree_nodes;
        public string last_generated_id;
        public StreamsEntry[] entries;
        public Group[] groups;

        public override string ToString() => $"{length}, {radix_tree_keys}, {radix_tree_nodes}, {last_generated_id}, [{string.Join("], [", entries?.Select(a => a?.ToString()))}], [{string.Join(", ", groups?.Select(a => a?.ToString()))}]";

        public class Group
        {
            public string name;
            public string last_delivered_id;
            public long pel_count;
            public Pending[] pending;
            public Consumer[] consumers;

            public override string ToString() => $"{name}, {last_delivered_id}, {pel_count}, [{string.Join("], [", pending?.Select(a => a?.ToString()))}], [{string.Join("], [", consumers?.Select(a => a?.ToString()))}]";

            public class Pending
            {
                public string id;
                public string consumer;
                public long seen_time;
                public long pel_count;

                public override string ToString() => $"{id}, {consumer}, {seen_time}, {pel_count}";
            }
            public class Consumer
            {
                public string name;
                public long seen_time;
                public long pel_count;
                public Pending[] pending;

                public override string ToString() => $"{name}, {seen_time}, {pel_count}, [{string.Join("], [", pending?.Select(a => a?.ToString()))}]";
            }
        }
    }

    public class StreamsEntryResult
    {
        public string key;
        public StreamsEntry[] entries;

        public override string ToString() => $"{key}, {string.Join("], [", entries.Select(a => a?.ToString()))}";
    }
    public class StreamsEntry
    {
        public string id;
        public object[] fieldValues;

        public override string ToString() => $"{id}, {string.Join(", ", fieldValues)}";
    }
    #endregion
}
