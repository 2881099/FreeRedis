using FreeRedis;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace console_net8
{
    class ResetCommandAop : IInterceptor
    {
        public void After(InterceptorAfterEventArgs args)
        {
        }

        public void Before(InterceptorBeforeEventArgs args)
        {
            if (args.Command._command == "AUTH")
            {
                args.Command.Command("passwd");
            }
        }
    }

    class Program
    {
        static Lazy<RedisClient> _cliLazy = new Lazy<RedisClient>(() =>
        {
            var r = new RedisClient("127.0.0.1:6379"); //redis 3.2 Single test
            //r.Interceptors.Add(() => new ResetCommandAop());
            //var r = new RedisClient("localhost:6379,database=9,password=123456"); //redis 3.2
            //var r = new RedisClient("127.0.0.1:6379,database=1", "127.0.0.1:6379,database=1");
            //var r = new RedisClient(new ConnectionStringBuilder[] { "192.168.164.10:6381,password=123456,subscribleReadBytes=true" }); //redis 7.0 cluster
            r.Serialize = obj => JsonConvert.SerializeObject(obj);
            r.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);
            r.Notice += (s, e) => 
            { 
                Console.WriteLine(e.Log); 
            };
            return r;
        });
        static RedisClient cli => _cliLazy.Value;

        //static StackExchange.Redis.ConnectionMultiplexer seredis = StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1:6379");
        //static StackExchange.Redis.IDatabase sedb = seredis.GetDatabase(1);

        static void Main(string[] args)
        {
            var vs1 = cli.Get<string[]>("sdlfksdjf");
            var vs2 = cli.Get<List<string>>("sdlfksdjf");


			Console.WriteLine(typeof(GeoUnit).FromObject(null));
            Console.WriteLine(typeof(GeoUnit).FromObject(""));
            Console.WriteLine(typeof(GeoUnit).FromObject("ft"));
            Console.WriteLine(Array.CreateInstance(typeof(GeoMember[]).GetElementType(), 0));
            var redis = new RedisClient("127.0.0.1:6379,ssl=false");

			foreach (var result1 in redis.SScan("tset1", "*", 100))
			{

			}
			foreach (var result1 in redis.SScan<byte[]>("tset1", "*", 100))
			{

			}
			foreach (var result1 in  redis.HScan("thash",  "*", 100))
            {

            }
			foreach (var result1 in redis.HScan<byte[]>("thash", "*", 100))
            {

            }

			redis.Get("xxx");

            var result71 = cli.AclGetUser("sample");

            void ondata(string channel, object data)
            {
                Console.WriteLine($"{channel} -> {data}");
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

            //cli.SSubscribe("key1", (key, msg) =>
            //{
            //    Console.WriteLine(key + ": " + msg);
            //});
            //cli.SSubscribe("key11", (key, msg) =>
            //{
            //    Console.WriteLine(key + ": " + msg);
            //});
            //Console.ReadKey();
            //cli.PubSubShardChannels("*");
            //cli.SPublish("key11", "111");
            //cli.SPublish("key1", "222");

            var result7 = cli.AclGetUser("sample");

            cli.Set("num", 10);
            using (var tran = cli.Multi())
            {
                tran.Watch("num");
                tran.IncrBy("num", 80);
                tran.Exec();
            }


            var keys = cli.Scan(0, "*", 100, null);
            Console.WriteLine(string.Join(",", keys.items));
            foreach (var ks in cli.Scan("*", 3, null)) Console.WriteLine(string.Join(",", ks));

            var hkeys = cli.HScan("key1", 0, "*", 100);
            Console.WriteLine(string.Join(",", hkeys.items.Select(a => $"{a.Key}={a.Value}")));
            foreach (var ks in cli.HScan("key1", "*", 3)) Console.WriteLine(string.Join(",", ks.Select(a => $"{a.Key}={a.Value}")));

            var skeys = cli.SScan("skey1", 0, "*", 100);
            Console.WriteLine(string.Join(",", skeys.items));
            foreach (var ks in cli.SScan("skey1", "*", 3)) Console.WriteLine(string.Join(",", ks));

            var zkeys = cli.ZScan("zkey1", 0, "*", 100);
            Console.WriteLine(string.Join(",", zkeys.items.Select(a => $"{a.member}={a.score}")));
            foreach (var ks in cli.ZScan("zkey1", "*", 3)) Console.WriteLine(string.Join(",", ks.Select(a => $"{a.member}={a.score}")));


            cli.Ping();

            var cmd = new CommandPacket("AUTH").Input("user1", "password1")
                .Command("password");

            for (var a = 0; a < 200; a++)
            {
                new Thread(() =>
                {
                    while (true)
                    {
                        try
                        {
                            cli.Get("key" + a);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"线程{a}: [{DateTime.Now.ToString("HH:mm:ss")}] " + ex.ToString());
                            //Thread.CurrentThread.Join(30000);
                        }
                        Thread.CurrentThread.Join(100);
                    }
                }).Start();
            }
            while(Console.ReadKey().Key != ConsoleKey.Escape)
            {

            }


            cli.JsonSet("freedis.test",System.Text.Json.JsonSerializer.Serialize( new TestClass
            {
                Id = 1,
                CreateTime = DateTime.Now,
                Name = "张三",
                TagId = new int[] { 1, 2, 3, 4 },
                Deleted = true,
            }));
            var mem = cli.JsonStrAppend("freedis.test", "狂徒", "$.Name");
            Console.WriteLine(JsonConvert.SerializeObject(mem));
            var result =System.Text.Json.JsonSerializer.Deserialize<TestClass[]>( cli.JsonGet("freedis.test"));
            Console.WriteLine(JsonConvert.SerializeObject(result));
            //cli.SubscribeList("list01", msg =>
            //{
            //    if (!string.IsNullOrEmpty(msg))
            //        Console.WriteLine("SubscribeList_list01: " + msg);
            //});
            //cli.SubscribeListBroadcast("list01", "client01", msg =>
            //{
            //    if (!string.IsNullOrEmpty(msg))
            //        Console.WriteLine("SubscribeListBroadcast_client01_list01: " + msg);
            //});
            //cli.SubscribeListBroadcast("list01", "client01", msg =>
            //{
            //    if (!string.IsNullOrEmpty(msg))
            //        Console.WriteLine("SubscribeListBroadcast_client01_list01: " + msg);
            //});
            //cli.SubscribeListBroadcast("list01", "client02", msg =>
            //{
            //    if (!string.IsNullOrEmpty(msg))
            //        Console.WriteLine("SubscribeListBroadcast_client02_list01: " + msg);
            //});
            //Console.ReadKey();

            //var info = cli.Info();
            //var info1 = cli.Info("server");

            //RedisHelper.Initialization(new CSRedis.CSRedisClient("127.0.0.1:6379,database=2"));
            //cli.Set("TestMGet_null1", String);
            //RedisHelper.Set("TestMGet_null1", String);
            //sedb.StringSet("TestMGet_string1", String);

            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            //cli.Set("TestMGet_string1", String);
            //cli.Set("TestMGet_bytes1", Bytes);
            //cli.Set("TestMGet_string2", String);
            //cli.Set("TestMGet_bytes2", Bytes);
            //cli.Set("TestMGet_string3", String);
            //cli.Set("TestMGet_bytes3", Bytes);
            //sw.Stop();
            //Console.WriteLine("FreeRedis: " + sw.ElapsedMilliseconds + "ms");

            //sw.Reset();
            //sw.Start();
            //cli.Set("TestMGet_string1", String);
            //cli.Set("TestMGet_bytes1", Bytes);
            //cli.Set("TestMGet_string2", String);
            //cli.Set("TestMGet_bytes2", Bytes);
            //cli.Set("TestMGet_string3", String);
            //cli.Set("TestMGet_bytes3", Bytes);
            //sw.Stop();
            //Console.WriteLine("FreeRedis: " + sw.ElapsedMilliseconds + "ms");

            //sw.Reset();
            //sw.Start();
            //RedisHelper.Set("TestMGet_string1", String);
            //RedisHelper.Set("TestMGet_bytes1", Bytes);
            //RedisHelper.Set("TestMGet_string2", String);
            //RedisHelper.Set("TestMGet_bytes2", Bytes);
            //RedisHelper.Set("TestMGet_string3", String);
            //RedisHelper.Set("TestMGet_bytes3", Bytes);
            //sw.Stop();
            //Console.WriteLine("CSRedisCore: " + sw.ElapsedMilliseconds + "ms");

            //sw.Reset();
            //sw.Start();
            //sedb.StringSet("TestMGet_string1", String);
            //sedb.StringSet("TestMGet_bytes1", Bytes);
            //sedb.StringSet("TestMGet_string2", String);
            //sedb.StringSet("TestMGet_bytes2", Bytes);
            //sedb.StringSet("TestMGet_string3", String);
            //sedb.StringSet("TestMGet_bytes3", Bytes);
            //sw.Stop();
            //Console.WriteLine("StackExchange: " + sw.ElapsedMilliseconds + "ms");

        }

        static readonly string String = "我是中国人";
        static readonly byte[] Bytes = Encoding.UTF8.GetBytes("这是一个byte字节");
    }

    public class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreateTime { get; set; }

        public int[] TagId { get; set; }
        public bool Deleted { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
