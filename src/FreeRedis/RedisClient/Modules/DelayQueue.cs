using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
        /// <summary>
        /// 延时队列
        /// </summary>
        /// <param name="queueKey">延时队列Key</param>
        /// <returns></returns>
        public DelayQueue DelayQueue(string queueKey) => new DelayQueue(this, queueKey);
    }

    /// <summary>
    /// 延时队列
    /// </summary>
    public class DelayQueue
    {
        private readonly RedisClient _redisClient = null;

        private readonly string _queueKey;


        public DelayQueue(RedisClient redisClient, string queueKey)
        {
            _redisClient = redisClient;
            _queueKey = queueKey;
        }


        /// <summary>
        /// 写入延时队列
        /// </summary>
        /// <param name="value">队列值：值不可重复</param>
        /// <param name="delay">延迟执行时间</param>
        /// <returns></returns>
        public bool Enqueue(string value, TimeSpan delay)
        {
            var time = DateTime.UtcNow.Add(delay);
            long timestamp = (time.Ticks - new DateTime(1970, 1, 1).Ticks) / TimeSpan.TicksPerMillisecond;

            var res = _redisClient.ZAdd(_queueKey, timestamp, value);
            return res > 0;
        }

        /// <summary>
        /// 写入延时队列
        /// </summary>
        /// <param name="value">队列值：值不可重复</param>
        /// <param name="delay">延迟执行时间</param>
        /// <returns></returns>
        public bool Enqueue(string value, DateTime delay)
        {
            var time = TimeZoneInfo.ConvertTimeToUtc(delay);
            long timestamp = (time.Ticks - new DateTime(1970, 1, 1).Ticks) / TimeSpan.TicksPerMillisecond;
            var res = _redisClient.ZAdd(_queueKey, timestamp, value);
            return res > 0;
        }

        /// <summary>
        /// 消费延时队列，多个消费端不会重复
        /// </summary>
        /// <param name="action">消费委托</param>
        /// <param name="choke">轮询队列时长，默认400毫秒，值越小越准确</param>
        public void Dequeue(Action<string> action, int choke = 400, CancellationToken? token = null)
        {
            Thread thread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        if (token != null && token.Value.IsCancellationRequested)
                            break;

                        //阻塞节省CPU
                        Thread.Sleep(choke);
                        var res = InternalDequeue();
                        if (!string.IsNullOrWhiteSpace(res))
                            action.Invoke(res);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            });

            thread.Start();
        }

#if isasync

        /// <summary>
        /// 消费延时队列，多个消费端不会重复
        /// </summary>
        /// <param name="action">消费委托</param>
        /// <param name="choke">轮询队列时长，默认400毫秒，值越小越准确</param>
        /// <param name="token"></param>
        public Task DequeueAsync(Func<string, Task> action, int choke = 400, CancellationToken? token = null)
        {
            return Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (token != null && token.Value.IsCancellationRequested)
                            break;

                        //阻塞节省CPU
                        await Task.Delay(choke);
                        var res = InternalDequeue();
                        if (!string.IsNullOrWhiteSpace(res))
                            await action.Invoke(res);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

#endif

        //取队列任务
        private string InternalDequeue()
        {
            long timestamp = (DateTime.UtcNow.Ticks - new DateTime(1970, 1, 1).Ticks) / TimeSpan.TicksPerMillisecond;

            //lua脚本保持原子性
            var script = @"
local zrange = redis.call('zrangebyscore',KEYS[1],0,ARGV[1],'LIMIT',0,1)
if next(zrange) ~= nil and #zrange > 0 then
    local rmnum = redis.call('zrem',KEYS[1],unpack(zrange))
    if(rmnum > 0) then
        return zrange
    end
else
    return {}
end";

            if (_redisClient.Eval(script, new[] { _queueKey }, timestamp) is object[] eval && eval.Any())
            {
                var item = eval[0].ToString() ?? string.Empty;
                return item;
            }

            return default;
        }
    }
}