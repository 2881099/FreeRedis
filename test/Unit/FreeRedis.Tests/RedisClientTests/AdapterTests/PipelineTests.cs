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
    public class PipelineTests : TestBase
    {
        [Fact]
        public void StartPipe()
        {
            var key = Guid.NewGuid().ToString();
            using (var pipe = cli.StartPipe())
            {
                pipe.IncrBy(key, 10);

                pipe.Set("StartPipeTestSet_null", Null);
                pipe.Get("StartPipeTestSet_null");

                pipe.Set("StartPipeTestSet_string", String);
                pipe.Get("StartPipeTestSet_string");

                pipe.Set("StartPipeTestSet_bytes", Bytes);
                pipe.Get<byte[]>("StartPipeTestSet_bytes");

                pipe.Set("StartPipeTestSet_class", Class);
                pipe.Get<TestClass>("StartPipeTestSet_class");
            }

            using (var pipe = cli.StartPipe())
            {
                pipe.IncrBy(key, 10);
                
                pipe.Set("StartPipeTestSet_null", Null);
                pipe.Get("StartPipeTestSet_null");
                
                pipe.Set("StartPipeTestSet_string", String);
                pipe.Get("StartPipeTestSet_string");
                
                pipe.Set("StartPipeTestSet_bytes", Bytes);
                pipe.Get<byte[]>("StartPipeTestSet_bytes");
                
                pipe.Set("StartPipeTestSet_class", Class);
                pipe.Get<TestClass>("StartPipeTestSet_class");

                var ret = pipe.EndPipe();

                Assert.Equal(10L, ret[0]);
                Assert.Equal("", ret[2].ToString());
                Assert.Equal(String, ret[4].ToString());
                Assert.Equal(Bytes, ret[6]);
                Assert.Equal(Class.ToString(), ret[8].ToString());
            }
        }

        //[Fact]
        //public void StartPipeAsync()
        //{
        //    var key = Guid.NewGuid().ToString();
        //    using (var pipe = cli.StartPipe())
        //    {
        //        long t1 = 0;
        //        pipe.IncrByAsync(key, 10);

        //        pipe.SetAsync("StartPipeAsyncTestSet_null", Null);
        //        string t3 = "";
        //        pipe.GetAsync("StartPipeAsyncTestSet_null");

        //        pipe.SetAsync("StartPipeAsyncTestSet_string", String);
        //        string t4 = null;
        //        pipe.GetAsync("StartPipeAsyncTestSet_string");

        //        pipe.SetAsync("StartPipeAsyncTestSet_bytes", Bytes);
        //        byte[] t6 = null;
        //        pipe.GetAsync<byte[]>("StartPipeAsyncTestSet_bytes");

        //        pipe.SetAsync("StartPipeAsyncTestSet_class", Class);
        //        TestClass t8 = null;
        //        pipe.GetAsync<TestClass>("StartPipeAsyncTestSet_class");
        //    }

        //    using (var pipe = cli.StartPipe())
        //    {
        //        var tasks = new List<Task>();
        //        long t1 = 0;
        //        tasks.Add(pipe.IncrByAsync(key, 10).ContinueWith(t => t1 = t.Result));

        //        pipe.SetAsync("StartPipeAsyncTestSet_null", Null);
        //        string t3 = "";
        //        tasks.Add(pipe.GetAsync("StartPipeAsyncTestSet_null").ContinueWith(t => t3 = t.Result));

        //        pipe.SetAsync("StartPipeAsyncTestSet_string", String);
        //        string t4 = null;
        //        tasks.Add(pipe.GetAsync("StartPipeAsyncTestSet_string").ContinueWith(t => t4 = t.Result));

        //        pipe.SetAsync("StartPipeAsyncTestSet_bytes", Bytes);
        //        byte[] t6 = null;
        //        tasks.Add(pipe.GetAsync<byte[]>("StartPipeAsyncTestSet_bytes").ContinueWith(t => t6 = t.Result));

        //        pipe.SetAsync("StartPipeAsyncTestSet_class", Class);
        //        TestClass t8 = null;
        //        tasks.Add(pipe.GetAsync<TestClass>("StartPipeAsyncTestSet_class").ContinueWith(t => t8 = t.Result));

        //        var ret = pipe.EndPipe();
        //        Task.WaitAll(tasks.ToArray());

        //        Assert.Equal(10L, ret[0]);
        //        Assert.Equal("", ret[2].ToString());
        //        Assert.Equal(String, ret[4].ToString());
        //        Assert.Equal(Bytes, ret[6]);
        //        Assert.Equal(Class.ToString(), ret[8].ToString());

        //        Assert.Equal(10L, t1);
        //        Assert.Equal("", t3);
        //        Assert.Equal(String, t4);
        //        Assert.Equal(Bytes, t6);
        //        Assert.Equal(Class.ToString(), t8.ToString());
        //    }
        //}
    }
}
