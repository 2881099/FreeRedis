using BenchmarkDotNet.Attributes;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace BytesReaderTest
{
    [MemoryDiagnoser, CoreJob, MarkdownExporter, RPlotExporter]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class OnceTest
    {
        private readonly string _target;
        private readonly byte[] _bytes;
        public OnceTest()
        {
            _target = "shjshdshdjshj";
            _bytes = Encoding.UTF8.GetBytes(_target);
        }
        [Benchmark]
        public unsafe void Unsafe()
        {

            fixed (byte* ptr = &_bytes[0])
            {
                var result = *(ulong*)ptr;
            }

        }
        [Benchmark]
        public void Safe()
        {

            BinaryPrimitives.ReadUInt64LittleEndian(_bytes);

        }
    }
}
