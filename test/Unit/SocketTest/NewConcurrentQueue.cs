using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;


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
    private readonly object _lockInstance = new object();
    private object _lock;
    public readonly SpinWait _spin;
    public void Enqueue(T value)
    {
        while (Interlocked.CompareExchange(ref _lock, _lockInstance, null) != null)
        {
            _spin.SpinOnce();
        }

        Count += 1;
        if (Count == 1)
        {
            _head = new Element(value);
            _tail = _head;
        }
        else
        {
            _tail.Next = new Element(value);
            _tail = _tail.Next;
        }
        _lock = null;
        return;

    }

    public T Dequeue()
    {
        while (Interlocked.CompareExchange(ref _lock, _lockInstance, null) != null)
        {
            _spin.SpinOnce();
        }


        if (Count > 0)
        {
            Count -= 1;
            var temp = _head;
            _head = _head.Next;
            temp.Next = null;
            _lock = null;
            return temp.Value;

        }
        else
        {
            _lock = null;
            return default;
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
