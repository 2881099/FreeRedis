using FreeRedis;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace console_netcore31
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
            using (var local = cli.GetDatabase())
            {
                var r1 = local.Call(new CommandPacket("Subscribe").Input("abc"));
                var r2 = local.Ping();
                var r3 = local.Ping("testping123");
                //var r4 = local.Call(new CommandPacket("punSubscribe").Input("*"));
            }

            using (cli.Subscribe("abc", ondata))
            {
                using (cli.Subscribe("abcc", ondata))
                {
                    using (cli.PSubscribe("*", ondata))
                    {
                        Console.ReadKey();
                    }
                    Console.ReadKey();
                }
                Console.ReadKey();
            }
            Console.WriteLine("one more time");
            Console.ReadKey();
            using (cli.Subscribe("abc", ondata))
            {
                using (cli.Subscribe("abcc", ondata))
                {
                    using (cli.PSubscribe("*", ondata))
                    {
                        Console.ReadKey();
                    }
                    Console.ReadKey();
                }
                Console.ReadKey();
            }
            void ondata(string channel, object data)
            {
                Console.WriteLine($"{channel} -> {data}");
            }
            //return;
        }

    }
}
