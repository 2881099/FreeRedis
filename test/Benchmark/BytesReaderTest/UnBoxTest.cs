using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace BytesReaderTest
{

    [MemoryDiagnoser, MarkdownExporter, RPlotExporter]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class UnBoxTest
    {
        public readonly object _strObj;
        public readonly object _intObj;
        public UnBoxTest()
        {
            _strObj = "aaaaaaasdsds";
            _intObj = 100;
        }

        [Benchmark]
        public void IntUnboxTestUnsafe()
        {
            ref int result = ref Unsafe.Unbox<int>(_intObj);
        }

        [Benchmark]
        public void IntUnboxTestForce()
        {
            int result = (int)_intObj;
        }
    }
}
