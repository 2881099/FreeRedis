using BenchmarkDotNet.Attributes;
using FreeRedis.Transport;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;

namespace BytesReaderTest
{
    [MemoryDiagnoser]
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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void WriteStringWithCrlf(string value)
        {
            var chars = value.AsSpan();
            int sizeHint = Utf8Encoder.GetByteCount(chars, true);
            Span<byte> span = _writer.GetSpan(sizeHint + 2);
            Utf8Encoder.Convert(chars, span, true, out _, out var bytesUsed2, out _);
            span[bytesUsed2++] = (byte)'\r';
            span[bytesUsed2++] = (byte)'\n';
            _writer.Advance(sizeHint + 2);
        }

        //[Benchmark]
        //public void WriteWithCrlf()
        //{
        //    i += 1;
        //    WriteStringWithCrlf(p1);
        //    WriteStringWithCrlf(i.ToString());
        //    WriteStringWithCrlf(p3);
        //    _writer.FlushAsync();
        //}
        [Benchmark]
        public void WriteWithCrlf1()
        {
            i += 1;
            _writer.Write(_fixBuffer1);
            WriteStringWithCrlf(i.ToString());
            WriteStringWithCrlf(p3);
            _writer.FlushAsync();
        }

        string p1 = "4141";
        string p2 = "abc";
        string p3 = "urueue6456465ab";

        //[Benchmark]
        //public void selfdivconvert()
        //{
        //    i += 1;
        //    var data = $"{p1}\r\n{i}\r\n{p3}\r\n";
        //    var chars = data.AsSpan();
        //    if (chars.Length <= 1048576)
        //    {
        //        int sizehint = Utf8Encoder.GetByteCount(chars, true);
        //        var span = _writer.GetSpan(sizehint);
        //        Utf8Encoder.Convert(chars, span, true, out _, out var bytesused2, out _);
        //        _writer.Advance(bytesused2);
        //    }
        //    else
        //    {
        //        do
        //        {
        //            int sizehint = Utf8Encoder.GetByteCount(chars.Slice(0, 1048576), flush: false);
        //            var span = _writer.GetSpan(sizehint);
        //            Utf8Encoder.Convert(chars, span, true, out var charsused, out var bytesused2, out _);
        //            chars = chars.Slice(charsused);
        //            _writer.Advance(bytesused2);
        //        }

        //        while (!chars.IsEmpty);
        //    }
        //    _writer.FlushAsync();

        //}

        private long i = 0;
        //[Benchmark]
        //public void EncodingUtf8GetBytes()
        //{
        //    i += 1;
        //    var data = $"{p1}\r\n{i}\r\n{p3}\r\n";
        //    _writer.WriteAsync(Encoding.UTF8.GetBytes(data));
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
