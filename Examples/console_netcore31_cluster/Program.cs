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
            var r = new RedisClient(new ConnectionStringBuilder[] { "180.102.130.181:7001", "180.102.130.184:7001", "180.102.130.181:7002" });
            r.Serialize = obj => JsonConvert.SerializeObject(obj);
            r.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);
            r.Notice += (s, e) => Trace.WriteLine(e.Log);
            return r;
        });

        static RedisClient cli => _cliLazy.Value;

        static CSRedis.CSRedisClient csredis = new CSRedis.CSRedisClient(null,
            new string[] {
            "180.102.130.181:7001,poolsize=100",
            "180.102.130.184:7001,poolsize=100",
            "180.102.130.181:7002,poolsize=100"});

        static ConnectionMultiplexer seredis = ConnectionMultiplexer.Connect("180.102.130.181:7001,180.102.130.184:7001,180.102.130.181:7002");
        static IDatabase sedb => seredis.GetDatabase();

        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < 100; i++)
            {
                var tmp = Guid.NewGuid().ToString();
                cli.Set(tmp, "我也不知道为什么刚刚好十五个字");
                _ = cli.Get(tmp);
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

            for (int i = 0; i < 100; i++)
            {
                var tmp = Guid.NewGuid().ToString();
                sedb.StringSet(tmp, "我也不知道为什么刚刚好十五个字");
                _ = sedb.StringGet(tmp);
            }

            stopwatch.Stop();
            Console.WriteLine("Seredis:" + stopwatch.ElapsedMilliseconds);

            return;
        }
    }
}
