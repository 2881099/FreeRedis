using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace console_netcore31_newsocket.Scheduler
{

    public class MyScheduler : IValueTaskSource<bool>
    {

        private ManualResetValueTaskSourceCore<bool> _manualResetValueTaskSourceCore;
        public long _ms;
        private long _start;
        public MyScheduler(int ms)
        {
            _ms = ms;
            _start = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            _manualResetValueTaskSourceCore = new ManualResetValueTaskSourceCore<bool>();
            _manualResetValueTaskSourceCore.RunContinuationsAsynchronously = true;
        }


        public ValueTaskSourceStatus GetStatus(short token)
        {
            return _manualResetValueTaskSourceCore.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {

            if ((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000 - _start >_ms)
            {
                continuation(state);
            }
            else
            {
                _manualResetValueTaskSourceCore.OnCompleted(continuation, state, token, flags);

            }
            
        }

        bool IValueTaskSource<bool>.GetResult(short token)
        {
            return _manualResetValueTaskSourceCore.GetResult(token);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            throw new NotImplementedException();
        }

       
    }
}
