using System.Threading;
using System.Threading.Tasks;

    public class SingleLinks5<T>
    {
        public bool InReading;
        public readonly T[] Buffer;
        public SingleLinks5(int length)
        {
            Buffer = new T[length];
        }

        public SingleLinks5<T> Next;

        public SingleLinks5<T> AppendNew(int length)
        {

            var temp = new SingleLinks5<T>(length);
            temp.Next = Next;
            Next = temp;
            return temp;

        }
    }

public class CircleTaskBuffer<T> where T : new()
{

    public int ArrayLength = 8192;
    private SingleLinks5<T> _writePtr;
    private SingleLinks5<T> _readPtr;
    private T[] _currentWrite;
    private T[] _currentRead;

    public CircleTaskBuffer()
    {
        var first = new SingleLinks5<T>(ArrayLength);
        first.InReading = true;
        first.Next = first;
        _writePtr = first;
        _readPtr = first;
        _currentWrite = first.Buffer;
        _currentRead = first.Buffer;
        Parallel.For(0, ArrayLength, (index) => { _currentWrite[index] = new T(); });
    }

    private int _write_offset;
    private int _read_offset;
    public T WriteNext()
    {
        SpinWait wait = default;
        while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0)
        {
            wait.SpinOnce();
        }
        var result = _currentWrite[_write_offset];
        _write_offset += 1;
        if (_write_offset == ArrayLength)
        {
            AddBuffer();
        }
        _lock = 0;
        //Volatile.Write(ref _lock, 0);
        return result;

    }

    private int _lock;
    private void AddBuffer()
    {

        //SpinWait wait = default;
        //while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0)
        //{
        //    wait.SpinOnce();
        //}
        if (_writePtr.Next.InReading)
        {
            _writePtr = _writePtr.AppendNew(ArrayLength);
            //_lock = 0;
            _currentWrite = _writePtr.Buffer;
            Parallel.For(0, ArrayLength, (index) => { _currentWrite[index] = new T(); });
        }
        else
        {
            _writePtr = _writePtr.Next;
            _currentWrite = _writePtr.Buffer;
            //_lock = 0;
        }
        _write_offset = 0;

    }


    public void ReadNext(T value)
    {
        SpinWait wait = default;
        while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0)
        {
            wait.SpinOnce();
        }
        var result = _currentRead[_read_offset];
        _read_offset += 1;
        if (_read_offset == ArrayLength)
        {
            CollectBuffer();
        }
        _lock = 0;
        //Volatile.Write(ref _lock, 0);
    }

    private void CollectBuffer()
    {

        //SpinWait wait = default;
        //while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0)
        //{
        //    wait.SpinOnce();
        //}
        _readPtr.InReading = false;
        _readPtr = _readPtr.Next;
        _readPtr.InReading = true;
        //_lock = 0;
        _read_offset = 0;
        _currentRead = _readPtr.Buffer;

    }
}
