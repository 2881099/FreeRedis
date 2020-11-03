using System;

namespace ConcurrentWriteBytes.Model
{
    public class Solution2
    {

        public int Total;
        public readonly byte[] Buffer;
        public Solution2()
        {
            Buffer = new byte[4096];
        }
        public void Write(byte[] data)
        {

            lock (Buffer)
            {

                Array.Copy(data, 0, Buffer, Total, data.Length);
                Total += data.Length;

            }
            
        }

        public void Clear()
        {
            Total = 0;
        }

    }

}
