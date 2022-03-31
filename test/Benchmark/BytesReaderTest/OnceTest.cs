using BenchmarkDotNet.Attributes;
using FreeRedis.Transport;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace BytesReaderTest
{
    [MemoryDiagnoser, MarkdownExporter, RPlotExporter]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class OnceTest
    {
        private string _target;
        private static readonly byte[] _fixBuffer1;
        private static readonly byte[] _fixBuffer2;
        private readonly PipeWriter _writer;
        private static readonly Encoder Utf8Encoder;
        private const string _field = "abc\r\nurueue454456456465ab";
        static OnceTest()
        {
            _fixBuffer1 = Encoding.UTF8.GetBytes("4141\r\n");
            _fixBuffer2 = Encoding.UTF8.GetBytes("\r\n");
            Utf8Encoder = Encoding.UTF8.GetEncoder();
        }
        public OnceTest()
        {
            _target = "shjshdshdjshj";
            var pipe = SocketConnectionFactory.GetIOOperator();
            _writer = pipe.Transport.Output;
        }

        private const string _data = "4141\r\nabc\r\nurueue454456456465ab\r\n";
        //[Benchmark]
        //public void EncodingConvertWithFalse()
        //{
        //    Utf8Encoder.Convert(_data, _writer, true, out _, out _);
        //    _writer.FlushAsync();
        //}

        [Benchmark]
        public void SelfDivConvert()
        {
            var chars = _data.AsSpan();
            if (chars.Length <= 1048576)
            {
                int sizeHint = Utf8Encoder.GetByteCount(chars, true);
                Span<byte> span = _writer.GetSpan(sizeHint);
                Utf8Encoder.Convert(chars, span, true, out _, out var bytesUsed2, out _);
                _writer.Advance(bytesUsed2);
            }
            else
            {
                do
                {
                    int sizeHint = Utf8Encoder.GetByteCount(chars.Slice(0, 1048576), flush: false);
                    Span<byte> span = _writer.GetSpan(sizeHint);
                    Utf8Encoder.Convert(chars, span, true, out var charsUsed, out var bytesUsed2, out _);
                    chars = chars.Slice(charsUsed);
                    _writer.Advance(bytesUsed2);
                }
                while (!chars.IsEmpty);
            }
            _writer.FlushAsync();

        }

        //[Benchmark]
        //public void EncodingUtf8GetBytes()
        //{
        //    _writer.WriteAsync(Encoding.UTF8.GetBytes(_data));
        //}

        //[Benchmark]
        //public void ContactAndConvert()
        //{
        //    _writer.Write(_fixBuffer1);
        //    Utf8Encoder.Convert(_field, _writer, false, out _, out _);
        //    _writer.Write(_fixBuffer2);
        //    _writer.FlushAsync();
        //}
        //[Benchmark]
        //public void Safe()
        //{

        //    BinaryPrimitives.ReadUInt64LittleEndian(_bytes);

        //}

        //[Benchmark]
        //public unsafe void Unsafe2()
        //{

        //    fixed (char* ptr = _target)
        //    {
        //        var result = *(ulong*)ptr;
        //        result = *(ulong*)(ptr+8);
        //    }

        //}

    }
}
