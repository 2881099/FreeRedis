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
    public class DelayQueueTest(ITestOutputHelper output)
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

        private readonly ITestOutputHelper _output = output;

        [Fact]
        public async Task Test()
        {
            var delayQueue = _client.DelayQueue("TestDelayQueue");
            
            //添加队列
            delayQueue.Enqueue($"Execute in 5 seconds.", TimeSpan.FromSeconds(5));
            delayQueue.Enqueue($"Execute in 10 seconds.", DateTime.Now.AddSeconds(10));
            delayQueue.Enqueue($"Execute in 15 seconds.", DateTime.Now.AddSeconds(15));
            delayQueue.Enqueue($"Execute in 20 seconds.", TimeSpan.FromSeconds(20));
            delayQueue.Enqueue($"Execute in 25 seconds.", DateTime.Now.AddSeconds(25));
            delayQueue.Enqueue($"Execute in 2024-07-02 14:30:15", DateTime.Parse("2024-07-02 14:30:15"));


            //消费延时队列
            await delayQueue.DequeueAsync(s =>
            {
                _output.WriteLine($"{DateTime.Now}：{s}");

                return Task.CompletedTask;
            });
            
        }
    }
}