using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ConcurrentWriteBytes.Model
{
    public class Solution1
    {

        private readonly List<TempData> _buffers;
        public readonly byte[] Buffer;
        public int Total;
        public Solution1()
        {
            Buffer = new byte[4096];
            _buffers = new List<TempData>();
        }
        public void Write(byte[] data)
        {

            lock (_buffers)
            {

                _buffers.Add(new TempData(data, Total));
                Total += data.Length;

            }
            
        }

        public byte[] GetBuffers()
        {

            Parallel.For(0, _buffers.Count - 1, (item, state) => {
                var node = _buffers[item];
                Array.Copy(node.Data, 0, Buffer, node.Offset, node.Data.Length);
            });
            return Buffer;

        }

        public void Clear()
        {
            _buffers.Clear();
            Total = 0;
        }
    }

    public readonly struct TempData
    {
        public TempData(byte[] data,int offset)
        {
            Data = data;
            Offset = offset;
        }
        public readonly byte[] Data;
        public readonly int Offset;
    }

}
