using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq.Expressions;
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


    public int ArrayLength = 1024;
    private SingleLinks6<T> _writePtr;
    private SingleLinks6<T> _readPtr;
    private ManualResetValueTaskSource<T>[] _currentWrite;
    private ManualResetValueTaskSource<T>[] _currentRead;
    private readonly PipeWriter _writer;
    public CircleTaskBuffer4()
    {
        //_writer = writer;
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

        //Task.Run(() =>
        //{
        //    for (int i = 0; i < 50; i++)
        //    {
        //        Thread.Sleep(200);
        //        var head = _readPtr;
        //        var temp = 0;
        //        if (_readPtr.InReading)
        //        {
        //            temp += 1;
        //        }
        //        while (head.Next != _readPtr)
        //        {
        //            head = head.Next;
        //            if (head.InReading)
        //            {
        //                temp += 1;
        //            }
        //        }
        //        if (temp>0)
        //        {
        //            System.Console.WriteLine("共：" + temp);
        //        }
                
        //    }

        //});
    }

    private int _write_offset;
    private int _read_offset;
    public ManualResetValueTaskSource<T> WriteNext()
    {
        DebugBuffer<T>.RecodSender();
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
            DebugBuffer<T>.RecodLock();
            wait.SpinOnce();
        }
        if (_writePtr.Next.InReading)
        {
            DebugBuffer<T>.RecodContact(true);
            _writePtr = _writePtr.AppendNew(ArrayLength);
            _lock = 0;
            _currentWrite = _writePtr.Buffer;
            Parallel.For(0, ArrayLength, (index) => { _currentWrite[index] = new ManualResetValueTaskSource<T>(); });
        }
        else
        {
            DebugBuffer<T>.RecodContact(false);
            _writePtr = _writePtr.Next;
            _lock = 0;
            _currentWrite = _writePtr.Buffer;
        }
        _write_offset = 0;

    }


    public void ReadNext(T value)
    {
        var result = _currentRead[_read_offset];
        DebugBuffer<T>.RecodReceiver();
        DebugBuffer<T>.AcceptTask(result.AwaitableTask);
        if (result.AwaitableTask.IsCompleted)
        {
            System.Console.WriteLine("Need False!");
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
            DebugBuffer<T>.RecodLock();
            wait.SpinOnce();
        }
        _readPtr.InReading = false;
        _readPtr = _readPtr.Next;
        _readPtr.InReading = true;
        _lock = 0;
        _currentRead = _readPtr.Buffer;
        _read_offset = 0;
    }
}

#if DEBUG
public static class DebugBuffer<T> 
{


    public static void Clear()
    {
        ShowInfo();
        _senderCount = 0;
        _receiverCount = 0;
        _sendOverflowCount = 0;
        _hasCompletedCount = 0;
        _contactCount.Clear();
    }

    public static int TimeInteval = 3000;

    static DebugBuffer()
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


    private static int _senderCount;
    public static void RecodSender()
    {
        Interlocked.Increment(ref _senderCount);
    }


    private static int _receiverCount;
    public static void RecodReceiver()
    {
        Interlocked.Increment(ref _receiverCount);
    }


    private static int _sendOverflowCount;
    public static void RecodSendOverflow()
    {
        Interlocked.Increment(ref _sendOverflowCount);
    }


    private static List<(int oldLength, bool isNew)> _contactCount;
    public static void RecodContact(bool isNew = false)
    {
        _contactCount.Add((_senderCount, isNew));
    }


    private static int _lockCount;
    public static void RecodLock()
    {
        Interlocked.Increment(ref _lockCount);
    }


    private static int _hasCompletedCount;

    public static void AcceptTask(ValueTask<T> task)
    {
        if (task.IsCompleted)
        {
            Interlocked.Increment(ref _hasCompletedCount);
        }
    }

    public static void AcceptTask(Task<T> task)
    {
        if (task.IsCompleted)
        {
            Interlocked.Increment(ref _hasCompletedCount);
        }
    }



    public static void ShowInfo()
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

