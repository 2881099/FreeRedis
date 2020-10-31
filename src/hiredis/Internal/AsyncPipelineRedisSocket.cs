#if pipeio
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace hiredis.Internal
{
    class AsyncPipelineRedisSocket
    {
        readonly IRedisSocket _redisSocket;
        readonly TimeSpan _oldReceiveTimeout;
        PipeWriter _writer;
        PipeReader _reader;
        bool _runing = true;
        Exception _exception;

        public AsyncPipelineRedisSocket(IRedisSocket redisSocket)
        {
            _redisSocket = redisSocket;
            _oldReceiveTimeout = _redisSocket.ReceiveTimeout;
            _redisSocket.ReceiveTimeout = TimeSpan.Zero;

            InitPipeIO();
        }
        public void Dispose()
        {
            _redisSocket.ReceiveTimeout = _oldReceiveTimeout;
            _runing = false;
            try { _reader?.CancelPendingRead(); } catch { }
            try { _reader?.Complete(); } catch { }
            try { _writer?.CancelPendingFlush(); } catch { }
            try { _writer?.Complete(); } catch { }
            _redisSocket.ReleaseSocket();
            ClearAndTrySetExpcetion(null);
        }

        async public Task<RedisResult> WriteAsync(CommandPacket cmd)
        {
            var ex = _exception;
            if (ex != null) throw ex;
            var iq = WriteInQueue(cmd);
            var flush = _writer.FlushAsync();
            if (flush.IsCompletedSuccessfully == false)
                await flush.AsTask();
            if (iq == null) return new RedisResult(null, true, RedisMessageType.SimpleString);
            return await iq.TaskCompletionSource.Task;
        }
        void ClearAndTrySetExpcetion(Exception ioex)
        {
            lock (_writeQueueLock)
            {
                while (_writeQueue.TryDequeue(out var witem))
                {
                    if (ioex != null) witem.TaskCompletionSource.TrySetException(ioex);
                    else witem.TaskCompletionSource.TrySetCanceled();
                }
            }
        }
        void InitPipeIO()
        {
            lock (_writeQueueLock)
            {
                try { _reader?.CancelPendingRead(); } catch { }
                try { _reader?.Complete(); } catch { }
                try { _writer?.CancelPendingFlush(); } catch { }
                try { _writer?.Complete(); } catch { }
                _redisSocket.ReleaseSocket();
                try
                {
                    if (_redisSocket.IsConnected == false) _redisSocket.Connect();
                    _writer = PipeWriter.Create(_redisSocket.Stream);
                    _reader = PipeReader.Create(_redisSocket.Stream);
                    StartRead();
                }
                catch
                {
                }
            }
        }

        class WriteAsyncInfo
        {
            public TaskCompletionSource<RedisResult> TaskCompletionSource = new TaskCompletionSource<RedisResult>();
            public CommandPacket Command;
        }
        ConcurrentQueue<WriteAsyncInfo> _writeQueue = new ConcurrentQueue<WriteAsyncInfo>();
        object _writeQueueLock = new object();
        object _writeLock = new object();
        MemoryStream _bufferStream;
        WriteAsyncInfo WriteInQueue(CommandPacket cmd)
        {
            var ret = _redisSocket.ClientReply == ClientReplyType.on ? new WriteAsyncInfo { Command = cmd } : null;

            var isnew = false;
            lock (_writeQueueLock)
            {
                if (ret != null) _writeQueue.Enqueue(ret);
                if (_bufferStream == null)
                {
                    _bufferStream = new MemoryStream();
                    isnew = true;
                }
                new RespHelper.Resp3Writer(_bufferStream, _redisSocket.Encoding, _redisSocket.Protocol).WriteCommand(cmd);
            }
            if (isnew)
            {
                Thread.CurrentThread.Join(TimeSpan.FromMilliseconds(1));

                var ms = new MemoryStream();
                lock (_writeQueueLock)
                {
                    _bufferStream.Position = 0;
                    _bufferStream.CopyTo(ms);
                    _bufferStream.Close();
                    _bufferStream = null;
                }

                try
                {
                    ms.Position = 0;
                    lock (_writeLock)
                    {
                        ms.CopyTo(_redisSocket.Stream);
                    }
                    Interlocked.Exchange(ref _exception, null);
                }
                catch (Exception ioex)
                {
                    ClearAndTrySetExpcetion(ioex);
                    Interlocked.Exchange(ref _exception, ioex);
                    _redisSocket.ReleaseSocket();
                }
                finally
                {
                    ms.Close();
                    ms.Dispose();
                }
            }
            return ret;
        }

        void StartRead()
        {
            new Thread(() =>
            {
                Task.Run(async () =>
                {
                    while (_runing)
                    {
                        ReadResult readResult = default;
                        try
                        {
                            readResult = await _reader.ReadAsync();
                        }
                        catch (Exception ioex)
                        {
                            ClearAndTrySetExpcetion(ioex);
                            Interlocked.Exchange(ref _exception, ioex);
                            InitPipeIO();
                            break;
                        }
                        var buffer = readResult.Buffer;
                        var bufferStart = buffer.Start;
                        var bufferEnd = buffer.End;
                        long offset = 0;
                        RedisResult redisResult = null;

                        try
                        {
                            if (TryRead(ref buffer, ref offset, out redisResult))
                            {
                                bufferStart = buffer.Start;
                                bufferEnd = buffer.End;
                            }
                        }
                        finally
                        {
                            _reader.AdvanceTo(bufferStart, bufferEnd);

                            if (redisResult != null)
                            {
                                if (_writeQueue.TryDequeue(out var witem))
                                    witem.TaskCompletionSource.TrySetResult(redisResult);
                                else
                                    ClearAndTrySetExpcetion(new RedisClientException($"AsyncRedisSocket: Message sequence error"));
                            }
                        }
                    }
                });
            }).Start();

            #region Read
            bool TryRead(ref ReadOnlySequence<byte> buffer, ref long offset, out RedisResult result)
            {
                if (!TryReadByte(ref buffer, ref offset, out var msgtype))
                {
                    result = null;
                    return false;
                }
                switch ((char)msgtype)
                {
                    case '$': return (result = ReadBlobString(ref buffer, ref offset, RedisMessageType.BlobString)) != null;
                    case '+': return (result = ReadLineString(ref buffer, ref offset, RedisMessageType.SimpleString)) != null;
                    case '=': return (result = ReadBlobString(ref buffer, ref offset, RedisMessageType.VerbatimString)) != null;
                    case '-': return (result = ReadLineString(ref buffer, ref offset, RedisMessageType.SimpleError)) != null;
                    case '!': return (result = ReadBlobString(ref buffer, ref offset, RedisMessageType.BlobError)) != null;
                    case ':': return (result = ReadNumber(ref buffer, ref offset, RedisMessageType.Number)) != null;
                    case '(': return (result = ReadBigNumber(ref buffer, ref offset, RedisMessageType.BigNumber)) != null;
                    case '_': return (result = ReadNull(ref buffer, ref offset, RedisMessageType.Null)) != null;
                    case ',': return (result = ReadDouble(ref buffer, ref offset, RedisMessageType.Double)) != null;
                    case '#': return (result = ReadBoolean(ref buffer, ref offset, RedisMessageType.Boolean)) != null;

                    case '*': return (result = ReadArray(ref buffer, ref offset, RedisMessageType.Array)) != null;
                    case '~': return (result = ReadArray(ref buffer, ref offset, RedisMessageType.Set)) != null;
                    case '>': return (result = ReadArray(ref buffer, ref offset, RedisMessageType.Push)) != null;
                    case '%': return (result = ReadMap(ref buffer, ref offset, RedisMessageType.Map)) != null;
                    case '|': return (result = ReadMap(ref buffer, ref offset, RedisMessageType.Attribute)) != null;
                    case '.': return (result = ReadEnd(ref buffer, ref offset, RedisMessageType.SimpleString)) != null;
                    case ' ': result = null; return true;
                    default: throw new ProtocolViolationException($"Expecting fail MessageType '{(char)msgtype}'");
                }
            }

            bool TryReadSize(ref ReadOnlySequence<byte> buffer, ref long offset, long size, out ReadOnlySequence<byte> result)
            {
                long remaining = 0;
                foreach (var it in buffer)
                {
                    remaining += it.Span.Length;
                    if (remaining >= size)
                    {
                        offset += size;
                        result = buffer.Slice(0, size);
                        buffer = buffer.Slice(size);
                        return true;
                    }
                }
                result = default;
                return false;
            }
            bool TryReadLine(ref ReadOnlySequence<byte> buffer, ref long offset, out string result)
            {
                var pos = buffer.PositionOf((byte)'\r');
                if (pos == null || buffer.Slice(pos.Value, 2).Slice(1).First.Span[0] != (byte)'\n')
                {
                    result = null;
                    return false;
                }
                var span = buffer.Slice(0, pos.Value);
                var spanlen = span.Length;
                offset += spanlen + 2;
                result = Encoding.UTF8.GetString(span.ToArray());
                buffer = buffer.Slice(spanlen + 2);
                return true;
            }
            bool TryReadSizeText(ref ReadOnlySequence<byte> buffer, ref long offset, long size, out string result)
            {
                if (!TryReadSize(ref buffer, ref offset, size, out var readbuffer))
                {
                    result = null;
                    return false;
                }
                result = Encoding.UTF8.GetString(readbuffer.ToArray());
                return true;
            }
            bool TryReadByte(ref ReadOnlySequence<byte> buffer, ref long offset, out byte result)
            {
                if (!TryReadSize(ref buffer, ref offset, 1, out var readbuffer))
                {
                    result = 0;
                    return false;
                }
                result = readbuffer.First.Span[0];
                return true;
            }

            RedisResult ReadBlobString(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
            {
                if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
                if (long.TryParse(line, out var line_int))
                {
                    if (!TryReadSizeText(ref buffer, ref offset, line_int, out var text)) return null;
                    if (!TryReadLine(ref buffer, ref offset, out var emptyline)) return null;
                    return new RedisResult(text, false, msgtype);
                }
                if (line == "?")
                {
                    var lst = new List<ReadOnlySequence<byte>>();
                    while (true)
                    {
                        if (!TryReadByte(ref buffer, ref offset, out var c)) return null;
                        if ((char)c != ';') throw new ProtocolViolationException();

                        if (!TryReadLine(ref buffer, ref offset, out line)) return null;
                        if (!long.TryParse(line, out line_int)) throw new ProtocolViolationException();

                        if (line_int > 0)
                        {
                            if (!TryReadSize(ref buffer, ref offset, line_int, out var blob2)) return null;
                            lst.Add(blob2);
                            continue;
                        }

                        using (var ms = new MemoryStream())
                        {
                            foreach (var ls in lst)
                                foreach (var it in ls)
                                    ms.Write(it.Span.ToArray(), 0, it.Span.Length);
                            var ret = new RedisResult(Encoding.UTF8.GetString(ms.ToArray()), false, msgtype);
                            ms.Close();
                            return ret;
                        }
                    }
                }
                throw new NotSupportedException();
            }
            RedisResult ReadLineString(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
            {
                if (!TryReadLine(ref buffer, ref offset, out var result)) return null;
                return new RedisResult(result, false, msgtype);
            }
            RedisResult ReadNumber(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
            {
                if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
                if (!long.TryParse(line, out var num)) throw new ProtocolViolationException($"Expecting fail Number '{msgtype}0', got '{msgtype}{line}'");
                return new RedisResult(num, false, msgtype);
            }
            RedisResult ReadBigNumber(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
            {
                if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
                if (!BigInteger.TryParse(line, NumberStyles.Any, null, out var num)) throw new ProtocolViolationException($"Expecting fail BigNumber '{msgtype}0', got '{msgtype}{line}'");
                return new RedisResult(num, false, msgtype);
            }
            RedisResult ReadNull(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
            {
                if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
                return new RedisResult(null, false, msgtype);
            }
            RedisResult ReadDouble(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
            {
                if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
                double num = 0;
                switch (line)
                {
                    case "inf": num = double.PositiveInfinity; break;
                    case "-inf": num = double.NegativeInfinity; break;
                    default:
                        if (!double.TryParse(line, NumberStyles.Any, null, out num)) throw new ProtocolViolationException($"Expecting fail Double '{msgtype}1.23', got '{msgtype}{line}'");
                        break;
                }
                return new RedisResult(num, false, msgtype);
            }
            RedisResult ReadBoolean(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
            {
                if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
                switch (line)
                {
                    case "t": return new RedisResult(true, false, msgtype);
                    case "f": return new RedisResult(false, false, msgtype);
                }
                throw new ProtocolViolationException($"Expecting fail Boolean '{msgtype}t', got '{msgtype}{line}'");
            }

            RedisResult ReadArray(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
            {
                if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
                if (int.TryParse(line, out var len))
                {
                    if (len < 0) return null;
                    var arr = new object[len];
                    for (var a = 0; a < len; a++)
                    {
                        if (!TryRead(ref buffer, ref offset, out var item)) return null;
                        arr[a] = item.Value;
                    }
                    if (len == 1 && arr[0] == null) arr = new object[0];
                    return new RedisResult(arr, false, msgtype);
                }
                if (line == "?")
                {
                    var arr = new List<object>();
                    while(true)
                    {
                        if (!TryRead(ref buffer, ref offset, out var item)) return null;
                        if (item.IsEnd) break;
                        arr.Add(item.Value);
                    }
                    return new RedisResult(arr.ToArray(), false, msgtype);
                }
                throw new ProtocolViolationException($"Expecting fail Array '{msgtype}3', got '{msgtype}{line}'");
            }
            RedisResult ReadMap(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
            {
                if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
                if (int.TryParse(line, out var len))
                {
                    if (len < 0) return null;
                    var arr = new object[len * 2];
                    for (var a = 0; a < len; a++)
                    {
                        if (!TryRead(ref buffer, ref offset, out var key)) return null;
                        if (!TryRead(ref buffer, ref offset, out var value)) return null;
                        arr[a * 2] = key.Value;
                        arr[a * 2 + 1] = value.Value;
                    }
                    return new RedisResult(arr, false, msgtype);
                }
                if (line == "?")
                {
                    var arr = new List<object>();
                    while (true)
                    {
                        if (!TryRead(ref buffer, ref offset, out var key)) return null;
                        if (key.IsEnd) break;
                        if (!TryRead(ref buffer, ref offset, out var value)) return null;
                        arr.Add(key.Value);
                        arr.Add(value.Value);
                    }
                    return new RedisResult(arr.ToArray(), false, msgtype);
                }
                throw new ProtocolViolationException($"Expecting fail Map '{msgtype}3', got '{msgtype}{line}'");
            }
            RedisResult ReadEnd(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
            {
                if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
                return new RedisResult(null, true, msgtype);
            }
            #endregion
        }
    }
}
#endif