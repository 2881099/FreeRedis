using BenchmarkDotNet.Attributes;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
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
        private readonly byte[] _bytes;
        public OnceTest()
        {
            _target = "shjshdshdjshj";
            _bytes = Encoding.UTF8.GetBytes(_target);

        }
        //[Benchmark]
        //public unsafe void Unsafe()
        //{

        //    fixed (byte* ptr = &_bytes[0])
        //    {
        //        var result = *(ulong*)ptr;
        //    }

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
