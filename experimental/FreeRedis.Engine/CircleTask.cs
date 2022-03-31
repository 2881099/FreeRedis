using System.Buffers;
using System.IO.Pipelines;

public sealed class TaskBuckets
{
    //public static int Increment;
    //public int Index;
    public bool InReading;
    public readonly IRedisProtocol[] Buffer;
    public TaskBuckets(int length)
    {
        Buffer = new IRedisProtocol[length];
        //Index = Interlocked.Increment(ref Increment);
    }

    public TaskBuckets Next = default!;

    public TaskBuckets AppendNew(int length)
    {
        //|----- First ------- Next(node2) -----|
        var temp = new TaskBuckets(length);

        //|----- Temp ----- node2 -----|
        temp.Next = Next;

        //|----- First ------- Next(temp) ------- node2 -----|
        Next = temp;
        return temp;
    }
}

public delegate void HandlerBufferDelegate(in ReadOnlySpan<byte> spans);

public sealed class CircleTask
{

#if DEBUG
    //public void Clear()
    //{
    //    _debug.Clear();
    //}

    //private readonly DebugBuffer _debug;
#endif

    public const int ArrayLength = 8192;
    private TaskBuckets _writePtr;
    public  TaskBuckets _readPtr;
    private IRedisProtocol[] _currentWrite;
    private IRedisProtocol[] _currentRead;
    public CircleTask()
    {
        //_writer = writer;
#if DEBUG
        //_debug = new();
#endif
        var first = new TaskBuckets(ArrayLength);
        first.InReading = true;
        first.Next = first;
        var head = first;
        for (int i = 0; i < 2; i++)
        {
            head = head.AppendNew(ArrayLength);
            //Parallel.For(0, ArrayLength, (index) => { buffer[index] = new TaskCompletionSource<T>(); });
        }

        _writePtr = first;
        _readPtr = first;
        _currentWrite = first.Buffer;
        _currentRead = first.Buffer;
        //Parallel.For(0, ArrayLength, (index) => { _currentWrite[index] = new TaskCompletionSource<T>(); });
        //System.Console.WriteLine($"总环数{SingleLinks7<T>.Increment}！");
    }

    private int _write_offset;
    private int _read_offset;
    public void WriteNext(IRedisProtocol protocal)
    {
#if DEBUG
        //_debug.RecodSender();
#endif
        _currentWrite[_write_offset] = protocal;
        //result.Reset();
        _write_offset += 1;
        if (_write_offset == ArrayLength)
        {
            AddBuffer();
            _write_offset = 0;
        }
    }

    private volatile int _lock;
    private void AddBuffer()
    {

        SpinWait wait = default;
        while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0)
        {
#if DEBUG
            //_debug.RecodLock();
#endif
            wait.SpinOnce();
        }
        //System.Console.Write($"环{_writePtr.Index}已满！");
        //| ------ writting & reading --------- |
        //| ------ wrtting --- new cicle --- reading --------- |
        if (_writePtr.Next.InReading)
        {
#if DEBUG
            //_debug.RecodContact(true);
#endif
            _writePtr = _writePtr.AppendNew(ArrayLength);
            _currentWrite = _writePtr.Buffer;
            _lock = 0;
            //System.Console.WriteLine($"开辟新环，总环数{SingleLinks7<T>.Increment}！");
            //Parallel.For(0, ArrayLength, (index) => { _currentWrite[index] = new TaskCompletionSource<T>(); });

        }
        else
        {
#if DEBUG
           // _debug.RecodContact(false);
#endif
            // ------- writting1 ---- writting2  ---- writting & writteing3 -------- |
            _writePtr = _writePtr.Next;
            _currentWrite = _writePtr.Buffer;
            //System.Console.WriteLine($"移动到环{_writePtr.Index}");
            _lock = 0;

        }


    }

    //private readonly PipeReader _protocolReader = new Pipe(null);
    //private readonly PipeWriter _protocolWriter = new Pipe(null);
    public void LoopHandle(ref SequenceReader<byte> reader)
    {
        var result = _currentRead[_read_offset].HandleBytes(ref reader);
        switch (result)
        {
            case ProtocolContinueResult.Completed:
                _read_offset += 1;
                if (_read_offset == ArrayLength)
                {
                    CollectBuffer();
                    _read_offset = 0;
                }
                if (!reader.End)
                {
                    LoopHandle(ref reader);
                }
                return;
            case ProtocolContinueResult.Wait:
                return;
            case ProtocolContinueResult.Continue:
                if (!reader.End)
                {
                    LoopHandle(ref reader);
                }
                return;
            default:
                return;
        }
    }

    private void CollectBuffer()
    {

        SpinWait wait = default;
        while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0)
        {
#if DEBUG
            //_debug.RecodLock();
#endif
            wait.SpinOnce();
        }
        //System.Console.Write($"环{_readPtr.Index}已处理完！");
        _readPtr.InReading = false;
        _readPtr = _readPtr.Next;
        //System.Console.WriteLine($"移动到环{_readPtr.Index}");
        _readPtr.InReading = true;
        _currentRead = _readPtr.Buffer;
        _lock = 0;


    }
}

/*
#if DEBUG
public class DebugBuffer
{


    public void Clear()
    {
        ShowInfo();
        _senderCount = 0;
        _receiverCount = 0;
        _sendOverflowCount = 0;
        _hasCompletedCount = 0;
        _contactCount.Clear();
    }

    public static int TimeInteval = 3000;

    public DebugBuffer()
    {
        _contactCount = new List<(int oldLength, bool isNew)>();
        Task.Run(() =>
        {

            while (true)
            {
                Thread.Sleep(TimeInteval);
                ShowInfo();
            }

        });
    }


    private int _senderCount;
    public void RecodSender()
    {
        Interlocked.Increment(ref _senderCount);
    }


    private int _receiverCount;
    public void RecodReceiver()
    {
        Interlocked.Increment(ref _receiverCount);
    }


    private int _sendOverflowCount;
    public void RecodSendOverflow()
    {
        Interlocked.Increment(ref _sendOverflowCount);
    }


    private List<(int oldLength, bool isNew)> _contactCount;
    public void RecodContact(bool isNew = false)
    {
        _contactCount.Add((_senderCount, isNew));
    }


    private int _lockCount;
    public void RecodLock()
    {
        Interlocked.Increment(ref _lockCount);
    }


    private int _hasCompletedCount;

    public void AcceptTask(ValueTask<T> task)
    {
        if (task.IsCompleted)
        {
            Interlocked.Increment(ref _hasCompletedCount);
        }
    }

    public void AcceptTask(Task<T> task)
    {
        if (task.IsCompleted)
        {
            Interlocked.Increment(ref _hasCompletedCount);
        }
    }



    public void ShowInfo()
    {

        System.Console.WriteLine("-------------------------------------------------------");
        System.Console.WriteLine($"缓冲区经历了{_contactCount.Count}次移动！");
        for (int i = 0; i < _contactCount.Count; i++)
        {
            var item = _contactCount[i];
            System.Console.Write($"第{i}次移动时，发送任务{item.oldLength}个！");
            if (item.isNew)
            {
                System.Console.WriteLine($"本次进行了扩容处理！");
            }
            System.Console.WriteLine();
        }

        System.Console.WriteLine(@$"
发送任务 次数: {_senderCount}
分配结果 次数: {_receiverCount}
提前任务 异常次数:{_hasCompletedCount}
扩 容 锁 争用次数:{_lockCount}
");

        System.Console.WriteLine("-------------------------------------------------------");
        System.Console.ReadKey();
    }


}
#endif
*/
