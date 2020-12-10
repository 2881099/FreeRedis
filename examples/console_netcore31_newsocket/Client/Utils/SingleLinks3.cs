using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{
    public class SingleLink3<T>
    {
        public readonly SingleLinkNode3<T> Head;
        public SingleLinkNode3<T> Tail;
        public SingleLink3()
        {
            Head = new SingleLinkNode3<T>(default);
            Tail = Head;
        }
        //public int Count;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Append(TaskCompletionSource<T> value)
        {
            //Count += 1;
            Tail.Next = new SingleLinkNode3<T>(value);
            Tail = Tail.Next;
        }



        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void ClearBefore(SingleLinkNode3<T> node)
        {

            Head.Next = node.Next;
            if (node.Next == null)
            {

                Tail = Head;

            }
            else
            {
                node.Next = null;
            }


        }

    }

    public class SingleLinkNode3<T>
    {

        public readonly TaskCompletionSource<T> Value;
        public T Result;
        public SingleLinkNode3<T> Next;
        public SingleLinkNode3(TaskCompletionSource<T> value)
        {
            Value = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Completed()
        {
            Value.SetResult(Result);
        }

    }
}
