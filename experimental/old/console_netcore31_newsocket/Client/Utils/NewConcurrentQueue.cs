using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading;

namespace console_netcore31_newsocket.Client.Utils
{
    public class NewConcurrentQueue<T> : IEnumerable<T>
    {
        private volatile Element _head;
        private volatile Element _tail;
        private readonly PipeWriter _sender;
        public NewConcurrentQueue(PipeWriter sender)
        {
            _sender = sender;
        }
        public volatile int Count;
        public async void Enqueue(T value,byte[] buffer)
        {
            SpinWait spin = default;
            var count = Count;
            while (true)
            {
                if (count == Interlocked.Increment(ref Count))
                {

                    //add T
                    if (count == 0)
                    {
                        _head = new Element(value);
                        _tail = _head;
                    }
                    else
                    {
                        _tail.Next = new Element(value);
                        _tail = _tail.Next;
                    }
                    //await _sender.WriteAsync(buffer);
                    return;
                }
                count = Count;
                spin.SpinOnce();
            }
        }
        public T Dequeue()
        {

            SpinWait spin = default;
            var count = Count;
            while (true)
            {
                if (count == Interlocked.Decrement(ref Count))
                {
                    //Out T
                    var temp = _head;
                    _head = _head.Next;
                    temp.Next = null;
                    return temp.Value;
                }
                count = Count;
                spin.SpinOnce();
            }

        }

        public IEnumerator<T> GetEnumerator()
        {
            yield return Dequeue();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return Dequeue();
        }

        internal class Element
        {
            public readonly T Value;
            public bool IsCompleted;
            public Element Next;
            public Element(T value)
            {
                Value = value;
            }
            public void Add(Element next)
            {
                Next = next;
            }
        }
        
    }
}
