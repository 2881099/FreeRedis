using FreeRedis.AliyunTair;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace FreeRedis.Tests
{
    public class AliyunTairUnitTest
    {
        protected static ConnectionStringBuilder Connection = new ConnectionStringBuilder()
        {
            Host = "",
            User = "",
            Password = "",
            Database = 30,
            MaxPoolSize = 10,
            Protocol = RedisProtocol.RESP2,
            ClientName = "FreeRedis"
        };
        static Lazy<TairRedisClient> _cliLazy = new Lazy<TairRedisClient>(() =>
        {
            var r = new TairRedisClient(Connection);
            r.Serialize = obj => JsonConvert.SerializeObject(obj);
            r.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);
            r.Notice += (s, e) => Trace.WriteLine(e.Log);
            return r;
        });
        public static TairRedisClient cli => _cliLazy.Value;
        protected readonly object Null = null;
        protected readonly string String = "我是中国人";
        protected readonly byte[] Bytes = Encoding.UTF8.GetBytes("这是一个byte字节");
        protected readonly TestClass Class = new TestClass { Id = 1, Name = "Class名称", CreateTime = DateTime.Now, TagId = new[] { 1, 3, 3, 3, 3 } };

        [Fact]
        public void ExHDel()
        {
            cli.Del("TestHDel");
            cli.ExHMSet("TestHDel", "string1", String, "bytes1", Bytes, "class1", Class);
            Assert.Equal(3, cli.ExHDel("TestHDel", "string1", "bytes1", "class1"));
        }
        [Fact]
        public void ExHExists()
        {
            cli.Del("TestHExists");
            Assert.False(cli.ExHExists("TestHExists", "null1"));
            Assert.Equal(1, cli.ExHSet("TestHExists", "null1", 1));
            Assert.True(cli.ExHExists("TestHExists", "null1"));
            Assert.Equal(1, cli.ExHDel("TestHExists", "null1"));
            Assert.False(cli.ExHExists("TestHExists", "null1"));
        }

        [Fact]
        public void ExHGet()
        {
            cli.Del("TestHGet");
            cli.ExHMSet("TestHGet", "null1", Null, "string1", String, "bytes1", Bytes, "class1", Class, "class1array", new[] { Class, Class });

            Assert.Equal(cli.ExHGet("TestHGet", "null1")?.ToString() ?? "", Null?.ToString() ?? "");
            Assert.Equal(cli.ExHGet("TestHGet", "string1"), String);
            Assert.Equal(cli.ExHGet<byte[]>("TestHGet", "bytes1"), Bytes);
            Assert.Equal(cli.ExHGet<TestClass>("TestHGet", "class1")?.ToString(), Class.ToString());

            Assert.Equal(2, cli.ExHGet<TestClass[]>("TestHGet", "class1array")?.Length);
            Assert.Equal(cli.ExHGet<TestClass[]>("TestHGet", "class1array")?.First().ToString(), Class.ToString());
            Assert.Equal(cli.ExHGet<TestClass[]>("TestHGet", "class1array")?.Last().ToString(), Class.ToString());
        }


        [Fact]
        public void ExHGetAll()
        {
            cli.Del("TestHGetAll");
            cli.ExHMSet("TestHGetAll", "string1", String, "bytes1", Bytes, "class1", Class, "class1array", new[] { Class, Class });
            Assert.Equal(4, cli.ExHGetAll("TestHGetAll").Count);
            Assert.Equal(String, cli.ExHGetAll("TestHGetAll")["string1"]);
            Assert.Equal(Encoding.UTF8.GetString(Bytes), cli.ExHGetAll("TestHGetAll")["bytes1"]);
            Assert.Equal(JsonConvert.SerializeObject(Class), cli.ExHGetAll("TestHGetAll")["class1"]);
        }

        [Fact]
        public void ExHIncrBy()
        {
            cli.Del("TestHIncrBy");
            cli.ExHMSet("TestHIncrBy", "null1", Null, "string1", String, "bytes1", Bytes, "class1", Class, "class1array", new[] { Class, Class });

            Assert.Equal(1, cli.ExHIncrBy("TestHIncrBy", "null112", 1));

            Assert.Throws<RedisServerException>(() => cli.ExHIncrBy("TestHIncrBy", "string1", 1));
            Assert.Throws<RedisServerException>(() => cli.ExHIncrBy("TestHIncrBy", "bytes1", 1));

            Assert.Equal(2, cli.ExHIncrBy("TestHIncrBy", "null112", 1));
            Assert.Equal(12, cli.ExHIncrBy("TestHIncrBy", "null112", 10));
        }


        [Fact]
        public void ExHIncrByFloat()
        {
            cli.Del("TestHIncrByFloat");
            cli.ExHMSet("TestHIncrByFloat", "null1", Null, "string1", String, "bytes1", Bytes, "class1", Class, "class1array", new[] { Class, Class });
            Assert.Equal(0.5m, cli.ExHIncrByFloat("TestHIncrByFloat", "null112", 0.5m));

            Assert.Throws<RedisServerException>(() => cli.ExHIncrByFloat("TestHIncrByFloat", "string1", 1.5m));
            Assert.Throws<RedisServerException>(() => cli.ExHIncrByFloat("TestHIncrByFloat", "bytes1", 5));

            Assert.Equal(3.8m, cli.ExHIncrByFloat("TestHIncrByFloat", "null112", 3.3m));
            Assert.Equal(14.0m, cli.ExHIncrByFloat("TestHIncrByFloat", "null112", 10.2m));
        }


        [Fact]
        public void ExHKeys()
        {
            cli.Del("HKeys");
            cli.ExHMSet("TestHKeys", "string1", String, "bytes1", Bytes, "class1", Class, "class1array", new[] { Class, Class });
            Assert.Equal(4, cli.ExHKeys("TestHKeys").Length);
            Assert.Contains("string1", cli.ExHKeys("TestHKeys"));
            Assert.Contains("bytes1", cli.ExHKeys("TestHKeys"));
            Assert.Contains("class1", cli.ExHKeys("TestHKeys"));
            Assert.Contains("class1array", cli.ExHKeys("TestHKeys"));
        }


        [Fact]
        public void ExHLen()
        {
            cli.Del("HLen");
            cli.ExHMSet("TestHLen", "string1", String, "bytes1", Bytes, "class1", Class, "class1array", new[] { Class, Class });
            Assert.Equal(4, cli.ExHLen("TestHLen"));
        }

        [Fact]
        public void ExHMGet()
        {
            cli.Del("TestHMGet");
            cli.ExHMSet("TestHMGet", "string1", String, "bytes1", Bytes, "class1", Class, "class1array", new[] { Class, Class });
            cli.ExHMSet("TestHMGet", "string2", String, "bytes2", Bytes, "class2", Class, "class2array", new[] { Class, Class });

            Assert.Equal(2, cli.ExHMGet("TestHMGet", "string1", "string2").Length);
            Assert.Contains(String, cli.ExHMGet("TestHMGet", "string1", "string2"));
            Assert.Equal(2, cli.ExHMGet<TestClass>("TestHMGet", "class1", "class2").Length);
            Assert.Contains(Class.ToString(), cli.ExHMGet<TestClass>("TestHMGet", "class1", "class2")?.Select(a => a.ToString()));
        }
        [Fact]
        public void ExHMSet()
        {
            cli.Del("TestHMSet");
            cli.ExHMSet("TestHMSet", "string1", String, "bytes1", Bytes, "class1", Class, "class1array", new[] { Class, Class });
            Assert.Equal(4, cli.ExHMGet("TestHMSet", "string1", "bytes1", "class1", "class1array").Length);
            Assert.Contains(String, cli.ExHMGet("TestHMSet", "string1", "bytes1", "class1", "class1array"));
            Assert.Contains(Encoding.UTF8.GetString(Bytes), cli.ExHMGet("TestHMSet", "string1", "bytes1", "class1", "class1array"));
            Assert.Contains(JsonConvert.SerializeObject(Class), cli.ExHMGet("TestHMSet", "string1", "bytes1", "class1", "class1array"));
        }


        [Fact]
        public void ExHSet()
        {
            cli.Del("TestHSet");
            Assert.Equal(1, cli.ExHSet("TestHSet", "string1", String));
            Assert.Equal(String, cli.ExHGet("TestHSet", "string1"));

            Assert.Equal(1, cli.ExHSet("TestHSet", "bytes1", Bytes));
            Assert.Equal(Bytes, cli.ExHGet<byte[]>("TestHSet", "bytes1"));

            Assert.Equal(1, cli.ExHSet("TestHSet", "class1", Class));
            Assert.Equal(Class.ToString(), cli.ExHGet<TestClass>("TestHSet", "class1").ToString());
        }

        [Fact]
        public void ExHSetNx()
        {
            cli.Del("TestHSetNx");
            Assert.Equal(1, cli.ExHSet("TestHSetNx", "string1", String));
            Assert.Equal(String, cli.ExHGet("TestHSetNx", "string1"));
            Assert.Equal(0, cli.ExHSet("TestHSetNx", "string1", String));

            Assert.Equal(1, cli.ExHSet("TestHSetNx", "bytes1", Bytes));
            Assert.Equal(Bytes, cli.ExHGet<byte[]>("TestHSetNx", "bytes1"));
            Assert.Equal(0, cli.ExHSet("TestHSetNx", "bytes1", Bytes));

            Assert.Equal(1, cli.ExHSet("TestHSetNx", "class1", Class));
            Assert.Equal(Class.ToString(), cli.ExHGet<TestClass>("TestHSetNx", "class1").ToString());
            Assert.Equal(0, cli.ExHSet("TestHSetNx", "class1", Class));
        }

        [Fact]
        public void ExHStrLen()
        {
            cli.Del("TestHStrLen1");
            cli.ExHMSet("TestHStrLen1", "f1", 123, "f2", 2222);
            Assert.Equal(3, cli.ExHStrLen("TestHStrLen1", "f1"));
            Assert.Equal(4, cli.ExHStrLen("TestHStrLen1", "f2"));
            Assert.Equal(0, cli.ExHStrLen("TestHStrLen1", "f3"));
        }

        [Fact]
        public void ExHVals()
        {
            cli.Del("TestHVals1", "TestHVals2", "TestHVals3", "TestHVals4", "TestHVals5");
            cli.ExHMSet("TestHVals1", "string1", String, "bytes1", Bytes, "class1", Class, "class1array1", new[] { Class, Class });
            cli.ExHMSet("TestHVals1", "string2", String, "bytes2", Bytes, "class2", Class, "class2array2", new[] { Class, Class });
            Assert.Equal(8, cli.ExHVals("TestHVals1").Length);

            cli.ExHMSet("TestHVals2", "string1", String, "string2", String);
            Assert.Equal(2, cli.ExHVals("TestHVals2").Length);
            Assert.Contains(String, cli.ExHVals("TestHVals2"));

            cli.ExHMSet("TestHVals3", "bytes1", Bytes, "bytes2", Bytes);
            Assert.Equal(2, cli.ExHVals<byte[]>("TestHVals3").Length);
            Assert.Contains(Bytes, cli.ExHVals<byte[]>("TestHVals3"));

            cli.ExHMSet("TestHVals4", "class1", Class, "class2", Class);
            Assert.Equal(2, cli.ExHVals<TestClass>("TestHVals4").Length);
            Assert.Contains(Class.ToString(), cli.ExHVals<TestClass>("TestHVals4").Select(a => a.ToString()));

            cli.ExHMSet("TestHVals5", "class2array1", new[] { Class, Class }, "class2array2", new[] { Class, Class });
            Assert.Equal(2, cli.ExHVals<TestClass[]>("TestHVals5").Length);
        }

        [Fact]
        public void ExHExpireTime()
        {
            cli.Del("TestHExpireTime");
            cli.ExHMSet("TestHExpireTime", "string1", String, "bytes1", Bytes, "class1", Class, "class1array", new[] { Class, Class });

            var expireTimeSeconds = cli.ExHTtl("TestHExpireTime", "string1");
            Assert.False(expireTimeSeconds >= 0);

            var isSuccess = cli.ExHExpireTime("TestHExpireTime", "string1", TimeSpan.FromSeconds(5));
            Assert.True(isSuccess);

            expireTimeSeconds = cli.ExHTtl("TestHExpireTime", "string1");
            Assert.True(expireTimeSeconds > 0);

            var fieldValue = cli.ExHGet("TestHExpireTime", "string1");
            Assert.Equal(String, fieldValue);

            Thread.Sleep(TimeSpan.FromSeconds(expireTimeSeconds));


            fieldValue = cli.ExHGet("TestHExpireTime", "string1");
            Assert.Null(fieldValue);


            expireTimeSeconds = cli.ExHTtl("TestHExpireTime", "bytes1");
            Assert.False(expireTimeSeconds >= 0); 

            isSuccess = cli.ExHExpireTime("TestHExpireTime", "bytes1", DateTime.Now.AddSeconds(5));
            Assert.True(isSuccess);

            expireTimeSeconds = cli.ExHTtl("TestHExpireTime", "bytes1");
            Assert.True(expireTimeSeconds > 0);
             
            Thread.Sleep(TimeSpan.FromSeconds(expireTimeSeconds));

            fieldValue = cli.ExHGet("TestHExpireTime", "bytes1");
            Assert.Null(fieldValue);

        }
    }
}
