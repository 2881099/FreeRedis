using System.Buffers;

public delegate Task IProtocolTaskDelegate(byte[] reader);

public sealed class CircleBuffer2
{

    public readonly int ArrayLength;
    private BufferBuckets _write_ptr;
    private BufferBuckets _read_ptr;
    private IProtocolTaskDelegate[] _write_buffer;
    private IProtocolTaskDelegate[] _read_buffer;
    public CircleBuffer2(int bucketsCount, int bucketsBufferLength)
    {
        ArrayLength = bucketsBufferLength;
        var first = new BufferBuckets(ArrayLength);
        first.Next = first;
        var head = first;
        for (int i = 0; i < bucketsCount; i++)
        {
            //添加两个环节点
            head = head.AppendNew(ArrayLength);
            //Parallel.For(0, ArrayLength, (index) => { buffer[index] = new IProtocolTaskDelegateaskCompletionSource(); });
        }
        first.State = BufferBucketsState.Reading | BufferBucketsState.Writing;
        _write_ptr = first;
        _read_ptr = first;
        _write_buffer = first.Buckets;
        _read_buffer = first.Buckets;
        //Parallel.For(0, ArrayLength, (index) => { _currentWrite[index] = new IProtocolTaskDelegateaskCompletionSource(); });
        //System.Console.WriteLine($"总环数{SingleLinks7.Increment}！");
    }

    private long _write_offset;
    private long _read_offset;
    public async Task HandlerCurrentTask(byte[] bytes)
    {
        var task = CurrentValue;
        MoveNext();
        await task(bytes);
    }
    public void ConcurrentEnqueue(IProtocolTaskDelegate value)
    {
        var index = Interlocked.Add(ref _write_offset, 1);
        if (index < ArrayLength)
        {

            _write_buffer[index - 1] = value;
        }
        else if (index == ArrayLength)
        {

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
            SpinWait wait = default;
            while (_write_offset > ArrayLength)
            {
                wait.SpinOnce();
            }
            ConcurrentEnqueue(value);
        }
    }

    public IProtocolTaskDelegate ConcurrentDequeue()
    {

        var index = Interlocked.Add(ref _read_offset,1);
        if (index < ArrayLength)
        {
            return _read_buffer[index - 1];
        }
        else if (index == ArrayLength)
        {
            IProtocolTaskDelegate result = _read_buffer[index - 1];
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
    public void Enqueue(IProtocolTaskDelegate value)
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

    public IProtocolTaskDelegate CurrentValue { get { return _read_buffer[_read_offset];  } }
    //出队
    public void MoveNext()
    {
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
    }
    public IProtocolTaskDelegate Dequeue()
    {
        IProtocolTaskDelegate result = _read_buffer[_read_offset];
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

    private sealed class BufferBuckets
    {

        internal readonly IProtocolTaskDelegate[] Buckets;
        internal BufferBuckets Next = default!;
        internal BufferBucketsState State;

        public BufferBuckets(int length)
        {
            Buckets = new IProtocolTaskDelegate[length];
        }


        public BufferBuckets AppendNew(int length)
        {

            //|----- First ------- Next(node2) -----|
            var IProtocolTaskDelegateemp = new BufferBuckets(length);

            //|----- IProtocolTaskDelegateemp ----- node2 -----|
            IProtocolTaskDelegateemp.Next = Next;

            //|----- First ------- Next(temp) ------- node2 -----|
            Next = IProtocolTaskDelegateemp;
            return IProtocolTaskDelegateemp;
        }
    }

}

