using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests
{
    public class StringsAsyncTests : TestBase
    {
        static Lazy<RedisClient> _cliLazy = new Lazy<RedisClient>(() =>
        {
            var connectionStringBuilder = (ConnectionStringBuilder)Connection.ToString();
            connectionStringBuilder.Database = 13;
            var r = new RedisClient(connectionStringBuilder);
            r.Serialize = obj => JsonConvert.SerializeObject(obj);
            r.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);
            r.Notice += (s, e) => Trace.WriteLine(e.Log);
            return r;
        });
        public new static RedisClient cli => _cliLazy.Value;

        [Fact]
        async public Task AppendAsync()
        {
            var key = "TestAppend_null";
            await cli.SetAsync(key, String);
            await cli.AppendAsync(key, Null);
            Assert.Equal(await cli.GetAsync(key), String);

            key = "TestAppend_string";
            await cli.SetAsync(key, String);
            await cli.AppendAsync(key, String);
            Assert.Equal(await cli.GetAsync(key), String + String);
            var ms = new MemoryStream();
            await cli.GetAsync(key, ms);
            Assert.Equal(Encoding.UTF8.GetString(ms.ToArray()), String + String);
            ms.Close();

            key = "TestAppend_bytes";
            await cli.SetAsync(key, Bytes);
            await cli.AppendAsync(key, Bytes);
            Assert.Equal(Convert.ToBase64String(cli.Get<byte[]>(key)), Convert.ToBase64String(Bytes.Concat(Bytes).ToArray()));
        }

        [Fact]
        async public Task BitCountAsync()
        {
            await cli.DelAsync("TestBitCount");
            var key = "TestBitCount";
            await cli.SetBitAsync(key, 100, true);
            await cli.SetBitAsync(key, 90, true);
            await cli.SetBitAsync(key, 80, true);
            Assert.Equal(3, await cli.BitCountAsync(key, 0, 101));
            Assert.Equal(3, await cli.BitCountAsync(key, 0, 100));
            Assert.Equal(3, await cli.BitCountAsync(key, 0, 99));
            Assert.Equal(3, await cli.BitCountAsync(key, 0, 60));
        }

        [Fact]
        async public Task BitOpAsync()
        {
            await cli.DelAsync("BitOp1", "BitOp2");
            await cli.SetBitAsync("BitOp1", 100, true);
            await cli.SetBitAsync("BitOp2", 100, true);
            var r1 = await cli.BitOpAsync(BitOpOperation.and, "BitOp3", "BitOp1", "BitOp2");
        }

        [Fact]
        async public Task BitPosAsync()
        {
            await cli.DelAsync("BitPos1");
            await cli.SetBitAsync("BitPos1", 100, true);
            Assert.Equal(100, await cli.BitPosAsync("BitPos1", true));
            Assert.Equal(-1, await cli.BitPosAsync("BitPos1", true, 1, 100));
        }

        [Fact]
        async public Task DecrAsync()
        {
            var key = Guid.NewGuid().ToString();
            Assert.Equal(-1, await cli.DecrAsync(key));
        }

        [Fact]
        async public Task DecrByAsync()
        {
            var key = Guid.NewGuid().ToString();
            Assert.Equal(-10, await cli.DecrByAsync(key, 10));
        }

        [Fact]
        async public Task GetAsync()
        {
            var key = "TestGet_null";
            await cli.SetAsync(key, Null);
            Assert.Equal((await cli.GetAsync(key))?.ToString() ?? "", Null?.ToString() ?? "");

            key = "TestGet_string";
            await cli.SetAsync(key, String);
            Assert.Equal(await cli.GetAsync(key), String);

            key = "TestGet_bytes";
            await cli.SetAsync(key, Bytes);
            Assert.Equal(cli.Get<byte[]>(key), Bytes);

            key = "TestGet_class";
            await cli.SetAsync(key, Class);
            Assert.Equal(JsonConvert.SerializeObject(cli.Get<TestClass>(key)), JsonConvert.SerializeObject(Class));

            key = "TestGet_classArray";
            await cli.SetAsync(key, new[] { Class, Class });
            Assert.Equal(2, cli.Get<TestClass[]>(key)?.Length);
            Assert.Equal(JsonConvert.SerializeObject(cli.Get<TestClass[]>(key)?.First()), JsonConvert.SerializeObject(Class));
            Assert.Equal(JsonConvert.SerializeObject(cli.Get<TestClass[]>(key)?.Last()), JsonConvert.SerializeObject(Class));
        }

        [Fact]
        async public Task GetBitAsync()
        {
            await cli.DelAsync("GetBit1");
            await cli.SetBitAsync("GetBit1", 100, true);
            Assert.False(await cli.GetBitAsync("GetBit1", 10));
            Assert.True(await cli.GetBitAsync("GetBit1", 100));
        }

        [Fact]
        async public Task GetRangeAsync()
        {
            var key = "TestGetRange_null";
            await cli.SetAsync(key, Null);
            Assert.Equal("", await cli.GetRangeAsync(key, 10, 20));

            key = "TestGetRange_string";
            await cli.SetAsync(key, "abcdefg");
            Assert.Equal("cde", await cli.GetRangeAsync(key, 2, 4));
            Assert.Equal("abcdefg", await cli.GetRangeAsync(key, 0, -1));

            key = "TestGetRange_bytes";
            await cli.SetAsync(key, Bytes);
            Assert.Equal(Bytes.AsSpan(2, 3).ToArray(), cli.GetRange<byte[]>(key, 2, 4));
            Assert.Equal(Bytes, cli.GetRange<byte[]>(key, 0, -1));
        }

        [Fact]
        async public Task GetSetAsync()
        {
            await cli.DelAsync("GetSet1");
            Assert.Null(await cli.GetSetAsync("GetSet1", "123456"));
            Assert.Equal("123456", await cli.GetSetAsync("GetSet1", "123456789"));
        }

        [Fact]
        async public Task IncrAsync()
        {
            var key = Guid.NewGuid().ToString();
            Assert.Equal(1, await cli.IncrAsync(key));
        }

        [Fact]
        async public Task IncrByAsync()
        {
            var key = Guid.NewGuid().ToString();
            Assert.Equal(10, await cli.IncrByAsync(key, 10));
        }

        [Fact]
        async public Task IncrByFloatAsync()
        {
            var key = Guid.NewGuid().ToString();
            Assert.Equal(10.1m, await cli.IncrByFloatAsync(key, 10.1m));
        }

        [Fact]
        async public Task MGetAsync()
        {
            await cli.SetAsync("TestMGet_null1", Null);
            await cli.SetAsync("TestMGet_string1", String);
            await cli.SetAsync("TestMGet_bytes1", Bytes);
            await cli.SetAsync("TestMGet_class1", Class);
            await cli.SetAsync("TestMGet_null2", Null);
            await cli.SetAsync("TestMGet_string2", String);
            await cli.SetAsync("TestMGet_bytes2", Bytes);
            await cli.SetAsync("TestMGet_class2", Class);
            await cli.SetAsync("TestMGet_null3", Null);
            await cli.SetAsync("TestMGet_string3", String);
            await cli.SetAsync("TestMGet_bytes3", Bytes);
            await cli.SetAsync("TestMGet_class3", Class);

            Assert.Equal(4, (await cli.MGetAsync("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1")).Length);
            Assert.Equal("", (await cli.MGetAsync("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1"))[0]);
            Assert.Equal(String, (await cli.MGetAsync("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1"))[1]);
            Assert.Equal(Encoding.UTF8.GetString(Bytes), (await cli.MGetAsync("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1"))[2]);
            Assert.Equal(JsonConvert.SerializeObject(Class), (await cli.MGetAsync("TestMGet_null1", "TestMGet_string1", "TestMGet_bytes1", "TestMGet_class1"))[3]);

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
        async public Task MSetAsync()
        {
            await cli.DelAsync("TestMSet_null1", "TestMSet_string1", "TestMSet_bytes1", "TestMSet_class1");
            await cli.MSetAsync(new Dictionary<string, object> { ["TestMSet_null1"] = Null, ["TestMSet_string1"] = String, ["TestMSet_bytes1"] = Bytes, ["TestMSet_class1"] = Class });
            Assert.Equal("", await cli.GetAsync("TestMSet_null1"));
            Assert.Equal(String, await cli.GetAsync("TestMSet_string1"));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestMSet_bytes1"));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestMSet_class1")));

            await cli.DelAsync("TestMSet_null1", "TestMSet_string1", "TestMSet_bytes1", "TestMSet_class1");
            await cli.MSetAsync("TestMSet_null1", Null, "TestMSet_string1", String, "TestMSet_bytes1", Bytes, "TestMSet_class1", Class);
            Assert.Equal("", await cli.GetAsync("TestMSet_null1"));
            Assert.Equal(String, await cli.GetAsync("TestMSet_string1"));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestMSet_bytes1"));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestMSet_class1")));
        }

        [Fact]
        async public Task MSetNxAsync()
        {
            await cli.DelAsync("TestMSetNx_null", "TestMSetNx_string", "TestMSetNx_bytes", "TestMSetNx_class", "abctest",
                "TestMSetNx_null1", "TestMSetNx_string1", "TestMSetNx_bytes1", "TestMSetNx_class1");

            Assert.True(await cli.MSetNxAsync(new Dictionary<string, object> { ["TestMSetNx_null"] = Null }));
            Assert.False(await cli.MSetNxAsync(new Dictionary<string, object> { ["TestMSetNx_null"] = Null }));
            Assert.Equal("", await cli.GetAsync("TestMSetNx_null"));

            Assert.True(await cli.MSetNxAsync(new Dictionary<string, object> { ["TestMSetNx_string"] = String }));
            Assert.False(await cli.MSetNxAsync(new Dictionary<string, object> { ["TestMSetNx_string"] = String }));
            Assert.Equal(String, await cli.GetAsync("TestMSetNx_string"));

            Assert.True(await cli.MSetNxAsync(new Dictionary<string, object> { ["TestMSetNx_bytes"] = Bytes }));
            Assert.False(await cli.MSetNxAsync(new Dictionary<string, object> { ["TestMSetNx_bytes"] = Bytes }));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestMSetNx_bytes"));

            Assert.True(await cli.MSetNxAsync(new Dictionary<string, object> { ["TestMSetNx_class"] = Class }));
            Assert.False(await cli.MSetNxAsync(new Dictionary<string, object> { ["TestMSetNx_class"] = Class }));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestMSetNx_class")));

            await cli.SetAsync("abctest", 1);
            Assert.False(await cli.MSetNxAsync(new Dictionary<string, object> { ["abctest"] = 2, ["TestMSetNx_null1"] = Null, ["TestMSetNx_string1"] = String, ["TestMSetNx_bytes1"] = Bytes, ["TestMSetNx_class1"] = Class }));
            Assert.True(await cli.MSetNxAsync(new Dictionary<string, object> { ["TestMSetNx_null1"] = Null, ["TestMSetNx_string1"] = String, ["TestMSetNx_bytes1"] = Bytes, ["TestMSetNx_class1"] = Class }));
            Assert.Equal(1, cli.Get<int>("abctest"));
            Assert.Equal("", await cli.GetAsync("TestMSetNx_null1"));
            Assert.Equal(String, await cli.GetAsync("TestMSetNx_string1"));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestMSetNx_bytes1"));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestMSetNx_class1")));

            await cli.DelAsync("TestMSetNx_null", "TestMSetNx_string", "TestMSetNx_bytes", "TestMSetNx_class");
            await cli.MSetNxAsync("TestMSetNx_null1", Null, "TestMSetNx_string1", String, "TestMSetNx_bytes1", Bytes, "TestMSetNx_class1", Class);
            Assert.Equal("", await cli.GetAsync("TestMSetNx_null1"));
            Assert.Equal(String, await cli.GetAsync("TestMSetNx_string1"));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestMSetNx_bytes1"));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestMSetNx_class1")));
        }

        [Fact]
        async public Task PSetNxAsync()
        {
            await cli.PSetExAsync("TestSetNx_null", 10000, Null);
            Assert.Equal("", await cli.GetAsync("TestSetNx_null"));

            await cli.PSetExAsync("TestSetNx_string", 10000, String);
            Assert.Equal(String, await cli.GetAsync("TestSetNx_string"));

            await cli.PSetExAsync("TestSetNx_bytes", 10000, Bytes);
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSetNx_bytes"));

            await cli.PSetExAsync("TestSetNx_class", 10000, Class);
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSetNx_class")));
        }

        [Fact]
        async public Task SetAsync()
        {
            await cli.DelAsync("TestSetNx_null", "TestSetNx_string", "TestSetNx_bytes", "TestSetNx_class");
            await cli.SetAsync("TestSet_null", Null);
            Assert.Equal("", await cli.GetAsync("TestSet_null"));

            await cli.SetAsync("TestSet_string", String);
            Assert.Equal(String, await cli.GetAsync("TestSet_string"));

            await cli.SetAsync("TestSet_bytes", Bytes);
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSet_bytes"));

            await cli.SetAsync("TestSet_class", Class);
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSet_class")));

            await cli.DelAsync("TestSetNx_null", "TestSetNx_string", "TestSetNx_bytes", "TestSetNx_class");
            await cli.SetAsync("TestSet_null", Null, 10);
            Assert.Equal("", await cli.GetAsync("TestSet_null"));

            await cli.SetAsync("TestSet_string", String, 10);
            Assert.Equal(String, await cli.GetAsync("TestSet_string"));

            await cli.SetAsync("TestSet_bytes", Bytes, 10);
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSet_bytes"));

            await cli.SetAsync("TestSet_class", Class, 10);
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSet_class")));

            await cli.DelAsync("TestSetNx_null", "TestSetNx_string", "TestSetNx_bytes", "TestSetNx_class");
            await cli.SetAsync("TestSet_null", Null, true);
            Assert.Equal("", await cli.GetAsync("TestSet_null"));

            await cli.SetAsync("TestSet_string", String, true);
            Assert.Equal(String, await cli.GetAsync("TestSet_string"));

            await cli.SetAsync("TestSet_bytes", Bytes, true);
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSet_bytes"));

            await cli.SetAsync("TestSet_class", Class, true);
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSet_class")));
        }

        [Fact]
        async public Task SetNxAsync()
        {
            await cli.DelAsync("TestSetNx_null", "TestSetNx_string", "TestSetNx_bytes", "TestSetNx_class");
            Assert.True(await cli.SetNxAsync("TestSetNx_null", Null));
            Assert.False(await cli.SetNxAsync("TestSetNx_null", Null));
            Assert.Equal("", await cli.GetAsync("TestSetNx_null"));

            Assert.True(await cli.SetNxAsync("TestSetNx_string", String));
            Assert.False(await cli.SetNxAsync("TestSetNx_string", String));
            Assert.Equal(String, await cli.GetAsync("TestSetNx_string"));

            Assert.True(await cli.SetNxAsync("TestSetNx_bytes", Bytes));
            Assert.False(await cli.SetNxAsync("TestSetNx_bytes", Bytes));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSetNx_bytes"));

            Assert.True(await cli.SetNxAsync("TestSetNx_class", Class));
            Assert.False(await cli.SetNxAsync("TestSetNx_class", Class));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSetNx_class")));

            await cli.DelAsync("TestSetNx_null", "TestSetNx_string", "TestSetNx_bytes", "TestSetNx_class");
            Assert.True(await cli.SetNxAsync("TestSetNx_null", Null, 10));
            Assert.False(await cli.SetNxAsync("TestSetNx_null", Null, 10));
            Assert.Equal("", await cli.GetAsync("TestSetNx_null"));

            Assert.True(await cli.SetNxAsync("TestSetNx_string", String, 10));
            Assert.False(await cli.SetNxAsync("TestSetNx_string", String, 10));
            Assert.Equal(String, await cli.GetAsync("TestSetNx_string"));

            Assert.True(await cli.SetNxAsync("TestSetNx_bytes", Bytes, 10));
            Assert.False(await cli.SetNxAsync("TestSetNx_bytes", Bytes, 10));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSetNx_bytes"));

            Assert.True(await cli.SetNxAsync("TestSetNx_class", Class, 10));
            Assert.False(await cli.SetNxAsync("TestSetNx_class", Class, 10));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSetNx_class")));
        }

        [Fact]
        async public Task SetXxAsync()
        {
            await cli.DelAsync("TestSetXx_null");
            Assert.False(await cli.SetXxAsync("TestSetXx_null", Null, 10));
            await cli.SetAsync("TestSetXx_null", 1, true);
            Assert.True(await cli.SetXxAsync("TestSetXx_null", Null, 10));
            Assert.Equal("", await cli.GetAsync("TestSetXx_null"));

            await cli.DelAsync("TestSetXx_string");
            Assert.False(await cli.SetXxAsync("TestSetXx_string", String, 10));
            await cli.SetAsync("TestSetXx_string", 1, true);
            Assert.True(await cli.SetXxAsync("TestSetXx_string", String, 10));
            Assert.Equal(String, await cli.GetAsync("TestSetXx_string"));

            await cli.DelAsync("TestSetXx_bytes");
            Assert.False(await cli.SetXxAsync("TestSetXx_bytes", Bytes, 10));
            await cli.SetAsync("TestSetXx_bytes", 1, true);
            Assert.True(await cli.SetXxAsync("TestSetXx_bytes", Bytes, 10));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSetXx_bytes"));

            await cli.DelAsync("TestSetXx_class");
            Assert.False(await cli.SetXxAsync("TestSetXx_class", Class, 10));
            await cli.SetAsync("TestSetXx_class", 1, true);
            Assert.True(await cli.SetXxAsync("TestSetXx_class", Class, 10));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSetXx_class")));

            await cli.DelAsync("TestSetXx_null");
            Assert.False(await cli.SetXxAsync("TestSetXx_null", Null, true));
            await cli.SetAsync("TestSetXx_null", 1, true);
            Assert.True(await cli.SetXxAsync("TestSetXx_null", Null, true));
            Assert.Equal("", await cli.GetAsync("TestSetXx_null"));

            await cli.DelAsync("TestSetXx_string");
            Assert.False(await cli.SetXxAsync("TestSetXx_string", String, true));
            await cli.SetAsync("TestSetXx_string", 1, true);
            Assert.True(await cli.SetXxAsync("TestSetXx_string", String, true));
            Assert.Equal(String, await cli.GetAsync("TestSetXx_string"));

            await cli.DelAsync("TestSetXx_bytes");
            Assert.False(await cli.SetXxAsync("TestSetXx_bytes", Bytes, true));
            await cli.SetAsync("TestSetXx_bytes", 1, true);
            Assert.True(await cli.SetXxAsync("TestSetXx_bytes", Bytes, true));
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSetXx_bytes"));

            await cli.DelAsync("TestSetXx_class");
            Assert.False(await cli.SetXxAsync("TestSetXx_class", Class, true));
            await cli.SetAsync("TestSetXx_class", 1, true);
            Assert.True(await cli.SetXxAsync("TestSetXx_class", Class, true));
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSetXx_class")));
        }

        [Fact]
        async public Task SetBitAsync()
        {
            await cli.DelAsync("SetBit1");
            await cli.SetBitAsync("SetBit1", 100, true);
            Assert.False(await cli.GetBitAsync("SetBit1", 10));
            Assert.True(await cli.GetBitAsync("SetBit1", 100));
        }

        [Fact]
        async public Task SetExAsync()
        {
            await cli.DelAsync("TestSetEx_null", "TestSetEx_string", "TestSetEx_bytes", "TestSetEx_class");
            await cli.SetExAsync("TestSetEx_null", 10, Null);
            Assert.Equal("", await cli.GetAsync("TestSetEx_null"));

            await cli.SetExAsync("TestSetEx_string", 10, String);
            Assert.Equal(String, await cli.GetAsync("TestSetEx_string"));

            await cli.SetExAsync("TestSetEx_bytes", 10, Bytes);
            Assert.Equal(Bytes, cli.Get<byte[]>("TestSetEx_bytes"));

            await cli.SetExAsync("TestSetEx_class", 10, Class);
            Assert.Equal(JsonConvert.SerializeObject(Class), JsonConvert.SerializeObject(cli.Get<TestClass>("TestSetEx_class")));
        }

        [Fact]
        async public Task SetRangeAsync()
        {
            var key = "TestSetRange_null";
            await cli.SetAsync(key, Null);
            await cli.SetRangeAsync(key, 10, String);
            Assert.Equal(String, await cli.GetRangeAsync(key, 10, -1));

            key = "TestSetRange_string";
            await cli.SetAsync(key, "abcdefg");
            await cli.SetRangeAsync(key, 2, "yyy");
            Assert.Equal("yyy", await cli.GetRangeAsync(key, 2, 4));

            key = "TestSetRange_bytes";
            await cli.SetAsync(key, Bytes);
            await cli.SetRangeAsync(key, 2, Bytes);
            Assert.Equal(Bytes, cli.GetRange<byte[]>(key, 2, Bytes.Length + 2));
        }

        [Fact]
        async public Task StrLenAsync()
        {
            var key = "TestStrLen_null";
            await cli.SetAsync(key, Null);
            Assert.Equal(0, await cli.StrLenAsync(key));

            key = "TestStrLen_string";
            await cli.SetAsync(key, "abcdefg");
            Assert.Equal(7, await cli.StrLenAsync(key));

            key = "TestStrLen_string";
            await cli.SetAsync(key, String);
            Assert.Equal(15, await cli.StrLenAsync(key));

            key = "TestStrLen_bytes";
            await cli.SetAsync(key, Bytes);
            Assert.Equal(Bytes.Length, await cli.StrLenAsync(key));
        }
    }
}
