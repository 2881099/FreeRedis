using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;

namespace IntrinsicsTest
{
    [MemoryDiagnoser, CoreJob, MarkdownExporter, RPlotExporter]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class ForTest
    {
        public const long Increment = 36;
        public void Intrinsics()
        {
            //Interlocked.Exchange(,)

            //   var result = new Vector<long>(0);
            //var offset = new Vector<long>(0);
            //var max = new Vector<long>(1000);
            //long temp = offset.CopyTo(temp);
            //while ()
            //{

            //}
            //for (int i = 0; i < 1000; i++)
            //{
            //    result += Increment + i;
            //}
        }

        public void Normal()
        {
            //long result = 0;
            //for (int i = 0; i < 1000; i++)
            //{
            //    result += Increment + i;
            //}
        }
    }
}
