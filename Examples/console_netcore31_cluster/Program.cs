using FreeRedis;
using Newtonsoft.Json;
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

        static void Main(string[] args)
        {
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

            new StackExchangeRedis().Start();

            Console.ReadKey();
            return;
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

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
