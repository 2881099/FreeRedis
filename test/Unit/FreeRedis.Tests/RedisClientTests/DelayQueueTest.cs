using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace FreeRedis.Tests.RedisClientTests
{
    public class DelayQueueTest
    {
        static readonly RedisClient _client = new RedisClient(
            new ConnectionStringBuilder[]
            {
                new()
                {
                    Host = "192.168.1.114:6381",
                    Password = "111111",
                    MaxPoolSize = 5,
                    MinPoolSize = 1
                },
                new()
                {
                    Host = "192.168.1.113:6381",
                    Password = "111111",
                    MaxPoolSize = 5,
                    MinPoolSize = 1
                },
                new()
                {
                    Host = "192.168.1.113:6382",
                    Password = "111111",
                    MaxPoolSize = 1,
                    MinPoolSize = 1
                },

                new()
                {
                    Host = "192.168.1.114:6382",
                    Password = "111111",
                    MaxPoolSize = 5,
                    MinPoolSize = 1
                },
                new()
                {
                    Host = "192.168.1.116:6381",
                    Password = "111111",
                    MaxPoolSize = 5,
                    MinPoolSize = 1
                },
                new()
                {
                    Host = "192.168.1.116:6382",
                    Password = "111111",
                    MaxPoolSize = 5,
                    MinPoolSize = 1
                },
            }
        );
        private ITestOutputHelper _output;

        public DelayQueueTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Test()
        {
            var delayQueue = _client.DelayQueue("TestDelayQueue");
            delayQueue.Enqueue("1", DateTime.Now.AddSeconds(5));
            delayQueue.Enqueue("2", DateTime.Now.AddSeconds(10));
            delayQueue.Enqueue("3", DateTime.Now.AddSeconds(15));
            delayQueue.Enqueue("4", DateTime.Now.AddSeconds(20));
            delayQueue.Enqueue("5", DateTime.Now.AddSeconds(25));
        }
    }
}