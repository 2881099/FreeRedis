using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization.Metadata;

namespace BytesReaderTest
{

    [MemoryDiagnoser, MarkdownExporter, RPlotExporter]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class MethodTest
    {
        public readonly object _strObj;
        public readonly object _intObj;
        public MethodTest()
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

    public interface IBechTest
    {
        static abstract void Show();
    }

    public class MyBechTest : IBechTest
    {
        public static void Show()
        {

        }
    }

    //public class 
}
