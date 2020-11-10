using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests
{
    public class HashesTests : TestBase
    {
        [Fact]
        public void HDel()
        {
            cli.Del("TestHDel");
            cli.HMSet("TestHDel", "string1", base.String, "bytes1", base.Bytes, "class1", base.Class);
            Assert.Equal(3, cli.HDel("TestHDel", "string1", "bytes1", "class1"));
        }

        [Fact]
        public void HExists()
        {
            cli.Del("TestHExists");
            Assert.False(cli.HExists("TestHExists", "null1"));
            Assert.Equal(1, cli.HSet("TestHExists", "null1", 1));
            Assert.True(cli.HExists("TestHExists", "null1"));
            Assert.Equal(1, cli.HDel("TestHExists", "null1"));
            Assert.False(cli.HExists("TestHExists", "null1"));
        }

        [Fact]
        public void HGet()
        {
            cli.Del("TestHGet");
            cli.HMSet("TestHGet", "null1", base.Null, "string1", base.String, "bytes1", base.Bytes, "class1", base.Class, "class1array", new[] { base.Class, base.Class });

            Assert.Equal(cli.HGet("TestHGet", "null1")?.ToString() ?? "", base.Null?.ToString() ?? "");
            Assert.Equal(cli.HGet("TestHGet", "string1"), base.String);
            Assert.Equal(cli.HGet<byte[]>("TestHGet", "bytes1"), base.Bytes);
            Assert.Equal(cli.HGet<TestClass>("TestHGet", "class1")?.ToString(), base.Class.ToString());

            Assert.Equal(2, cli.HGet<TestClass[]>("TestHGet", "class1array")?.Length);
            Assert.Equal(cli.HGet<TestClass[]>("TestHGet", "class1array")?.First().ToString(), base.Class.ToString());
            Assert.Equal(cli.HGet<TestClass[]>("TestHGet", "class1array")?.Last().ToString(), base.Class.ToString());
        }

        [Fact]
        public void HGetAll()
        {
            cli.Del("TestHGetAll");
            cli.HMSet("TestHGetAll", "string1", base.String, "bytes1", base.Bytes, "class1", base.Class, "class1array", new[] { base.Class, base.Class });
            Assert.Equal(4, cli.HGetAll("TestHGetAll").Count);
            Assert.Equal(base.String, cli.HGetAll("TestHGetAll")["string1"]);
            Assert.Equal(Encoding.UTF8.GetString(base.Bytes), cli.HGetAll("TestHGetAll")["bytes1"]);
            Assert.Equal(base.Class.ToString(), cli.HGetAll("TestHGetAll")["class1"]);
        }

        [Fact]
        public void HIncrBy()
        {
            cli.Del("TestHIncrBy");
            cli.HMSet("TestHIncrBy", "null1", base.Null, "string1", base.String, "bytes1", base.Bytes, "class1", base.Class, "class1array", new[] { base.Class, base.Class });
            Assert.Equal(1, cli.HIncrBy("TestHIncrBy", "null112", 1));
            Assert.Throws<RedisServerException>(() => cli.HIncrBy("TestHIncrBy", "string1", 1));
            Assert.Throws<RedisServerException>(() => cli.HIncrBy("TestHIncrBy", "bytes1", 1));

            Assert.Equal(2, cli.HIncrBy("TestHIncrBy", "null112", 1));
            Assert.Equal(12, cli.HIncrBy("TestHIncrBy", "null112", 10));
        }

        [Fact]
        public void HIncrByFloat()
        {
            cli.Del("TestHIncrByFloat");
            cli.HMSet("TestHIncrByFloat", "null1", base.Null, "string1", base.String, "bytes1", base.Bytes, "class1", base.Class, "class1array", new[] { base.Class, base.Class });
            Assert.Equal(0.5m, cli.HIncrByFloat("TestHIncrByFloat", "null112", 0.5m));
            Assert.Throws<RedisServerException>(() => cli.HIncrByFloat("TestHIncrByFloat", "string1", 1.5m));
            Assert.Throws<RedisServerException>(() => cli.HIncrByFloat("TestHIncrByFloat", "bytes1", 5));

            Assert.Equal(3.8m, cli.HIncrByFloat("TestHIncrByFloat", "null112", 3.3m));
            Assert.Equal(14.0m, cli.HIncrByFloat("TestHIncrByFloat", "null112", 10.2m));
        }

        [Fact]
        public void HKeys()
        {
            cli.Del("HKeys");
            cli.HMSet("TestHKeys", "string1", base.String, "bytes1", base.Bytes, "class1", base.Class, "class1array", new[] { base.Class, base.Class });
            Assert.Equal(4, cli.HKeys("TestHKeys").Length);
            Assert.Contains("string1", cli.HKeys("TestHKeys"));
            Assert.Contains("bytes1", cli.HKeys("TestHKeys"));
            Assert.Contains("class1", cli.HKeys("TestHKeys"));
            Assert.Contains("class1array", cli.HKeys("TestHKeys"));
        }

        [Fact]
        public void HLen()
        {
            cli.Del("HLen");
            cli.HMSet("TestHLen", "string1", base.String, "bytes1", base.Bytes, "class1", base.Class, "class1array", new[] { base.Class, base.Class });
            Assert.Equal(4, cli.HLen("TestHLen"));
        }

        [Fact]
        public void HMGet()
        {
            cli.Del("TestHMGet");
            cli.HMSet("TestHMGet", "string1", base.String, "bytes1", base.Bytes, "class1", base.Class, "class1array", new[] { base.Class, base.Class });
            cli.HMSet("TestHMGet", "string2", base.String, "bytes2", base.Bytes, "class2", base.Class, "class2array", new[] { base.Class, base.Class });

            Assert.Equal(2, cli.HMGet("TestHMGet", "string1", "string2").Length);
            Assert.Contains(base.String, cli.HMGet("TestHMGet", "string1", "string2"));
            Assert.Equal(2, cli.HMGet<TestClass>("TestHMGet", "class1", "class2").Length);
            Assert.Contains(base.Class.ToString(), cli.HMGet<TestClass>("TestHMGet", "class1", "class2")?.Select(a => a.ToString()));
        }

        [Fact]
        public void HMSet()
        {
            cli.Del("TestHMSet");
            cli.HMSet("TestHMSet", "string1", base.String, "bytes1", base.Bytes, "class1", base.Class, "class1array", new[] { base.Class, base.Class });
            Assert.Equal(4, cli.HMGet("TestHMSet", "string1", "bytes1", "class1", "class1array").Length);
            Assert.Contains(base.String, cli.HMGet("TestHMSet", "string1", "bytes1", "class1", "class1array"));
            Assert.Contains(Encoding.UTF8.GetString(base.Bytes), cli.HMGet("TestHMSet", "string1", "bytes1", "class1", "class1array"));
            Assert.Contains(base.Class.ToString(), cli.HMGet("TestHMSet", "string1", "bytes1", "class1", "class1array"));
        }

        [Fact]
        public void HScan()
        {

        }

        [Fact]
        public void HSet()
        {
            cli.Del("TestHSet");
            Assert.Equal(1, cli.HSet("TestHSet", "string1", base.String));
            Assert.Equal(base.String, cli.HGet("TestHSet", "string1"));

            Assert.Equal(1, cli.HSet("TestHSet", "bytes1", base.Bytes));
            Assert.Equal(base.Bytes, cli.HGet<byte[]>("TestHSet", "bytes1"));

            Assert.Equal(1, cli.HSet("TestHSet", "class1", base.Class));
            Assert.Equal(base.Class.ToString(), cli.HGet<TestClass>("TestHSet", "class1").ToString());
        }

        [Fact]
        public void HSetNx()
        {
            cli.Del("TestHSetNx");
            Assert.Equal(1, cli.HSet("TestHSetNx", "string1", base.String));
            Assert.Equal(base.String, cli.HGet("TestHSetNx", "string1"));
            Assert.Equal(0, cli.HSet("TestHSetNx", "string1", base.String));

            Assert.Equal(1, cli.HSet("TestHSetNx", "bytes1", base.Bytes));
            Assert.Equal(base.Bytes, cli.HGet<byte[]>("TestHSetNx", "bytes1"));
            Assert.Equal(0, cli.HSet("TestHSetNx", "bytes1", base.Bytes));

            Assert.Equal(1, cli.HSet("TestHSetNx", "class1", base.Class));
            Assert.Equal(base.Class.ToString(), cli.HGet<TestClass>("TestHSetNx", "class1").ToString());
            Assert.Equal(0, cli.HSet("TestHSetNx", "class1", base.Class));
        }

        [Fact]
        public void HStrLen()
        {
            cli.Del("HStrLen1");
            cli.HMSet("HStrLen1", "f1", 123, "f2", 2222);
            Assert.Equal(3, cli.HStrLen("HStrLen1", "f1"));
            Assert.Equal(4, cli.HStrLen("HStrLen1", "f2"));
            Assert.Equal(0, cli.HStrLen("HStrLen1", "f3"));
        }

        [Fact]
        public void HVals()
        {
            cli.Del("TestHVals1", "TestHVals2", "TestHVals3", "TestHVals4", "TestHVals5");
            cli.HMSet("TestHVals1", "string1", base.String, "bytes1", base.Bytes, "class1", base.Class, "class1array1", new[] { base.Class, base.Class });
            cli.HMSet("TestHVals1", "string2", base.String, "bytes2", base.Bytes, "class2", base.Class, "class2array2", new[] { base.Class, base.Class });
            Assert.Equal(8, cli.HVals("TestHVals1").Length);

            cli.HMSet("TestHVals2", "string1", base.String, "string2", base.String);
            Assert.Equal(2, cli.HVals("TestHVals2").Length);
            Assert.Contains(base.String, cli.HVals("TestHVals2"));

            cli.HMSet("TestHVals3", "bytes1", base.Bytes, "bytes2", base.Bytes);
            Assert.Equal(2, cli.HVals<byte[]>("TestHVals3").Length);
            Assert.Contains(base.Bytes, cli.HVals<byte[]>("TestHVals3"));

            cli.HMSet("TestHVals4", "class1", base.Class, "class2", base.Class);
            Assert.Equal(2, cli.HVals<TestClass>("TestHVals4").Length);
            Assert.Contains(base.Class.ToString(), cli.HVals<TestClass>("TestHVals4").Select(a => a.ToString()));

            cli.HMSet("TestHVals5", "class2array1", new[] { base.Class, base.Class }, "class2array2", new[] { base.Class, base.Class });
            Assert.Equal(2, cli.HVals<TestClass[]>("TestHVals5").Length);
        }
    }
}
