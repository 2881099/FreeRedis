using FreeRedis;
using FreeRedis.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace console_netcore31_client_side_caching
{
    class Program
    {
        static Lazy<RedisClient> _cliLazy = new Lazy<RedisClient>(() =>
        {
            //var r = new RedisClient("127.0.0.1:6379", false); //redis 3.2 Single test
            //var r = new RedisClient("127.0.0.1:6379,database=1,min pool size=500,max pool size=500"); //redis 3.2
            //var r = new RedisClient("127.0.0.1:6379,database=1", "127.0.0.1:6379,database=1");
            var r = new RedisClient("192.168.164.10:6379,database=1"); //redis 6.0
            r.Serialize = obj => JsonConvert.SerializeObject(obj);
            r.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);
            r.Notice += (s, e) => Console.WriteLine(e.Log);
            return r;
        });
        static RedisClient cli => _cliLazy.Value;

        static void Main(string[] args)
        {
            cli.UseClientSideCaching(new ClientSideCachingOptions
            {
                //本地缓存的容量
                Capacity = 3,
                //过滤哪些键能被本地缓存
                KeyFilter = key => key.StartsWith("Interceptor"),
                //检查长期未使用的缓存
                CheckExpired = (key, dt) => DateTime.Now.Subtract(dt) > TimeSpan.FromSeconds(2)
            });

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
            Console.ReadKey();
        }
    }

    
}
