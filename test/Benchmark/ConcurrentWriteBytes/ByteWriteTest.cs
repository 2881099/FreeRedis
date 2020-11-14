using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConcurrentWriteBytes
{
    [MemoryDiagnoser, CoreJob, MarkdownExporter, RPlotExporter]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]

    public unsafe class ByteWriteTest
    {
        private readonly byte[] Buffer;
        private readonly long SetOffsetPre;
        public ByteWriteTest()
        {
            Buffer = Encoding.UTF8.GetBytes("SET ");
            fixed (byte* c = Buffer)
            {
                SetOffsetPre = *(long*)(c);
            }
        }


        [Benchmark(Description = "EncodingGetBytes")]
        public void TestEncodingGetBytes()
        {
            var result = Encoding.UTF8.GetBytes("SET ");
        }
        [Benchmark(Description = "ArrayCopy")]
        public void TestArrayCopy()
        {
            var result = new byte[4];
            Array.Copy(Buffer, result, 4);
        }
        [Benchmark(Description = "BufferAsSpan")]
        public void TestSpan()
        {
            var span = Buffer.AsSpan();
            var result = new byte[4];
            result[0] = span[0];
            result[1] = span[1];
            result[2] = span[2];
            result[3] = span[3];
        }
        [Benchmark(Description = "Buffer")]
        public void TestDirectAssent()
        {
            var result = new byte[4];
            result[0] = Buffer[0];
            result[1] = Buffer[1];
            result[2] = Buffer[2];
            result[3] = Buffer[3];
        }

        [Benchmark(Description = "UnsafePtr")]
        public void TestUnsafePtr()
        {
            var result = new byte[4];
            fixed (byte* c = Buffer)
            {
                *(long*)c = SetOffsetPre;
            }
        }
    }
}
