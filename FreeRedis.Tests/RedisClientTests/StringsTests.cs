using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests
{
    public class StringsTests : TestBase
    {
        [Fact]
        public void Append()
        {
            var key = "TestAppend_null";
            cli.Set(key, String);
            cli.Append(key, null);
            Assert.Equal(cli.Get(key), String);

            key = "TestAppend_string";
            cli.Set(key, String);
            cli.Append(key, String);
            Assert.Equal(cli.Get(key), String + String);
            var ms = new MemoryStream();
            cli.Get(key, ms);
            Assert.Equal(Encoding.UTF8.GetString(ms.ToArray()), String + String);
            ms.Close();

            key = "TestAppend_bytes";
            cli.Set(key, Bytes);
            cli.Append(key, Bytes);
            Assert.Equal(Convert.ToBase64String(cli.Get<byte[]>(key)), Convert.ToBase64String(Bytes.Concat(Bytes).ToArray()));
        }

        [Fact]
        public void BitCount()
        {
            var key = "TestBitCount";
            cli.SetBit(key, 100, true);
            cli.SetBit(key, 90, true);
            cli.SetBit(key, 80, true);
            Assert.Equal(3, cli.BitCount(key, 0, 101));
            Assert.Equal(3, cli.BitCount(key, 0, 100));
            Assert.Equal(3, cli.BitCount(key, 0, 99));
            Assert.Equal(3, cli.BitCount(key, 0, 60));
        }

        [Fact]
        public void BitOp()
        {
            cli.SetBit("BitOp1", 100, true);
            cli.SetBit("BitOp2", 100, true);
            var r1 = cli.BitOp(BitOpOperation.and, "BitOp3", "BitOp1", "BitOp2");
        }

        [Fact]
        public void BitPos()
        {
            cli.SetBit("BitPos1", 100, true);
            var r1 = cli.BitPos("BitPos1", true);
            var r2 = cli.BitPos("BitPos1", true, 1, 100);
        }

        [Fact]
        public void Decr()
        {
            var key = Guid.NewGuid().ToString();
            Assert.Equal(-1, cli.Decr(key));
        }

        [Fact]
        public void DecrBy()
        {
            var key = Guid.NewGuid().ToString();
            Assert.Equal(-10, cli.DecrBy(key, 10));
        }

        [Fact]
        public void Get()
        {
            var key = "TestGet_null";
            cli.Set(key, Null);
            Assert.Equal((cli.Get(key))?.ToString() ?? "", Null?.ToString() ?? "");

            key = "TestGet_string";
            cli.Set(key, String);
            Assert.Equal(cli.Get(key), String);

            key = "TestGet_bytes";
            cli.Set(key, Bytes);
            Assert.Equal(cli.Get<byte[]>(key), Bytes);

            key = "TestGet_class";
            cli.Set(key, Class);
            Assert.Equal((cli.Get<TestClass>(key))?.ToString(), Class.ToString());

            key = "TestGet_classArray";
            cli.Set(key, new[] { Class, Class });
            Assert.Equal(2, cli.Get<TestClass[]>(key)?.Length);
            Assert.Equal((cli.Get<TestClass[]>(key))?.First().ToString(), Class.ToString());
            Assert.Equal((cli.Get<TestClass[]>(key))?.Last().ToString(), Class.ToString());
        }

        [Fact]
        public void GetBit()
        {
            cli.SetBit("GetBit1", 100, true);
            var r1 = cli.GetBit("BitPos1", 10);
        }

        [Fact]
        public void GetRange()
        {
            var key = "TestGetRange_null";
            cli.Set(key, Null);
            Assert.Equal("", cli.GetRange(key, 10, 20));

            key = "TestGetRange_string";
            cli.Set(key, "abcdefg");
            Assert.Equal("cde", cli.GetRange(key, 2, 4));
            Assert.Equal("abcdefg", cli.GetRange(key, 0, -1));

            key = "TestGetRange_bytes";
            cli.Set(key, Bytes);
            Assert.Equal(Bytes.AsSpan(2, 3).ToArray(), cli.GetRange<byte[]>(key, 2, 4));
            Assert.Equal(Bytes, cli.GetRange<byte[]>(key, 0, -1));
        }

        [Fact]
        public void GetSet()
        {
            cli.Del("GetSet1");
            var r1 = cli.GetSet("GetSet1", "123456");
            var r2 = cli.GetSet("GetSet1", "123456789");
        }

        [Fact]
        public void Incr()
        {
            var key = Guid.NewGuid().ToString();
            Assert.Equal(1, cli.Incr(key));
        }

        [Fact]
        public void IncrBy()
        {
            var key = Guid.NewGuid().ToString();
            Assert.Equal(10, cli.IncrBy(key, 10));
        }

        [Fact]
        public void IncrByFloat()
        {
            var key = Guid.NewGuid().ToString();
            Assert.Equal(10.1m, cli.IncrByFloat(key, 10.1m));
        }

        [Fact]
        public void MGet()
        {
            cli.Set("TestMGet_null1", Null);
            cli.Set("TestMGet_string1", String);
            cli.Set("TestMGet_bytes1", Bytes);
            cli.Set("TestMGet_class1", Class);
            cli.Set("TestMGet_null2", Null);
            cli.Set("TestMGet_string2", String);
            cli.Set("TestMGet_bytes2", Bytes);
            cli.Set("TestMGet_class2", Class);
            cli.Set("TestMGet_null3", Null);
            cli.Set("TestMGet_string3", String);
            cli.Set("TestMGet_bytes3", Bytes);
            cli.Set("TestMGet_class3", Class);

            Assert.Equal(4, cli.MGet("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1").Length);
            Assert.Equal("", cli.MGet("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1")[0]);
            Assert.Equal(String, cli.MGet("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1")[1]);
            Assert.Equal(Encoding.UTF8.GetString(Bytes), cli.MGet("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1")[2]);
            Assert.Equal(Class.ToString(), cli.MGet("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1")[3]);

            Assert.Equal(4, cli.MGet<byte[]>("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1").Length);
            Assert.Equal(new byte[0], cli.MGet<byte[]>("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1")[0]);
            Assert.Equal(Encoding.UTF8.GetBytes(String), cli.MGet<byte[]>("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1")[1]);
            Assert.Equal(Bytes, cli.MGet<byte[]>("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1")[2]);
            Assert.Equal(Encoding.UTF8.GetBytes(Class.ToString()), cli.MGet<byte[]>("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1")[3]);

            Assert.Equal(3, cli.MGet<TestClass>("TestMGet_class1", "TestMGet_class2", "TestMGet_class3").Length);
            Assert.Equal(Class.ToString(), cli.MGet<TestClass>("TestMGet_class1", "TestMGet_class2", "TestMGet_class3")[0]?.ToString());
            Assert.Equal(Class.ToString(), cli.MGet<TestClass>("TestMGet_class1", "TestMGet_class2", "TestMGet_class3")[1]?.ToString());
            Assert.Equal(Class.ToString(), cli.MGet<TestClass>("TestMGet_class1", "TestMGet_class2", "TestMGet_class3")[2]?.ToString());
        }

        [Fact]
        public void MSet()
        {
            cli.MSet(new Dictionary<string, object> { ["TestMSet_null1"] = Null, ["TestMSet_string1"] = String, ["TestMSet_bytes1"] = Bytes, ["TestMSet_class1"] = Class });
            Assert.Equal("", cli.Get("TestMSet_null1"));
            Assert.Equal(String, cli.Get("TestMSet_string1"));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestMSet_bytes1"));
            Assert.Equal(Class.ToString(), cli.Get<TestClass>("TestMSet_class1").ToString());
        }

        [Fact]
        public void MSetNx()
        {
            cli.Del("TestMSetNx_null", "TestMSetNx_string", "TestMSetNx_bytes", "TestMSetNx_class", "abctest",
                "TestMSetNx_null1", "TestMSetNx_string1", "TestMSetNx_bytes1", "TestMSetNx_class1");

            Assert.True(cli.MSetNx(new Dictionary<string, object> { ["TestMSetNx_null"] = Null }));
            Assert.False(cli.MSetNx(new Dictionary<string, object> { ["TestMSetNx_null"] = Null }));
            Assert.Equal("", cli.Get("TestMSetNx_null"));

            Assert.True(cli.MSetNx(new Dictionary<string, object> { ["TestMSetNx_string"] = String }));
            Assert.False(cli.MSetNx(new Dictionary<string, object> { ["TestMSetNx_string"] = String }));
            Assert.Equal(String, cli.Get("TestMSetNx_string"));

            Assert.True(cli.MSetNx(new Dictionary<string, object> { ["TestMSetNx_bytes"] = Bytes }));
            Assert.False(cli.MSetNx(new Dictionary<string, object> { ["TestMSetNx_bytes"] = Bytes }));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestMSetNx_bytes"));

            Assert.True(cli.MSetNx(new Dictionary<string, object> { ["TestMSetNx_class"] = Class }));
            Assert.False(cli.MSetNx(new Dictionary<string, object> { ["TestMSetNx_class"] = Class }));
            Assert.Equal(Class.ToString(), cli.Get<TestClass>("TestMSetNx_class").ToString());

            cli.Set("abctest", 1);
            Assert.False(cli.MSetNx(new Dictionary<string, object> { ["abctest"] = 2, ["TestMSetNx_null1"] = Null, ["TestMSetNx_string1"] = String, ["TestMSetNx_bytes1"] = Bytes, ["TestMSetNx_class1"] = Class }));
            Assert.True(cli.MSetNx(new Dictionary<string, object> { ["TestMSetNx_null1"] = Null, ["TestMSetNx_string1"] = String, ["TestMSetNx_bytes1"] = Bytes, ["TestMSetNx_class1"] = Class }));
            Assert.Equal(1, cli.Get<int>("abctest"));
            Assert.Equal("", cli.Get("TestMSetNx_null1"));
            Assert.Equal(String, cli.Get("TestMSetNx_string1"));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestMSetNx_bytes1"));
            Assert.Equal(Class.ToString(), cli.Get<TestClass>("TestMSetNx_class1").ToString());
        }

        [Fact]
        public void PSetNx()
        {
            cli.PSetEx("TestSetNx_null", 10000, Null);
            Assert.Equal("", cli.Get("TestSetNx_null"));

            cli.PSetEx("TestSetNx_string", 10000, String);
            Assert.Equal(String, cli.Get("TestSetNx_string"));

            cli.PSetEx("TestSetNx_bytes", 10000, Bytes);
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSetNx_bytes"));

            cli.PSetEx("TestSetNx_class", 10000, Class);
            Assert.Equal(Class.ToString(), cli.Get<TestClass>("TestSetNx_class").ToString());
        }

        [Fact]
        public void Set()
        {
            cli.Set("TestSet_null", Null);
            Assert.Equal("", cli.Get("TestSet_null"));

            cli.Set("TestSet_string", String);
            Assert.Equal(String, cli.Get("TestSet_string"));

            cli.Set("TestSet_bytes", Bytes);
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSet_bytes"));

            cli.Set("TestSet_class", Class);
            Assert.Equal(Class.ToString(), cli.Get<TestClass>("TestSet_class").ToString());
        }

        [Fact]
        public void SetNx()
        {
            cli.Del("TestSetNx_null", "TestSetNx_string", "TestSetNx_bytes", "TestSetNx_class");

            Assert.True(cli.SetNx("TestSetNx_null", Null));
            Assert.False(cli.SetNx("TestSetNx_null", Null));
            Assert.Equal("", cli.Get("TestSetNx_null"));

            Assert.True(cli.SetNx("TestSetNx_string", String));
            Assert.False(cli.SetNx("TestSetNx_string", String));
            Assert.Equal(String, cli.Get("TestSetNx_string"));

            Assert.True(cli.SetNx("TestSetNx_bytes", Bytes));
            Assert.False(cli.SetNx("TestSetNx_bytes", Bytes));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSetNx_bytes"));

            Assert.True(cli.SetNx("TestSetNx_class", Class));
            Assert.False(cli.SetNx("TestSetNx_class", Class));
            Assert.Equal(Class.ToString(), cli.Get<TestClass>("TestSetNx_class").ToString());
        }

        [Fact]
        public void SetXx()
        {
        }

        [Fact]
        public void SetBit()
        {
        }

        [Fact]
        public void SetEx()
        {
        }

        [Fact]
        public void SetRange()
        {
            var key = "TestSetRange_null";
            cli.Set(key, Null);
            cli.SetRange(key, 10, String);
            Assert.Equal(String, cli.GetRange(key, 10, -1));

            key = "TestSetRange_string";
            cli.Set(key, "abcdefg");
            cli.SetRange(key, 2, "yyy");
            Assert.Equal("yyy", cli.GetRange(key, 2, 4));

            key = "TestSetRange_bytes";
            cli.Set(key, Bytes);
            cli.SetRange(key, 2, Bytes);
            Assert.Equal(Bytes, cli.GetRange<byte[]>(key, 2, Bytes.Length + 2));
        }

        [Fact]
        public void StrLen()
        {
            var key = "TestStrLen_null";
            cli.Set(key, Null);
            Assert.Equal(0, cli.StrLen(key));

            key = "TestStrLen_string";
            cli.Set(key, "abcdefg");
            Assert.Equal(7, cli.StrLen(key));

            key = "TestStrLen_string";
            cli.Set(key, String);
            Assert.Equal(15, cli.StrLen(key));

            key = "TestStrLen_bytes";
            cli.Set(key, Bytes);
            Assert.Equal(Bytes.Length, cli.StrLen(key));
        }
    }
}
