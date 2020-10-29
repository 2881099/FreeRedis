using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace hiredis.Internal
{
    partial class IdleBus<TKey, TValue>
    {
        bool _threadStarted = false;
        object _threadStartedLock = new object();

        void ThreadScanWatch(ItemInfo item)
        {
            var startThread = false;
            if (_threadStarted == false)
                lock (_threadStartedLock)
                    if (_threadStarted == false)
                        startThread = _threadStarted = true;

            if (startThread)
                new Thread(() =>
                {
                    this.ThreadScanWatchHandler();
                    lock (_threadStartedLock)
                        _threadStarted = false;
                }).Start();
        }


        public class TimeoutScanOptions
        {
            /// <summary>
            /// 扫描线程间隔（默认值：2秒）
            /// </summary>
            public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(2);
            /// <summary>
            /// 扫描线程空闲多少秒退出（默认值：10秒）
            /// </summary>
            public int QuitWaitSeconds { get; set; } = 10;
            /// <summary>
            /// 扫描的每批数量（默认值：512）<para></para>
            /// 可防止注册数量太多时导致 CPU 占用过高
            /// </summary>
            public int BatchQuantity { get; set; } = 512;
            /// <summary>
            /// 达到扫描的每批数量时，线程等待（默认值：1秒）
            /// </summary>
            public TimeSpan BatchQuantityWait { get; set; } = TimeSpan.FromSeconds(1);
        }
        /// <summary>
        /// 扫描过期对象的设置<para></para>
        /// 机制：当窗口里有存活对象时，扫描线程才会开启（只开启一个线程）。<para></para>
        /// 连续多少秒都没存活的对象时，才退出扫描。
        /// </summary>
        public TimeoutScanOptions ScanOptions { get; } = new TimeoutScanOptions();

        void ThreadScanWatchHandler()
        {
            var couter = 0;
            while (isdisposed == false)
            {
                if (ThreadJoin(ScanOptions.Interval) == false) return;
                this.InternalRemoveDelayHandler();

                if (_usageQuantity == 0)
                {
                    couter = couter + (int)ScanOptions.Interval.TotalSeconds;
                    if (couter < ScanOptions.QuitWaitSeconds) continue;
                    break;
                }
                couter = 0;

                var keys = _dic.Keys.ToArray();
                long keysIndex = 0;
                foreach (var key in keys)
                {
                    if (isdisposed) return;
                    ++keysIndex;
                    if (ScanOptions.BatchQuantity > 0 && keysIndex % ScanOptions.BatchQuantity == 0)
                    {
                        if (ThreadJoin(ScanOptions.BatchQuantityWait) == false) return;
                    }

                    if (_dic.TryGetValue(key, out var item) == false) continue;
                    if (item.value == null) continue;
                    if (DateTime.Now.Subtract(item.lastActiveTime) <= item.idle) continue;
                    try
                    {
                        var now = DateTime.Now;
                        if (item.Release(() => DateTime.Now.Subtract(item.lastActiveTime) > item.idle && item.lastActiveTime >= item.createTime))
                            //防止并发有其他线程创建，最后活动时间 > 创建时间
                            this.OnNotice(new NoticeEventArgs(NoticeType.AutoRelease, item.key, null, $"{key} ---自动释放成功，耗时 {DateTime.Now.Subtract(now).TotalMilliseconds}ms，{_usageQuantity}/{Quantity}"));
                    }
                    catch (Exception ex)
                    {
                        this.OnNotice(new NoticeEventArgs(NoticeType.AutoRelease, item.key, ex, $"{key} ---自动释放执行出错：{ex.Message}"));
                    }
                }
            }
        }

        bool ThreadJoin(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero) return true;
            var milliseconds = interval.TotalMilliseconds;
            var seconds = Math.Floor(milliseconds / 1000);
            milliseconds = milliseconds - seconds * 1000;

            for (var a = 0; a < seconds; a++)
            {
                Thread.CurrentThread.Join(TimeSpan.FromSeconds(1));
                if (isdisposed) return false;
            }
            for (var a = 0; a < milliseconds; a += 200)
            {
                Thread.CurrentThread.Join(TimeSpan.FromMilliseconds(200));
                if (isdisposed) return false;
            }
            return true;
        }
    }
}