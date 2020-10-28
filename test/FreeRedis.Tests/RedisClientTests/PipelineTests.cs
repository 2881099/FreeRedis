using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests.Other
{
    public class PipelineTests : TestBase
    {
        [Fact]
        public void StartPipe()
        {
            var key = Guid.NewGuid().ToString();
            using (var pipe = cli.StartPipe())
            {
                pipe.IncrBy(key, 10);

                pipe.Set("TestSet_null", Null);
                pipe.Get("TestSet_null");

                pipe.Set("TestSet_string", String);
                pipe.Get("TestSet_string");

                pipe.Set("TestSet_bytes", Bytes);
                pipe.Get<byte[]>("TestSet_bytes");

                pipe.Set("TestSet_class", Class);
                pipe.Get<TestClass>("TestSet_class");
            }

            using (var pipe = cli.StartPipe())
            {
                pipe.IncrBy(key, 10);
                
                pipe.Set("TestSet_null", Null);
                pipe.Get("TestSet_null");
                
                pipe.Set("TestSet_string", String);
                pipe.Get("TestSet_string");
                
                pipe.Set("TestSet_bytes", Bytes);
                pipe.Get<byte[]>("TestSet_bytes");
                
                pipe.Set("TestSet_class", Class);
                pipe.Get<TestClass>("TestSet_class");

                var ret = pipe.EndPipe();

                Assert.Equal(10L, ret[0]);
                Assert.Equal("", ret[2].ToString());
                Assert.Equal(String, ret[4].ToString());
                Assert.Equal(Bytes, ret[6]);
                Assert.Equal(Class.ToString(), ret[8].ToString());
            }
        }

        [Fact]
        public void StartPipeAsync()
        {
            var key = Guid.NewGuid().ToString();
            using (var pipe = cli.StartPipe())
            {
                long t1 = 0;
                pipe.IncrByAsync(key, 10).ContinueWith(t => t1 = t.Result);

                pipe.SetAsync("TestSet_null", Null);
                string t3 = "";
                pipe.GetAsync("TestSet_null").ContinueWith(t => t3 = t.Result);

                pipe.SetAsync("TestSet_string", String);
                string t4 = null;
                pipe.GetAsync("TestSet_string").ContinueWith(t => t4 = t.Result);

                pipe.SetAsync("TestSet_bytes", Bytes);
                byte[] t6 = null;
                pipe.GetAsync<byte[]>("TestSet_bytes").ContinueWith(t => t6 = t.Result);

                pipe.SetAsync("TestSet_class", Class);
                TestClass t8 = null;
                pipe.GetAsync<TestClass>("TestSet_class").ContinueWith(t => t8 = t.Result);
            }

            using (var pipe = cli.StartPipe())
            {
                long t1 = 0;
                pipe.IncrByAsync(key, 10).ContinueWith(t => t1 = t.Result);

                pipe.SetAsync("TestSet_null", Null);
                string t3 = "";
                pipe.GetAsync("TestSet_null").ContinueWith(t => t3 = t.Result);

                pipe.SetAsync("TestSet_string", String);
                string t4 = null;
                pipe.GetAsync("TestSet_string").ContinueWith(t => t4 = t.Result);

                pipe.SetAsync("TestSet_bytes", Bytes);
                byte[] t6 = null;
                pipe.GetAsync<byte[]>("TestSet_bytes").ContinueWith(t => t6 = t.Result);

                pipe.SetAsync("TestSet_class", Class);
                TestClass t8 = null;
                pipe.GetAsync<TestClass>("TestSet_class").ContinueWith(t => t8 = t.Result);

                var ret = pipe.EndPipe();

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
    }
}
