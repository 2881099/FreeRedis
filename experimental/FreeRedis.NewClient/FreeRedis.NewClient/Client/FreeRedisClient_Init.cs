using FreeRedis.Engine;
using FreeRedis.NewClient.Protocols;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace FreeRedis.NewClient.Client
{
    public sealed partial class FreeRedisClient : FreeRedisClientBase
    {

        public FreeRedisClient(ConnectionStringBuilder connectionString, Action<string>? logger = null) : base(connectionString.Ip, connectionString.Port, logger)
        {

            try
            {

                string password = connectionString.Password;
                if (password != string.Empty)
                {
                    if (!AuthAsync(password).Result)
                    {
                        throw new Exception("Reids 服务器密码不正确!");
                    }
                }

                int dbIndex = connectionString.Database;
                if (!SelectDBAsync(dbIndex).Result)
                {
                    throw new Exception($"Reids 服务器选择数据库出错,数据库索引{dbIndex}!");
                }

            }
            catch (Exception ex)
            {

                DisposeAsync();
                throw new Exception($"创建链接遇到问题:{ex.Message!}");

            }
        }
#if DEBUG
        //public void Clear()
        //{
        //    _taskBuffer.Clear();
        //}
#endif

        public Task<bool> AuthAsync(string password)
        {
            TaskCompletionSource<bool> result = new(TaskCreationOptions.RunContinuationsAsynchronously);
            ProtocolContinueResult delegateMethod(ref SequenceReader<byte> reader) { return StateProtocol.HandleBytes(result, ref reader); }
            InternalStateTask($"AUTH {password}\r\n", delegateMethod);
            return result.Task;
        }
        public Task<bool> FlushDBAsync()
        {
            TaskCompletionSource<bool> result = new(TaskCreationOptions.RunContinuationsAsynchronously);
            ProtocolContinueResult delegateMethod(ref SequenceReader<byte> reader) { return StateProtocol.HandleBytes(result, ref reader); }
            InternalStateTask(_flush_command_buffer, delegateMethod);
            return result.Task;
        }
        public Task<bool> SelectDBAsync(int dbIndex)
        {
            TaskCompletionSource<bool> result = new(TaskCreationOptions.RunContinuationsAsynchronously);
            ProtocolContinueResult delegateMethod(ref SequenceReader<byte> reader) { return StateProtocol.HandleBytes(result, ref reader); }
            var commandBytes = _utf8.GetBytes($"SELECT {dbIndex}\r\n");
            InternalStateTask($"SELECT {dbIndex}\r\n", delegateMethod);
            return result.Task;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void InternalStateTask(string command, IProtocolReaderDelegate protocolDelegate)
        {
            var bytes = _utf8.GetByteCount(command);
            WaitSendLock();
            var span = _sender.GetSpan(bytes);
            _sender.Advance(bytes);
            _utf8.GetBytes(command, span);
            _taskBuffer.Enqueue(protocolDelegate);
            ReleaseSendLock();
            if (TryGetSendLock())
            {
                _sender.FlushAsync().ConfigureAwait(false);
                ReleaseSendLock();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void InternalStateTask(byte[] commandBytes, IProtocolReaderDelegate protocolDelegate)
        {
            WaitSendLock();
            var span = _sender.GetSpan(commandBytes.Length);
            commandBytes.CopyTo(span);
            _sender.Advance(commandBytes.Length);
            _taskBuffer.Enqueue(protocolDelegate);
            ReleaseSendLock();
            if (TryGetSendLock())
            {
                _sender.FlushAsync().ConfigureAwait(false);
                ReleaseSendLock();
            }
        }
    }
}