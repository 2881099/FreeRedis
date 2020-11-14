using FreeRedis;
using System;
using System.Threading;

namespace console_netcore31_pooling
{
    class Program
    {
        static Lazy<RedisClient> _cliLazy = new Lazy<RedisClient>(() =>
        {
            //var r = new RedisClient("127.0.0.1:6379", false); //redis 3.2 Single test
            var r = new RedisClient("127.0.0.1:6379,database=10"); //redis 3.2
            //var r = new RedisClient("127.0.0.1:6379,database=1", "127.0.0.1:6379,database=1");
            //var r = new RedisClient("192.168.164.10:6379,database=1"); //redis 6.0
            //r.Serialize = obj => JsonConvert.SerializeObject(obj);
            //r.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);
            //r.Notice += (s, e) => Trace.WriteLine(e.Log);
            return r;
        });
        static RedisClient cli => _cliLazy.Value;

        static void Main(string[] args)
        {
            //网络出错后，断熔，后台线程定时检查恢复
            for (var k = 0; k < 1; k++)
            {
                new Thread(() =>
                {
                    for (var a = 0; a < 10000; a++)
                    {
                        try
                        {
                            cli.Get(Guid.NewGuid().ToString());
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        Thread.CurrentThread.Join(100);
                    }
                }).Start();
            }

            Console.ReadKey();
            return;
        }
    }
}
