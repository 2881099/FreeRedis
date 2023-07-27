using System.Runtime.CompilerServices;

internal enum BufferBucketsState
{
    Writing = 1,
    WriteCompleted = 2,
    Reading = 4,
    ReadCompleted = 8,
}

internal sealed class BufferBuckets<T>
{

    internal readonly T[] Buckets;
    internal BufferBuckets<T> Next = default!;
    internal BufferBucketsState State;

    public BufferBuckets(int length)
    {
        Buckets = new T[length];
    }


    public BufferBuckets<T> AppendNew(int length)
    {

        //|----- First ------- Next(node2) -----|
        var temp = new BufferBuckets<T>(length);

        //|----- Temp ----- node2 -----|
        temp.Next = Next;

        //|----- First ------- Next(temp) ------- node2 -----|
        Next = temp;
        return temp;
    }
}

public sealed class CircleBuffer<T>
{

    public readonly int ArrayLength;
    private BufferBuckets<T> _write_ptr;
    private BufferBuckets<T> _read_ptr;
    private T[] _write_buffer;
    private T[] _read_buffer;
    public CircleBuffer(int bucketsCount, int bucketsBufferLength)
    {
        ArrayLength = bucketsBufferLength;
        var first = new BufferBuckets<T>(ArrayLength);
        first.Next = first;
        var head = first;
        for (int i = 0; i < bucketsCount; i++)
        {
            //添加两个环节点
            head = head.AppendNew(ArrayLength);
            //Parallel.For(0, ArrayLength, (index) => { buffer[index] = new TaskCompletionSource<T>(); });
        }
        first.State = BufferBucketsState.Reading | BufferBucketsState.Writing;
        _write_ptr = first;
        _read_ptr = first;
        _write_buffer = first.Buckets;
        _read_buffer = first.Buckets;
        //Parallel.For(0, ArrayLength, (index) => { _currentWrite[index] = new TaskCompletionSource<T>(); });
        //System.Console.WriteLine($"总环数{SingleLinks7<T>.Increment}！");
    }

    private long _write_offset;
    private long _read_offset;
    private long _count1;
    private long _count2;
    private long _count3;

    public void Show()
    {
        Console.WriteLine(_count1);
        Console.WriteLine(_count2);
        Console.WriteLine(_count3);
    }
    public void ConcurrentEnqueue(T value)
    {
        var index = Interlocked.Add(ref _write_offset, 1);
        if (index < ArrayLength)
        {
#if DEBUG
            Interlocked.Increment(ref _count1);
#endif

            _write_buffer[index - 1] = value;
        }
        else if (index == ArrayLength)
        {
#if DEBUG
            Interlocked.Increment(ref _count2);
#endif

            _write_buffer[index - 1] = value;
            _write_ptr.State |= BufferBucketsState.WriteCompleted;
            var state = _write_ptr.Next.State;
            if (
                (state & BufferBucketsState.WriteCompleted) != 0 &&
                (state & BufferBucketsState.ReadCompleted) == 0
               )
            {
                _write_ptr = _write_ptr.AppendNew(ArrayLength);
            }
            else
            {
                _write_ptr = _write_ptr.Next;
            }
            _write_ptr.State = BufferBucketsState.Writing;
            _write_buffer = _write_ptr.Buckets;
            _write_offset = 0;

        }
        else
        {
#if DEBUG
            Interlocked.Increment(ref _count3);
#endif
            SpinWait wait = default;
            while (_write_offset > ArrayLength)
            {
                wait.SpinOnce();
            }
            ConcurrentEnqueue(value);
        }
    }
    public T ConcurrentDequeue()
    {

        var index = Interlocked.Add(ref _read_offset,1);
        if (index < ArrayLength)
        {
            return _read_buffer[index - 1];
        }
        else if (index == ArrayLength)
        {
            T result = _read_buffer[index - 1];
            _read_ptr.State |= BufferBucketsState.ReadCompleted;
            SpinWait wait = default;
            while ((_read_ptr.Next.State & BufferBucketsState.ReadCompleted) != 0)
            {
                wait.SpinOnce();
            }
            _read_ptr = _read_ptr.Next;
            _read_ptr.State |= BufferBucketsState.Reading;
            _read_buffer = _read_ptr.Buckets;
            _read_offset = 0;
            return result;
        }
        else
        {
            SpinWait wait = default;
            while (_read_offset > ArrayLength)
            {
                wait.SpinOnce();
            }
            return ConcurrentDequeue();
        }
    }
    //进队
    public void Enqueue(T value)
    {

        _write_buffer[_write_offset] = value;
        _write_offset += 1;
        if (_write_offset == ArrayLength)
        {
            _write_ptr.State |= BufferBucketsState.WriteCompleted;
            var state = _write_ptr.Next.State;
            if (
                (state & BufferBucketsState.WriteCompleted) != 0 &&
                (state & BufferBucketsState.ReadCompleted) == 0
               )
            {
                _write_ptr = _write_ptr.AppendNew(ArrayLength);
            }
            else
            {
                _write_ptr = _write_ptr.Next;
            }
            _write_ptr.State = BufferBucketsState.Writing;
            _write_buffer = _write_ptr.Buckets;
            _write_offset = 0;
        }
    }
    //出队
    public T Dequeue()
    {
        T result = _read_buffer[_read_offset];
        _read_offset += 1;
        if (_read_offset == ArrayLength)
        {
            _read_ptr.State |= BufferBucketsState.ReadCompleted;
            SpinWait wait = default;
            while ((_read_ptr.Next.State & BufferBucketsState.ReadCompleted) != 0)
            {
                wait.SpinOnce();
            }
            _read_ptr = _read_ptr.Next;
            _read_ptr.State |= BufferBucketsState.Reading;
            _read_buffer = _read_ptr.Buckets;
            _read_offset = 0;
        }
        return result;
    }

    private long _extend_lock;




}

