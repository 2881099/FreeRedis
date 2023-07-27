﻿using FreeRedis.Transport;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Runtime.CompilerServices;

namespace FreeRedis.Engine
{
    public abstract class FreeRedisClientBase : IAsyncDisposable
    {
        protected readonly SocketConnection? _connection;
        protected readonly PipeWriter _sender;
        protected readonly PipeReader _reciver;
        protected readonly CircleBuffer _taskBuffer;

        protected readonly Action<string>? errorLogger;

        public FreeRedisClientBase(string ip, int port, Action<string>? logger)
        {
            _taskBuffer = new CircleBuffer(2,4096);
            errorLogger = logger;
            SocketConnectionFactory client = new(new SocketTransportOptions());
            _connection = client.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip), port)).Result;
            _sender = _connection!.Transport.Output;
            _reciver = _connection!.Transport.Input;
            Task.Run(RunReciver);
        }


        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task RunReciver()
        {
            SequencePosition _postion;

            while (true)
            {

#if DEBUG
                FreeRedisTracer.CurrentStep = $"等待接收字节!";
#endif
                var result = await _reciver.ReadAsync().ConfigureAwait(false);
                var buffer = result.Buffer;
#if DEBUG
                FreeRedisTracer.CurrentStep = $"准备解析比特!";
                FreeRedisTracer.PreBytesLength = buffer.Length;
#endif
                HandlerRecvData(in buffer);
                _reciver.AdvanceTo(_postion);
                if (result.IsCompleted)
                {
                    return;
                }
#if DEBUG
                FreeRedisTracer.CurrentStep = $"已解析完当前比特!";
#endif
            }

            void HandlerRecvData(in ReadOnlySequence<byte> recvData)
            {
                var reader = new SequenceReader<byte>(recvData);
#if DEBUG
                FreeRedisTracer.CurrentStep = $"准备解析字节共:{recvData.Length}";
#endif
                LoopHandle(ref reader);
                _postion = reader.Position;
            }

        }



        /// <summary>
        /// 外界拿到数据后包装 Reader, 调用该方法:
        /// 处理结果是: 
        ///     ProtocolContinueResult.Completed 则进行下一个任务
        ///     ProtocolContinueResult.Wait 则返回方法,等下一个新的 reader.
        ///     ProtocolContinueResult.Continue 如果 reader 被动态包装,动态增加内容,可使用 Continue 等待当前 reader 包装结果.
        /// 
        /// </summary>
        /// <param name="reader"></param>
        public void LoopHandle(ref SequenceReader<byte> reader)
        {
            ProtocolContinueResult result = ProtocolContinueResult.Completed;
            while (reader.Remaining > 5)
            {

                result = _taskBuffer.CurrentValue(ref reader);
                if (result == ProtocolContinueResult.Completed)
                {
#if DEBUG
                FreeRedisTracer.CurrentCompletedTaskCount += 1;
#endif
                    _taskBuffer.MoveNext();
                }
                else if (result == ProtocolContinueResult.Wait)
                {
#if DEBUG
                Interlocked.Increment(ref FreeRedisTracer.WaitBytesCount);
#endif
                    return;
                }
            }

        }



        public bool AnalysisingFlag;

        #region MyRegion
        private int _flush_lock_flag;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void WaitFlushLock()
        {

            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _flush_lock_flag, 1, 0) != 0)
            {
                //Interlocked.Increment(ref LockCount);
                wait.SpinOnce();
            }

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryGetFlushLock()
        {
            return Interlocked.CompareExchange(ref _flush_lock_flag, 1, 0) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ReleaseFlushLock()
        {

            _flush_lock_flag = 0;

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
        protected bool TryGetReceiverLock()
        {
            return _receiver_lock_flag == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ReleaseReceiver()
        {

            _receiver_lock_flag = 0;

        }
        #endregion




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

        private SpinWait _send_waiter;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void WaitSendLock()
        {

            while (Interlocked.CompareExchange(ref _send_lock_flag, 1, 0) != 0)
            {
#if DEBUG
             Interlocked.Increment(ref FreeRedisTracer.SenderLockLootCount);
#endif
                _send_waiter.SpinOnce();
            }

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryGetSendLock()
        {
            return Interlocked.CompareExchange(ref _send_lock_flag, 1, 0) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ReleaseSendLock()
        {

            _send_lock_flag = 0;
            _send_waiter.Reset();
        }

        

        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                await this._connection.DisposeAsync();
            }

        }
    }
}
