using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

    public class SingleLinks6<T>
    {
        public bool InReading;
        public readonly ManualResetValueTaskSource<T>[] Buffer;
        public SingleLinks6(int length)
        {
            Buffer = new ManualResetValueTaskSource<T>[length];
        }

        public SingleLinks6<T> Next;

        public SingleLinks6<T> AppendNew(int length)
        {

            var temp = new SingleLinks6<T>(length);
            temp.Next = Next;
            Next = temp;
            return temp;

        }
    }

public class CircleTaskBuffer4<T> where T : new()
{

    private long _send_lock_flag;

    public bool IsCompleted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return _send_lock_flag != 1;

        }
    }
    public int LockCount;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void LockSend()
    {

        SpinWait wait = default;
        while (Interlocked.CompareExchange(ref _send_lock_flag, 1, 0) != 0)
        {
            //Interlocked.Increment(ref LockCount);
            wait.SpinOnce();
        }

    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSendLock()
    {
        return Interlocked.CompareExchange(ref _send_lock_flag, 1, 0) == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReleaseSend()
    {

        _send_lock_flag = 0;

    }

    public int ArrayLength = 8192;
    public int TopLength = 8190;
    private SingleLinks6<T> _writePtr;
    private SingleLinks6<T> _readPtr;
    private ManualResetValueTaskSource<T>[] _currentWrite;
    private ManualResetValueTaskSource<T>[] _currentRead;
    private readonly PipeWriter _writer;
    public CircleTaskBuffer4(PipeWriter writer)
    {
        _writer = writer;
        var first = new SingleLinks6<T>(ArrayLength);
        first.InReading = true;
        first.Next = first;
        _writePtr = first;
        _readPtr = first;
        _currentWrite = first.Buffer;
        _currentRead = first.Buffer;
        Parallel.For(0, ArrayLength, (index) => { _currentWrite[index] = new ManualResetValueTaskSource<T>(); });
    }

    private int _write_offset;
    private int _read_offset;
    public ManualResetValueTaskSource<T> WriteNext(byte[] bytes)
    {
        int newhigh = Interlocked.Increment(ref _write_offset);
        if (newhigh < TopLength)
        {
            LockSend();
            _writer.WriteAsync(bytes);
            var result = _currentWrite[newhigh];
            ReleaseSend();
            return result;

        }
        else if (newhigh == TopLength)
        {

            AddBuffer();
            LockSend();
            _writer.WriteAsync(bytes);
            var result = _currentWrite[newhigh];
            ReleaseSend();
            return result;

        }
        else
        {
            SpinWait wait = default;
            wait.SpinOnce();
            while (_write_offset > ArrayLength)
            {
                wait.SpinOnce();
            }
            return WriteNext(bytes);
        }

    }

    private int _lock;
    private void AddBuffer()
    {

        SpinWait wait = default;
        while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0)
        {
            wait.SpinOnce();
        }
        if (_writePtr.Next.InReading)
        {
            _writePtr = _writePtr.AppendNew(ArrayLength);
            _currentWrite = _writePtr.Buffer;
            _lock = 0;
            Parallel.For(0, ArrayLength, (index) => { _currentWrite[index] = new ManualResetValueTaskSource<T>(); });
        }
        else
        {
            _writePtr = _writePtr.Next;
            _currentWrite = _writePtr.Buffer;
            _lock = 0;
        }
        _write_offset = 0;
    }


    public void ReadNext(T value)
    {
        //int newhigh = Interlocked.Increment(ref _read_offset);
        ////SpinWait wait = default;
        ////while (Interlocked.CompareExchange(ref _read_offset, newhigh + 1, newhigh) != newhigh)
        ////{

        ////    wait.SpinOnce();
        ////    newhigh = _read_offset;
        ////}

        //if (newhigh < TopLength)
        //{
        //    _currentRead[newhigh].SetResult(value);
        //}
        //else if (newhigh == TopLength)
        //{
        //    CollectBuffer();
        //    _currentRead[newhigh].SetResult(value);
        //}
        //else
        //{
        //    SpinWait wait = default;
        //    wait.SpinOnce();
        //    while (_read_offset > ArrayLength)
        //    {
        //        wait.SpinOnce();
        //    }
        //    ReadNext(value);
        //}
        var result = _currentRead[_read_offset];
        result.SetResult(value);
        _read_offset += 1;
        if (_read_offset == ArrayLength)
        {
            CollectBuffer();
        }

    }

    private void CollectBuffer()
    {

        SpinWait wait = default;
        while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0)
        {
            wait.SpinOnce();
        }
        _readPtr.InReading = false;
        _readPtr = _readPtr.Next;
        _readPtr.InReading = true;
        _currentRead = _readPtr.Buffer;
        _read_offset = 0;
        _lock = 0;
        //Interlocked.Exchange(ref _read_offset, 0);
    }
}
