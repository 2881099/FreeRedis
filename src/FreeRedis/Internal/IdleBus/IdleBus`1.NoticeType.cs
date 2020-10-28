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

        public enum NoticeType
        {

            /// <summary>
            /// 执行 Register 方法的时候
            /// </summary>
            Register,

            /// <summary>
            /// 执行 Remove 方法的时候，注意：实际会延时释放【实例】
            /// </summary>
            Remove,

            /// <summary>
            /// 自动创建【实例】的时候
            /// </summary>
            AutoCreate,

            /// <summary>
            /// 自动释放不活跃【实例】的时候
            /// </summary>
            AutoRelease,

            /// <summary>
            /// 获取【实例】的时候
            /// </summary>
            Get

        }

    }
}