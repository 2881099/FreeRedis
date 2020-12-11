
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{
    internal sealed class SocketAwaitableEventArgs : SocketAsyncEventArgs, ICriticalNotifyCompletion
    {
        private static readonly Action _callbackCompleted = () => { };

        private readonly PipeScheduler _ioScheduler;

        private Action _callback;

        private string _name;

        public SocketAwaitableEventArgs(PipeScheduler ioScheduler,string name)
        {
            _ioScheduler = ioScheduler;
            _name = name;
        }

        public SocketAwaitableEventArgs GetAwaiter() => this;
        public bool IsCompleted => ReferenceEquals(_callback, _callbackCompleted);

        public int GetResult()
        {
            //Console.WriteLine(_name+":In GetResult!");
            //Debug.Assert(ReferenceEquals(_callback, _callbackCompleted));
            //Console.WriteLine(_name + ":In GetResult! Set _callback = null!");
            _callback = null;

            //if (SocketError != SocketError.Success)
            //{
            //    ThrowSocketException(SocketError);
            //}

            return BytesTransferred;

            //static void ThrowSocketException(SocketError e)
            //{
            //    throw new SocketException((int)e);
            //}
        }

        public void OnCompleted(Action continuation)
        {
            //Console.WriteLine(_name + ":In OnCompleted Action!");
            //Console.WriteLine(_name + $":In OnCompleted Action! continuation code is {continuation.Method.GetHashCode()}?");
            //Console.WriteLine(_name + $":In OnCompleted Action! _callback is {(_callback == null?"":"not")}null.");

            if (ReferenceEquals(_callback, _callbackCompleted) ||
                ReferenceEquals(Interlocked.CompareExchange(ref _callback, continuation, null), _callbackCompleted))
            {
                
                //Console.WriteLine(_name + ":In OnCompleted Action! Will Task.Run Continuation!");
                //Console.WriteLine(_name + $":In OnCompleted Action! callback is movenext? {_callback.Method.Name.Contains("Next")}");
                Task.Run(continuation);
            }

        }

        public void UnsafeOnCompleted(Action continuation)
        {
            //Console.WriteLine(_name + ":In UnsafeOnCompleted! -> OnCompleted!");
            OnCompleted(continuation);
        }

        public void Complete()
        {
            //Console.WriteLine(_name + ":In Complete! -> OnCompleted!");
            OnCompleted(this);
        }

        protected override void OnCompleted(SocketAsyncEventArgs _)
        {
            //Console.WriteLine(_name + ":In OnCompleted!");
            var continuation = Interlocked.Exchange(ref _callback, _callbackCompleted);
            if (continuation != null)
            {
                //Console.WriteLine(_name + ":In OnCompleted! _callback is movenext!");
                //Console.WriteLine(_name + $":In OnCompleted! _callback code is {continuation.Method.GetHashCode()}");
                //{
                //    var callBack = (Action)state;
                //    // Console.WriteLine("Scheduler : Running customer callback!");
                //    callBack();
                //    // Console.WriteLine("Scheduler : Run customer callback completed!");
                //}
                _ioScheduler.Schedule(state => ((Action)state)()  , continuation);
            }
        }
    }
}