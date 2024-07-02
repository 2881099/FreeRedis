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
        static readonly RedisClient _client = new RedisClient("127.0.0.1:6379,password=123");

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
                output.WriteLine($"{DateTime.Now}：{s}");

                return Task.CompletedTask;
            });
        }
    }
}