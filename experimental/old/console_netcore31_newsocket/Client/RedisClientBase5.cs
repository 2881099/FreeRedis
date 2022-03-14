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
    public abstract class RedisClientBase5
    {
        

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

       
    }
}
