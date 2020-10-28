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
    public class ListsTests : TestBase
    {
        [Fact]
        public void BLPop()
        {
            Assert.Null(cli.BRPop(new[] { "TestBLPop1", "TestBLPop2" }, 1));

            new Thread(() =>
            {
                Thread.CurrentThread.Join(500);
                cli.RPush("TestBLPop1", "testv1");
            }).Start();
            Assert.Equal("testv1", cli.BRPop(new[] { "TestBLPop1", "TestBLPop2" }, 5)?.value);

            new Thread(() =>
            {
                Thread.CurrentThread.Join(500);
                cli.RPush("TestBLPop2", "testv2");
            }).Start();
            Assert.Equal("testv2", cli.BRPop(new[] { "TestBLPop1", "TestBLPop2" }, 5)?.value);
        }

        [Fact]
        public void BRPop()
        {
            Assert.Null(cli.BRPop(new[] { "TestBRPop1", "TestBRPop2" }, 1));

            new Thread(() =>
            {
                Thread.CurrentThread.Join(500);
                cli.LPush("TestBRPop1", "testv1");
            }).Start();
            Assert.Equal("testv1", cli.BRPop(new[] { "TestBRPop1", "TestBRPop2" }, 5)?.value);

            new Thread(() =>
            {
                cli.LPush("TestBRPop2", "testv2");
            }).Start();
            Assert.Equal("testv2", cli.BRPop(new[] { "TestBRPop1", "TestBRPop2" }, 5)?.value);
        }

        [Fact]
        public void BRPopLPush()
        {
        }

        [Fact]
        public void LIndex()
        {
            cli.Del("TestLIndex");
            Assert.Equal(8, cli.RPush("TestLIndex", Class, Class, Bytes, Bytes, String, String, Null, Null));

            Assert.Equal(Class.ToString(), cli.LIndex<TestClass>("TestLIndex", 0).ToString());
            Assert.Equal(Bytes, cli.LIndex<byte[]>("TestLIndex", 2));
            Assert.Equal(String, cli.LIndex("TestLIndex", 4));
            Assert.Equal("", cli.LIndex("TestLIndex", 6));
        }

        [Fact]
        public void LInsert()
        {
            cli.Del("TestLInsertBefore", "TestLInsertAfter");
            Assert.Equal(8, cli.RPush("TestLInsertBefore", Class, Class, Bytes, Bytes, String, String, Null, Null));
            Assert.Equal(9, cli.LInsert("TestLInsertBefore", InsertDirection.before, Class, "TestLInsertBefore"));
            Assert.Equal("TestLInsertBefore", cli.LIndex("TestLInsertBefore", 0));
            Assert.Equal(Class.ToString(), cli.LIndex<TestClass>("TestLInsertBefore", 1).ToString());

            Assert.Equal(8, cli.RPush("TestLInsertAfter", Class, Class, Bytes, Bytes, String, String, Null, Null));
            Assert.Equal(9, cli.LInsert("TestLInsertAfter", InsertDirection.after, Class, "TestLInsertAfter"));
            Assert.Equal("TestLInsertAfter", cli.LIndex("TestLInsertAfter", 1));
            Assert.Equal(Class.ToString(), cli.LIndex<TestClass>("TestLInsertAfter", 0).ToString());
            Assert.Equal(Class.ToString(), cli.LIndex<TestClass>("TestLInsertAfter", 2).ToString());
        }

        [Fact]
        public void LLen()
        {
            cli.Del("TestLLen");
            Assert.Equal(8, cli.RPush("TestLLen", Class, Class, Bytes, Bytes, String, String, Null, Null));

            Assert.Equal(8, cli.LLen("TestLLen"));
            cli.LTrim("TestLLen", -1, -1);
            Assert.Equal(1, cli.LLen("TestLLen"));
        }

        [Fact]
        public void LPop()
        {
            cli.Del("TestLPop");
            Assert.Equal(8, cli.LPush("TestLPop", Class, Class, Bytes, Bytes, String, String, Null, Null));
            Assert.Equal("", cli.LPop("TestLPop"));
            Assert.Equal("", cli.LPop("TestLPop"));
            Assert.Equal(String, cli.LPop("TestLPop"));
            Assert.Equal(String, cli.LPop("TestLPop"));
            Assert.Equal(Bytes, cli.LPop<byte[]>("TestLPop"));
            Assert.Equal(Bytes, cli.LPop<byte[]>("TestLPop"));
            Assert.Equal(Class.ToString(), cli.LPop<TestClass>("TestLPop").ToString());
            Assert.Equal(Class.ToString(), cli.LPop<TestClass>("TestLPop").ToString());
        }

        [Fact]
        public void LPos()
        {
        }

        [Fact]
        public void LPush()
        {
            cli.Del("TestLPush");
            Assert.Equal(8, cli.LPush("TestLPush", Class, Class, Bytes, Bytes, String, String, Null, Null));

            Assert.Equal(2, cli.LRange("TestLPush", 0, 1).Length);
            Assert.Equal("", cli.LRange("TestLPush", 0, 1)[0]);
            Assert.Equal("", cli.LRange("TestLPush", 0, 1)[1]);

            Assert.Equal(2, cli.LRange("TestLPush", 2, 3).Length);
            Assert.Equal(String, cli.LRange("TestLPush", 2, 3)[0]);
            Assert.Equal(String, cli.LRange("TestLPush", 2, 3)[1]);

            Assert.Equal(2, cli.LRange("TestLPush", 4, 5).Length);
            Assert.Equal(Bytes, cli.LRange<byte[]>("TestLPush", 4, 5)[0]);
            Assert.Equal(Bytes, cli.LRange<byte[]>("TestLPush", 4, 5)[1]);

            Assert.Equal(2, cli.LRange("TestLPush", 6, -1).Length);
            Assert.Equal(Class.ToString(), cli.LRange<TestClass>("TestLPush", 6, -1)[0].ToString());
            Assert.Equal(Class.ToString(), cli.LRange<TestClass>("TestLPush", 6, -1)[1].ToString());
        }

        [Fact]
        public void LPushX()
        {
            cli.Del("TestLPushX");
            Assert.Equal(0, cli.LPushX("TestLPushX", Null));
            Assert.Equal(0, cli.LPushX("TestLPushX", String));
            Assert.Equal(0, cli.LPushX("TestLPushX", Bytes));
            Assert.Equal(0, cli.LPushX("TestLPushX", Class));

            Assert.Equal(1, cli.RPush("TestLPushX", Null));
            Assert.Equal(2, cli.LPushX("TestLPushX", Null));
            Assert.Equal(3, cli.LPushX("TestLPushX", String));
            Assert.Equal(4, cli.LPushX("TestLPushX", Bytes));
            Assert.Equal(5, cli.LPushX("TestLPushX", Class));
        }

        [Fact]
        public void LRange()
        {
            cli.Del("TestLRange");
            cli.LPush("TestLRange", Class, Class, Bytes, Bytes, String, String, Null, Null);

            Assert.Equal(2, cli.LRange("TestLRange", 0, 1).Length);
            Assert.Equal("", cli.LRange("TestLRange", 0, 1)[0]);
            Assert.Equal("", cli.LRange("TestLRange", 0, 1)[1]);

            Assert.Equal(2, cli.LRange("TestLRange", 2, 3).Length);
            Assert.Equal(String, cli.LRange("TestLRange", 2, 3)[0]);
            Assert.Equal(String, cli.LRange("TestLRange", 2, 3)[1]);

            Assert.Equal(2, cli.LRange("TestLRange", 4, 5).Length);
            Assert.Equal(Bytes, cli.LRange<byte[]>("TestLRange", 4, 5)[0]);
            Assert.Equal(Bytes, cli.LRange<byte[]>("TestLRange", 4, 5)[1]);

            Assert.Equal(2, cli.LRange("TestLRange", 6, -1).Length);
            Assert.Equal(Class.ToString(), cli.LRange<TestClass>("TestLRange", 6, -1)[0].ToString());
            Assert.Equal(Class.ToString(), cli.LRange<TestClass>("TestLRange", 6, -1)[1].ToString());
        }

        [Fact]
        public void LRem()
        {
            cli.Del("TestLRem");
            Assert.Equal(8, cli.LPush("TestLRem", Class, Class, Bytes, Bytes, String, String, Null, Null));

            Assert.Equal(2, cli.LRem("TestLRem", 0, Class));
            Assert.Equal(0, cli.LRem("TestLRem", 0, Class));
            Assert.Equal(2, cli.LRem("TestLRem", 0, Bytes));
            Assert.Equal(0, cli.LRem("TestLRem", 0, Bytes));
            Assert.Equal(2, cli.LRem("TestLRem", 0, String));
            Assert.Equal(0, cli.LRem("TestLRem", 0, String));
            Assert.Equal(2, cli.LRem("TestLRem", 0, Null));
            Assert.Equal(0, cli.LRem("TestLRem", 0, Null));
        }

        [Fact]
        public void LSet()
        {
            cli.Del("TestLSet");
            Assert.Equal(8, cli.RPush("TestLSet", Class, Class, Bytes, Bytes, String, String, Null, Null));

            var now = DateTime.Now;
            cli.LSet("TestLSet", -1, now);
            Assert.Equal(now.ToString(), cli.LIndex<DateTime>("TestLSet", -1).ToString());
        }

        [Fact]
        public void LTrim()
        {
            cli.Del("TestLTrim");
            Assert.Equal(8, cli.RPush("TestLTrim", Class, Class, Bytes, Bytes, String, String, Null, Null));

            cli.LTrim("TestLTrim", -1, -1);
            Assert.Equal(1, cli.LLen("TestLTrim"));
            Assert.Equal("", cli.LRange("TestLTrim", 0, -1)[0]);
        }

        [Fact]
        public void RPop()
        {
            cli.Del("TestRPop");
            Assert.Equal(8, cli.RPush("TestRPop", Class, Class, Bytes, Bytes, String, String, Null, Null));
            Assert.Equal("", cli.RPop("TestRPop"));
            Assert.Equal("", cli.RPop("TestRPop"));
            Assert.Equal(String, cli.RPop("TestRPop"));
            Assert.Equal(String, cli.RPop("TestRPop"));
            Assert.Equal(Bytes, cli.RPop<byte[]>("TestRPop"));
            Assert.Equal(Bytes, cli.RPop<byte[]>("TestRPop"));
            Assert.Equal(Class.ToString(), cli.RPop<TestClass>("TestRPop").ToString());
            Assert.Equal(Class.ToString(), cli.RPop<TestClass>("TestRPop").ToString());
        }

        [Fact]
        public void RPopLPush()
        {
            cli.Del("TestRPopLPush");
            Assert.Equal(8, cli.RPush("TestRPopLPush", Class, Class, Bytes, Bytes, String, String, Null, Null));

            Assert.Equal("", cli.RPopLPush("TestRPopLPush", "TestRPopLPush"));
            Assert.Equal("", cli.RPopLPush("TestRPopLPush", "TestRPopLPush"));
            Assert.Equal(String, cli.RPopLPush("TestRPopLPush", "TestRPopLPush"));
            Assert.Equal(String, cli.RPopLPush("TestRPopLPush", "TestRPopLPush"));
            Assert.Equal(Bytes, cli.RPopLPush<byte[]>("TestRPopLPush", "TestRPopLPush"));
            Assert.Equal(Bytes, cli.RPopLPush<byte[]>("TestRPopLPush", "TestRPopLPush"));
            Assert.Equal(Class.ToString(), cli.RPopLPush<TestClass>("TestRPopLPush", "TestRPopLPush").ToString());
            Assert.Equal(Class.ToString(), cli.RPopLPush<TestClass>("TestRPopLPush", "TestRPopLPush").ToString());
        }

        [Fact]
        public void RPush()
        {
            cli.Del("TestRPush");
            Assert.Equal(8, cli.RPush("TestRPush", Null, Null, String, String, Bytes, Bytes, Class, Class));

            Assert.Equal(2, cli.LRange("TestRPush", 0, 1).Length);
            Assert.Equal("", cli.LRange("TestRPush", 0, 1)[0]);
            Assert.Equal("", cli.LRange("TestRPush", 0, 1)[1]);

            Assert.Equal(2, cli.LRange("TestRPush", 2, 3).Length);
            Assert.Equal(String, cli.LRange("TestRPush", 2, 3)[0]);
            Assert.Equal(String, cli.LRange("TestRPush", 2, 3)[1]);

            Assert.Equal(2, cli.LRange("TestRPush", 4, 5).Length);
            Assert.Equal(Bytes, cli.LRange<byte[]>("TestRPush", 4, 5)[0]);
            Assert.Equal(Bytes, cli.LRange<byte[]>("TestRPush", 4, 5)[1]);

            Assert.Equal(2, cli.LRange("TestRPush", 6, -1).Length);
            Assert.Equal(Class.ToString(), cli.LRange<TestClass>("TestRPush", 6, -1)[0].ToString());
            Assert.Equal(Class.ToString(), cli.LRange<TestClass>("TestRPush", 6, -1)[1].ToString());
        }

        [Fact]
        public void RPushX()
        {
            cli.Del("TestRPushX");
            Assert.Equal(0, cli.RPushX("TestRPushX", Null));
            Assert.Equal(0, cli.RPushX("TestRPushX", String));
            Assert.Equal(0, cli.RPushX("TestRPushX", Bytes));
            Assert.Equal(0, cli.RPushX("TestRPushX", Class));

            Assert.Equal(1, cli.RPush("TestRPushX", Null));
            Assert.Equal(2, cli.RPushX("TestRPushX", Null));
            Assert.Equal(3, cli.RPushX("TestRPushX", String));
            Assert.Equal(4, cli.RPushX("TestRPushX", Bytes));
            Assert.Equal(5, cli.RPushX("TestRPushX", Class));
        }
    }
}
