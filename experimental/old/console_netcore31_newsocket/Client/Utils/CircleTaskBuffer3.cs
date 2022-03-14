using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket.Client.Utils
{
    public class CircleTaskBuffer3<T>
    {
        private int _readLock = 0;
        private int _writeLock = 0;
        public int ArrayLength = 8192;
        private readonly Queue<TaskCompletionSource<T>[]> _writeQueue;
        private readonly Queue<TaskCompletionSource<T>[]> _readQueue;
        private TaskCompletionSource<T>[] _currentWrite;
        private TaskCompletionSource<T>[] _currentRead;
        public CircleTaskBuffer3()
        {
            _writeQueue = new Queue<TaskCompletionSource<T>[]>(32);
            _readQueue = new Queue<TaskCompletionSource<T>[]>(32);
            var _current = new TaskCompletionSource<T>[ArrayLength];
            _currentWrite = _current;
            _currentRead = _current;
            _readQueue.Enqueue(_currentRead);
        }

        private int _write_offset;
        private int _read_offset;
        public TaskCompletionSource<T> WriteNext()
        {
            var source = new TaskCompletionSource<T>(null, TaskCreationOptions.RunContinuationsAsynchronously);
            _currentWrite[_write_offset] = source;
            //Console.WriteLine(result.GetHashCode());
            _write_offset += 1;
            if (_write_offset == ArrayLength)
            {
                AddBuffer();
            }
            return source;

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
                _currentWrite = new TaskCompletionSource<T>[ArrayLength];

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
            //Console.WriteLine("a"+result.GetHashCode());
            result.SetResult(value);
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
