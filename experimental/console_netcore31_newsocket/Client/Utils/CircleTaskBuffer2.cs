using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket.Client.Utils
{
    public class CircleTaskBuffer2<T>
    {
        private int _readLock = 0;
        private int _writeLock = 0;
        public int ArrayLength = 10240;
        private readonly Queue<ManualResetValueTaskSource<T>[]> _writeQueue;
        private readonly Queue<ManualResetValueTaskSource<T>[]> _readQueue;
        private ManualResetValueTaskSource<T>[] _currentWrite;
        private ManualResetValueTaskSource<T>[] _currentRead;
        public CircleTaskBuffer2()
        {
            _writeQueue = new Queue<ManualResetValueTaskSource<T>[]>(32);
            _readQueue = new Queue<ManualResetValueTaskSource<T>[]>(32);
            var _current = new ManualResetValueTaskSource<T>[ArrayLength];
            for (int i = 0; i < ArrayLength; i++)
            {
                _current[i] =new ManualResetValueTaskSource<T>();
            }
            _currentWrite = _current;
            _currentRead = _current;
            _readQueue.Enqueue(_currentRead);
            //_list = new List<int>();
            //_cache = new HashSet<int>();
            //_index = new HashSet<int>();
            //_repeates = new List<ManualResetValueTaskSource<T>>();
            //Task.Run(() =>
            //{
            //    for (int i = 0; i < 10; i++)
            //    {
            //        Thread.Sleep(10000);
            //        if (_index.Count>0)
            //        {
            //            Console.WriteLine("HashCode:" + _list.Count);
            //            Console.WriteLine("自增索引："+_index.Count);
            //            Console.WriteLine("无重复HashCode：" + _cache.Count);
            //        }
                    
            //    }
            //    Console.WriteLine("End");

            //    foreach (var item in _repeates)
            //    {
            //        if (item.AwaitableTask.IsCanceled)
            //        {
            //            Console.WriteLine("已取消！");
            //        }
            //        item.SetResult((T)(object)(true));
            //    }
            //});
        }

        private int _write_offset;
        private int _read_offset;
        //private List<int> _list;
        //private HashSet<int> _cache;
        //private HashSet<int> _index;
        //private List<ManualResetValueTaskSource<T>> _repeates;
        public ManualResetValueTaskSource<T> WriteNext()
        {
            //_index.Add(_write_offset);
            var result = _currentWrite[_write_offset];
            //var code = result.GetHashCode();
            //if (_cache.Contains(code))
            //{
            //    result.IsRepeate = true;
            //    _repeates.Add(result);
            //    Console.WriteLine("重复！按键放行！");
            //    //Console.ReadKey();
            //}
            //_list.Add(result.GetHashCode());
            //_cache.Add(result.GetHashCode());
            result.Reset();
            _write_offset += 1;
            if (_write_offset == ArrayLength)
            {
                AddBuffer();
            }
            return result;

        }

        

        private void AddBuffer()
        {
            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _writeLock,1,0) != 0)
            {
                wait.SpinOnce();
            }
            bool hasGetNewBUffer = _writeQueue.TryDequeue(out _currentWrite);
            _writeLock = 0;
            if (!hasGetNewBUffer)
            {

                _currentWrite = new ManualResetValueTaskSource<T>[ArrayLength];
                Parallel.For(0, ArrayLength, (index) => { _currentWrite[index] = new ManualResetValueTaskSource<T>(); });

            }
            while (Interlocked.CompareExchange(ref _readLock, 1, 0) != 0)
            {
                wait.SpinOnce();
            }
            _readQueue.Enqueue(_currentWrite);
            _readLock = 0;
            _write_offset = 0;
           
        }


        public void ReadNext(T value)
        {

            var result = _currentRead[_read_offset];
            result.SetResult(value);
            //Console.WriteLine("a"+result.GetHashCode());
            //if (!result.IsRepeate)
            //{
            //  result.SetResult(value);
            //}
            //else
            //{
            //    result.SetResult((T)(object)false);
            //}
            _read_offset += 1;
            if (_read_offset == ArrayLength)
            {
                CollectBuffer();
            }
            
        }

        private void CollectBuffer()
        {
            _read_offset = 0;
            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _readLock, 1, 0) != 0)
            {
                wait.SpinOnce();
            }
            var temp = _readQueue.Dequeue();
            _currentRead = _readQueue.Peek();
            while (_currentRead == null)
            {
                _currentRead = _readQueue.Peek();
                wait.SpinOnce();
            }
            _readLock = 0;
            while (Interlocked.CompareExchange(ref _writeLock, 1, 0) != 0)
            {
                wait.SpinOnce();
            }
            _writeQueue.Enqueue(temp);
            _writeLock = 0;

        }
    }
}
