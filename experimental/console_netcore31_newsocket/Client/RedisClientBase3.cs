using Microsoft.AspNetCore.Connections;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{
    public abstract class RedisClientBase3
    {
        private readonly static Func<Task<bool>, bool, bool> _setResult;
        protected readonly static Func<object, TaskCreationOptions, Task<bool>> CreateTask;
        protected readonly static Func<Task<bool>> CreateTaskWithoutParameters;
        static RedisClientBase3()
        {
            _setResult = typeof(Task<bool>)
                .GetMethod("TrySetResult",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(bool) }, null)
                .CreateDelegate<Func<Task<bool>, bool, bool>>();


            var ctor = typeof(Task<bool>).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(object),typeof(TaskCreationOptions) }, null);

            DynamicMethod dynamicMethod = new DynamicMethod("GETTASK1", typeof(Task<bool>), new Type[] { typeof(object), typeof(TaskCreationOptions) });
            var iLGenerator = dynamicMethod.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Ldarg_1);
            iLGenerator.Emit(OpCodes.Newobj, ctor);
            iLGenerator.Emit(OpCodes.Ret);
            CreateTask = (Func<object, TaskCreationOptions, Task<bool>>)dynamicMethod.CreateDelegate(typeof(Func<object, TaskCreationOptions, Task<bool>>));
            ctor = typeof(Task<bool>).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[0], null);
            dynamicMethod = new DynamicMethod("GETTASK2", typeof(Task<bool>), new Type[0]);
            iLGenerator = dynamicMethod.GetILGenerator();
            iLGenerator.Emit(OpCodes.Newobj, ctor);
            iLGenerator.Emit(OpCodes.Ret);
            CreateTaskWithoutParameters = (Func<Task<bool>>)dynamicMethod.CreateDelegate(typeof(Func<Task<bool>>));
            //CreateTask = () => (new TaskCompletionSource<bool>(null, TaskCreationOptions.RunContinuationsAsynchronously)).Task;
        }

        protected readonly ConnectionContext _connection;
        protected readonly PipeWriter _sender;
        protected readonly PipeReader _reciver;

        public virtual void CreateConnection(string ip,int port)
        {

            SocketConnectionFactory client = new SocketConnectionFactory(new SocketTransportOptions());
            Unsafe.AsRef(_connection) = client.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip), port)).Result;
            Unsafe.AsRef(_sender) = _connection.Transport.Output;
            Unsafe.AsRef(_reciver) = _connection.Transport.Input;
            Init();
            RunReciver();

        }

        private async void RunReciver()
        {

            while (true)
            {

                var result = await _reciver.ReadAsync().ConfigureAwait(false);
                var buffer = result.Buffer;
                if (buffer.IsSingleSegment)
                {
                    Handler(buffer.FirstSpan);
                }
                else
                {
                    Handler(buffer);

                }

                _reciver.AdvanceTo(buffer.End);
                if (result.IsCompleted)
                {
                    return;
                }

            }
        }


        protected internal abstract void Handler(in ReadOnlySequence<byte> sequence);
        protected internal abstract void Handler(in ReadOnlySpan<byte> span);

        protected virtual void Init()
        {

        }

        public bool AnalysisingFlag;

        private int _analysis_lock_flag;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void LockAnalysis()
        {

            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _analysis_lock_flag, 1, 0) != 0)
            {
                //Interlocked.Increment(ref LockCount);
                wait.SpinOnce(8);
            }

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetLockAnalysisLock()
        {
            return Interlocked.CompareExchange(ref _analysis_lock_flag, 1, 0) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ReleaseLockAnalysis()
        {

            _analysis_lock_flag = 0;

        }



        private int _send_lock_flag;

        public bool IsCompleted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _send_lock_flag != 1;

            }
        }
        public int LockCount;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void LockSend()
        {
            
            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _send_lock_flag, 1, 0) != 0)
            {
                //Interlocked.Increment(ref LockCount);
                wait.SpinOnce();
            }

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetSendLock()
        {
            return Interlocked.CompareExchange(ref _send_lock_flag, 1, 0) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseSend()
        {

            _send_lock_flag = 0;

        }

        private int _receiver_lock_flag;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void LockReceiver()
        {

            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _receiver_lock_flag, 1, 0) != 0)
            {
                //Interlocked.Increment(ref LockCount);
                wait.SpinOnce();
            }

        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetReceiverLock()
        {
            return _receiver_lock_flag == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ReleaseReceiver()
        {

            _receiver_lock_flag = 0;

        }
        public abstract ValueTask<bool> SetAsync(string key,string value);

        public bool TrySetResult(Task<bool> task, bool result)
        {
            bool rval = _setResult(task, result);
            if (!rval)
            {
                SpinWait sw = default;
                while (!task.IsCompleted)
                {
                    sw.SpinOnce();
                }
            }

            return rval;
        }
    }
}
