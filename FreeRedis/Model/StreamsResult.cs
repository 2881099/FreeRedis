using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeRedis.Model
{
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
}
