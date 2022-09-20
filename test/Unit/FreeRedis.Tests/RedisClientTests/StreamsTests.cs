using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests
{
    public class StreamsTests : TestBase
    {

        [Fact]
        public void Issues457()
        {
            var redis = cli;
            var key = "key_Issues457";
            var group = "group_Issues457";
            var consumer = "consumer_Issues457";
            var maxLen = 9999;

            //删除，重新创建，并加入数据，进行测试
            redis.Del(key);
            redis.XGroupCreate(key, group, "0", true);
            redis.XAdd(key, maxLen, "*", "__data", "my data1");
            redis.XAdd(key, maxLen, "*", "__data", "my data2");

            //检查pending表的长度
            //!!!!!!pending表不存在时，读取会报错!!!!!!!!!
            var pending0 = redis.XPending(key, group);
            //消费确认前，pending 应该等于0
            Assert.True(pending0.count == 0);

            //读取未阅读的消息1,读取2次
            var new1 = redis.XReadGroup(group, consumer, 1, 1, false, key, ">");
            var new2 = redis.XReadGroup(group, consumer, 1, 1, false, key, ">");
            Assert.NotNull(new1[0].entries);
            Assert.NotEmpty(new1[0].entries);
            Assert.NotNull(new2[0].entries);
            Assert.NotEmpty(new2[0].entries);

            //检查pending表的长度
            var pending = redis.XPending(key, group);
            //消费确认前，pending 应该等于2
            Assert.True(pending.count == 2);

            //消费确认
            var id1 = new1[0].entries[0].id;
            var id2 = new2[0].entries[0].id;
            redis.XAck(key, group, id1);
            redis.XAck(key, group, id2);

            //检查pending表的长度
            //!!!!!!pending表不存在时，读取会报错!!!!!!!!!
            var pending2 = redis.XPending(key, group);
            //消费确认后，pending 应该等于0
            //Assert.True(pending2.count == 0);
        }

        [Fact]
        public void XAck()
        {
            var key1 = "XAck1";
            cli.Del(key1);
            var id1 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            var id2 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1", ["f2"] = "v2" });
            cli.XGroupCreate(key1, "xack-group1", "0");

            var r2 = cli.XReadGroup("xack-group1", "xack-consumer", 2, 1, false, key1, ">");
            Assert.NotNull(r2);
            Assert.Single(r2);
            Assert.Equal(2, r2[0].entries.Length);
            Assert.Equal(id1, r2[0].entries[0].id);
            Assert.Equal(id2, r2[0].entries[1].id);
            var r2ids = r2.Select(a => a.entries.Select(b => b.id)).SelectMany(a => a).ToArray();

            var r3 = cli.XAck(key1, "xack-group1", r2ids);
            Assert.Equal(2, r3);

            var r4 = cli.XReadGroup("xack-group1", "xack-consumer", 1, key1, "0-0");
            Assert.Null(r4);
        }

        [Fact]
        public void XAdd()
        {
            var key1 = "XAdd1";
            var key2 = "XAdd2";
            var key3 = "XAdd3";

            cli.Del(key1, key2, key3);
            Assert.Equal("ERR wrong number of arguments for 'xadd' command", Assert.Throws<RedisServerException>(() => cli.XAdd(key1, new Dictionary<string, object>()))?.Message);
            Assert.NotNull(cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" }));
            Assert.NotNull(cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1", ["f2"] = "v2" }));
            Assert.NotNull(cli.XAdd(key2, 1000, "123321", new Dictionary<string, object> { ["f11"] = "v11" }));
            Assert.NotNull(cli.XAdd(key2, 1000, "123322", new Dictionary<string, object> { ["f11"] = "v11", ["f22"] = "v22" }));
            Assert.NotNull(cli.XAdd(key3, -1000, "1233211", new Dictionary<string, object> { ["f111"] = "v111" }));
            Assert.NotNull(cli.XAdd(key3, -1000, "1233222", new Dictionary<string, object> { ["f111"] = "v111", ["f222"] = "v222" }));

            cli.Del(key1, key2, key3);
            Assert.NotNull(cli.XAdd(key1, "f1", "v1"));
            Assert.NotNull(cli.XAdd(key1, "f1", "v1", "f2", "v2"));
            Assert.NotNull(cli.XAdd(key2, 1000, "123321", "f11", "v11"));
            Assert.NotNull(cli.XAdd(key2, 1000, "123322", "f11", "v11", "f22", "v22"));
            Assert.NotNull(cli.XAdd(key3, -1000, "1233211", "f111", "v111"));
            Assert.NotNull(cli.XAdd(key3, -1000, "1233222", "f111", "v111", "f222", "v222"));
        }

        [Fact]
        public void XClaim()
        {
            var key1 = "XClaim1";
            var key2 = "XClaim2";
            var key3 = "XClaim3";

            cli.Del(key1, key2, key3);
            var id1 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            cli.XGroupCreate(key1, "XClaim-group1", "0");

            var r2 = cli.XReadGroup("XClaim-group1", "XClaim-consumer1", 1, 1, false, key1, ">");
            Assert.NotNull(r2);
            Assert.Single(r2);
            Assert.Single(r2[0].entries);
            Assert.Equal(id1, r2[0].entries[0].id);

            // Get the pending event
            var r3 = cli.XPending(key1, "XClaim-group1");
            Assert.NotNull(r3);
            Assert.Equal(id1, r3.maxId);
            Assert.Equal(id1, r3.minId);
            Assert.Equal(1, r3.count);
            Assert.Single(r3.consumers);
            Assert.Equal(1, r3.consumers[0].count);
            Assert.Equal("XClaim-consumer1", r3.consumers[0].consumer);

            var r4 = cli.XPending(key1, "XClaim-group1", "-", "+", 3, "XClaim-consumer1");
            Assert.Single(r4);
            Assert.Equal(id1, r4[0].id);
            Assert.Equal("XClaim-consumer1", r4[0].consumer);
            Assert.Equal(1, r4[0].deliveredTimes);

            // Sleep for 1000ms so we can claim events pending for more than 500ms
            Thread.Sleep(1000);

            var r5 = cli.XClaim(key1, "XClaim-group1", "XClaim-consumer2", 500, id1);
            Assert.Single(r5);
            Assert.Equal(id1, r5[0].id);

            // Deleted events should return as null on XClaim 
            Assert.Equal(1, cli.XDel(key1, id1));
            var r6 = cli.XClaim(key1, "XClaim-group1", "XClaim-consumer2", 500, id1);
            Assert.Empty(r6);

            var r7 = cli.XGroupDelConsumer(key1, "XClaim-group1", "XClaim-consumer2");
            Assert.Equal(1, r7);
        }

        [Fact]
        public void XClaimJustId()
        {
            var key1 = "XClaimJustId1";
            var key2 = "XClaimJustId2";
            var key3 = "XClaimJustId3";

            cli.Del(key1, key2, key3);
            var id1 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            cli.XGroupCreate(key1, "XClaimJustId-group1", "0");

            var r2 = cli.XReadGroup("XClaimJustId-group1", "XClaimJustId-consumer1", 1, 1, false, key1, ">");
            Assert.NotNull(r2);
            Assert.Single(r2);
            Assert.Single(r2[0].entries);
            Assert.Equal(id1, r2[0].entries[0].id);

            // Get the pending event
            var r3 = cli.XPending(key1, "XClaimJustId-group1");
            Assert.NotNull(r3);
            Assert.Equal(id1, r3.maxId);
            Assert.Equal(id1, r3.minId);
            Assert.Equal(1, r3.count);
            Assert.Single(r3.consumers);
            Assert.Equal(1, r3.consumers[0].count);
            Assert.Equal("XClaimJustId-consumer1", r3.consumers[0].consumer);

            var r4 = cli.XPending(key1, "XClaimJustId-group1", "-", "+", 3, "XClaimJustId-consumer1");
            Assert.Single(r4);
            Assert.Equal(id1, r4[0].id);
            Assert.Equal("XClaimJustId-consumer1", r4[0].consumer);
            Assert.Equal(1, r4[0].deliveredTimes);

            // Sleep for 1000ms so we can claim events pending for more than 500ms
            Thread.Sleep(1000);

            var r5 = cli.XClaimJustId(key1, "XClaimJustId-group1", "XClaimJustId-consumer2", 500, id1);
            Assert.Single(r5);
            Assert.Equal(id1, r5[0]);

            // Deleted events should return as null on XClaim 
            Assert.Equal(1, cli.XDel(key1, id1));
            var r6 = cli.XClaimJustId(key1, "XClaimJustId-group1", "XClaimJustId-consumer2", 500, id1);
            Assert.Empty(r6);

            var r66 = cli.XClaim(key1, "XClaimJustId-group1", "XClaimJustId-consumer2", 500, id1);

            var r7 = cli.XGroupDelConsumer(key1, "XClaimJustId-group1", "XClaimJustId-consumer2");
            Assert.Equal(1, r7);
        }

        [Fact]
        public void XDel()
        {
            var key1 = "XDel1";
            cli.Del(key1);

            Assert.Equal(0, cli.XDel(key1, "1603636512916-0"));
            Assert.Equal(0, cli.XDel(key1, "1603636512916-0", "1603636512911-0"));
        }

        [Fact]
        public void XGroupCreate()
        {
            var key1 = "XGroupCreate1";
            cli.Del(key1);

            cli.XGroupCreate(key1, "XGroupCreate-group1", "0", true);
            Assert.True(cli.XGroupDestroy(key1, "XGroupCreate-group1"));
        }

        [Fact]
        public void XGroupSetId()
        {
            var key1 = "XGroupSetId1";
            cli.Del(key1);

            cli.XGroupCreate(key1, "XGroupSetId-group1", "0", true);
            cli.XGroupSetId(key1, "XGroupSetId-group1", "$");
            Assert.True(cli.XGroupDestroy(key1, "XGroupSetId-group1"));
        }

        [Fact]
        public void XGroupDestroy()
        {
            var key1 = "XGroupDestroy1";
            cli.Del(key1);

            cli.XGroupCreate(key1, "XGroupDestroy-group1", "0", true);
            Assert.True(cli.XGroupDestroy(key1, "XGroupDestroy-group1"));
        }

        //[Fact]
        //public void XGroupCreateConsumer()
        //{
        //    var key1 = "XGroupCreateConsumer1";
        //    cli.Del(key1);

        //    cli.XGroupCreate(key1, "XGroupCreateConsumer-group1", "0", true);
        //    cli.XGroupCreateConsumer(key1, "XGroupCreateConsumer-group1", "XGroupCreateConsumer-consumer1");

        //    Assert.True(cli.XGroupDestroy(key1, "XGroupCreateConsumer-group1"));
        //}

        [Fact]
        public void XGroupDelConsumer()
        {
            var key1 = "XGroupDelConsumer1";

            cli.Del(key1);
            var id1 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            cli.XGroupCreate(key1, "XGroupDelConsumer-group1", "0");

            var r2 = cli.XReadGroup("XGroupDelConsumer-group1", "XGroupDelConsumer-consumer1", 1, 1, false, key1, ">");
            Assert.NotNull(r2);
            Assert.Single(r2);
            Assert.Single(r2[0].entries);
            Assert.Equal(id1, r2[0].entries[0].id);

            var r7 = cli.XGroupDelConsumer(key1, "XGroupDelConsumer-group1", "XGroupDelConsumer-consumer1");
            Assert.Equal(1, r7);
        }

        [Fact]
        public void XInfoStream()
        {
            var key1 = "XInfoStream1";

            cli.Del(key1);
            var id1 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            cli.XGroupCreate(key1, "XInfoStream1-group1", "0");
            var r1 = cli.XInfoStream(key1);
            Assert.NotNull(r1);
            Assert.Equal(1, r1.length);
            Assert.Equal(1, r1.radix_tree_keys);
            Assert.Equal(2, r1.radix_tree_nodes);
            Assert.Equal(id1, r1.last_generated_id);
            Assert.Equal(1, r1.groups);

            Assert.NotNull(r1.first_entry);
            Assert.Equal(id1, r1.first_entry.id);
            Assert.Equal("f1", r1.first_entry.fieldValues[0]?.ToString());
            Assert.Equal("v1", r1.first_entry.fieldValues[1]?.ToString()); 
            
            Assert.NotNull(r1.last_entry);
            Assert.Equal(id1, r1.last_entry.id);
            Assert.Equal("f1", r1.last_entry.fieldValues[0]?.ToString());
            Assert.Equal("v1", r1.last_entry.fieldValues[1]?.ToString());
            Assert.True(cli.XGroupDestroy(key1, "XInfoStream1-group1"));

            cli.Del(key1);
            cli.XGroupCreate(key1, "XInfoStream1-group1", "0", true);
            var r2 = cli.XInfoStream(key1);
            Assert.NotNull(r2);
            Assert.Equal(0, r2.length);
            Assert.Equal(0, r2.radix_tree_keys);
            Assert.Equal(1, r2.radix_tree_nodes);
            Assert.Equal("0-0", r2.last_generated_id);
            Assert.Equal(1, r2.groups);
            Assert.Null(r2.first_entry);
            Assert.Null(r2.last_entry);
            Assert.True(cli.XGroupDestroy(key1, "XInfoStream1-group1"));
        }

        [Fact]
        public void XInfoGroups()
        {
            var key1 = "XInfoGroups1";

            cli.Del(key1);
            var id1 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            cli.XGroupCreate(key1, "XInfoGroups1-group1", "0");

            var r1 = cli.XReadGroup("XInfoGroups1-group1", "XInfoGroups1-consumer1", 1, 1, false, key1, ">");
            Assert.NotNull(r1);
            Assert.Single(r1);
            Assert.Single(r1[0].entries);
            Assert.Equal(id1, r1[0].entries[0].id);

            var r2 = cli.XInfoGroups(key1);
            Assert.Single(r2);
            Assert.Equal("XInfoGroups1-group1", r2[0].name);
            Assert.Equal(1, r2[0].consumers);
            Assert.Equal(1, r2[0].pending);
            Assert.Equal(id1, r2[0].last_delivered_id);
            Assert.True(cli.XGroupDestroy(key1, "XInfoGroups1-group1"));

            cli.Del(key1);
            cli.XGroupCreate(key1, "XInfoGroups1-group1", "0", true);
            var r3 = cli.XInfoGroups(key1);
            Assert.Single(r3);
            Assert.Equal("XInfoGroups1-group1", r3[0].name);
            Assert.Equal(0, r3[0].consumers);
            Assert.Equal(0, r3[0].pending);
            Assert.Equal("0-0", r3[0].last_delivered_id);
            Assert.True(cli.XGroupDestroy(key1, "XInfoGroups1-group1"));
        }

        [Fact]
        public void XInfoConsumers()
        {
            var key1 = "XInfoConsumers1";

            cli.Del(key1);
            var id1 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            cli.XGroupCreate(key1, "XInfoConsumers1-group1", "0");

            var r1 = cli.XReadGroup("XInfoConsumers1-group1", "XInfoConsumers1-consumer1", 1, 1, false, key1, ">");
            Assert.NotNull(r1);
            Assert.Single(r1);
            Assert.Single(r1[0].entries);
            Assert.Equal(id1, r1[0].entries[0].id);

            var r2 = cli.XInfoConsumers(key1, "XInfoConsumers1-group1");
            Assert.Single(r2);
            Assert.Equal("XInfoConsumers1-consumer1", r2[0].name);
            Assert.Equal(1, r2[0].pending);
            Assert.True(r2[0].idle > 0);
            Assert.True(cli.XGroupDestroy(key1, "XInfoConsumers1-group1"));

            cli.Del(key1);
            cli.XGroupCreate(key1, "XInfoConsumers1-group1", "0", true);
            var r3 = cli.XInfoConsumers(key1, "XInfoConsumers1-group1");
            Assert.Empty(r3);
            Assert.True(cli.XGroupDestroy(key1, "XInfoConsumers1-group1"));
        }

        [Fact]
        public void XInfoStreamFull()
        {
            var key1 = "XInfoStreamFull1";

            cli.Del(key1);
            var id1 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            cli.XGroupCreate(key1, "XInfoStreamFull1-group1", "0");

            var r1 = cli.XReadGroup("XInfoStreamFull1-group1", "XInfoStreamFull1-consumer1", 1, 1, false, key1, ">");
            Assert.NotNull(r1);
            Assert.Single(r1);
            Assert.Single(r1[0].entries);
            Assert.Equal(id1, r1[0].entries[0].id);

            var r2 = cli.XInfoStreamFull(key1, 11);
            
            Assert.True(cli.XGroupDestroy(key1, "XInfoStreamFull1-group1"));
        }

        [Fact]
        public void XLen()
        {
            var key1 = "XLen1";
            cli.Del(key1); 
            cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });

            Assert.Equal(3, cli.XLen(key1));
        }

        [Fact]
        public void XPending()
        {
            var key1 = "XPending1";
            var key2 = "XPending2";
            var key3 = "XPending3";

            cli.Del(key1, key2, key3);
            var id1 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            cli.XGroupCreate(key1, "XPending-group1", "0");

            var r2 = cli.XReadGroup("XPending-group1", "XPending-consumer1", 1, 1, false, key1, ">");
            Assert.NotNull(r2);
            Assert.Single(r2);
            Assert.Single(r2[0].entries);
            Assert.Equal(id1, r2[0].entries[0].id);

            // Get the pending event
            var r3 = cli.XPending(key1, "XPending-group1");
            Assert.NotNull(r3);
            Assert.Equal(id1, r3.maxId);
            Assert.Equal(id1, r3.minId);
            Assert.Equal(1, r3.count);
            Assert.Single(r3.consumers);
            Assert.Equal(1, r3.consumers[0].count);
            Assert.Equal("XPending-consumer1", r3.consumers[0].consumer);

            var r4 = cli.XPending(key1, "XPending-group1", "-", "+", 3, "XPending-consumer1");
            Assert.Single(r4);
            Assert.Equal(id1, r4[0].id);
            Assert.Equal("XPending-consumer1", r4[0].consumer);
            Assert.Equal(1, r4[0].deliveredTimes);

            // Sleep for 1000ms so we can claim events pending for more than 500ms
            Thread.Sleep(1000);
        }

        [Fact]
        public void XRange()
        {
            var key1 = "XRange1";
            cli.Del(key1);
            
            var r1 = cli.XRange(key1, null, null);
            Assert.Empty(r1);

            var id1 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            var id2 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });

            var r2 = cli.XRange(key1, null, null, 3);
            Assert.Equal(2, r2.Length);
            Assert.Equal(id1, r2[0].id);

            var r3 = cli.XRange(key1, id1, null, 3);
            Assert.Equal(2, r3.Length);
            Assert.Equal(id1, r3[0].id);

            var r4 = cli.XRange(key1, id1, id2, 1);
            Assert.Single(r4);
            Assert.Equal(id1, r4[0].id);

            var r5 = cli.XRange(key1, id1, id2, 2);
            Assert.Equal(2, r5.Length);
            Assert.Equal(id1, r5[0].id);

            var r6 = cli.XRange(key1, id2, null, 4);
            Assert.Single(r6);
            Assert.Equal(id2, r6[0].id);

            var id3 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            var r7 = cli.XRange(key1, id2, id2, 4);
            Assert.Single(r7);
            Assert.Equal(id2, r7[0].id);
        }

        [Fact]
        public void XRevRange()
        {
            var key1 = "XRevRange1";
            cli.Del(key1);

            var r1 = cli.XRevRange(key1, null, null);
            Assert.Empty(r1);

            var id1 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            var id2 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });

            var r2 = cli.XRevRange(key1, null, null, 3);
            Assert.Equal(2, r2.Length);
            Assert.Equal(id2, r2[0].id);

            var r3 = cli.XRevRange(key1, id2, null, 3);
            Assert.Equal(2, r3.Length);
            Assert.Equal(id2, r3[0].id);

            var r4 = cli.XRevRange(key1, id2, id1, 1);
            Assert.Single(r4);
            Assert.Equal(id2, r4[0].id);

            var r5 = cli.XRevRange(key1, id2, id1, 2);
            Assert.Equal(2, r5.Length);
            Assert.Equal(id2, r5[0].id);

            var r6 = cli.XRevRange(key1, id1, null, 4);
            Assert.Single(r6);
            Assert.Equal(id1, r6[0].id);

            var id3 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            var r7 = cli.XRevRange(key1, id1, id1, 4);
            Assert.Single(r7);
            Assert.Equal(id1, r7[0].id);
        }

        [Fact]
        public void XRead()
        {
            var key1 = "XRead1";
            cli.Del(key1);

            // Empty Stream
            Assert.Null(cli.XRead(0, key1, "0"));
            Assert.Empty(cli.XRead(1, 0, key1, "0"));

            var id1 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            var id2 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });

            // Read only a single Stream
            var r1 = cli.XRead(0, key1, "0");
            Assert.NotNull(r1);
            Assert.Equal(id1, r1.id);

            var r2 = cli.XRead(1, 0, key1, "0");
            Assert.Single(r2);
            Assert.Single(r2[0].entries);
            Assert.Equal(id1, r2[0].entries[0].id);

            // Read from two Streams
            var r3 = cli.XRead(2, 0, key1, "0");
            Assert.Single(r3);
            Assert.Equal(2, r3[0].entries.Length);
            Assert.Equal(id1, r3[0].entries[0].id);
            Assert.Equal(id2, r3[0].entries[1].id);
        }

        [Fact]
        public void XReadGroup()
        {
            var key1 = "XReadGroup1";
            cli.Del(key1);

            var id1 = cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            cli.XGroupCreate(key1, "XReadGroup-group1", "0");

            var r2 = cli.XReadGroup("XReadGroup-group1", "XReadGroup-consumer1", 1, 1, false, key1, ">");
            Assert.NotNull(r2);
            Assert.Single(r2);
            Assert.Single(r2[0].entries);
            Assert.Equal(id1, r2[0].entries[0].id);
        }

        [Fact]
        public void XTrim()
        {
            var key1 = "XTrim1";
            cli.Del(key1);
            cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });
            cli.XAdd(key1, new Dictionary<string, object> { ["f1"] = "v1" });

            Assert.Equal(5, cli.XLen(key1));
            Assert.Equal(2, cli.XTrim(key1, 3));
            Assert.Equal(3, cli.XLen(key1));
        }
    }
}
