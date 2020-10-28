using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace FreeRedis.Internal
{
    /// <summary>
    /// 空闲对象容器管理，可实现自动创建、销毁、扩张收缩，解决【实例】长时间占用问题
    /// </summary>
    public class IdleBus : IdleBus<string, IDisposable>
    {
        /// <summary>
        /// 按空闲时间1分钟，创建空闲容器
        /// </summary>
        public IdleBus() : base() { }
        /// <summary>
        /// 指定空闲时间、创建空闲容器
        /// </summary>
        /// <param name="idle">空闲时间</param>
        public IdleBus(TimeSpan idle) : base(idle) { }
    }

    /// <summary>
    /// 空闲对象容器管理，可实现自动创建、销毁、扩张收缩，解决【实例】长时间占用问题
    /// </summary>
    public class IdleBus<TValue> : IdleBus<string, TValue> where TValue : class, IDisposable
    {
        /// <summary>
        /// 按空闲时间1分钟，创建空闲容器
        /// </summary>
        public IdleBus() : base() { }
        /// <summary>
        /// 指定空闲时间、创建空闲容器
        /// </summary>
        /// <param name="idle">空闲时间</param>
        public IdleBus(TimeSpan idle) : base(idle) { }
    }
}