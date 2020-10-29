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

        public class NoticeEventArgs : EventArgs
        {

            public NoticeType NoticeType { get; }
            public TKey Key { get; }
            public Exception Exception { get; }
            public string Log { get; }

            public NoticeEventArgs(NoticeType noticeType, TKey key, Exception exception, string log)
            {
                this.NoticeType = noticeType;
                this.Key = key;
                this.Exception = exception;
                this.Log = log;
            }

        }

    }
}