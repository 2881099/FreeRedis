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
                
                tran.Set("MultiTestSet_null", Null);
                tran.Get("MultiTestSet_null");
                
                tran.Set("MultiTestSet_string", String);
                tran.Get("MultiTestSet_string");
                
                tran.Set("MultiTestSet_bytes", Bytes);
                tran.Get<byte[]>("MultiTestSet_bytes");
                
                tran.Set("MultiTestSet_class", Class);
                tran.Get<TestClass>("MultiTestSet_class");

                tran.Discard();
            }

            using (var tran = cli.Multi())
            {
                tran.IncrBy(key, 10);
                
                tran.Set("MultiTestSet_null", Null);
                tran.Get("MultiTestSet_null");
                
                tran.Set("MultiTestSet_string", String);
                tran.Get("MultiTestSet_string");
                
                tran.Set("MultiTestSet_bytes", Bytes);
                tran.Get<byte[]>("MultiTestSet_bytes");
                
                tran.Set("MultiTestSet_class", Class);
                tran.Get<TestClass>("MultiTestSet_class");

                var ret = tran.Exec();

                Assert.Equal(10L, ret[0]);
                Assert.Equal("", ret[2].ToString());
                Assert.Equal(String, ret[4].ToString());
                Assert.Equal(Bytes, ret[6]);
                Assert.Equal(Class.ToString(), ret[8].ToString());
            }
        }

        //[Fact]
        //public void MultiAsync()
        //{
        //    using (var tran = cli.Multi())
        //    {
        //        tran.Discard();
        //    }

        //    var key = Guid.NewGuid().ToString();
        //    using (var tran = cli.Multi())
        //    {
        //        long t1 = 0;
        //        tran.IncrByAsync(key, 10);

        //        tran.SetAsync("MultiAsyncTestSet_null", Null);
        //        string t3 = "";
        //        tran.GetAsync("MultiAsyncTestSet_null");

        //        tran.SetAsync("MultiAsyncTestSet_string", String);
        //        string t4 = null;
        //        tran.GetAsync("MultiAsyncTestSet_string");

        //        tran.SetAsync("MultiAsyncTestSet_bytes", Bytes);
        //        byte[] t6 = null;
        //        tran.GetAsync<byte[]>("TestSet_bytes");

        //        tran.SetAsync("MultiAsyncTestSet_class", Class);
        //        TestClass t8 = null;
        //        tran.GetAsync<TestClass>("MultiAsyncTestSet_class");

        //        tran.Discard();
        //    }

        //    using (var tran = cli.Multi())
        //    {
        //        var tasks = new List<Task>();
        //        long t1 = 0;
        //        tasks.Add(tran.IncrByAsync(key, 10).ContinueWith(t => 
        //        t1 = t.Result));

        //        tran.SetAsync("MultiAsyncTestSet_null", Null);
        //        string t3 = "";
        //        tasks.Add(tran.GetAsync("MultiAsyncTestSet_null").ContinueWith(t => t3 = t.Result));

        //        tran.SetAsync("MultiAsyncTestSet_string", String);
        //        string t4 = null;
        //        tasks.Add(tran.GetAsync("MultiAsyncTestSet_string").ContinueWith(t =>  t4 = t.Result));

        //        tran.SetAsync("MultiAsyncTestSet_bytes", Bytes);
        //        byte[] t6 = null;
        //        tasks.Add(tran.GetAsync<byte[]>("MultiAsyncTestSet_bytes").ContinueWith(t => t6 = t.Result));

        //        tran.SetAsync("MultiAsyncTestSet_class", Class);
        //        TestClass t8 = null;
        //        tasks.Add(tran.GetAsync<TestClass>("MultiAsyncTestSet_class").ContinueWith(t => t8 = t.Result));

        //        var ret = tran.Exec();
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
