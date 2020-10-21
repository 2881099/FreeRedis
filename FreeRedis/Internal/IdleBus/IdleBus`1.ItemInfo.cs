using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace FreeRedis.Internal
{
    partial class IdleBus<TKey, TValue>
    {

        class ItemInfo : IDisposable
        {
            internal IdleBus<TKey, TValue> ib;
            internal TKey key;
            internal Func<TValue> create;
            internal TimeSpan idle;
            internal DateTime createTime;
            internal DateTime lastActiveTime;
            internal long activeCounter;
            internal int releaseErrorCounter;

            internal TValue value { get; private set; }
            internal TValue firstValue { get; private set; }
            internal bool IsRegisterError { get; private set; }
            object valueLock = new object();

            internal TValue GetOrCreate()
            {
                if (isdisposed == true) return null;
                if (value == null)
                {
                    var iscreate = false;
                    var now = DateTime.Now;
                    try
                    {
                        lock (valueLock)
                        {
                            if (isdisposed == true) return null;
                            if (value == null)
                            {
                                value = create();
                                createTime = DateTime.Now;
                                Interlocked.Increment(ref ib._usageQuantity);
                                iscreate = true;
                            }
                            else
                            {
                                return value;
                            }
                        }
                        if (iscreate)
                        {
                            if (value != null)
                            {
                                if (firstValue == null) firstValue = value; //记录首次值
                                else if (firstValue == value) IsRegisterError = true; //第二次与首次相等，注册姿势错误
                            }
                            ib.OnNotice(new NoticeEventArgs(NoticeType.AutoCreate, key, null, $"{key} 实例+++创建成功，耗时 {DateTime.Now.Subtract(now).TotalMilliseconds}ms，{ib._usageQuantity}/{ib.Quantity}"));
                        }
                    }
                    catch (Exception ex)
                    {
                        ib.OnNotice(new NoticeEventArgs(NoticeType.AutoCreate, key, ex, $"{key} 实例+++创建失败：{ex.Message}"));
                        throw ex;
                    }
                }
                lastActiveTime = DateTime.Now;
                Interlocked.Increment(ref activeCounter);
                return value;
            }

            internal bool Release(Func<bool> lockInIf)
            {
                lock (valueLock)
                {
                    if (value != null && lockInIf())
                    {
                        value?.Dispose();
                        value = null;
                        Interlocked.Decrement(ref ib._usageQuantity);
                        Interlocked.Exchange(ref activeCounter, 0);
                        return true;
                    }
                }
                return false;
            }

            ~ItemInfo() => Dispose();
            bool isdisposed = false;
            public void Dispose()
            {
                if (isdisposed) return;
                lock (valueLock)
                {
                    if (isdisposed) return;
                    isdisposed = true;
                }
                try
                {
                    Release(() => true);
                }
                catch { }
            }

        }

    }
}