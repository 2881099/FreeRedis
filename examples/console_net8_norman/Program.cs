using FreeRedis;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace console_net8_norman
{
    class Program
    {
        static Lazy<RedisClient> _cliLazy = new Lazy<RedisClient>(() =>
        {
            var r = new RedisClient(new ConnectionStringBuilder[] { "127.0.0.1:6379,database=1", "127.0.0.1:6379,database=2" }, default(Func<string,string>));
            r.Serialize = obj => JsonConvert.SerializeObject(obj);
            r.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);
            r.Notice += (s, e) => Console.WriteLine(e.Log);
            return r;
        });
        static RedisClient cli => _cliLazy.Value;

        static void Main(string[] args)
        {
            //预热
            cli.Set(Guid.NewGuid().ToString(), "我也不知道为什么刚刚好十五个字");

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
            Console.WriteLine("FreeRedis:" + stopwatch.ElapsedMilliseconds);
        }

        static readonly string String = "我是中国人";
    }
}
