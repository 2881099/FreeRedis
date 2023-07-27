using BenchmarkDotNet.Attributes;
using ConcurrentWriteBytes.Model;
using System.Buffers;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using FreeRedis.Transport;
using System.IO.Pipelines;

namespace ConcurrentWriteBytes
{

    [MemoryDiagnoser, CoreJob, MarkdownExporter, RPlotExporter]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]

    public class EncodeAndWriteTest
    {
        private static readonly Encoding _utf8;
        private static readonly Encoder _utf8_encoder;
        private static readonly byte[] _set_header_buffer;
        private static readonly int _set_command_length;
        private static readonly int _set_header_length;
        internal readonly struct UTF8KeyByte
        {
            public UTF8KeyByte(byte[] values, int length, int realLength)
            {
                Values = values;
                Length = length;
                ValueLength = realLength;
            }
            public readonly byte[] Values;
            public readonly int Length;
            public readonly int ValueLength;
        }

        private static UTF8KeyByte[] _protocolFillArray;
        private long _write_counter;
        private long _write_backlog_counter;
        private SpinWait _flush_waiter;
        private static readonly PipeWriter _sender;
        static EncodeAndWriteTest()
        {
            _utf8 = Encoding.UTF8;
            _utf8_encoder = _utf8.GetEncoder();
            _protocolFillArray = new UTF8KeyByte[10001];
            for (int i = 0; i < 10001; i++)
            {
                if (i == 0)
                {
                    continue;
                }
                if (i < 10)
                {
                    _protocolFillArray[i] = new(_utf8.GetBytes(i.ToString()), 1, i);
                }
                else if (i < 100)
                {
                    _protocolFillArray[i] = new(_utf8.GetBytes(i.ToString()), 2, i);
                }
                else if (i < 1000)
                {
                    _protocolFillArray[i] = new(_utf8.GetBytes(i.ToString()), 3, i);
                }
            }
            _set_header_buffer = _utf8.GetBytes("*3\r\n$3\r\nSET\r\n$");
            _set_header_length = _set_header_buffer.Length;
            _set_command_length = _set_header_buffer.Length + 9;
            var pipe = SocketConnectionFactory.GetIOOperator();
            _sender = pipe.Transport.Output;
        }

        //[Benchmark]
        //public void SingleOP3()
        //{
        //   SetAsync3("AAAAAAAA", "BBBBBBBBBBB");
        //}
        //[Benchmark]
        //public void SingleOP5()
        //{
        //    SetAsync5("AAAAAAAA", "BBBBBBBBBBB"); 
        //}

        [Benchmark]
        public void ConcurrentOP3()
        {
            var result = Parallel.For(0, 10000, item => { SetAsync3("AAAA3434AAAA", "BBBBBBBB5556BBB"); });
            while (!result.IsCompleted) { }
        }
        [Benchmark]
        public void ConcurrentOP5()
        {
            var result = Parallel.For(0, 10000, item => { SetAsync5("AAAA3434AAAA", "BBBBBBBB5556BBB"); });
            while (!result.IsCompleted) { }
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
        private long _current_send_token = 0;
        public void SetAsync3(string key, string value)
        {
            long currentSendToken = 0;
            var chars1 = key.Length.ToString().AsSpan();
            var chars2 = key.AsSpan();
            var chars3 = value.Length.ToString().AsSpan();
            var chars4 = value.AsSpan();
            TaskCompletionSource<bool> result = new();
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
                //Allocate a stateful Encoder instance and chunk this.
                _utf8_encoder.Convert($"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n", _sender, true, out _, out _);
            }

            ReleaseSendLock();



            if (TryGetSendLock())
            {
                if (currentSendToken == _current_send_token)
                {
                    _sender.FlushAsync().ConfigureAwait(false);
                    NextToken();
                }
                ReleaseSendLock();
            }

        }

        public void SetAsync5(string key, string value)
        {
            long currentSendToken = 0;
            var valueLength = value.Length;
            var keyChars = key.AsSpan();
            var valueChars = value.AsSpan();
            TaskCompletionSource<bool> result = new();
            if (valueLength < 1000)
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
                    Span<byte> scratchBuffer = _sender.GetSpan(length);
                    _sender.Advance(length);
                    Interlocked.Increment(ref _write_counter);
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
                    Interlocked.Decrement(ref _write_counter);


                }

            }



            if (TryGetSendLock())
            {
                if (currentSendToken == _current_send_token)
                {
                    FlushSender();
                    NextToken();

                }
                ReleaseSendLock();
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void FlushSender()
        {
            //等待缓冲区填充完
            while (Interlocked.CompareExchange(ref _write_counter, 0, 0) != 0)
            {
                _flush_waiter.SpinOnce();
            }

            _sender.FlushAsync().ConfigureAwait(false);
            _flush_waiter.Reset();
        }

        private int _send_lock_flag;
        private SpinWait _send_waiter;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void WaitSendLock()
        {

            while (Interlocked.CompareExchange(ref _send_lock_flag, 1, 0) != 0)
            {
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
        
    }

}
