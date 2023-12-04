using FreeRedis;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Threading;

namespace console_net8_client_side_caching
{
    class Program
    {
        static Lazy<RedisClient> _cliLazy = new Lazy<RedisClient>(() =>
        {
            var r = new RedisClient("127.0.0.1:6379"); //redis 3.2 Single test
            //var r = new RedisClient("192.168.164.10:6379"); //redis 3.2 Single test
            //var r = new RedisClient("127.0.0.1:6379,database=1,min pool size=500,max pool size=500"); //redis 3.2
            //var r = new RedisClient("127.0.0.1:6379,database=10", "127.0.0.1:6380,database=10", "127.0.0.1:6381,database=10");
            //var r = new RedisClient(new [] { (ConnectionStringBuilder)"192.168.164.10:6379,database=1", (ConnectionStringBuilder)"192.168.164.10:6379,database=2" }); //redis 6.0
            r.Serialize = obj => JsonConvert.SerializeObject(obj);
            r.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);
            r.Notice += (s, e) => Console.WriteLine(e.Log);
            return r;
            // redis6 cluster
            // https://www.cnblogs.com/sharktech/p/14475748.html
            // /redis6-cluster.sh
            // ps -ef | grep redis
            // redis-cli --cluster create 0.0.0.0:6379 0.0.0.0:6380 0.0.0.0:6381 0.0.0.0:6382 0.0.0.0:6383 0.0.0.0:6384 --cluster-replicas 1 -a 123456
        });
        static RedisClient cli => _cliLazy.Value;

        static void Main(string[] args)
        {
			Thread.CurrentThread.CurrentCulture = new CultureInfo("nb"); 
            var test = long.Parse("-1");

			cli.UseClientSideCaching(new ClientSideCachingOptions
            {
                //本地缓存的容量
                Capacity = 3,
                //过滤哪些键能被本地缓存
                //KeyFilter = key => key.StartsWith("Interceptor"),
                //检查长期未使用的缓存
                CheckExpired = (key, dt) => DateTime.Now.Subtract(dt) > TimeSpan.FromSeconds(600)
            });

            while (Console.ReadKey().Key != ConsoleKey.Escape)
            {
                Console.WriteLine(cli.HGetAll("hash01"));
                Console.WriteLine(cli.HGet("hash01", "f3"));
                Console.WriteLine(cli.HMGet("hash01", "f3", "f2"));
			}

            cli.Set("Interceptor01", "123123"); //redis-server

            var val1 = cli.Get("Interceptor01"); //redis-server
            var val2 = cli.Get("Interceptor01"); //本地
            var val3 = cli.Get("Interceptor01"); //断点等3秒，redis-server

            cli.Set("Interceptor01", "234567"); //redis-server
            var val4 = cli.Get("Interceptor01"); //redis-server
            var val5 = cli.Get("Interceptor01"); //本地

            var val6 = cli.MGet("Interceptor01", "Interceptor02", "Interceptor03"); //redis-server
            var val7 = cli.MGet("Interceptor01", "Interceptor02", "Interceptor03"); //本地
            var val8 = cli.MGet("Interceptor01", "Interceptor02", "Interceptor03"); //本地

            cli.MSet("Interceptor01", "Interceptor01Value", "Interceptor02", "Interceptor02Value", "Interceptor03", "Interceptor03Value");  //redis-server
            var val9 = cli.MGet("Interceptor01", "Interceptor02", "Interceptor03");  //redis-server
            var val10 = cli.MGet("Interceptor01", "Interceptor02", "Interceptor03");  //本地

            //以下 KeyFilter 返回 false，从而不使用本地缓存
            cli.Set("123Interceptor01", "123123"); //redis-server

            var val11 = cli.Get("123Interceptor01"); //redis-server
            var val12 = cli.Get("123Interceptor01"); //redis-server
            var val23 = cli.Get("123Interceptor01"); //redis-server


            cli.Set("Interceptor011", Class); //redis-server
            var val0111 = cli.Get<TestClass>("Interceptor011"); //redis-server
            var val0112 = cli.Get<TestClass>("Interceptor011"); //本地
            var val0113 = cli.Get<TestClass>("Interceptor011"); //断点等3秒，redis-server

            Console.WriteLine("all test has done running");
            Console.ReadKey();

            cli.Dispose();
        }

        static readonly TestClass Class = new TestClass { Id = 1, Name = "Class名称", CreateTime = DateTime.Now, TagId = new[] { 1, 3, 3, 3, 3 } };
    }

    public class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreateTime { get; set; }

        public int[] TagId { get; set; }
    }
    
}
