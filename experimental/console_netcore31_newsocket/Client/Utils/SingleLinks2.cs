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
    public class SingleLinks2<T>
    {
        public readonly SingleLinkNode2<T> Head;
        public SingleLinkNode2<T> Tail;
        public SingleLinks2()
        {
            Head = new SingleLinkNode2<T>(default);
            Tail = Head;
        }
        //public int Count;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Append(Task<T> value)
        {
            //Count += 1;
            Tail.Next = new SingleLinkNode2<T>(value);
            Tail = Tail.Next;
        }



        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void ClearBefore(SingleLinkNode2<T> node)
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

    public class SingleLinkNode2<T>
    {

        private readonly static Func<Task<T>, T, T> _setResult;
        static SingleLinkNode2()
        {
            _setResult = typeof(Task<T>)
                .GetMethod("TrySetResult",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(T) }, null)
                .CreateDelegate<Func<Task<T>, T, T>>();
        }
        public readonly Task<T> Value;
        public T Result;
        public SingleLinkNode2<T> Next;
        public SingleLinkNode2(Task<T> value)
        {
            Value = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Completed()
        {
            _setResult(Value, Result);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Completed(T result)
        {
            _setResult(Value, result);
        }

    }
}
