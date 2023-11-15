﻿using FreeRedis;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace console_net8_vs
{
    class Program
    {
        static Lazy<RedisClient> _cliLazy = new Lazy<RedisClient>(() =>
        {
            //var r = new RedisClient("127.0.0.1:6379", false); //redis 3.2 Single test
            var r = new RedisClient("127.0.0.1:6379,min pool size=500,max pool size=500"); //redis 3.2
            //var r = new RedisClient("127.0.0.1:6379,database=1", "127.0.0.1:6379,database=1");
            //var r = new RedisClient("192.168.164.10:6379,database=1"); //redis 6.0
            r.Serialize = obj => JsonConvert.SerializeObject(obj);
            r.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);
            //r.Notice += (s, e) => Trace.WriteLine(e.Log);
            return r;
        });
        static RedisClient cli => _cliLazy.Value;
        static StackExchange.Redis.ConnectionMultiplexer seredis = StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1:6379");
        static StackExchange.Redis.IDatabase sedb = seredis.GetDatabase(1);

        static void Main(string[] args)
        {

            sedb.StringSet("key1", (string)null);

            var val111 = sedb.StringGet("key1");














            RedisHelper.Initialization(new CSRedis.CSRedisClient("127.0.0.1:6379,asyncPipeline=true,preheat=100,poolsize=100"));
            cli.Set("TestMGet_null1", "");
            RedisHelper.Set("TestMGet_null1", "");
            sedb.StringSet("TestMGet_string1", String);
            ThreadPool.SetMinThreads(10001, 10001);
            Stopwatch sw = new Stopwatch();
            var tasks = new List<Task>();
            var results = new ConcurrentQueue<string>();

            cli.FlushDb();
            results.Clear();
            sw.Reset();
            sw.Start();
            for (var a = 0; a < 10000; a++)
            {
                var tmp = Guid.NewGuid().ToString();
                sedb.StringSet(tmp, String);
                var val = sedb.StringGet(tmp);
                if (val != String) throw new Exception("not equal");
                results.Enqueue(val);
            }
            sw.Stop();
            Console.WriteLine("StackExchange(0-10000): " + sw.ElapsedMilliseconds + "ms results: " + results.Count);
            tasks.Clear();
            results.Clear();
            cli.FlushDb();

            //sw.Reset();
            //sw.Start();
            //tasks = new List<Task>();
            //for (var a = 0; a < 100000; a++)
            //{
            //    tasks.Add(Task.Run(() =>
            //    {
            //        var tmp = Guid.NewGuid().ToString();
            //        sedb.StringSet(tmp, String);
            //        var val = sedb.StringGet(tmp);
            //        if (val != String) throw new Exception("not equal");
            //        results.Enqueue(val);
            //    }));
            //}
            //Task.WaitAll(tasks.ToArray());
            //sw.Stop();
            //Console.WriteLine("StackExchange(Task.WaitAll 100000): " + sw.ElapsedMilliseconds + "ms results: " + results.Count);
            //tasks.Clear();
            //results.Clear();
            //cli.FlushDb();

            sw.Reset();
            sw.Start();
            Task.Run(async () =>
            {
                for (var a = 0; a < 10000; a++)
                {
                    var tmp = Guid.NewGuid().ToString();
                    await sedb.StringSetAsync(tmp, String);
                    var val = await sedb.StringGetAsync(tmp);
                    if (val != String) throw new Exception("not equal");
                    results.Enqueue(val);
                }
            }).Wait();
            sw.Stop();
            Console.WriteLine("StackExchangeAsync(0-10000): " + sw.ElapsedMilliseconds + "ms results: " + results.Count);
            tasks.Clear();
            results.Clear();
            cli.FlushDb();

            sw.Reset();
            sw.Start();
            tasks = new List<Task>();
            for (var a = 0; a < 100000; a++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var tmp = Guid.NewGuid().ToString();
                    await sedb.StringSetAsync(tmp, String);
                    var val = await sedb.StringGetAsync(tmp);
                    if (val != String) throw new Exception("not equal");
                    results.Enqueue(val);
                }));
            }
            Task.WaitAll(tasks.ToArray());
            sw.Stop();
            Console.WriteLine("StackExchangeAsync(Task.WaitAll 100000): " + sw.ElapsedMilliseconds + "ms results: " + results.Count + "\r\n");
            tasks.Clear();
            results.Clear();
            cli.FlushDb();


            sw.Reset();
            sw.Start();
            for (var a = 0; a < 10000; a++)
            {
                var tmp = Guid.NewGuid().ToString();
                cli.Set(tmp, String);
                var val = cli.Get(tmp);
                if (val != String) throw new Exception("not equal");
                results.Enqueue(val);
            }
            sw.Stop();
            Console.WriteLine("FreeRedisSync(0-10000): " + sw.ElapsedMilliseconds + "ms results: " + results.Count);
            tasks.Clear();
            results.Clear();
            cli.FlushDb();

            sw.Reset();
            sw.Start();
            tasks = new List<Task>();
            for (var a = 0; a < 100000; a++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var tmp = Guid.NewGuid().ToString();
                    cli.Set(tmp, String);
                    var val = cli.Get(tmp);
                    if (val != String) throw new Exception("not equal");
                    results.Enqueue(val);
                }));
            }
            Task.WaitAll(tasks.ToArray());
            sw.Stop();
            Console.WriteLine("FreeRedisSync(Task.WaitAll 100000): " + sw.ElapsedMilliseconds + "ms results: " + results.Count);
            tasks.Clear();
            results.Clear();
            cli.FlushDb();


            sw.Reset();
            sw.Start();
            Task.Run(async () =>
            {
                for (var a = 0; a < 10000; a++)
                {
                    var tmp = Guid.NewGuid().ToString();
                    await cli.SetAsync(tmp, String);
                    var val = await cli.GetAsync(tmp);
                    if (val != String) throw new Exception("not equal");
                    results.Enqueue(val);
                }
            }).Wait();
            sw.Stop();
            Console.WriteLine("FreeRedisAsync(0-10000): " + sw.ElapsedMilliseconds + "ms results: " + results.Count);
            tasks.Clear();
            results.Clear();
            cli.FlushDb();

            sw.Reset();
            sw.Start();
            tasks = new List<Task>();
            for (var a = 0; a < 100000; a++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var tmp = Guid.NewGuid().ToString();
                    await cli.SetAsync(tmp, String);
                    var val = await cli.GetAsync(tmp);
                    if (val != String) throw new Exception("not equal");
                    results.Enqueue(val);
                }));
            }
            Task.WaitAll(tasks.ToArray());
            sw.Stop();
            Console.WriteLine("FreeRedisAsync(Task.WaitAll 100000): " + sw.ElapsedMilliseconds + "ms results: " + results.Count);
            tasks.Clear();
            results.Clear();
            cli.FlushDb();

            return;

            //sw.Reset();
            //sw.Start();
            //Task.Run(async () =>
            //{
            //    for (var a = 0; a < 100000; a++)
            //    {
            //        var tmp = Guid.NewGuid().ToString();
            //        await cli.SetAsync(tmp, String);
            //        var val = await cli.GetAsync(tmp);
            //        if (val != String) throw new Exception("not equal");
            //        results.Enqueue(val);
            //    }
            //}).Wait();
            //sw.Stop();
            //Console.WriteLine("FreeRedisAsync(0-100000): " + sw.ElapsedMilliseconds + "ms results: " + results.Count);
            //tasks.Clear();
            //results.Clear();
            //cli.FlushDb();

            //FreeRedis.Internal.AsyncRedisSocket.sb.Clear();
            //FreeRedis.Internal.AsyncRedisSocket.sw.Start();
            //sw.Reset();
            //sw.Start();
            //tasks = new List<Task>();
            //for (var a = 0; a < 100000; a++)
            //{
            //    tasks.Add(Task.Run(async () =>
            //    {
            //        var tmp = Guid.NewGuid().ToString();
            //        await cli.SetAsync(tmp, String);
            //        var val = await cli.GetAsync(tmp);
            //        if (val != String) throw new Exception("not equal");
            //        results.Enqueue(val);
            //    }));
            //}
            //Task.WaitAll(tasks.ToArray());
            //sw.Stop();
            ////var sbstr = FreeRedis.Internal.AsyncRedisSocket.sb.ToString()
            ////sbstr = sbstr + sbstr.Split("\r\n").Length + "条消息 ;
            //Console.WriteLine("FreeRedisAsync(Task.WaitAll 100000): " + sw.ElapsedMilliseconds + "ms results: " + results.Count);
            //tasks.Clear();
            //results.Clear();
            //cli.FlushDb();

            sw.Reset();
            sw.Start();
            using (var pipe = cli.StartPipe())
            {
                for (var a = 0; a < 100000; a++)
                {
                    var tmp = Guid.NewGuid().ToString();
                    pipe.Set(tmp, String);
                    var val = pipe.Get(tmp);
                }
                var vals = pipe.EndPipe();
                for (var a = 1; a < 200000; a += 2)
                {
                    var val = vals[a].ToString();
                    if (val != String) throw new Exception("not equal");
                    results.Enqueue(val);
                }
            }
            sw.Stop();
            Console.WriteLine("FreeRedisPipeline(0-100000): " + sw.ElapsedMilliseconds + "ms results: " + results.Count + "\r\n");
            tasks.Clear();
            results.Clear();
            cli.FlushDb();

            //sw.Reset();
            //sw.Start();
            //for (var a = 0; a < 100000; a++)
            //    cli.Call(new CommandPacket("SET").Input("TestMGet_string1").InputRaw(String));
            //sw.Stop();
            //Console.WriteLine("FreeRedis2: " + sw.ElapsedMilliseconds + "ms");
            tasks.Clear();
            results.Clear();
            cli.FlushDb();

            //sw.Reset();
            //sw.Start();
            //for (var a = 0; a < 100000; a++)
            //{
            //    using (var rds = cli.GetTestRedisSocket())
            //    {
            //        var cmd = new CommandPacket("SET").Input("TestMGet_string1").InputRaw(String);
            //        rds.Write(cmd);
            //        cmd.Read<string>();
            //    }
            //}
            //sw.Stop();
            //Console.WriteLine("FreeRedis4: " + sw.ElapsedMilliseconds + "ms");
            tasks.Clear();
            results.Clear();
            cli.FlushDb();


            sw.Reset();
            sw.Start();
            for (var a = 0; a < 100000; a++)
            {
                var tmp = Guid.NewGuid().ToString();
                RedisHelper.Set(tmp, String);
                var val = RedisHelper.Get(tmp);
                if (val != String) throw new Exception("not equal");
                results.Enqueue(val);
            }
            sw.Stop();
            Console.WriteLine("CSRedisCore(0-100000): " + sw.ElapsedMilliseconds + "ms results: " + results.Count);
            tasks.Clear();
            results.Clear();
            cli.FlushDb();

            sw.Reset();
            sw.Start();
            tasks = new List<Task>();
            for (var a = 0; a < 100000; a++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var tmp = Guid.NewGuid().ToString();
                    RedisHelper.Set(tmp, String);
                    var val = RedisHelper.Get(tmp);
                    if (val != String) throw new Exception("not equal");
                    results.Enqueue(val);
                }));
            }
            Task.WaitAll(tasks.ToArray());
            sw.Stop();
            Console.WriteLine("CSRedisCore(Task.WaitAll 100000): " + sw.ElapsedMilliseconds + "ms results: " + results.Count);
            tasks.Clear();
            results.Clear();
            cli.FlushDb();

            sw.Reset();
            sw.Start();
            Task.Run(async () =>
            {
                for (var a = 0; a < 100000; a++)
                {
                    var tmp = Guid.NewGuid().ToString();
                    await RedisHelper.SetAsync(tmp, String);
                    var val = await RedisHelper.GetAsync(tmp);
                    if (val != String) throw new Exception("not equal");
                    results.Enqueue(val);
                }
            }).Wait();
            sw.Stop();
            Console.WriteLine("CSRedisCoreAsync(0-100000): " + sw.ElapsedMilliseconds + "ms results: " + results.Count);
            tasks.Clear();
            results.Clear();
            cli.FlushDb();

            sw.Reset();
            sw.Start();
            tasks = new List<Task>();
            for (var a = 0; a < 100000; a++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var tmp = Guid.NewGuid().ToString();
                    await RedisHelper.SetAsync(tmp, String);
                    var val = await RedisHelper.GetAsync(tmp);
                    //if (val != String) throw new Exception("not equal");
                    results.Enqueue(val);
                }));
            }
            Task.WaitAll(tasks.ToArray());
            sw.Stop();
            Console.WriteLine("CSRedisCoreAsync(Task.WaitAll 100000): " + sw.ElapsedMilliseconds + "ms results: " + results.Count + "\r\n");
            tasks.Clear();
            results.Clear();
            cli.FlushDb();
        }

        static readonly string String = "我是中国人";
    }
}
