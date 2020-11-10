using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using Xunit;
using FreeRedis.Internal;
using System.Linq;

namespace FreeRedis.Tests
{
    public class InterceptorTests
    {
        //public static RedisClient CreateClient() => new RedisClient("127.0.0.1:6379");
        public static RedisClient CreateClient() => new RedisClient("192.168.164.10:6379");

        [Fact]
        public void Interceptor()
        {
            using (var cli = CreateClient())
            {
                cli.UseClientSideCaching();

                cli.Set("Interceptor01", "123123");

                var val1 = cli.Get("Interceptor01");
                var val2 = cli.Get("Interceptor01");
                var val3 = cli.Get("Interceptor01");

                Assert.Equal("123123", val1);
                Assert.Equal("123123", val2);
                Assert.Equal("123123", val3);
            }
        }
    }

    static class MemoryCacheAopExtensions
    {
        public static void UseClientSideCaching(this RedisClient cli)
        {
            var context = new ClientSideCachingContext(cli);
            cli.Subscribe("__redis__:invalidate", (chan, msg) =>
            {
            });

            cli.Interceptors.Add(() => new MemoryCacheAop(context));
            cli.Unavailable += (_, e) => 
            {
                _dicStrings.Clear();
            };
            cli.Connected += (_, e) =>
            {
                e.Client.ClientTracking(true, 100, null, false, false, false, false);
            };
        }

        class ClientSideCachingContext
        {
            internal RedisClient _cli;
            internal long _clientid;
            public ClientSideCachingContext(RedisClient cli)
            {
                _cli = cli;
            }
        }

        static ConcurrentDictionary<string, object> _dicStrings = new ConcurrentDictionary<string, object>();
        class MemoryCacheAop : IInterceptor
        {
            ClientSideCachingContext _context;
            public MemoryCacheAop(ClientSideCachingContext context)
            {
                _context = context;
            }

            public void After(InterceptorAfterEventArgs args)
            {
                switch (args.Command._command)
                {
                    case "GET":
                        if (_iscached == false && args.Exception == null)
                            _dicStrings.TryAdd(args.Command.GetKey(0), args.Value);
                        break;
                    case "SUBSCRIBLE":
                        if (args.Command._input.Where((a, b) => b > 0 && string.Compare(a as string, "__redis__:invalidate", true) == 0).Any())
                        {
                            _context._clientid = _context._cli.ClientId();
                        }
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
