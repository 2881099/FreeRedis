using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace console_netcore31_taskcompletesource
{
    internal class ResettableBooleanCompletionSource : IValueTaskSource<bool>
    {
        ManualResetValueTaskSourceCore<bool> _valueTaskSource;
        private readonly StackPolicy _queue;

        public ResettableBooleanCompletionSource(StackPolicy queue)
        {
            _queue = queue;
            _valueTaskSource.RunContinuationsAsynchronously = true;
        }

        public ValueTask<bool> GetValueTask()
        {
            return new ValueTask<bool>(this, _valueTaskSource.Version);
        }

        bool IValueTaskSource<bool>.GetResult(short token)
        {
            var isValid = token == _valueTaskSource.Version;
            try
            {
                return _valueTaskSource.GetResult(token);
            }
            finally
            {
                if (isValid)
                {
                    _valueTaskSource.Reset();
                    _queue._cachedResettableTCS = this;
                }
            }
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            return _valueTaskSource.GetStatus(token);
        }

        void IValueTaskSource<bool>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            _valueTaskSource.OnCompleted(continuation, state, token, flags);
        }

        public void Complete(bool result)
        {
            _valueTaskSource.SetResult(result);
        }
    }
}
