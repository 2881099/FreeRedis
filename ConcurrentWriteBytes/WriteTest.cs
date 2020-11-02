using BenchmarkDotNet.Attributes;
using ConcurrentWriteBytes.Model;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConcurrentWriteBytes
{

    [MemoryDiagnoser, CoreJob, MarkdownExporter, RPlotExporter]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    
    [MaxWarmupCount(8)]
    [IterationCount(20)]
    [MaxIterationCount(40)]
    public class WriteTest
    {
        private readonly byte[] _buffer;
        private readonly Solution1 _solution1;
        private readonly Solution2 _solution2;
        private readonly MemoryStream _memory;
        private readonly BufferedStream _buffer_memory;
        public WriteTest()
        {

            _memory = new MemoryStream(4096);
            _buffer_memory = new BufferedStream(new MemoryStream(4096));
            _solution1 = new Solution1();
            _solution2 = new Solution2();
            _buffer = Encoding.UTF8.GetBytes(@"24242424224242424242423sfdv4242424224242424242423sfd_ + ER$#$#$#$242424242
sf + _ + _#$@#4$+_+_+_+_+ER$#$#$#$24242424242423sfdsf+_+_#$@#4$+_+_+_+_+ER$#$#$#$_+ER$#$#$#$24242424242423sfdsf+_+_#$@#4$+_+_+_+_+E
_ + ER$#$#$#$24242424242423sfdsf+_+_#$@#4$+_+_+_+_+E_+ER$#$#$#$24242424242423sfdsf+_+_#$@#4$+_+_+_+_+E_+ER$#$+E
24242424242423sfdsf + _ + _#$@#4$+_+_+_+_+ER$#$#$#$24242424242423sfdsf+_+_#$@#4$+_+_+_+_+ER$#$#$#$242424242424$#$#$#$42423sfdsf+_+_#$@#4$+_+_+_+_+ER$#$#$#$24242424242423sfdsf+_+_#$@#4$+_+_+_+_+ER$#$#$#$");
        }


        
        [Benchmark(Description = "Solution1")]
        public void Test1()
        {
            _solution1.Clear();
            Parallel.For(0, 7, item => { _solution1.Write(_buffer); });
            var result = _solution1.GetBuffers();

        }

        [Benchmark(Description = "Solution2")]
        public void Test2()
        {

            _solution2.Clear();
            Parallel.For(0, 7, item => { _solution2.Write(_buffer); });

        }


        [Benchmark(Description = "BufferedStream")]
        public void Test3()
        {

            _buffer_memory.Position = 0;
            Parallel.For(0, 7, item => { _buffer_memory.Write(_buffer); });

        }


        [Benchmark(Description = "MemoryStream")]
        public void Test4()
        {

            _memory.Position = 0;
            Parallel.For(0, 7, item => { _memory.Write(_buffer); });

        }


    }

}
