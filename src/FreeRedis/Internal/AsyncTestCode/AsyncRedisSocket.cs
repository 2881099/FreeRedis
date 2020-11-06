#if isasync
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeRedis.Internal
{
    public class AsyncRedisSocket
    {
        internal readonly IRedisSocket _rds;
        readonly Action _begin;
        readonly Action<IRedisSocket, Exception> _end;
        readonly Func<bool> _finish;
        internal long _writeCounter;
        public AsyncRedisSocket(IRedisSocket rds, Action begin, Action<IRedisSocket, Exception> end, Func<bool> finish)
        {
            _rds = rds;
            _begin = begin;
            _end = end;
            _finish = finish;
            _writeCounter = 0;
        }

        public Task<RedisResult> WriteAsync(CommandPacket cmd)
        {
            var iq = WriteInQueue(cmd);
            if (iq == null) return Task.FromResult(new RedisResult(null, true, RedisMessageType.SimpleString));
            return iq.TaskCompletionSource.Task;
        }

        class WriteAsyncInfo
        {
            public TaskCompletionSource<RedisResult> TaskCompletionSource;
            public CommandPacket Command;
        }
        ConcurrentQueue<WriteAsyncInfo> _writeQueue = new ConcurrentQueue<WriteAsyncInfo>();
        object _writeAsyncLock = new object();
        MemoryStream _bufferStream;
        bool _isfirst = false;
        WriteAsyncInfo WriteInQueue(CommandPacket cmd)
        {
            var ret = new WriteAsyncInfo { Command = cmd };
            if (_rds.ClientReply == ClientReplyType.on) ret.TaskCompletionSource = new TaskCompletionSource<RedisResult>();
            var isnew = false;
            lock (_writeAsyncLock)
            {
                _writeQueue.Enqueue(ret);
                if (_isfirst == false)
                    isnew = _isfirst = true;
                if (_bufferStream == null) _bufferStream = new MemoryStream();
                new RespHelper.Resp3Writer(_bufferStream, _rds.Encoding, _rds.Protocol).WriteCommand(cmd);
            }
            if (isnew)
            {
                //Thread.CurrentThread.Join(TimeSpan.FromTicks(1000));
                try
                {
                    if (_rds.IsConnected == false) _rds.Connect();
                }
                catch (Exception ioex)
                {
                    lock (_writeAsyncLock)
                    {
                        while (_writeQueue.TryDequeue(out var wq)) wq.TaskCompletionSource?.TrySetException(ioex);
                        _bufferStream.Close();
                        _bufferStream.Dispose();
                        _bufferStream = null;
                    }
                    _rds.ReleaseSocket();
                    _end(_rds, ioex);
                    throw ioex;
                }

                for (var a = 0; a < 100; a++)
                {
                    var cou = _writeQueue.Count;
                    if (cou > 100) break;
                }
                var localQueue = new Queue<WriteAsyncInfo>();
                long localQueueThrowException(Exception exception)
                {
                    long counter = 0;
                    while (localQueue.Any())
                    {
                        var witem = localQueue.Dequeue();
                        if (exception != null) witem.TaskCompletionSource?.TrySetException(exception);
                        else witem.TaskCompletionSource?.TrySetCanceled();
                        counter = Interlocked.Decrement(ref _writeCounter);
                    }
                    return counter;
                }

                var iswait = _writeCounter > 1;
                if (iswait) Thread.CurrentThread.Join(10);
                _begin();
                while (true)
                {
                    if (_writeQueue.Any() == false)
                    {
                        Thread.CurrentThread.Join(10);
                        continue;
                    }
                    lock (_writeAsyncLock)
                    {
                        while (_writeQueue.TryDequeue(out var wq))
                            localQueue.Enqueue(wq);
                        _bufferStream.Position = 0;
                        try
                        {
                            _bufferStream.CopyTo(_rds.Stream);
                            _bufferStream.Close();
                            _bufferStream.Dispose();
                            _bufferStream = null;
                        }
                        catch (Exception ioex)
                        {
                            localQueueThrowException(ioex);
                            _bufferStream.Close();
                            _bufferStream.Dispose();
                            _bufferStream = null;
                            _rds.ReleaseSocket();
                            _end(_rds, ioex);
                            throw ioex;
                        }
                    }
                    long counter = 0;
                    RedisResult rt = null;
                    //sb.AppendLine($"{name} 线程{Thread.CurrentThread.ManagedThreadId}：合并读取 {localQueue.Count} 个命令 total:{sw.ElapsedMilliseconds} ms");
                    while (localQueue.Any())
                    {
                        var witem = localQueue.Dequeue();
                        try
                        {
                            rt = _rds.Read(false);
                        }
                        catch (Exception ioex)
                        {
                            localQueueThrowException(ioex);
                            _rds.ReleaseSocket();
                            _end(_rds, ioex);
                            throw ioex;
                        }
                        witem.TaskCompletionSource.TrySetResult(rt);
                        counter = Interlocked.Decrement(ref _writeCounter);
                    }
                    if (counter == 0 && _finish())
                        break;
                    Thread.CurrentThread.Join(1);
                    //sb.AppendLine($"{name} 线程{Thread.CurrentThread.ManagedThreadId}：等待 1ms + {_writeQueue.Count} total:{sw.ElapsedMilliseconds} ms");
                }
                //sb.AppendLine($"{name} 线程{Thread.CurrentThread.ManagedThreadId}：退出 total:{sw.ElapsedMilliseconds} ms");
                _end(_rds, null);
            }
            return ret;
        }

        public string name = Guid.NewGuid().ToString();
        public static StringBuilder sb = new StringBuilder();
        public static Stopwatch sw = new Stopwatch();
    }
}
#endif