using FreeRedis;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace console_netcore31_cluster
{
    class Program
    {
        static Lazy<RedisClient> _cliLazy = new Lazy<RedisClient>(() =>
        {
            var r = new RedisClient(new ConnectionStringBuilder[] { "127.0.0.1:6379", "127.0.0.1:6380" });
            r.Serialize = obj => JsonConvert.SerializeObject(obj);
            r.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);
            //r.Notice += (s, e) => Trace.WriteLine(e.Log);
            return r;
        });

        static RedisClient cli => _cliLazy.Value;

        static CSRedis.CSRedisClient csredis = new CSRedis.CSRedisClient("127.0.0.1:6379");

        static ConnectionMultiplexer seredis = ConnectionMultiplexer.Connect("127.0.0.1:6379,127.0.0.1:6380,127.0.0.1:6381");
        static IDatabase sedb => seredis.GetDatabase();

        static void Main(string[] args)
        {
            //预热
            cli.Set(Guid.NewGuid().ToString(), "我也不知道为什么刚刚好十五个字");
            sedb.StringSet(Guid.NewGuid().ToString(), "我也不知道为什么刚刚好十五个字");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < 10000; i++)
            {
                var tmp = Guid.NewGuid().ToString();
                cli.Set(tmp, "我也不知道为什么刚刚好十五个字");
                var val = cli.Get(tmp);
                if (val != "我也不知道为什么刚刚好十五个字") throw new Exception("not equal");
            }

            stopwatch.Stop();
            Console.WriteLine("FreeRedis:"+stopwatch.ElapsedMilliseconds);

            //stopwatch.Restart();
            // csredis 会出现连接不能打开的情况
            //for (int i = 0; i < 100; i++)
            //{
            //    var tmp = Guid.NewGuid().ToString();
            //    csredis.Set(tmp, "我也不知道为什么刚刚好十五个字");
            //    _ = csredis.Get(tmp);
            //}

            //stopwatch.Stop();
            //Console.WriteLine("csredis:" + stopwatch.ElapsedMilliseconds);

            stopwatch.Restart();

            for (int i = 0; i < 10000; i++)
            {
                var tmp = Guid.NewGuid().ToString();
                sedb.StringSet(tmp, "我也不知道为什么刚刚好十五个字");
                var val = sedb.StringGet(tmp);
                if (val != "我也不知道为什么刚刚好十五个字") throw new Exception("not equal");
            }

            stopwatch.Stop();
            Console.WriteLine("Seredis:" + stopwatch.ElapsedMilliseconds);

            cli.Subscribe("abc", (chan, msg) =>
            {
                Console.WriteLine($"FreeRedis {chan} => {msg}");
            });

            seredis.GetSubscriber().Subscribe("abc", (chan, msg) =>
            {
                Console.WriteLine($"Seredis {chan} => {msg}");
            });
            Console.ReadKey();

            return;
        }
    }
}
