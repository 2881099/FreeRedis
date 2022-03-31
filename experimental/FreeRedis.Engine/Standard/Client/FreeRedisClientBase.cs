using FreeRedis.Transport;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace FreeRedis.Engine
{
    public abstract class FreeRedisClientBase : IAsyncDisposable
    {
        protected readonly SocketConnection? _connection;
        protected readonly PipeWriter _sender;
        protected readonly PipeReader _reciver;
        protected readonly CircleTask _taskBuffer;
        protected readonly Action<string>? errorLogger;
        private static readonly Encoder Utf8Encoder;
        static FreeRedisClientBase()
        {
            Utf8Encoder = Encoding.UTF8.GetEncoder();
#if DEBUG
            System.Threading.Tasks.Task.Run(() =>
            {

                while (true)
                {
                    Thread.Sleep(5000);
                    Console.WriteLine("已接收长度:" + _bufferLength);
                }

            });
#endif
        }


        private static long _bufferLength;


        public FreeRedisClientBase(string ip, int port, Action<string>? logger)
        {
            _taskBuffer = new CircleTask();
            errorLogger = logger;
            SocketConnectionFactory client = new(new SocketTransportOptions());
            _connection = client.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip), port)).Result;
            _sender = _connection!.Transport.Output;
            _reciver = _connection!.Transport.Input;
            RunReciver();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<T> SendProtocal<T>(byte[] commandData, IRedisProtocal<T> redisProtocal)
        {
            Interlocked.Increment(ref _concurrentCount);
            WaitAndLockSend();
            _sender.Write(commandData);
            _taskBuffer.WriteNext(redisProtocal);
            ReleaseSend();
            Interlocked.Decrement(ref _concurrentCount);
            if (_concurrentCount == 0)
            {
                _sender.FlushAsync();
            }

            return redisProtocal.WaitTask;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<T> SendProtocal<T>(string commandData, IRedisProtocal<T> redisProtocal)
        {
            Interlocked.Increment(ref _concurrentCount);
            WaitAndLockSend();
            _sender.WriteUtf8String(commandData);
            _taskBuffer.WriteNext(redisProtocal);
            ReleaseSend();
            Interlocked.Decrement(ref _concurrentCount);
            if (_concurrentCount == 0)
            {
                _sender.FlushAsync();
            }

            return redisProtocal.WaitTask;
        }

        private string _sendData;
        private long _concurrentCount;
        private int _commandCount;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<T> SendProtocal<T>(int commandCount, string commandData , IRedisProtocal<T> redisProtocal)
        {
            Interlocked.Increment(ref _concurrentCount);
            WaitAndLockSend();
            _sendData += commandData;
            _commandCount += commandCount;
            if (_concurrentCount == 1)
            {
                _sender.WriteUtf8String($"*{_commandCount}\r\n{_sendData}");
                _commandCount = 0;
                _sendData = string.Empty;

            }
            _taskBuffer.WriteNext(redisProtocal);
            ReleaseSend();
            Interlocked.Decrement(ref _concurrentCount);
            if (_concurrentCount==0)
            {
                _sender.FlushAsync();
            }

            return redisProtocal.WaitTask;
        }

        private SequencePosition _postion;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task RunReciver()
        {

            while (true)
            {
                var result = await _reciver.ReadAsync().ConfigureAwait(false);
                var buffer = result.Buffer;
#if DEBUG
                _bufferLength += buffer.Length;
#endif
                HandlerRecvData(in buffer);
                _reciver.AdvanceTo(_postion);
                if (result.IsCompleted)
                {
                    return;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandlerRecvData(in ReadOnlySequence<byte> revData)
        {
            var reader = new SequenceReader<byte>(revData);
            _taskBuffer.LoopHandle(ref reader);
            _postion = reader.Position;
        }


        public bool AnalysisingFlag;

        #region MyRegion
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
        protected bool TryGetLockAnalysisLock()
        {
            return Interlocked.CompareExchange(ref _analysis_lock_flag, 1, 0) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ReleaseLockAnalysis()
        {

            _analysis_lock_flag = 0;

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void WaitAndLockSend()
        {

            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _send_lock_flag, 1, 0) != 0)
            {
                //Interlocked.Increment(ref LockCount);
                wait.SpinOnce();
            }

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryGetSendLock()
        {
            return Interlocked.CompareExchange(ref _send_lock_flag, 1, 0) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ReleaseSend()
        {

            _send_lock_flag = 0;

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
