using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Disassemblers;
using FreeRedis.Transport;
using System;
using System.Buffers;
using System.ComponentModel.DataAnnotations;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using Utilities;

namespace BytesReaderTest
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class OnceTest
    {
        private string _target;
        private static readonly byte[] _fixBuffer1;
        private static readonly int _head_length;
        private static readonly int _tail_length;
        private static readonly int _set_length;
        private static readonly byte[] _fixBuffer2;
        private static readonly byte[] _fixBuffer3;
        private readonly PipeWriter _writer;
        private static readonly Encoder Utf8Encoder;
        private static readonly Encoding Utf8Handler;
        private const string _field = "abc\r\nurueue454456456465ab";

        static OnceTest()
        {
           
            _fixBuffer1 = Encoding.UTF8.GetBytes("SET\r\n\r\n");
            _fixBuffer2 = Encoding.UTF8.GetBytes("\r\n");
            _fixBuffer3 = Encoding.UTF8.GetBytes("123123123777777777123");
            Utf8Encoder = Encoding.UTF8.GetEncoder();
            Utf8Handler = Encoding.UTF8;
            _head_length = _fixBuffer1.Length;
            _tail_length = _fixBuffer2.Length;
            _set_length = _head_length + _tail_length * 4;
        }
        public OnceTest()
        {
            _target = "shjshdshdjshj";
            var pipe = SocketConnectionFactory.GetIOOperator();
            _writer = pipe.Transport.Output;
        }

        private const string _data = "4141\r\nabc\r\nurueue454456456465ab\r\n";

       


        //[Benchmark]
        public void WriteWithCopy()
        {
            var data = $"SET\r\n\r\n{i}\r\n{p2}\r\n{p3}\r\n888666999\r\n";
            _writer.Write(Utf8Handler.GetBytes(data));
        }


        //[Benchmark]
        public void WriteWithoutCopy()
        {
            var data = $"SET\r\n\r\n{i}\r\n{p2}\r\n{p3}\r\n888666999\r\n";
            Utf8Handler.GetBytes(data, _writer);
        }

       // [Benchmark(Description ="优化2")]
        public void WriteWithoutCopy2()
        {
            var data = $"SET\r\n\r\n{i}\r\n{p2}\r\n{p3}\r\n888666999\r\n";
            var chars = data.AsSpan();
            if (chars.Length <= 1048576)
            {
                // The input span is small enough where we can one-shot this.

                int byteCount = Utf8Handler.GetByteCount(chars);
                Span<byte> scratchBuffer = _writer.GetSpan(byteCount);
                int actualBytesWritten = Utf8Handler.GetBytes(chars, scratchBuffer);
                _writer.Advance(actualBytesWritten);
            }
            else
            {
                // Allocate a stateful Encoder instance and chunk this.
                Utf8Encoder.Convert(chars, _writer, true, out _, out _);
            }
        }

        //[Benchmark(Description = "优化3")]
        public void WriteWithoutCopy3()
        {
            var data = $"{i}\r\n{p2}\r\n{p3}\r\n888666999\r\n";
            var chars = data.AsSpan();
            if (chars.Length <= 1048576)
            {
                // The input span is small enough where we can one-shot this.

                int byteCount = Utf8Handler.GetByteCount(chars);
                Span<byte> scratchBuffer = _writer.GetSpan(byteCount + _head_length);
                _fixBuffer1.CopyTo(scratchBuffer);
                Utf8Handler.GetBytes(chars, scratchBuffer.Slice(_head_length, byteCount));
                _writer.Advance(byteCount + _head_length);
            }
            else
            {
                // Allocate a stateful Encoder instance and chunk this.
                Utf8Encoder.Convert(chars, _writer, true, out _, out _);
            }
        }
        //[Benchmark(Description = "Write合并")]
        public void WriteMulti()
        {

            var chars1 = i.ToString();
            _writer.Write(_fixBuffer1);
            _writer.Write(Encoding.UTF8.GetBytes(chars1));
            _writer.Write(_fixBuffer2);
            _writer.Write(Encoding.UTF8.GetBytes(p2));
            _writer.Write(_fixBuffer2);
            _writer.Write(Encoding.UTF8.GetBytes(p3));
            _writer.Write(_fixBuffer2);
            _writer.Write(Encoding.UTF8.GetBytes("888666999"));
            _writer.Write(_fixBuffer2);

        }

        [Benchmark(Description = "优化3.5")]
        public void WriteWithoutCopy35()
        {

            var chars1 = i.ToString().AsSpan();
            var chars2 = p2.AsSpan();
            var chars3 = p3.AsSpan();
            var chars4 = "888666999".AsSpan();
            if (chars1.Length + chars2.Length + chars3.Length + chars4.Length + _set_length <= 1048576)
            {
                // The input span is small enough where we can one-shot this.

                int byteCount1 = Utf8Handler.GetByteCount(chars1);
                int byteCount2 = Utf8Handler.GetByteCount(chars2);
                int byteCount3 = Utf8Handler.GetByteCount(chars3);
                int byteCount4 = Utf8Handler.GetByteCount(chars4);
                var length = byteCount1 + byteCount2 + byteCount3 + byteCount4 + _set_length;

                Span<byte> scratchBuffer = _writer.GetSpan(length);

                _fixBuffer1.CopyTo(scratchBuffer);
                var currentFetch = _head_length;
                _fixBuffer2.CopyTo(scratchBuffer.Slice(currentFetch, 2));
                currentFetch += 2;

                Utf8Handler.GetBytes(chars1, scratchBuffer.Slice(currentFetch, byteCount1));
                currentFetch += byteCount1;
                _fixBuffer2.CopyTo(scratchBuffer.Slice(currentFetch, 2));
                currentFetch += 2;

                Utf8Handler.GetBytes(chars2, scratchBuffer.Slice(currentFetch, byteCount2));
                currentFetch += byteCount2;
                _fixBuffer2.CopyTo(scratchBuffer.Slice(currentFetch, 2));
                currentFetch += 2;

                Utf8Handler.GetBytes(chars3, scratchBuffer.Slice(currentFetch, byteCount3));
                currentFetch += byteCount3;
                _fixBuffer2.CopyTo(scratchBuffer.Slice(currentFetch, 2));
                currentFetch += 2;

                Utf8Handler.GetBytes(chars4, scratchBuffer.Slice(currentFetch, byteCount4));
                currentFetch += byteCount4;
                _fixBuffer2.CopyTo(scratchBuffer.Slice(currentFetch, 2));
                //currentFetch += 2;

                _writer.Advance(length);
            }
            else
            {
                // Allocate a stateful Encoder instance and chunk this.
                //Utf8Encoder.Convert(chars, _writer, true, out _, out _);
            }
        }


        [Benchmark(Description = "优化4")]
        public void WriteWithoutCopy4()
        {

            var chars1 = i.ToString().AsSpan();
            var chars2 = p2.AsSpan();
            var chars3 = p3.AsSpan();
            var chars4 = "888666999".AsSpan();
            if (chars1.Length + chars2.Length + chars3.Length + chars4.Length + _set_length <= 1048576)
            {
                // The input span is small enough where we can one-shot this.

                int byteCount1 = Utf8Handler.GetByteCount(chars1);
                int byteCount2 = Utf8Handler.GetByteCount(chars2);
                int byteCount3 = Utf8Handler.GetByteCount(chars3);
                int byteCount4 = Utf8Handler.GetByteCount(chars4);
                var length = byteCount1 + byteCount2 + byteCount3 + byteCount4 + _set_length;
                
                Span<byte> scratchBuffer = _writer.GetSpan(length);

                _fixBuffer1.CopyTo(scratchBuffer);
                var currentFetch = _head_length;
                scratchBuffer[currentFetch] = 13;
                scratchBuffer[currentFetch + 1] = 10;
                currentFetch += 2;

                Utf8Handler.GetBytes(chars1, scratchBuffer.Slice(currentFetch, byteCount1));
                currentFetch += byteCount1;
                scratchBuffer[currentFetch] = 13;
                scratchBuffer[currentFetch + 1] = 10;
                currentFetch += 2;

                Utf8Handler.GetBytes(chars2, scratchBuffer.Slice(currentFetch, byteCount2));
                currentFetch += byteCount2;
                scratchBuffer[currentFetch] = 13;
                scratchBuffer[currentFetch + 1] = 10;
                currentFetch += 2;

                Utf8Handler.GetBytes(chars3, scratchBuffer.Slice(currentFetch, byteCount3));
                currentFetch += byteCount3;
                scratchBuffer[currentFetch] = 13;
                scratchBuffer[currentFetch + 1] = 10;
                currentFetch += 2;

                Utf8Handler.GetBytes(chars4, scratchBuffer.Slice(currentFetch, byteCount4));
                currentFetch += byteCount4;
                scratchBuffer[currentFetch] = 13;
                scratchBuffer[currentFetch + 1] = 10;

                _writer.Advance(length);
            }
            else
            {
                // Allocate a stateful Encoder instance and chunk this.
                //Utf8Encoder.Convert(chars, _writer, true, out _, out _);
            }
        }


        [Benchmark(Description = "优化5")]
        public void WriteWithoutCopy5()
        {

            WriteMutilWithoutCopy(_writer, i.ToString(), p2, p3,"888666999");
        }

        private unsafe static void WriteMutilWithoutCopy(IBufferWriter<byte> write, params string[] keys)
        {
            var lengthStore = stackalloc int[keys.Length];
            int totalLength = 0;
            int utf8BytesLength = 0;
            for (int i = 0; i < keys.Length; i+=1)
            {
                var chars = keys[i].AsSpan();
                totalLength += keys[i].AsSpan().Length;
                var needBytesLength = Utf8Handler.GetByteCount(chars);
                lengthStore[i] = needBytesLength;
                utf8BytesLength += needBytesLength;
            }
            totalLength += _set_length;
            if (totalLength <= 1048576)
            {
                // The input span is small enough where we can one-shot this.
                var currentFetch = _head_length;
                Span<byte> scratchBuffer = write.GetSpan(utf8BytesLength);
                _fixBuffer1.CopyTo(scratchBuffer);

                for (int i = 0; i < keys.Length; i += 1)
                {
                    var chars = keys[i].AsSpan();
                    Utf8Handler.GetBytes(chars, scratchBuffer.Slice(currentFetch, lengthStore[i]));
                    currentFetch += lengthStore[i];
                    scratchBuffer[currentFetch] = 13;
                    scratchBuffer[currentFetch + 1] = 10;
                    currentFetch += 2;
                }
                write.Advance(currentFetch);
            }
            else
            {
                // Allocate a stateful Encoder instance and chunk this.
                //Utf8Encoder.Convert(chars, _writer, true, out _, out _);
            }

        }


        //[Benchmark]
        public void NormalWriterByIBufferSpan()
        {
            var data = $"SET\r\n{i}\r\n{p3}\r\n123123123123";
            var bytes = Utf8Handler.GetByteCount(data);
            var span = _writer.GetSpan(bytes);
            _writer.Advance(bytes);
            Utf8Handler.GetBytes(data, span);
        }


        //[Benchmark]
        public void NormalAsyncWriter()
        {
            var data = $"SET\r\n{i}\r\n{p3}\r\n123123123123";
            var datas = Utf8Handler.GetBytes(data);
        }
        //[Benchmark]
        public void NormalWriteWithSpanCopy()
        {
            var data = $"SET\r\n{i}\r\n{p3}\r\n123123123123";
            var bytes = Utf8Handler.GetBytes(data);
            var span = _writer.GetSpan(bytes.Length);
            _writer.Advance(bytes.Length);
            bytes.CopyTo(span);
        }





        string p1 = "4141";
        string p2 = "abc";
        string p3 = "urueue6456465ab";
        private long i = 0;
       

    }
}
