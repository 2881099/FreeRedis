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
    }
}
