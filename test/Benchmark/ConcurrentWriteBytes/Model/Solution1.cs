using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ConcurrentWriteBytes.Model
{
    public class Solution1
    {

        private readonly List<(byte[],int)> _buffers;
        public readonly byte[] Buffer;
        public int Total;
        public Solution1()
        {
            Buffer = new byte[4096];
            _buffers = new List<(byte[], int)>();
        }
        public void Write(byte[] data)
        {

            lock (_buffers)
            {

                _buffers.Add((data, Total));
                Total += data.Length;

            }
            
        }

        public byte[] GetBuffers()
        {

            Parallel.For(0, _buffers.Count - 1, (item, state) => {
                var node = _buffers[item];
                Array.Copy(node.Item1, 0, Buffer, node.Item2, node.Item1.Length);
            });
            return Buffer;

        }

        public void Clear()
        {
            _buffers.Clear();
            Total = 0;
        }
    }

}
