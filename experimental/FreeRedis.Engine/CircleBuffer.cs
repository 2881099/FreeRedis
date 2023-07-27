using System.Buffers;

internal enum BufferBucketsState
{
    Writing = 1,
    WriteCompleted = 2,
    Reading = 4,
    ReadCompleted = 8,
}

public delegate ProtocolContinueResult IProtocolReaderDelegate(ref SequenceReader<byte> reader);

public sealed class CircleBuffer
{

    public readonly int ArrayLength;
    private BufferBuckets _write_ptr;
    private BufferBuckets _read_ptr;
    private IProtocolReaderDelegate[] _write_buffer;
    private IProtocolReaderDelegate[] _read_buffer;
    public CircleBuffer(int bucketsCount, int bucketsBufferLength)
    {
        ArrayLength = bucketsBufferLength;
        var first = new BufferBuckets(ArrayLength);
        first.Next = first;
        var head = first;
        for (int i = 0; i < bucketsCount; i++)
        {
            //添加两个环节点
            head = head.AppendNew(ArrayLength);
            //Parallel.For(0, ArrayLength, (index) => { buffer[index] = new IProtocolReaderDelegateaskCompletionSource(); });
        }
        first.State = BufferBucketsState.Reading | BufferBucketsState.Writing;
        _write_ptr = first;
        _read_ptr = first;
        _write_buffer = first.Buckets;
        _read_buffer = first.Buckets;
        //Parallel.For(0, ArrayLength, (index) => { _currentWrite[index] = new IProtocolReaderDelegateaskCompletionSource(); });
        //System.Console.WriteLine($"总环数{SingleLinks7.Increment}！");
    }

    private long _write_offset;
    private long _read_offset;

    public void ConcurrentEnqueue(IProtocolReaderDelegate value)
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

    public IProtocolReaderDelegate ConcurrentDequeue()
    {

        var index = Interlocked.Add(ref _read_offset,1);
        if (index < ArrayLength)
        {
            return _read_buffer[index - 1];
        }
        else if (index == ArrayLength)
        {
            IProtocolReaderDelegate result = _read_buffer[index - 1];
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
    public void Enqueue(IProtocolReaderDelegate value)
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

    public IProtocolReaderDelegate CurrentValue { get { return _read_buffer[_read_offset];  } }
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
    public IProtocolReaderDelegate Dequeue()
    {
        IProtocolReaderDelegate result = _read_buffer[_read_offset];
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

        internal readonly IProtocolReaderDelegate[] Buckets;
        internal BufferBuckets Next = default!;
        internal BufferBucketsState State;

        public BufferBuckets(int length)
        {
            Buckets = new IProtocolReaderDelegate[length];
        }


        public BufferBuckets AppendNew(int length)
        {

            //|----- First ------- Next(node2) -----|
            var IProtocolReaderDelegateemp = new BufferBuckets(length);

            //|----- IProtocolReaderDelegateemp ----- node2 -----|
            IProtocolReaderDelegateemp.Next = Next;

            //|----- First ------- Next(temp) ------- node2 -----|
            Next = IProtocolReaderDelegateemp;
            return IProtocolReaderDelegateemp;
        }
    }
}

