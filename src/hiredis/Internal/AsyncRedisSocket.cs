#if pipeio
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace hiredis.Internal
{
    class AsyncRedisSocket : IDisposable
    {
        readonly IRedisSocket _rds;
        readonly TimeSpan _oldReceiveTimeout;
        bool _runing = true;
        Exception _exception;
        public AsyncRedisSocket(IRedisSocket rds)
        {
            _rds = rds;
            _oldReceiveTimeout = _rds.ReceiveTimeout;
            _rds.ReceiveTimeout = TimeSpan.Zero;
            if (rds.IsConnected == false) rds.Connect();
            StartRead();
        }
        public void Dispose()
        {
            _rds.ReceiveTimeout = _oldReceiveTimeout;
            _runing = false;
            _rds.ReleaseSocket();
            ClearAndTrySetExpcetion(null);
        }

        public Task<RedisResult> WriteAsync(CommandPacket cmd)
        {
            var ex = _exception;
            if (ex != null) throw ex;
            var iq = WriteInQueue(cmd);
            if (iq == null) return Task.FromResult(new RedisResult(null, true, RedisMessageType.SimpleString));
            return iq.TaskCompletionSource.Task;
        }

        void StartRead()
        {
            new Thread(() =>
            {
                var timer = new Timer(state => { try { WriteInQueue("PING"); } catch { } }, null, 10000, 10000);
                while (_runing)
                {
                    RedisResult rt;
                    try
                    {
                        rt = _rds.Read(false);
                    }
                    catch (Exception ioex)
                    {
                        ClearAndTrySetExpcetion(ioex);
                        Interlocked.Exchange(ref _exception, ioex);
                        continue;
                    }
                    if (_writeQueue.TryDequeue(out var witem))
                        witem.TaskCompletionSource.TrySetResult(rt);
                    else
                        ClearAndTrySetExpcetion(new RedisClientException($"AsyncRedisSocket: Message sequence error"));
                }
                timer.Dispose();
            }).Start();
        }

        void ClearAndTrySetExpcetion(Exception ioex)
        {
            lock (_writeAsyncLock)
            {
                while (_writeQueue.TryDequeue(out var witem))
                {
                    if (ioex != null) witem.TaskCompletionSource.TrySetException(ioex);
                    else witem.TaskCompletionSource.TrySetCanceled();
                }
            }
        }

        class WriteAsyncInfo
        {
            public TaskCompletionSource<RedisResult> TaskCompletionSource = new TaskCompletionSource<RedisResult>();
            public CommandPacket Command;
        }
        ConcurrentQueue<WriteAsyncInfo> _writeQueue = new ConcurrentQueue<WriteAsyncInfo>();
        object _writeAsyncLock = new object();
        MemoryStream _bufferStream;
        WriteAsyncInfo WriteInQueue(CommandPacket cmd)
        {
            var ret = _rds.ClientReply == ClientReplyType.on ? new WriteAsyncInfo { Command = cmd } : null;
            lock (_writeAsyncLock)
            {
                if (ret != null) _writeQueue.Enqueue(ret);
                if (_bufferStream == null)
                    _bufferStream = new MemoryStream();
                new RespHelper.Resp3Writer(_bufferStream, _rds.Encoding, _rds.Protocol).WriteCommand(cmd);
            }
            Thread.CurrentThread.Join(TimeSpan.FromTicks(1000));
            lock (_writeAsyncLock)
            {
                try
                {
                    _bufferStream.Position = 0;
                    _bufferStream.CopyTo(_rds.Stream);
                    _bufferStream.Close();
                    _bufferStream = null;
                    Interlocked.Exchange(ref _exception, null);
                }
                catch (Exception ioex)
                {
                    ClearAndTrySetExpcetion(ioex);
                    Interlocked.Exchange(ref _exception, ioex);
                    _rds.ReleaseSocket();
                }
            }
            return ret;
        }
    }
}
#endif