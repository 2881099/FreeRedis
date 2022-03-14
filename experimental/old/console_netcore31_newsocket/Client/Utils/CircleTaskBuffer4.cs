using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

public class SingleLinks6<T>
{
    public static int Increment;
    public int Index;
    public bool InReading;
    public readonly ManualResetValueTaskSource<T>[] Buffer;
    public SingleLinks6(int length)
    {
        Buffer = new ManualResetValueTaskSource<T>[length];
        Index = Interlocked.Increment(ref Increment);
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

    //private readonly DebugBuffer<T> _debug;
    public int ArrayLength = 8192;
    private SingleLinks6<T> _writePtr;
    public SingleLinks6<T> _readPtr;
    private ManualResetValueTaskSource<T>[] _currentWrite;
    private ManualResetValueTaskSource<T>[] _currentRead;
    private readonly PipeWriter _writer;

    public void Clear()
    {
        //_debug.Clear();
    }
    public CircleTaskBuffer4()
    {
        //_writer = writer;
        //_debug = new();
        var first = new SingleLinks6<T>(ArrayLength);
        first.InReading = true;
        first.Next = first;
        var head = first;
        for (int i = 0; i < 2; i++)
        {
            head = head.AppendNew(ArrayLength);
            var buffer = head.Buffer;
            Parallel.For(0, ArrayLength, (index) => { buffer[index] = new ManualResetValueTaskSource<T>(); });
        }

        _writePtr = first;
        _readPtr = first;
        _currentWrite = first.Buffer;
        _currentRead = first.Buffer;
        Parallel.For(0, ArrayLength, (index) => { _currentWrite[index] = new ManualResetValueTaskSource<T>(); });
        System.Console.WriteLine($"总环数{SingleLinks6<T>.Increment}！");
    }

    private int _write_offset;
    private int _read_offset;
    public ManualResetValueTaskSource<T> WriteNext()
    {
        //_debug.RecodSender();
        var result = _currentWrite[_write_offset];
        result.Reset();
        _write_offset += 1;
        if (_write_offset == ArrayLength)
        {
            AddBuffer();
        }
        return result;
    }

    private int _lock;
    private void AddBuffer()
    {

        SpinWait wait = default;
        while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0)
        {
            //_debug.RecodLock();
            wait.SpinOnce();
        }
        System.Console.Write($"环{_writePtr.Index}已满！");
        if (_writePtr.Next.InReading)
        {
            //_debug.RecodContact(true);
            _writePtr = _writePtr.AppendNew(ArrayLength);
            _lock = 0;
            _currentWrite = _writePtr.Buffer;
            System.Console.WriteLine($"开辟新环，总环数{SingleLinks6<T>.Increment}！");
            Parallel.For(0, ArrayLength, (index) => { _currentWrite[index] = new ManualResetValueTaskSource<T>(); });
        }
        else
        {
            //_debug.RecodContact(false);
            _writePtr = _writePtr.Next;
            System.Console.WriteLine($"移动到环{_writePtr.Index}");
            _lock = 0;
            _currentWrite = _writePtr.Buffer;
        }
        _write_offset = 0;

    }


    public void ReadNext(T value)
    {
       
        var result = _currentRead[_read_offset];
        //_debug.RecodReceiver();
        //_debug.AcceptTask(result.AwaitableTask);
        if (result.AwaitableTask.IsCompleted)
        {
            //System.Console.WriteLine("Need False!");
            //result.SetResult((T)(object)false);
        }
        else
        {
            result.SetResult(value);
        }
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
            //_debug.RecodLock();
            wait.SpinOnce();
        }
        System.Console.Write($"环{_readPtr.Index}已处理完！");
        _readPtr.InReading = false;
        _readPtr = _readPtr.Next;
        System.Console.WriteLine($"移动到环{_readPtr.Index}");
        _readPtr.InReading = true;
        _lock = 0;
        _currentRead = _readPtr.Buffer;
        _read_offset = 0;
    }
}
