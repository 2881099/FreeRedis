using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests
{
    public class StringsTests
    {
        public class TestClass
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime CreateTime { get; set; }

            public int[] TagId { get; set; }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        object Null = null;
        string String = "我是中国人";
        byte[] Bytes = Encoding.UTF8.GetBytes("这是一个byte字节");
        TestClass Class = new TestClass { Id = 1, Name = "Class名称", CreateTime = DateTime.Now, TagId = new[] { 1, 3, 3, 3, 3 } };

        [Fact]
        public void Append()
        {
            using (var cli = Util.GetRedisClient())
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
        }

        [Fact]
        public void BitCount()
        {
            using (var cli = Util.GetRedisClient())
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
        }

        [Fact]
        public void BitOp()
        {
            using (var cli = Util.GetRedisClient())
            {
                cli.SetBit("BitOp1", 100, true);
                cli.SetBit("BitOp2", 100, true);
                var r1 = cli.BitOp(BitOpOperation.And, "BitOp3", "BitOp1", "BitOp2");
            }
        }

        [Fact]
        public void BitPos()
        {
            using (var cli = Util.GetRedisClient())
            {
                cli.SetBit("BitPos1", 100, true);
                var r1 = cli.BitPos("BitPos1", true);
                var r2 = cli.BitPos("BitPos1", true, 1, 100);
            }
        }

        [Fact]
        public void Decr()
        {
            using (var cli = Util.GetRedisClient())
            {
                var key = Guid.NewGuid().ToString();
                Assert.Equal(-1, cli.Decr(key));
            }
        }

        [Fact]
        public void DecrBy()
        {
            using (var cli = Util.GetRedisClient())
            {
                var key = Guid.NewGuid().ToString();
                Assert.Equal(-10, cli.DecrBy(key, 10));
            }
        }

        [Fact]
        public void Get()
        {
            using (var cli = Util.GetRedisClient())
            {
                cli.Serialize = obj => JsonConvert.SerializeObject(obj);
                cli.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);

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
        }

        [Fact]
        public void GetBit()
        {
            using (var cli = Util.GetRedisClient())
            {
                cli.SetBit("GetBit1", 100, true);
                var r1 = cli.GetBit("BitPos1", 10);
            }
        }

        [Fact]
        public void GetRange()
        {
            using (var cli = Util.GetRedisClient())
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
        }

        [Fact]
        public void GetSet()
        {
            using (var cli = Util.GetRedisClient())
            {
            }
        }

        [Fact]
        public void Incr()
        {
            using (var cli = Util.GetRedisClient())
            {
            }
        }

        [Fact]
        public void IncrBy()
        {
            using (var cli = Util.GetRedisClient())
            {
            }
        }

        [Fact]
        public void IncrByFloat()
        {
            using (var cli = Util.GetRedisClient())
            {
            }
        }

        [Fact]
        public void MGet()
        {
            using (var cli = Util.GetRedisClient())
            {
            }
        }

        [Fact]
        public void MSet()
        {
            using (var cli = Util.GetRedisClient())
            {
            }
        }

        [Fact]
        public void MSetNx()
        {
            using (var cli = Util.GetRedisClient())
            {
            }
        }

        [Fact]
        public void PSetNx()
        {
            using (var cli = Util.GetRedisClient())
            {
            }
        }

        [Fact]
        public void Set()
        {
            using (var cli = Util.GetRedisClient())
            {
            }
        }

        [Fact]
        public void SetNx()
        {
            using (var cli = Util.GetRedisClient())
            {
            }
        }

        [Fact]
        public void SetXx()
        {
            using (var cli = Util.GetRedisClient())
            {
            }
        }

        [Fact]
        public void SetBit()
        {
            using (var cli = Util.GetRedisClient())
            {
            }
        }

        [Fact]
        public void SetEx()
        {
            using (var cli = Util.GetRedisClient())
            {
            }
        }

        [Fact]
        public void SetRange()
        {
            using (var cli = Util.GetRedisClient())
            {
            }
        }

        [Fact]
        public void StrLen()
        {
            using (var cli = Util.GetRedisClient())
            {
            }
        }
    }
}
