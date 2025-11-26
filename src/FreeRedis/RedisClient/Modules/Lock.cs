using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace FreeRedis
{
    partial class RedisClient
    {
        /// <summary>
        /// 开启分布式锁，若超时返回null
        /// </summary>
        /// <param name="name">锁名称</param>
        /// <param name="timeoutSeconds">超时（秒）</param>
        /// <param name="autoDelay">自动延长锁超时时间，看门狗线程的超时时间为timeoutSeconds/2 ， 在看门狗线程超时时间时自动延长锁的时间为timeoutSeconds。除非程序意外退出，否则永不超时。</param>
        /// <returns></returns>
        public LockController Lock(string name, int timeoutSeconds, bool autoDelay = true)
        {
            name = $"RedisClientLock:{name}";
            var startTime = DateTime.Now;
            while (DateTime.Now.Subtract(startTime).TotalSeconds < timeoutSeconds)
            {
                var value = Guid.NewGuid().ToString();
                if (SetNx(name, value, timeoutSeconds) == true)
                {
                    double refreshSeconds = (double)timeoutSeconds / 2.0;
                    return new LockController(this, name, value, timeoutSeconds, refreshSeconds, autoDelay, CancellationToken.None);
                }
                Thread.CurrentThread.Join(3);
            }
            return null;
        }

        /// <summary>
        /// 开启分布式锁，若超时返回null
        /// </summary>
        /// <param name="name">锁名称</param>
        /// <param name="timeoutSeconds">独占锁过期时间</param>
        /// <param name="refrshTimeoutSeconds">每隔多久自动延长一次锁，时间要比 timeoutSeconds 小，否则没有意义</param>
        /// <param name="waitTimeoutSeconds">等待锁释放超时时间，如果这个时间内获取不到锁，则 LockController 为 null</param>
        /// <param name="autoDelay">自动延长锁超时时间，看门狗线程的超时时间为timeoutSeconds/2 ， 在看门狗线程超时时间时自动延长锁的时间为timeoutSeconds。除非程序意外退出，否则永不超时。</param>
        /// <param name="token">CancellationToken 自动取消锁</param>
        /// <returns></returns>
        public LockController Lock(string name, int timeoutSeconds, int refrshTimeoutSeconds, int waitTimeoutSeconds, bool autoDelay = true, CancellationToken token = default)
        {
            if (refrshTimeoutSeconds == 0) throw new ArgumentException(nameof(refrshTimeoutSeconds), "刷新间隔时间不能为0");
            if (waitTimeoutSeconds == 0) waitTimeoutSeconds = refrshTimeoutSeconds;

            name = $"RedisClientLock:{name}";
            var startTime = DateTime.Now;

            // 规定时间内等待锁释放
            while (DateTime.Now.Subtract(startTime).TotalSeconds < waitTimeoutSeconds)
            {
                var value = Guid.NewGuid().ToString();
                if (SetNx(name, value, timeoutSeconds) == true)
                {
                    return new LockController(this, name, value, timeoutSeconds, refrshTimeoutSeconds, autoDelay, token);
                }
                if (token.IsCancellationRequested) return null;
                Thread.CurrentThread.Join(millisecondsTimeout: 3);
            }
            return null;
        }

        public class LockController : IDisposable
        {

            RedisClient _client;
            string _name;
            string _value;
            int _timeoutSeconds;
            Timer _autoDelayTimer;
            private CancellationTokenSource _handleLostTokenSource;
            private readonly CancellationToken _token;

            /// <summary>
            /// 当刷新锁时间的看门狗线程失去与Redis连接时，导致无法刷新延长锁时间时，触发此HandelLostToken Cancel
            /// </summary>
            public CancellationToken? HandleLostToken { get; }

            internal LockController(RedisClient rds, string name, string value, int timeoutSeconds, double refreshSeconds, bool autoDelay, CancellationToken token)
            {
                _client = rds;
                _name = name;
                _value = value;
                _timeoutSeconds = timeoutSeconds;
                _token = token;
                if (autoDelay)
                {
                    _handleLostTokenSource = new CancellationTokenSource();
                    HandleLostToken = _handleLostTokenSource.Token;

                    var refreshMilli = (int)(refreshSeconds * 1000);
                    var timeoutMilli = timeoutSeconds * 1000;
                    _autoDelayTimer = new Timer(state2 => Refresh(timeoutMilli), null, refreshMilli, refreshMilli);
                }
            }

            /// <summary>
            /// 延长锁时间，锁在占用期内操作时返回true，若因锁超时被其他使用者占用则返回false
            /// </summary>
            /// <param name="milliseconds">延长的毫秒数</param>
            /// <returns>成功/失败</returns>
            public bool Delay(int milliseconds)
            {
                var ret = _client.Eval(@"local gva = redis.call('GET', KEYS[1])
if gva == ARGV[1] then
  local ttlva = redis.call('PTTL', KEYS[1])
  redis.call('PEXPIRE', KEYS[1], ARGV[2] + ttlva)
  return 1
end
return 0", new[] { _name }, _value, milliseconds)?.ToString() == "1";
                if (ret == false) _autoDelayTimer?.Dispose(); //未知情况，关闭定时器
                return ret;
            }

            /// <summary>
            /// 刷新锁时间，把key的ttl重新设置为milliseconds，锁在占用期内操作时返回true，若因锁超时被其他使用者占用则返回false
            /// </summary>
            /// <param name="milliseconds">刷新的毫秒数</param>
            /// <returns>成功/失败</returns>
            public bool Refresh(int milliseconds)
            {
                if (_token.IsCancellationRequested)
                {
                    _autoDelayTimer?.Dispose();
                }

                try
                {
                    var ret = _client.Eval(@"local gva = redis.call('GET', KEYS[1])
if gva == ARGV[1] then
  redis.call('PEXPIRE', KEYS[1], ARGV[2])
  return 1
end
return 0", new[] { _name }, _value, milliseconds)?.ToString() == "1";
                    if (ret == false)
                    {
                        _handleLostTokenSource?.Cancel();
                        _autoDelayTimer?.Dispose(); //未知情况，关闭定时器
                    }

                    return ret;
                }
                catch
                {
                    try { _handleLostTokenSource?.Cancel(); } catch { }
                    _autoDelayTimer?.Dispose(); //未知情况，关闭定时器
                    return false;//这里必须要吞掉异常，否则会导致整个程序崩溃，因为Timer的异常没有地方去处理
                }
            }

            /// <summary>
            /// 释放分布式锁
            /// </summary>
            /// <returns>成功/失败</returns>
            public bool Unlock()
            {
                _handleLostTokenSource?.Dispose();
                _autoDelayTimer?.Dispose();
                return _client.Eval(@"local gva = redis.call('GET', KEYS[1])
if gva == ARGV[1] then
  redis.call('DEL', KEYS[1])
  return 1
end
return 0", new[] { _name }, _value)?.ToString() == "1";
            }

            public void Dispose() => this.Unlock();
        }
    }

}
