using FreeRedis;
using FreeRedis.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Linq;

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
            cli.UseClientSideCaching();

            cli.Set("Interceptor01", "123123");

            var val1 = cli.Get("Interceptor01");
            var val2 = cli.Get("Interceptor01");
            var val3 = cli.Get("Interceptor01");

            Console.ReadKey();

            var val4 = cli.Get("Interceptor01");

            Console.ReadKey();
        }
    }

    static class MemoryCacheAopExtensions
    {
        public static void UseClientSideCaching(this RedisClient cli)
        {
            var sub = cli.Subscribe("__redis__:invalidate", (chan, msg) =>
            {
                var keys = msg as object[];
                foreach (var key in keys)
                {
                    _dicStrings.TryRemove(string.Concat(key), out var old);
                }
            }) as IPubSubSubscriber;

            var context = new ClientSideCachingContext(cli, sub);
            cli.Interceptors.Add(() => new MemoryCacheAop());
            cli.Unavailable += (_, e) =>
            {
                _dicStrings.Clear();
            };
            cli.Connected += (_, e) =>
            {
                e.Client.ClientTracking(true, context._sub.RedisSocket.ClientId, null, false, false, false, false);
            };
        }

        class ClientSideCachingContext
        {
            internal RedisClient _cli;
            internal IPubSubSubscriber _sub;
            public ClientSideCachingContext(RedisClient cli, IPubSubSubscriber sub)
            {
                _cli = cli;
                _sub = sub;
            }
        }

        static ConcurrentDictionary<string, object> _dicStrings = new ConcurrentDictionary<string, object>();
        class MemoryCacheAop : IInterceptor
        {
            public void After(InterceptorAfterEventArgs args)
            {
                switch (args.Command._command)
                {
                    case "GET":
                        if (_iscached == false && args.Exception == null)
                            _dicStrings.TryAdd(args.Command.GetKey(0), args.Value);
                        break;
                }
            }

            bool _iscached = false;
            public void Before(InterceptorBeforeEventArgs args)
            {
                switch (args.Command._command)
                {
                    case "GET":
                        if (_dicStrings.TryGetValue(args.Command.GetKey(0), out var tryval))
                        {
                            args.Value = tryval;
                            _iscached = true;
                        }
                        break;
                }
            }
        }
    }
}
