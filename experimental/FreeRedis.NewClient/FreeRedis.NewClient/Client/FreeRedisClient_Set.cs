using FreeRedis.Engine;
using FreeRedis.NewClient.Protocols;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace FreeRedis.NewClient.Client
{
    public sealed partial class FreeRedisClient : FreeRedisClientBase
    {
        private long _write_counter;
        private long _write_backlog_counter;
        private SpinWait _flush_waiter;

        public Task<bool> SetAsync3(string key, string value)
        {
            long currentSendToken = 0;
            var chars1 = key.Length.ToString().AsSpan();
            var chars2 = key.AsSpan();
            var chars3 = value.Length.ToString().AsSpan();
            var chars4 = value.AsSpan();
            TaskCompletionSource<bool> result = new(TaskCreationOptions.RunContinuationsAsynchronously);
            ProtocolContinueResult delegateMethod(ref SequenceReader<byte> reader) { return StateProtocol.HandleBytes(result, ref reader); }
            if (chars1.Length + chars2.Length + chars3.Length + chars4.Length + _set_command_length <= 1048576)
            {
                // The input span is small enough where we can one-shot this.

                int byteCount1 = _utf8.GetByteCount(chars1);
                int byteCount2 = _utf8.GetByteCount(chars2);
                int byteCount3 = _utf8.GetByteCount(chars3);
                int byteCount4 = _utf8.GetByteCount(chars4);
                var length = byteCount1 + byteCount2 + byteCount3 + byteCount4 + _set_command_length;
                var currentFetch = _set_header_length + byteCount1;

                WaitSendLock();
                currentSendToken = _current_send_token;
#if DEBUG
                FreeRedisTracer.CurrentBacklogTaskCount += 1;
#endif

                _taskBuffer.Enqueue(delegateMethod);

                Span<byte> scratchBuffer = _sender.GetSpan(length);
                _set_header_buffer.CopyTo(scratchBuffer);

                _utf8.GetBytes(chars1, scratchBuffer.Slice(_set_header_length, byteCount1));
                scratchBuffer[currentFetch] = 13;
                scratchBuffer[currentFetch + 1] = 10;
                currentFetch += 2;

                _utf8.GetBytes(chars2, scratchBuffer.Slice(currentFetch, byteCount2));
                currentFetch += byteCount2;
                scratchBuffer[currentFetch] = 13;
                scratchBuffer[currentFetch + 1] = 10;
                scratchBuffer[currentFetch + 2] = 36;
                currentFetch += 3;

                _utf8.GetBytes(chars3, scratchBuffer.Slice(currentFetch, byteCount3));
                currentFetch += byteCount3;
                scratchBuffer[currentFetch] = 13;
                scratchBuffer[currentFetch + 1] = 10;
                currentFetch += 2;

                _utf8.GetBytes(chars4, scratchBuffer.Slice(currentFetch, byteCount4));
                currentFetch += byteCount4;
                scratchBuffer[currentFetch] = 13;
                scratchBuffer[currentFetch + 1] = 10;

                _sender.Advance(length);

            }
            else
            {
                WaitSendLock();
#if DEBUG
                FreeRedisTracer.CurrentBacklogTaskCount += 1;
#endif        
                _taskBuffer.Enqueue(delegateMethod);
                //Allocate a stateful Encoder instance and chunk this.
                _utf8_encoder.Convert($"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n", _sender, true, out _, out _);
            }

            if (_sender.CanGetUnflushedBytes && _sender.UnflushedBytes > 1024)
            {
#if DEBUG
                FreeRedisTracer.ForceFlushCount += 1;
#endif
                _sender.FlushAsync().ConfigureAwait(false);
            }

            ReleaseSendLock();


#if RELEASE
            if (TryGetSendLock())
            {
                if (currentSendToken == _current_send_token)
                {
                    _sender.FlushAsync().ConfigureAwait(false);
                    NextToken();
                }
                ReleaseSendLock();
            }
#else
            bool flushShut = _sender.CanGetUnflushedBytes;
            if (flushShut)
            {
                if (TryGetSendLock())
                {
                    if (currentSendToken == _current_send_token)
                    {
                        FreeRedisTracer.NormalFlushCount += 1;
                        FreeRedisTracer.CurrentBacklogTaskCount = 0;
                        _sender.FlushAsync().ConfigureAwait(false);
                        NextToken();
                    }
                    else
                    {
                        FreeRedisTracer.InnerFlushEscapeCount += 1;
                    }
                    ReleaseSendLock();
                }
                else
                {
                    if (flushShut)
                    {
                        FreeRedisTracer.AvailableFlushEscapeCount += 1;
                    }
                    else
                    {
                        FreeRedisTracer.UnAvailableFlushEscapeCount += 1;
                    }

                }
            }
#endif
            return result.Task;
        }

        private long _current_send_token;

        public Task<bool> SetAsync5(string key, string value)
        {
            long currentSendToken = 0;
            var valueLength = value.Length;
            var keyChars = key.AsSpan();
            var valueChars = value.AsSpan();
            TaskCompletionSource<bool> result = new(TaskCreationOptions.RunContinuationsAsynchronously);
            ProtocolContinueResult delegateMethod(ref SequenceReader<byte> reader) { return StateProtocol.HandleBytes(result, ref reader); }
            if (valueLength<1000)
            {
                var keyInfo = _protocolFillArray[key.Length];
                var valueInfo = _protocolFillArray[valueLength];
                if (keyInfo.Length + keyChars.Length + valueInfo.Length + valueChars.Length + _set_command_length <= 1048576)
                {
                    // The input span is small enough where we can one-shot this.
                    int byteCount1 = _utf8.GetByteCount(keyChars);
                    int byteCount2 = _utf8.GetByteCount(valueChars);
                    var length = byteCount1 + byteCount2 + keyInfo.Length + valueInfo.Length + _set_command_length;
                    var currentFetch = _set_header_length;

                    WaitSendLock();
                    currentSendToken = _current_send_token;
                    _taskBuffer.Enqueue(delegateMethod);
                    Span<byte> scratchBuffer = _sender.GetSpan(length);
                    _sender.Advance(length);
                    Interlocked.Add(ref _write_counter,1);
#if DEBUG
                    FreeRedisTracer.CurrentBacklogTaskCount += 1;
#endif
                    ReleaseSendLock();


                    _set_header_buffer.CopyTo(scratchBuffer);


                    if (keyInfo.Length == 2)
                    {
                        scratchBuffer[currentFetch] = keyInfo.Values[0];
                        scratchBuffer[currentFetch + 1] = keyInfo.Values[1];
                        currentFetch += 2;
                    }
                    else if (keyInfo.Length == 1)
                    {
                        scratchBuffer[currentFetch] = keyInfo.Values[0];
                        currentFetch += 1;
                    }
                    else if (keyInfo.Length == 3)
                    {
                        scratchBuffer[currentFetch] = keyInfo.Values[0];
                        scratchBuffer[currentFetch + 1] = keyInfo.Values[1];
                        scratchBuffer[currentFetch + 2] = keyInfo.Values[2];
                        currentFetch += 3;
                    }
                    scratchBuffer[currentFetch] = 13;
                    scratchBuffer[currentFetch + 1] = 10;
                    currentFetch += 2;

                    _utf8.GetBytes(keyChars, scratchBuffer.Slice(currentFetch, byteCount1));
                    currentFetch += byteCount1;
                    scratchBuffer[currentFetch] = 13;
                    scratchBuffer[currentFetch + 1] = 10;
                    scratchBuffer[currentFetch + 2] = 36;
                    currentFetch += 3;

                    if (valueInfo.Length == 2)
                    {
                        scratchBuffer[currentFetch] = valueInfo.Values[0];
                        scratchBuffer[currentFetch + 1] = valueInfo.Values[1];
                        currentFetch += 2;
                    }
                    else if (valueInfo.Length == 1)
                    {
                        scratchBuffer[currentFetch] = valueInfo.Values[0];
                        currentFetch += 1;
                    }
                    else if (valueInfo.Length == 3)
                    {
                        scratchBuffer[currentFetch] = valueInfo.Values[0];
                        scratchBuffer[currentFetch + 1] = valueInfo.Values[1];
                        scratchBuffer[currentFetch + 2] = valueInfo.Values[2];
                        currentFetch += 3;
                    }
                    scratchBuffer[currentFetch] = 13;
                    scratchBuffer[currentFetch + 1] = 10;
                    currentFetch += 2;


                    _utf8.GetBytes(valueChars, scratchBuffer.Slice(currentFetch, byteCount2));
                    currentFetch += byteCount2;
                    scratchBuffer[currentFetch] = 13;
                    scratchBuffer[currentFetch + 1] = 10;
                    Interlocked.Add(ref _write_counter,-1);


                }
                else
                {
                    WaitSendLock();
                    _taskBuffer.Enqueue(delegateMethod);
                    //Allocate a stateful Encoder instance and chunk this.
                    _utf8_encoder.Convert($"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n", _sender, true, out _, out _);
#if DEBUG
                    FreeRedisTracer.CurrentBacklogTaskCount += 1;
#endif
                    ReleaseSendLock();
                }
            }
            else
            {
                var valueLengthChars = value.Length.ToString().AsSpan();
                var keyInfo = _protocolFillArray[key.Length];
                if (keyInfo.Length + keyChars.Length + valueLengthChars.Length + valueChars.Length + _set_command_length <= 1048576)
                {
                    // The input span is small enough where we can one-shot this.
                    int byteCount1 = _utf8.GetByteCount(keyChars);
                    int byteCount2 = _utf8.GetByteCount(valueChars);
                    int byteCount3 = _utf8.GetByteCount(valueLengthChars);
                    var length = byteCount1 + byteCount2 + keyInfo.Length + byteCount3 + _set_command_length;
                    var currentFetch = _set_header_length;

                    WaitSendLock();
                    currentSendToken = _current_send_token;
                    _taskBuffer.Enqueue(delegateMethod);
                    Span<byte> scratchBuffer = _sender.GetSpan(length);
                    _sender.Advance(length);
                    Interlocked.Add(ref _write_counter,1);
#if DEBUG
                    FreeRedisTracer.CurrentBacklogTaskCount += 1;
#endif
                    ReleaseSendLock();


                    _set_header_buffer.CopyTo(scratchBuffer);


                    if (keyInfo.Length == 2)
                    {
                        scratchBuffer[currentFetch] = keyInfo.Values[0];
                        scratchBuffer[currentFetch + 1] = keyInfo.Values[1];
                        currentFetch += 2;
                    }
                    else if (keyInfo.Length == 1)
                    {
                        scratchBuffer[currentFetch] = keyInfo.Values[0];
                        currentFetch += 1;
                    }
                    else if (keyInfo.Length == 3)
                    {
                        scratchBuffer[currentFetch] = keyInfo.Values[0];
                        scratchBuffer[currentFetch + 1] = keyInfo.Values[1];
                        scratchBuffer[currentFetch + 2] = keyInfo.Values[2];
                        currentFetch += 3;
                    }
                    scratchBuffer[currentFetch] = 13;
                    scratchBuffer[currentFetch + 1] = 10;
                    currentFetch += 2;

                    _utf8.GetBytes(keyChars, scratchBuffer.Slice(currentFetch, byteCount1));
                    currentFetch += byteCount1;
                    scratchBuffer[currentFetch] = 13;
                    scratchBuffer[currentFetch + 1] = 10;
                    scratchBuffer[currentFetch + 2] = 36;
                    currentFetch += 3;

                    _utf8.GetBytes(valueLengthChars, scratchBuffer.Slice(currentFetch, byteCount3));
                    currentFetch += byteCount3;
                    scratchBuffer[currentFetch] = 13;
                    scratchBuffer[currentFetch + 1] = 10;
                    currentFetch += 2;


                    _utf8.GetBytes(valueChars, scratchBuffer.Slice(currentFetch, byteCount2));
                    currentFetch += byteCount2;
                    scratchBuffer[currentFetch] = 13;
                    scratchBuffer[currentFetch + 1] = 10;

#if DEBUG
                    FreeRedisTracer.CurrentBacklogTaskCount += 1;
#endif
                    Interlocked.Add(ref _write_counter,-1);


                }
                else
                {
                    WaitSendLock();
                    _taskBuffer.Enqueue(delegateMethod);
                    //Allocate a stateful Encoder instance and chunk this.
                    _utf8_encoder.Convert($"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n", _sender, true, out _, out _);
#if DEBUG
                    FreeRedisTracer.CurrentBacklogTaskCount += 1;
#endif
                    ReleaseSendLock();
                }
            }




#if RELEASE
            if (TryGetSendLock())
            {
                if (currentSendToken == _current_send_token)
                {
                    FlushSender();
                   NextToken();
                   
                }
                 ReleaseSendLock();
            }


#else
            bool flushShut = _sender.CanGetUnflushedBytes;

            if (TryGetSendLock())
            {
                if (currentSendToken == _current_send_token)
                {
                    FlushSender();
                    NextToken();
                    FreeRedisTracer.CurrentBacklogTaskCount = 0;
                }
                else
                {
                    FreeRedisTracer.InnerFlushEscapeCount += 1;
                }
                ReleaseSendLock();
            }
            else
            {
                if (flushShut)
                {
                    FreeRedisTracer.AvailableFlushEscapeCount += 1;
                }
                else
                {
                    FreeRedisTracer.UnAvailableFlushEscapeCount += 1;
                }

            }
#endif
            return result.Task;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void NextToken()
        {
            if (_current_send_token + 1 == long.MaxValue)
            {
                _current_send_token = 0;
            }
            else
            {
                _current_send_token += 1;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void FlushSender()
        {

#if RELEASE
            //等待缓冲区填充完
            while (Interlocked.CompareExchange(ref _write_counter, 0, 0) != 0)
            {
                _flush_waiter.SpinOnce();
            }
#else
            FreeRedisTracer.CurrentBacklogTaskCount = _write_backlog_counter;
            //等待缓冲区填充完
            while (Interlocked.CompareExchange(ref _write_counter, 0, 0) != 0)
            {
                FreeRedisTracer.CurrentWaitHandleBacklogCount += 1;
                _flush_waiter.SpinOnce();
            }
            FreeRedisTracer.CurrentBacklogTaskCount = 0;
            FreeRedisTracer.NormalFlushCount += 1;

#endif
            _sender.FlushAsync().ConfigureAwait(false);
            _write_backlog_counter = 0;
            _flush_waiter.Reset();
        }

    }

}
