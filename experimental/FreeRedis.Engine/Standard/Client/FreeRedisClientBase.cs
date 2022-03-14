using FreeRedis.Transport;
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
        protected readonly CircleTask _taskBuffer;
        public FreeRedisClientBase()
        {
            _taskBuffer = new CircleTask();
            _sender = default!;
            _reciver = default!;
            _connection = default!;
        }

        public virtual void CreateConnection(string ip,int port)
        {

            SocketConnectionFactory client = new(new SocketTransportOptions());
            Unsafe.AsRef(_connection) = client.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip), port)).Result;
            Unsafe.AsRef(_sender) = _connection!.Transport.Output;
            Unsafe.AsRef(_reciver) = _connection!.Transport.Input;
            Init();
            RunReciver();

        }


        public Task<T> SendProtocal<T>(IRedisProtocal<T> redisProtocal)
        {
            LockSend();
            _sender.WriteAsync(redisProtocal.GetSendBytes());
            _taskBuffer.WriteNext(redisProtocal);
            ReleaseSend();
            return redisProtocal.WaitTask;
        }

        private int _revc_offset;
        private async void RunReciver()
        {

            while (true)
            {

                var result = await _reciver.ReadAsync().ConfigureAwait(false);
                var buffer = result.Buffer;
                foreach (ReadOnlyMemory<byte> segment in buffer)
                {
                    _taskBuffer.ReadNext(in segment, ref _revc_offset);
                }
                _reciver.AdvanceTo(buffer.End);
                if (result.IsCompleted)
                {
                    return;
                }

            }
        }

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

        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                await this._connection.DisposeAsync();
            }

        }
    }
}
