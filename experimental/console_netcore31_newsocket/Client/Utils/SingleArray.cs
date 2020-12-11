using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace console_netcore31_newsocket.Client.Utils
{
    public class SingleArray<T>
    {

        private T[] _values;
        public SingleArray(int length)
        {
            Memory<T> A =default;
            A.Span[0] = default;
            _length = length;
            _values = new T[length];

        }


        private int _length;
        public int Length;
        public int Count;
        public void Add(T value) 
        {
            _values[Count] = value;
            Count += 1;
            Length += 1;
            if (Count == Length)
            {
                AddNewArray();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddNewArray()
        {
            Count = 0;
            var temp = new T[_length + 500];
            Array.Copy(_values, temp, Length);
            _values = temp;
        }

        public void Add(SingleArray<T> array)
        {

        }

    }
}
