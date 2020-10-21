using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests
{
    public class TransactionTests : TestBase
    {
        [Fact]
        public void Multi()
        {
            cli.Serialize = obj => JsonConvert.SerializeObject(obj);
            cli.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);

            using (var tran = cli.Multi())
            {
                tran.Discard();
            }

            var key = Guid.NewGuid().ToString();
            using (var tran = cli.Multi())
            {
                tran.IncrBy(key, 10);
                
                tran.Set("TestSet_null", Null);
                tran.Get("TestSet_null");
                
                tran.Set("TestSet_string", String);
                tran.Get("TestSet_string");
                
                tran.Set("TestSet_bytes", Bytes);
                tran.Get<byte[]>("TestSet_bytes");
                
                tran.Set("TestSet_class", Class);
                tran.Get<TestClass>("TestSet_class");

                tran.Discard();
            }

            using (var tran = cli.Multi())
            {
                tran.IncrBy(key, 10);
                
                tran.Set("TestSet_null", Null);
                tran.Get("TestSet_null");
                
                tran.Set("TestSet_string", String);
                tran.Get("TestSet_string");
                
                tran.Set("TestSet_bytes", Bytes);
                tran.Get<byte[]>("TestSet_bytes");
                
                tran.Set("TestSet_class", Class);
                tran.Get<TestClass>("TestSet_class");

                var ret = tran.Exec();

                Assert.Equal(10L, ret[0]);
                Assert.Equal("", ret[2].ToString());
                Assert.Equal(String, ret[4].ToString());
                Assert.Equal(Bytes, ret[6]);
                Assert.Equal(Class.ToString(), ret[8].ToString());
            }
        }

        [Fact]
        public void Discard()
        {

        }

        [Fact]
        public void Exec()
        {

        }

        [Fact]
        public void UnWatch()
        {

        }

        [Fact]
        public void Watch()
        {

        }
    }
}
