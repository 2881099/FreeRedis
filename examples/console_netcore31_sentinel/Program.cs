using hiredis;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace console_netcore31_sentinel
{
    class Program
    {
        static Lazy<RedisClient> _cliLazy = new Lazy<RedisClient>(() =>
        {
            var r = new RedisClient("mymaster,default=3", new[] { "127.0.0.1:26379", "127.0.0.1:26479", "127.0.0.1:26579" }, false);
            r.Serialize = obj => JsonConvert.SerializeObject(obj);
            r.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);
            //r.Notice += (s, e) => Trace.WriteLine(e.Log);
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

            Console.ReadKey();
            return;
        }

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
