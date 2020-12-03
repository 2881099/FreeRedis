using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{
    public class SingleLinks<T>
    {
        public readonly SingleLinkNode<T> Head;
        public SingleLinkNode<T> Tail;
        private readonly SingleLinkNode<T> _first;
        public SingleLinks()
        {
            _first = new SingleLinkNode<T>(default);
            Head = _first;
            Tail = _first;
        }
        //public int Count;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Append(T value)
        {
            //Count += 1;
            Tail.Next = new SingleLinkNode<T>(value);
            Tail = Tail.Next;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Append(SingleLinks<T> node)
        {
            //Console.WriteLine("In Append!");
            if (node._first.Next != null)
            {

                //Count += node.Count;
                Tail.Next = node._first.Next;
                Tail = node.Tail;
                node._first.Next = null;

            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Clear()
        {
            _first.Next = null;
            Tail = _first;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void ClearBefore(SingleLinkNode<T> node)
        {

            _first.Next = node.Next;
            if (node.Next == null)
            {

                Tail = _first;

            }


        }

    }

    public class SingleLinkNode<T>
    {
        public readonly T Value;
        public SingleLinkNode<T> Next;
        public SingleLinkNode(T value)
        {
            Value = value;
        }

    }
}
