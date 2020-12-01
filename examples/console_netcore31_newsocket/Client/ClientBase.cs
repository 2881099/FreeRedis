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
    public abstract class RedisClientBase
    {
        private readonly static Func<Task<bool>, bool, bool> _setResult;
        protected readonly static Func<Task<bool>> CreateTask;
        static RedisClientBase()
        {
            _setResult = typeof(Task<bool>)
                .GetMethod("TrySetResult",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(bool) }, null)
                .CreateDelegate<Func<Task<bool>, bool, bool>>();


            var ctor = typeof(Task<bool>).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[0], null);

            DynamicMethod dynamicMethod = new DynamicMethod("GETTASK", typeof(Task<bool>), new Type[0]);
            var iLGenerator = dynamicMethod.GetILGenerator();
            iLGenerator.Emit(OpCodes.Newobj, ctor);
            iLGenerator.Emit(OpCodes.Ret);
            CreateTask = (Func<Task<bool>>)dynamicMethod.CreateDelegate(typeof(Func<Task<bool>>));
        }

        protected readonly ConnectionContext _connection;
        protected readonly PipeWriter _sender;
        protected readonly PipeReader _reciver;

        public void CreateConnection(string ip,int port)
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
                //if (buffer.IsSingleSegment)
                //{
                  //  Handler(buffer.FirstSpan);
                //}
               // else
               // {
                    Handler(buffer);
                    //SequencePosition _nextPosition = buffer.Start;
                    //while (buffer.TryGet(ref _nextPosition, out ReadOnlyMemory<byte> memory, advance: true))
                    //{

                    //    if (memory.Length > 0)
                    //    {
                    //        Handler(memory.Span);
                    //        break;
                    //    }
                    //}
                    
                //}
                
                _reciver.AdvanceTo(buffer.End);
                if (result.IsCompleted)
                {
                    return;
                }

            }
        }


        protected internal abstract void Handler(in ReadOnlySequence<byte> sequence);

        protected virtual void Init()
        {

        }


        private long _send_lock_flag;

        public bool IsCompleted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _send_lock_flag != 1;

            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void LockSend()
        {

            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _send_lock_flag, 1, 0) != 0)
            {
                wait.SpinOnce();
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ReleaseSend()
        {

            _send_lock_flag = 0;

        }

        private long _receiver_lock_flag;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void LockReceiver()
        {

            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _receiver_lock_flag, 1, 0) != 0)
            {
                wait.SpinOnce();
            }

        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryGetReceiverLock()
        {
            return Interlocked.CompareExchange(ref _receiver_lock_flag, 1, 0) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ReleaseReceiver()
        {

            _receiver_lock_flag = 0;

        }
        public abstract Task<bool> SetAsync(string key,string value);

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
