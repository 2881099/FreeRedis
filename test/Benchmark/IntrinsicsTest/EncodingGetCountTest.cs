using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using System.Threading;

namespace IntrinsicsTest
{
    [MemoryDiagnoser, CoreJob, MarkdownExporter, RPlotExporter]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class EncodingGetCountTest
    {

        private const string Key1 = "a";
        private const string Key2 = "12345678901";
        private const string Key3 = "123456789012345678901";
        private readonly Encoding _utf8 = Encoding.UTF8;
        public readonly struct UTF8KeyByte
        {
            public UTF8KeyByte(byte[] values,int length,int realLength)
            {
                Values = values;
                Length = length;
                ValueLength = realLength;
            }
            public readonly byte[] Values;
            public readonly int Length;
            public readonly int ValueLength;
        }

        private UTF8KeyByte[] TempArray;
        

        public EncodingGetCountTest()
        {
            TempArray = new UTF8KeyByte[10001];
            for (int i = 0; i < 10001; i++)
            {
                if (i ==0)
                {
                    continue;
                }
                if (i<10)
                {
                    TempArray[i] = new(_utf8.GetBytes(i.ToString()), 1,i);
                }
                else if (i<100)
                {
                    TempArray[i] = new(_utf8.GetBytes(i.ToString()), 2, i);
                }
                else if (i<1000)
                {
                    TempArray[i] = new(_utf8.GetBytes(i.ToString()), 3, i);
                }
            }
        }

        [Benchmark]
        public void Optimic()
        {
           
            var key1Struct = TempArray[Key1.Length];
            var key2Struct = TempArray[Key2.Length];
            var key3Struct = TempArray[Key3.Length];
            var total_length = key1Struct.Length + key1Struct.ValueLength + key2Struct.Length + key2Struct.ValueLength + key3Struct.Length + key3Struct.ValueLength;
            if (total_length < 1048576)
            {
                int byteCount1 = _utf8.GetByteCount(Key1);
                int byteCount2 = _utf8.GetByteCount(Key2);
                int byteCount3 = _utf8.GetByteCount(Key3);
                var total_bytes_length = byteCount1 + byteCount2 + byteCount3 + key1Struct.Length + key2Struct.Length + key3Struct.Length;
                var scratchBuffer = new byte[total_bytes_length].AsSpan();
                var currentFetch = byteCount1;
                _utf8.GetBytes(Key1.AsSpan(), scratchBuffer[..byteCount1]);
                if (key1Struct.Length == 2)
                {
                    scratchBuffer[currentFetch] = key1Struct.Values[0];
                    scratchBuffer[currentFetch+1] = key1Struct.Values[1];
                    currentFetch += 2;
                }
                else if (key1Struct.Length == 1)
                {
                    scratchBuffer[currentFetch] = key1Struct.Values[0];
                    currentFetch += 1;
                }
                else if (key1Struct.Length == 3)
                {
                    scratchBuffer[currentFetch] = key1Struct.Values[0];
                    scratchBuffer[currentFetch + 1] = key1Struct.Values[1];
                    scratchBuffer[currentFetch + 2] = key1Struct.Values[2];
                    currentFetch += 3;
                }
                _utf8.GetBytes(Key2.AsSpan(), scratchBuffer.Slice(currentFetch, byteCount2));
                currentFetch += byteCount2;
                if (key2Struct.Length == 2)
                {
                    scratchBuffer[currentFetch] = key2Struct.Values[0];
                    scratchBuffer[currentFetch + 1] = key2Struct.Values[1];
                    currentFetch += 2;
                }
                else if (key2Struct.Length == 1)
                {
                    scratchBuffer[currentFetch] = key2Struct.Values[0];
                    currentFetch += 1;
                }
                else if (key2Struct.Length == 3)
                {
                    scratchBuffer[currentFetch] = key2Struct.Values[0];
                    scratchBuffer[currentFetch + 1] = key2Struct.Values[1];
                    scratchBuffer[currentFetch + 2] = key2Struct.Values[2];
                    currentFetch += 3;
                }
                _utf8.GetBytes(Key3.AsSpan(), scratchBuffer.Slice(currentFetch, byteCount3));
                currentFetch += byteCount3;
                if (key3Struct.Length == 2)
                {
                    scratchBuffer[currentFetch] = key3Struct.Values[0];
                    scratchBuffer[currentFetch + 1] = key3Struct.Values[1];
                    currentFetch += 2;
                }
                else if (key3Struct.Length == 1)
                {
                    scratchBuffer[currentFetch] = key3Struct.Values[0];
                    currentFetch += 1;
                }
                else if (key3Struct.Length == 3)
                {
                    scratchBuffer[currentFetch] = key3Struct.Values[0];
                    scratchBuffer[currentFetch + 1] = key3Struct.Values[1];
                    scratchBuffer[currentFetch + 2] = key3Struct.Values[2];
                    currentFetch += 3;
                }

                if (scratchBuffer[scratchBuffer.Length - 1] != '1')
                {
                    throw new Exception("LaJi");
                }
            }
        }
        [Benchmark]
        public void Normal()
        {
            var chars1 = Key1.AsSpan();
            var chars2 = Key1.Length.ToString().AsSpan();
            var chars3 = Key2.AsSpan(); 
            var chars4 = Key2.Length.ToString().AsSpan();
            var chars5 = Key3.AsSpan(); 
            var chars6 = Key3.Length.ToString().AsSpan();
            if (chars1.Length + chars2.Length + chars3.Length + chars4.Length + chars5.Length + chars6.Length <= 1048576)
            {
                // The input span is small enough where we can one-shot this.

                int byteCount1 = _utf8.GetByteCount(chars1);
                int byteCount2 = _utf8.GetByteCount(chars2);
                int byteCount3 = _utf8.GetByteCount(chars3);
                int byteCount4 = _utf8.GetByteCount(chars4);
                int byteCount5 = _utf8.GetByteCount(chars5);
                int byteCount6 = _utf8.GetByteCount(chars6);
                var length = byteCount1 + byteCount2 + byteCount3 + byteCount4 + byteCount5 + byteCount6;
                var scratchBuffer = new byte[length].AsSpan();
                var currentFetch = byteCount1;
                _utf8.GetBytes(chars1, scratchBuffer.Slice(0, byteCount1));
                _utf8.GetBytes(chars2, scratchBuffer.Slice(currentFetch, byteCount2));
                currentFetch += byteCount2;
                _utf8.GetBytes(chars3, scratchBuffer.Slice(currentFetch, byteCount3));
                currentFetch += byteCount3;
                _utf8.GetBytes(chars4, scratchBuffer.Slice(currentFetch, byteCount4));
                currentFetch += byteCount4;
                _utf8.GetBytes(chars5, scratchBuffer.Slice(currentFetch, byteCount5));
                currentFetch += byteCount5;
                _utf8.GetBytes(chars6, scratchBuffer.Slice(currentFetch, byteCount6));
                currentFetch += byteCount6;

                if (scratchBuffer[scratchBuffer.Length - 1] != '1')
                {
                    throw new Exception("LaJi");
                }
            }

        }
    }
}
