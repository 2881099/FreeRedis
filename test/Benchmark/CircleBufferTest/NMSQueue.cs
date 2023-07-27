public sealed class NMSBuckets<T>
{

    //public bool InReading;
    private long _read_offset;
    private long _write_offset;
    private long _capacity;
    private readonly T[] _buckets;

    public bool IsWritingAvailable
    { 
        get 
        {
            return _read_offset == _capacity || (_read_offset == 0 && _write_offset == 0);
        } 
    }
    public bool IsReadingAvailable
    {
        get
        {
            return _write_offset == 0 && _read_offset == 0;
        }
    }
    public long Count { get { return _write_offset; } }
    public long ReadCount { get { return _read_offset; } }
    public T NextValue 
    { 
        get 
        {
            T result = _buckets[_read_offset];
            _read_offset += 1;
            return result;
        }
        set
        {
            _buckets[_write_offset] = value;
            _write_offset += 1;
        }
    }
    

    public NMSBuckets(int length)
    {
        _capacity = length;
        _buckets = new T[length];
    }

    public void Reset()
    {
        _read_offset = _write_offset = 0;
    }

    public NMSBuckets<T> Next = default!;

    public NMSBuckets<T> AppendNew(int length)
    {

        //|----- First ------- Next(node2) -----|
        var temp = new NMSBuckets<T>(length);

        //|----- Temp ----- node2 -----|
        temp.Next = Next;

        //|----- First ------- Next(temp) ------- node2 -----|
        Next = temp;
        return temp;
    }
}

public sealed class NMSQueue<T>
{

    public readonly int ArrayLength;
    private NMSBuckets<T> _writePtr;
    private NMSBuckets<T> _readPtr;
    public NMSQueue(int length)
    {
        ArrayLength = length;
        var first = new NMSBuckets<T>(ArrayLength);
        first.Next = first;
        var head = first;
        for (int i = 0; i < 2; i++)
        {
            //添加两个环节点
            head = head.AppendNew(ArrayLength);
        }

        _writePtr = first;
        _readPtr = first;

    }


    //进队
    public void Enqueue(T value)
    {
        _writePtr.NextValue = value;
        if (_writePtr.Count == ArrayLength)
        {

            if (_writePtr.Next.IsWritingAvailable)
            {
                _writePtr = _writePtr.Next;
                _writePtr.Reset();
            }
            else
            {
                _writePtr = _writePtr.AppendNew(ArrayLength);
            }
        }
    }
    //出队
    public T Dequeue()
    {
        T result = _readPtr.NextValue;
        if (_readPtr.ReadCount == ArrayLength)
        {
            SpinWait wait = default;
            while (!_readPtr.Next.IsReadingAvailable)
            {
                wait.SpinOnce();
            }
            _readPtr = _readPtr.Next;
        }
        return result;
    }

}

