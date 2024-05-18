//#if isasync
//using System;
//using System.Buffers;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Globalization;
//using System.IO;
//using System.IO.Pipelines;
//using System.Linq;
//using System.Net;
//using System.Net.Sockets;
//using System.Numerics;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace FreeRedis.Internal
//{
//    class AsyncPipelineRedisSocket
//    {
//        readonly IRedisSocket _redisSocket;
//        readonly TimeSpan _oldReceiveTimeout;
//        Pipe _readPipe;
//        Pipe _writePipe;
//        bool _runing = true;
//        Exception _exception;

//        public AsyncPipelineRedisSocket(IRedisSocket redisSocket)
//        {
//            _redisSocket = redisSocket;
//            _oldReceiveTimeout = _redisSocket.ReceiveTimeout;
//            _redisSocket.ReceiveTimeout = TimeSpan.Zero;

//            _readPipe = new Pipe();
//            _writePipe = new Pipe();
//            StartReadPipe(_readPipe);
//            StartWritePipe(_writePipe);
//        }
//        public void Dispose()
//        {
//            _runing = false;

//            try { _readPipe?.Reader?.CancelPendingRead(); } catch { }
//            try { _readPipe?.Reader?.Complete(); } catch { }
//            try { _readPipe?.Writer?.CancelPendingFlush(); } catch { }
//            try { _readPipe?.Writer?.Complete(); } catch { }

//            try { _writePipe?.Reader?.CancelPendingRead(); } catch { }
//            try { _writePipe?.Reader?.Complete(); } catch { }
//            try { _writePipe?.Writer?.CancelPendingFlush(); } catch { }
//            try { _writePipe?.Writer?.Complete(); } catch { }

//            _redisSocket.ReceiveTimeout = _oldReceiveTimeout;
//            _redisSocket.ReleaseSocket();
//            ClearAndTrySetExpcetion(null);
//        }

//        async public Task<RedisResult> WriteAsync(CommandPacket cmd)
//        {
//            var ex = _exception;
//            if (ex != null) throw ex;
//            var iq = WriteInQueue(cmd);
//            if (iq == null) return new RedisResult(null, true, RedisMessageType.SimpleString);
//            return await iq.TaskCompletionSource.Task;
//        }
//        void ClearAndTrySetExpcetion(Exception ioex)
//        {
//            lock (_writeQueueLock)
//            {
//                while (_writeQueue.TryDequeue(out var witem))
//                {
//                    if (ioex != null) witem.TaskCompletionSource.TrySetException(ioex);
//                    else witem.TaskCompletionSource.TrySetCanceled();
//                }
//            }
//        }

//        class WriteAsyncInfo
//        {
//            public TaskCompletionSource<RedisResult> TaskCompletionSource = new TaskCompletionSource<RedisResult>();
//            public CommandPacket Command;
//        }
//        ConcurrentQueue<WriteAsyncInfo> _writeQueue = new ConcurrentQueue<WriteAsyncInfo>();
//        object _writeQueueLock = new object();
//        WriteAsyncInfo WriteInQueue(CommandPacket cmd)
//        {
//            var ret = _redisSocket.ClientReply == ClientReplyType.on ? new WriteAsyncInfo { Command = cmd } : null;
//            Span<byte> span = default;
//            int spanlen = 0;
//            using (var ms = new MemoryStream())
//            {
//                ms.Write(new byte[] { 32, 32, 32, 32 }, 0, 4);
//                new RespHelper.Resp3Writer(ms, _redisSocket.Encoding, _redisSocket.Protocol).WriteCommand(cmd);
//                var bytes = ms.ToArray();
//                span = bytes.AsSpan();
//                spanlen = bytes.Length;
//                var size = (spanlen - 4).ToString("x");
//                for (var a = 0; a < size.Length; a++) span[a] = (byte)size[a];
//                ms.Close();
//            }
//            lock (_writeQueueLock)
//            {
//                if (ret != null) _writeQueue.Enqueue(ret);
//                var wtspan = _writePipe.Writer.GetSpan(spanlen);
//                for (var a = 0; a < spanlen; a++) wtspan[a] = span[a];

//                try
//                {
//                    _writePipe.Writer.Advance(spanlen);
//                    var flush = _writePipe.Writer.FlushAsync();
//                    if (!flush.IsCompletedSuccessfully)
//                    {
//                        flush.AsTask().Wait();
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Trace.WriteLine(ex.Message);
//                    throw ex;
//                }
//            }

//            return ret;
//        }

//        void StartWritePipe(Pipe pipe)
//        {
//            new Thread(() =>
//            {
//                Task.Run(async () =>
//                {
//                    while (_runing)
//                    {
//                        var readResult = await pipe.Reader.ReadAsync().ConfigureAwait(false);
//                        var buffer = readResult.Buffer;
//                        var bufferStart = buffer.Start;
//                        var bufferEnd = buffer.End;
//                        long offset = 0;

//                        try
//                        {
//                            if (TryReadSize(ref buffer, ref offset, 4, out var size))
//                            {
//                                if (!int.TryParse(Encoding.UTF8.GetString(size.ToArray()).TrimEnd(' '), NumberStyles.HexNumber, null, out var size_int))
//                                    throw new ProtocolViolationException();

//                                if (TryReadSize(ref buffer, ref offset, size_int, out var body))
//                                {
//                                    foreach (var it in body)
//                                    {
//                                        _redisSocket.Stream.Write(it.ToArray(), 0, it.Length);
//                                        //Console.WriteLine(Encoding.UTF8.GetString(it.ToArray()));
//                                    }

//                                    while (TryReadSize(ref buffer, ref offset, 1, out var tmp))
//                                    {
//                                    }
//                                    bufferStart = readResult.Buffer.GetPosition(offset);
//                                }
//                            }
//                        }
//                        finally
//                        {
//                            lock (_writeQueueLock)
//                            {
//                                pipe.Reader.AdvanceTo(bufferStart, bufferEnd);
//                            }
//                        }
//                    }
//                }).Wait();
//            }).Start();
//        }

//        void StartReadPipe(Pipe pipe)
//        {
//            var pipeLock = new object();
//            new Thread(() =>
//            {
//                var buffer = new byte[1024];
//                while (_runing)
//                {
//                    var readsize = 0;
//                    try
//                    {
//                        readsize = _redisSocket.Stream.Read(buffer, 0, buffer.Length);
//                    }
//                    catch
//                    {
//                        Thread.CurrentThread.Join(100);
//                    }

//                    lock (pipeLock)
//                    {
//                        var span = pipe.Writer.GetSpan(readsize);
//                        for (var a = 0; a < readsize; a++) span[a] = buffer[a];
//                        pipe.Writer.Advance(readsize);
//                    }
//                    var flush = pipe.Writer.FlushAsync();
//                    if (!flush.IsCompletedSuccessfully) flush.AsTask().Wait();
//                }
//            }).Start();

//            new Thread(() =>
//            {
//                Task.Run(async () =>
//                {
//                    while (_runing)
//                    {
//                        var readResult = await pipe.Reader.ReadAsync().ConfigureAwait(false);
//                        var buffer = readResult.Buffer;
//                        var bufferStart = buffer.Start;
//                        var bufferEnd = buffer.End;
//                        long offset = 0;
//                        RedisResult redisResult = null;

//                        try
//                        {
//                            if (TryRead(ref buffer, ref offset, out redisResult))
//                            {
//                                bufferStart = buffer.Start;
//                                bufferEnd = buffer.End;
//                            }
//                        }
//                        finally
//                        {
//                            lock (pipeLock)
//                            {
//                                pipe.Reader.AdvanceTo(bufferStart, bufferEnd);
//                            }

//                            if (redisResult != null)
//                            {
//                                if (_writeQueue.TryDequeue(out var witem))
//                                    witem.TaskCompletionSource.TrySetResult(redisResult);
//                                else
//                                    ClearAndTrySetExpcetion(new RedisClientException($"AsyncRedisSocket: Message sequence error"));
//                            }
//                        }
//                    }
//                }).Wait();
//            }).Start();


//        }

//        #region Read
//        static bool TryRead(ref ReadOnlySequence<byte> buffer, ref long offset, out RedisResult result)
//        {
//            if (!TryReadByte(ref buffer, ref offset, out var msgtype))
//            {
//                result = null;
//                return false;
//            }
//            switch ((char)msgtype)
//            {
//                case '$': return (result = ReadBlobString(ref buffer, ref offset, RedisMessageType.BlobString)) != null;
//                case '+': return (result = ReadLineString(ref buffer, ref offset, RedisMessageType.SimpleString)) != null;
//                case '=': return (result = ReadBlobString(ref buffer, ref offset, RedisMessageType.VerbatimString)) != null;
//                case '-': return (result = ReadLineString(ref buffer, ref offset, RedisMessageType.SimpleError)) != null;
//                case '!': return (result = ReadBlobString(ref buffer, ref offset, RedisMessageType.BlobError)) != null;
//                case ':': return (result = ReadNumber(ref buffer, ref offset, RedisMessageType.Number)) != null;
//                case '(': return (result = ReadBigNumber(ref buffer, ref offset, RedisMessageType.BigNumber)) != null;
//                case '_': return (result = ReadNull(ref buffer, ref offset, RedisMessageType.Null)) != null;
//                case ',': return (result = ReadDouble(ref buffer, ref offset, RedisMessageType.Double)) != null;
//                case '#': return (result = ReadBoolean(ref buffer, ref offset, RedisMessageType.Boolean)) != null;

//                case '*': return (result = ReadArray(ref buffer, ref offset, RedisMessageType.Array)) != null;
//                case '~': return (result = ReadArray(ref buffer, ref offset, RedisMessageType.Set)) != null;
//                case '>': return (result = ReadArray(ref buffer, ref offset, RedisMessageType.Push)) != null;
//                case '%': return (result = ReadMap(ref buffer, ref offset, RedisMessageType.Map)) != null;
//                case '|': return (result = ReadMap(ref buffer, ref offset, RedisMessageType.Attribute)) != null;
//                case '.': return (result = ReadEnd(ref buffer, ref offset, RedisMessageType.SimpleString)) != null;
//                case ' ': result = null; return true;
//                default: throw new ProtocolViolationException($"Expecting fail MessageType '{(char)msgtype}'");
//            }
//        }

//        static bool TryReadSize(ref ReadOnlySequence<byte> buffer, ref long offset, long size, out ReadOnlySequence<byte> result)
//        {
//            long remaining = 0;
//            foreach (var it in buffer)
//            {
//                remaining += it.Span.Length;
//                if (remaining >= size)
//                {
//                    offset += size;
//                    result = buffer.Slice(0, size);
//                    buffer = buffer.Slice(size);
//                    return true;
//                }
//            }
//            result = default;
//            return false;
//        }
//        static bool TryReadLine(ref ReadOnlySequence<byte> buffer, ref long offset, out string result)
//        {
//            var pos = buffer.PositionOf((byte)'\r');
//            if (pos == null || buffer.Slice(pos.Value, 2).Slice(1).First.Span[0] != (byte)'\n')
//            {
//                result = null;
//                return false;
//            }
//            var span = buffer.Slice(0, pos.Value);
//            var spanlen = span.Length;
//            offset += spanlen + 2;
//            result = Encoding.UTF8.GetString(span.ToArray());
//            buffer = buffer.Slice(spanlen + 2);
//            return true;
//        }
//        static bool TryReadSizeText(ref ReadOnlySequence<byte> buffer, ref long offset, long size, out string result)
//        {
//            if (!TryReadSize(ref buffer, ref offset, size, out var readbuffer))
//            {
//                result = null;
//                return false;
//            }
//            result = Encoding.UTF8.GetString(readbuffer.ToArray());
//            return true;
//        }
//        static bool TryReadByte(ref ReadOnlySequence<byte> buffer, ref long offset, out byte result)
//        {
//            if (!TryReadSize(ref buffer, ref offset, 1, out var readbuffer))
//            {
//                result = 0;
//                return false;
//            }
//            result = readbuffer.First.Span[0];
//            return true;
//        }

//        static RedisResult ReadBlobString(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
//        {
//            if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
//            if (long.TryParse(line, out var line_int))
//            {
//                if (!TryReadSizeText(ref buffer, ref offset, line_int, out var text)) return null;
//                if (!TryReadLine(ref buffer, ref offset, out var emptyline)) return null;
//                return new RedisResult(text, false, msgtype);
//            }
//            if (line == "?")
//            {
//                var lst = new List<ReadOnlySequence<byte>>();
//                while (true)
//                {
//                    if (!TryReadByte(ref buffer, ref offset, out var c)) return null;
//                    if ((char)c != ';') throw new ProtocolViolationException();

//                    if (!TryReadLine(ref buffer, ref offset, out line)) return null;
//                    if (!long.TryParse(line, out line_int)) throw new ProtocolViolationException();

//                    if (line_int > 0)
//                    {
//                        if (!TryReadSize(ref buffer, ref offset, line_int, out var blob2)) return null;
//                        lst.Add(blob2);
//                        continue;
//                    }

//                    using (var ms = new MemoryStream())
//                    {
//                        foreach (var ls in lst)
//                            foreach (var it in ls)
//                                ms.Write(it.Span.ToArray(), 0, it.Span.Length);
//                        var ret = new RedisResult(Encoding.UTF8.GetString(ms.ToArray()), false, msgtype);
//                        ms.Close();
//                        return ret;
//                    }
//                }
//            }
//            throw new NotSupportedException();
//        }
//        static RedisResult ReadLineString(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
//        {
//            if (!TryReadLine(ref buffer, ref offset, out var result)) return null;
//            return new RedisResult(result, false, msgtype);
//        }
//        static RedisResult ReadNumber(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
//        {
//            if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
//            if (!long.TryParse(line, out var num)) throw new ProtocolViolationException($"Expecting fail Number '{msgtype}0', got '{msgtype}{line}'");
//            return new RedisResult(num, false, msgtype);
//        }
//        static RedisResult ReadBigNumber(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
//        {
//            if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
//            if (!BigInteger.TryParse(line, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var num)) throw new ProtocolViolationException($"Expecting fail BigNumber '{msgtype}0', got '{msgtype}{line}'");
//            return new RedisResult(num, false, msgtype);
//        }
//        static RedisResult ReadNull(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
//        {
//            if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
//            return new RedisResult(null, false, msgtype);
//        }
//        static RedisResult ReadDouble(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
//        {
//            if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
//            double num = 0;
//            switch (line)
//            {
//                case "inf": num = double.PositiveInfinity; break;
//                case "-inf": num = double.NegativeInfinity; break;
//                default:
//                    if (!double.TryParse(line, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out num)) throw new ProtocolViolationException($"Expecting fail Double '{msgtype}1.23', got '{msgtype}{line}'");
//                    break;
//            }
//            return new RedisResult(num, false, msgtype);
//        }
//        static RedisResult ReadBoolean(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
//        {
//            if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
//            switch (line)
//            {
//                case "t": return new RedisResult(true, false, msgtype);
//                case "f": return new RedisResult(false, false, msgtype);
//            }
//            throw new ProtocolViolationException($"Expecting fail Boolean '{msgtype}t', got '{msgtype}{line}'");
//        }

//        static RedisResult ReadArray(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
//        {
//            if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
//            if (int.TryParse(line, out var len))
//            {
//                if (len < 0) return null;
//                var arr = new object[len];
//                for (var a = 0; a < len; a++)
//                {
//                    if (!TryRead(ref buffer, ref offset, out var item)) return null;
//                    arr[a] = item.Value;
//                }
//                if (len == 1 && arr[0] == null) arr = new object[0];
//                return new RedisResult(arr, false, msgtype);
//            }
//            if (line == "?")
//            {
//                var arr = new List<object>();
//                while (true)
//                {
//                    if (!TryRead(ref buffer, ref offset, out var item)) return null;
//                    if (item.IsEnd) break;
//                    arr.Add(item.Value);
//                }
//                return new RedisResult(arr.ToArray(), false, msgtype);
//            }
//            throw new ProtocolViolationException($"Expecting fail Array '{msgtype}3', got '{msgtype}{line}'");
//        }
//        static RedisResult ReadMap(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
//        {
//            if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
//            if (int.TryParse(line, out var len))
//            {
//                if (len < 0) return null;
//                var arr = new object[len * 2];
//                for (var a = 0; a < len; a++)
//                {
//                    if (!TryRead(ref buffer, ref offset, out var key)) return null;
//                    if (!TryRead(ref buffer, ref offset, out var value)) return null;
//                    arr[a * 2] = key.Value;
//                    arr[a * 2 + 1] = value.Value;
//                }
//                return new RedisResult(arr, false, msgtype);
//            }
//            if (line == "?")
//            {
//                var arr = new List<object>();
//                while (true)
//                {
//                    if (!TryRead(ref buffer, ref offset, out var key)) return null;
//                    if (key.IsEnd) break;
//                    if (!TryRead(ref buffer, ref offset, out var value)) return null;
//                    arr.Add(key.Value);
//                    arr.Add(value.Value);
//                }
//                return new RedisResult(arr.ToArray(), false, msgtype);
//            }
//            throw new ProtocolViolationException($"Expecting fail Map '{msgtype}3', got '{msgtype}{line}'");
//        }
//        static RedisResult ReadEnd(ref ReadOnlySequence<byte> buffer, ref long offset, RedisMessageType msgtype)
//        {
//            if (!TryReadLine(ref buffer, ref offset, out var line)) return null;
//            return new RedisResult(null, true, msgtype);
//        }
//        #endregion
//    }
//}
//#endif