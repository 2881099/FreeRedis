using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace hiredis.Internal
{
    partial class IdleBus<TKey, TValue>
    {

        public static void Test()
        {
            //超过1分钟没有使用，连续检测2次都这样，就销毁【实例】
            var ib = new IdleBus<string, IDisposable>(TimeSpan.FromMinutes(1));
            ib.Notice += (_, e) =>
            {
                var log = $"[{DateTime.Now.ToString("HH:mm:ss")}] 线程{Thread.CurrentThread.ManagedThreadId}：{e.Log}";
                //Trace.WriteLine(log);
                Console.WriteLine(log);
            };

            ib.Register("key1", () => new ManualResetEvent(false));
            ib.Register("key2", () => new AutoResetEvent(false));

            var item = ib.Get("key2") as AutoResetEvent;
            //获得 key2 对象，创建

            item = ib.Get("key2") as AutoResetEvent;
            //获得 key2 对象，已创建

            int num1 = ib.UsageQuantity;
            //【实例】有效数量（即已经创建了的），后台定时清理不活跃的【实例】，此值就会减少

            ib.Dispose();
        }

    }
}