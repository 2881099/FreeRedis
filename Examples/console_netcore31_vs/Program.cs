using FreeRedis;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Text;

namespace console_netcore31_vs
{
    class Program
    {
        static Lazy<RedisClient> _cliLazy = new Lazy<RedisClient>(() =>
        {
            //var r = new RedisClient("127.0.0.1:6379", false); //redis 3.2 Single test
            var r = new RedisClient("127.0.0.1:6379,database=1"); //redis 3.2
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
            RedisHelper.Initialization(new CSRedis.CSRedisClient("127.0.0.1:6379,database=2"));
            cli.Set("TestMGet_null1", "");
            RedisHelper.Set("TestMGet_null1", "");
            sedb.StringSet("TestMGet_string1", String);
            Stopwatch sw = new Stopwatch();

            //sw.Reset();
            //sw.Start();
            //using (var local = cli.GetShareClient())
            //{
            //    local.ClientReply(ClientReplyType.off);
            //    for (var a = 0; a < 10000; a++)
            //        local.Set("TestMGet_string1", String);
            //    local.ClientReply(ClientReplyType.on);
            //}
            //sw.Stop();
            //Console.WriteLine("FreeRedis0: " + sw.ElapsedMilliseconds + "ms");

            //var sw2 = new Stopwatch();
            //sw.Reset();
            //sw.Start();
            //using (var rds = cli.GetTestRedisSocket())
            //{
            //    var strea = rds.Stream;
            //    for (var a = 0; a < 10000; a++)
            //    {
            //        rds.Write(new CommandPacket("SET").Input("TestMGet_string1").InputRaw(String));
            //    }

            //    sw2.Reset();
            //    sw2.Start();
            //    for (var a = 0; a < 10000; a++)
            //    {

            //        strea.ReadByte();

            //        var sb = new StringBuilder();
            //        char c;
            //        bool should_break = false;
            //        while (true)
            //        {
            //            c = (char)strea.ReadByte();
            //            if (c == '\r') // TODO: remove hardcoded
            //                should_break = true;
            //            else if (c == '\n' && should_break)
            //                break;
            //            else
            //            {
            //                sb.Append(c);
            //                should_break = false;
            //            }
            //        }
            //    }
            //    sw2.Stop();
            //}
            //sw.Stop();
            //Console.WriteLine("FreeRedis1: " + sw.ElapsedMilliseconds + "ms, " + sw2.ElapsedMilliseconds + "ms");

            sw.Reset();
            sw.Start();
            for (var a = 0; a < 10000; a++)
                cli.Set("TestMGet_string1", String);
            sw.Stop();
            Console.WriteLine("FreeRedis1: " + sw.ElapsedMilliseconds + "ms");

            sw.Reset();
            sw.Start();
            for (var a = 0; a < 10000; a++)
                cli.Set("TestMGet_string1", String);
            sw.Stop();
            Console.WriteLine("FreeRedis2: " + sw.ElapsedMilliseconds + "ms");

            //sw.Reset();
            //sw.Start();
            //for (var a = 0; a < 10000; a++)
            //    cli.Call(new CommandPacket("SET").Input("TestMGet_string1").InputRaw(String));
            //sw.Stop();
            //Console.WriteLine("FreeRedis2: " + sw.ElapsedMilliseconds + "ms");

            //sw.Reset();
            //sw.Start();
            //for (var a = 0; a < 10000; a++)
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

            sw.Reset();
            sw.Start();
            for (var a = 0; a < 10000; a++)
                RedisHelper.Set("TestMGet_string1", String);
            sw.Stop();
            Console.WriteLine("CSRedisCore: " + sw.ElapsedMilliseconds + "ms");

            sw.Reset();
            sw.Start();
            for (var a = 0; a < 10000; a++)
                sedb.StringSet("TestMGet_string1", String);
            sw.Stop();
            Console.WriteLine("StackExchange: " + sw.ElapsedMilliseconds + "ms");
        }

        static readonly string String = "我是中国人";
    }
}
