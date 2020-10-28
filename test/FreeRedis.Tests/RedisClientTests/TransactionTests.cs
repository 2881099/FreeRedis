using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests.Other
{
    public class TransactionTests : TestBase
    {
        [Fact]
        public void Multi()
        {
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
        public void MultiAsync()
        {
            using (var tran = cli.Multi())
            {
                tran.Discard();
            }

            var key = Guid.NewGuid().ToString();
            using (var tran = cli.Multi())
            {
                long t1 = 0;
                tran.IncrByAsync(key, 10).ContinueWith(t => t1 = t.Result);

                tran.SetAsync("TestSet_null", Null);
                string t3 = "";
                tran.GetAsync("TestSet_null").ContinueWith(t => t3 = t.Result);

                tran.SetAsync("TestSet_string", String);
                string t4 = null;
                tran.GetAsync("TestSet_string").ContinueWith(t => t4 = t.Result);

                tran.SetAsync("TestSet_bytes", Bytes);
                byte[] t6 = null;
                tran.GetAsync<byte[]>("TestSet_bytes").ContinueWith(t => t6 = t.Result);

                tran.SetAsync("TestSet_class", Class);
                TestClass t8 = null;
                tran.GetAsync<TestClass>("TestSet_class").ContinueWith(t => t8 = t.Result);

                tran.Discard();
            }

            using (var tran = cli.Multi())
            {
                long t1 = 0;
                tran.IncrByAsync(key, 10).ContinueWith(t => t1 = t.Result);

                tran.SetAsync("TestSet_null", Null);
                string t3 = "";
                tran.GetAsync("TestSet_null").ContinueWith(t => t3 = t.Result);

                tran.SetAsync("TestSet_string", String);
                string t4 = null;
                tran.GetAsync("TestSet_string").ContinueWith(t => t4 = t.Result);

                tran.SetAsync("TestSet_bytes", Bytes);
                byte[] t6 = null;
                tran.GetAsync<byte[]>("TestSet_bytes").ContinueWith(t => t6 = t.Result);

                tran.SetAsync("TestSet_class", Class);
                TestClass t8 = null;
                tran.GetAsync<TestClass>("TestSet_class").ContinueWith(t => t8 = t.Result);

                var ret = tran.Exec();

                Assert.Equal(10L, ret[0]);
                Assert.Equal("", ret[2].ToString());
                Assert.Equal(String, ret[4].ToString());
                Assert.Equal(Bytes, ret[6]);
                Assert.Equal(Class.ToString(), ret[8].ToString());

                Assert.Equal(10L, t1);
                Assert.Equal("", t3);
                Assert.Equal(String, t4);
                Assert.Equal(Bytes, t6);
                Assert.Equal(Class.ToString(), t8.ToString());
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
