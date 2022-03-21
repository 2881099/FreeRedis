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


        //[Benchmark]
        //public void EncodingConvertWithFalse()
        //{
        //    Utf8Encoder.Convert("4141\r\nabc\r\nab\r\n", _writer, false, out _, out _);
        //    _writer.FlushAsync();
        //}

        //[Benchmark]
        //public void EncodingConvertWithTrue()
        //{
        //    Utf8Encoder.Convert("4141\r\nabc\r\nab\r\n", _writer, true, out _, out _);
        //    _writer.FlushAsync();
        //}

        //[Benchmark]
        //public void EncodingUtf8GetBytes()
        //{
        //    var bytes = Encoding.UTF8.GetBytes("4141\r\nabc\r\nab\r\n");
        //    _writer.WriteAsync(bytes);
        //}

        [Benchmark]
        public void ContactAndConvert()
        {
            _writer.Write(_fixBuffer1);
            Utf8Encoder.Convert("abc\r\nab", _writer, false, out _, out _);
            _writer.Write(_fixBuffer2);
            _writer.FlushAsync();
        }
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
