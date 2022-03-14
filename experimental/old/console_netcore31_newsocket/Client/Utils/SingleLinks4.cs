using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{
    public class SingleLinks4<T>
    {
        public readonly SingleLinkNode4<T> Head;
        public SingleLinkNode4<T> Tail;
        public SingleLinks4()
        {
            Head = new SingleLinkNode4<T>(default);
            Tail = Head;
        }
        //public int Count;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Append(Task<T> value)
        {
            //Count += 1;
            Tail.Next = new SingleLinkNode4<T>(value);
            Tail = Tail.Next;
        }



        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void ClearBefore(SingleLinkNode4<T> node)
        {

            Head.Next = node;
            if (node == null)
            {

                Tail = Head;

            }


        }

    }

    public class SingleLinkNode4<T>
    {

        private readonly static Func<Task<T>, T, T> _setResult;
        static SingleLinkNode4()
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
        public SingleLinkNode4<T> Next;
        public SingleLinkNode4(Task<T> value)
        {
            Value = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public SingleLinkNode4<T> Completed()
        {
            _setResult(Value, Result);
            return Next;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public SingleLinkNode4<T> Completed(T result)
        {
            _setResult(Value, result);
            return Next;
        }

    }
}
