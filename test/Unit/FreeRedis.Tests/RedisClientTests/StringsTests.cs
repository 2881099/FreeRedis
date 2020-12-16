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
            cli.Append(key, Null);
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
            cli.Del("TestBitCount");
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
            cli.Del("BitOp1", "BitOp2");
            cli.SetBit("BitOp1", 100, true);
            cli.SetBit("BitOp2", 100, true);
            var r1 = cli.BitOp(BitOpOperation.and, "BitOp3", "BitOp1", "BitOp2");
        }

        [Fact]
        public void BitPos()
        {
            cli.Del("BitPos1");
            cli.SetBit("BitPos1", 100, true);
            Assert.Equal(100, cli.BitPos("BitPos1", true));
            Assert.Equal(-1, cli.BitPos("BitPos1", true, 1, 100));
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
            Assert.Equal(JsonConvert.SerializeObject(cli.Get<TestClass>(key)), JsonConvert.SerializeObject(Class));

            key = "TestGet_classArray";
            cli.Set(key, new[] { Class, Class });
            Assert.Equal(2, cli.Get<TestClass[]>(key)?.Length);
            Assert.Equal(JsonConvert.SerializeObject(cli.Get<TestClass[]>(key)?.First()), JsonConvert.SerializeObject(Class));
            Assert.Equal(JsonConvert.SerializeObject(cli.Get<TestClass[]>(key)?.Last()), JsonConvert.SerializeObject(Class));
        }

        [Fact]
        public void GetBit()
        {
            cli.Del("GetBit1");
            cli.SetBit("GetBit1", 100, true);
            Assert.False(cli.GetBit("GetBit1", 10));
            Assert.True(cli.GetBit("GetBit1", 100));
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
            Assert.Null(cli.GetSet("GetSet1", "123456"));
            Assert.Equal("123456", cli.GetSet("GetSet1", "123456789"));
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
            Assert.Equal(JsonConvert.SerializeObject(Class), cli.MGet("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1")[3]);

            Assert.Equal(4, cli.MGet<byte[]>("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1").Length);
            Assert.Equal(new byte[0], cli.MGet<byte[]>("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1")[0]);
            Assert.Equal(Encoding.UTF8.GetBytes(String), cli.MGet<byte[]>("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1")[1]);
            Assert.Equal(Bytes, cli.MGet<byte[]>("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1")[2]);
            Assert.Equal(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Class)), cli.MGet<byte[]>("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1")[3]);

            Assert.Equal(3, cli.MGet<TestClass>("TestMGet_class1", "TestMGet_class2", "TestMGet_class3").Length);
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.MGet<TestClass>("TestMGet_class1", "TestMGet_class2", "TestMGet_class3")[0]));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.MGet<TestClass>("TestMGet_class1", "TestMGet_class2", "TestMGet_class3")[1]));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.MGet<TestClass>("TestMGet_class1", "TestMGet_class2", "TestMGet_class3")[2]));
        }

        [Fact]
        public void MSet()
        {
            cli.Del("TestMSet_null1", "TestMSet_string1", "TestMSet_bytes1", "TestMSet_class1");
            cli.MSet(new Dictionary<string, object> { ["TestMSet_null1"] = Null, ["TestMSet_string1"] = String, ["TestMSet_bytes1"] = Bytes, ["TestMSet_class1"] = Class });
            Assert.Equal("", cli.Get("TestMSet_null1"));
            Assert.Equal(String, cli.Get("TestMSet_string1"));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestMSet_bytes1"));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestMSet_class1")));

            cli.Del("TestMSet_null1", "TestMSet_string1", "TestMSet_bytes1", "TestMSet_class1");
            cli.MSet("TestMSet_null1", Null, "TestMSet_string1", String, "TestMSet_bytes1", Bytes, "TestMSet_class1", Class);
            Assert.Equal("", cli.Get("TestMSet_null1"));
            Assert.Equal(String, cli.Get("TestMSet_string1"));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestMSet_bytes1"));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestMSet_class1")));
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
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestMSetNx_class")));

            cli.Set("abctest", 1);
            Assert.False(cli.MSetNx(new Dictionary<string, object> { ["abctest"] = 2, ["TestMSetNx_null1"] = Null, ["TestMSetNx_string1"] = String, ["TestMSetNx_bytes1"] = Bytes, ["TestMSetNx_class1"] = Class }));
            Assert.True(cli.MSetNx(new Dictionary<string, object> { ["TestMSetNx_null1"] = Null, ["TestMSetNx_string1"] = String, ["TestMSetNx_bytes1"] = Bytes, ["TestMSetNx_class1"] = Class }));
            Assert.Equal(1, cli.Get<int>("abctest"));
            Assert.Equal("", cli.Get("TestMSetNx_null1"));
            Assert.Equal(String, cli.Get("TestMSetNx_string1"));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestMSetNx_bytes1"));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestMSetNx_class1")));

            cli.Del("TestMSetNx_null", "TestMSetNx_string", "TestMSetNx_bytes", "TestMSetNx_class");
            cli.MSetNx("TestMSetNx_null1", Null, "TestMSetNx_string1", String, "TestMSetNx_bytes1", Bytes, "TestMSetNx_class1", Class);
            Assert.Equal("", cli.Get("TestMSetNx_null1"));
            Assert.Equal(String, cli.Get("TestMSetNx_string1"));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestMSetNx_bytes1"));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestMSetNx_class1")));
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
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSetNx_class")));
        }

        [Fact]
        public void Set()
        {
            cli.Del("TestSetNx_null", "TestSetNx_string", "TestSetNx_bytes", "TestSetNx_class");
            cli.Set("TestSet_null", Null);
            Assert.Equal("", cli.Get("TestSet_null"));

            cli.Set("TestSet_string", String);
            Assert.Equal(String, cli.Get("TestSet_string"));

            cli.Set("TestSet_bytes", Bytes);
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSet_bytes"));

            cli.Set("TestSet_class", Class);
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSet_class")));

            cli.Del("TestSetNx_null", "TestSetNx_string", "TestSetNx_bytes", "TestSetNx_class");
            cli.Set("TestSet_null", Null, 10);
            Assert.Equal("", cli.Get("TestSet_null"));

            cli.Set("TestSet_string", String, 10);
            Assert.Equal(String, cli.Get("TestSet_string"));

            cli.Set("TestSet_bytes", Bytes, 10);
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSet_bytes"));

            cli.Set("TestSet_class", Class, 10);
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSet_class")));

            cli.Del("TestSetNx_null", "TestSetNx_string", "TestSetNx_bytes", "TestSetNx_class");
            cli.Set("TestSet_null", Null, true);
            Assert.Equal("", cli.Get("TestSet_null"));

            cli.Set("TestSet_string", String, true);
            Assert.Equal(String, cli.Get("TestSet_string"));

            cli.Set("TestSet_bytes", Bytes, true);
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSet_bytes"));

            cli.Set("TestSet_class", Class, true);
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSet_class")));
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
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSetNx_class")));

            cli.Del("TestSetNx_null", "TestSetNx_string", "TestSetNx_bytes", "TestSetNx_class");
            Assert.True(cli.SetNx("TestSetNx_null", Null, 10));
            Assert.False(cli.SetNx("TestSetNx_null", Null, 10));
            Assert.Equal("", cli.Get("TestSetNx_null"));

            Assert.True(cli.SetNx("TestSetNx_string", String, 10));
            Assert.False(cli.SetNx("TestSetNx_string", String, 10));
            Assert.Equal(String, cli.Get("TestSetNx_string"));

            Assert.True(cli.SetNx("TestSetNx_bytes", Bytes, 10));
            Assert.False(cli.SetNx("TestSetNx_bytes", Bytes, 10));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSetNx_bytes"));

            Assert.True(cli.SetNx("TestSetNx_class", Class, 10));
            Assert.False(cli.SetNx("TestSetNx_class", Class, 10));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSetNx_class")));
        }

        [Fact]
        public void SetXx()
        {
            cli.Del("TestSetXx_null");
            Assert.False(cli.SetXx("TestSetXx_null", Null, 10));
            cli.Set("TestSetXx_null", 1, true);
            Assert.True(cli.SetXx("TestSetXx_null", Null, 10));
            Assert.Equal("", cli.Get("TestSetXx_null"));

            cli.Del("TestSetXx_string");
            Assert.False(cli.SetXx("TestSetXx_string", String, 10));
            cli.Set("TestSetXx_string", 1, true);
            Assert.True(cli.SetXx("TestSetXx_string", String, 10));
            Assert.Equal(String, cli.Get("TestSetXx_string"));

            cli.Del("TestSetXx_bytes");
            Assert.False(cli.SetXx("TestSetXx_bytes", Bytes, 10));
            cli.Set("TestSetXx_bytes", 1, true);
            Assert.True(cli.SetXx("TestSetXx_bytes", Bytes, 10));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSetXx_bytes"));

            cli.Del("TestSetXx_class");
            Assert.False(cli.SetXx("TestSetXx_class", Class, 10));
            cli.Set("TestSetXx_class", 1, true);
            Assert.True(cli.SetXx("TestSetXx_class", Class, 10));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSetXx_class")));

            cli.Del("TestSetXx_null");
            Assert.False(cli.SetXx("TestSetXx_null", Null, true));
            cli.Set("TestSetXx_null", 1, true);
            Assert.True(cli.SetXx("TestSetXx_null", Null, true));
            Assert.Equal("", cli.Get("TestSetXx_null"));

            cli.Del("TestSetXx_string");
            Assert.False(cli.SetXx("TestSetXx_string", String, true));
            cli.Set("TestSetXx_string", 1, true);
            Assert.True(cli.SetXx("TestSetXx_string", String, true));
            Assert.Equal(String, cli.Get("TestSetXx_string"));

            cli.Del("TestSetXx_bytes");
            Assert.False(cli.SetXx("TestSetXx_bytes", Bytes, true));
            cli.Set("TestSetXx_bytes", 1, true);
            Assert.True(cli.SetXx("TestSetXx_bytes", Bytes, true));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSetXx_bytes"));

            cli.Del("TestSetXx_class");
            Assert.False(cli.SetXx("TestSetXx_class", Class, true));
            cli.Set("TestSetXx_class", 1, true);
            Assert.True(cli.SetXx("TestSetXx_class", Class, true));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSetXx_class")));
        }

        [Fact]
        public void SetBit()
        {
            cli.Del("SetBit1");
            cli.SetBit("SetBit1", 100, true);
            Assert.False(cli.GetBit("SetBit1", 10));
            Assert.True(cli.GetBit("SetBit1", 100));
        }

        [Fact]
        public void SetEx()
        {
            cli.Del("TestSetEx_null", "TestSetEx_string", "TestSetEx_bytes", "TestSetEx_class");
            cli.SetEx("TestSetEx_null", 10, Null);
            Assert.Equal("", cli.Get("TestSetEx_null"));

            cli.SetEx("TestSetEx_string", 10, String);
            Assert.Equal(String, cli.Get("TestSetEx_string"));

            cli.SetEx("TestSetEx_bytes", 10, Bytes);
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSetEx_bytes"));

            cli.SetEx("TestSetEx_class", 10, Class);
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSetEx_class")));
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
