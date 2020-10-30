using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using hiredis;
using Newtonsoft.Json;
using System;
using System.Text;

namespace console_netcore31_benchmark
{
    public class Program
    {
        static Lazy<RedisClient> _cliLazy = new Lazy<RedisClient>(() =>
        {
            //var r = new RedisClient("127.0.0.1:6379", false); //redis 3.2 Single test
            var r = new RedisClient("127.0.0.1:6379,database=1,poolsize=100,min pool size=100"); //redis 3.2
            //var r = new RedisClient("127.0.0.1:6379,database=1", "127.0.0.1:6379,database=1");
            //var r = new RedisClient("192.168.164.10:6379,database=1"); //redis 6.0
            r.Serialize = obj => JsonConvert.SerializeObject(obj);
            r.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);
            //r.Notice += (s, e) => Trace.WriteLine(e.Log);
            return r;
        });
        static RedisClient cli => _cliLazy.Value;
        static CSRedis.CSRedisClient csredis = new CSRedis.CSRedisClient("127.0.0.1:6379,database=2,poolsize=100");
        static StackExchange.Redis.ConnectionMultiplexer seredis = StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1:6379");
        static StackExchange.Redis.IDatabase sedb = seredis.GetDatabase(1);

        public static void Main(string[] args)
        {
            RedisHelper.Initialization(csredis);
            cli.Set("TestMGet_string1", String);
            RedisHelper.Set("TestMGet_string1", String);
            sedb.StringSet("TestMGet_string1", String);

            var summary = BenchmarkRunner.Run<SetVs>();
        }

        public class SetVs
        {
            [Benchmark]
            public void hiredis()
            {
                //cli.Set("TestMGet_string1", String);
                cli.Call(new CommandPacket("SET").Input("TestMGet_string1").InputRaw(String));
            }

            [Benchmark]
            public void CSRedisCore()
            {
                csredis.Set("TestMGet_string1", String);
            }

            [Benchmark]
            public void StackExchange()
            {
                sedb.StringSet("TestMGet_string1", String);
            }
        }

        static readonly string String = "我是中国人";
    }

    public class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreateTime { get; set; }

        public int[] TagId { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
