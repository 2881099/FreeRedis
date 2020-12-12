using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket.Client.Utils
{
    public class CircleTaskBuffer<T>
    {
        private int _readLock = 0;
        private int _writeLock = 0;
        public int ArrayLength = 10240;
        private readonly Queue<ManualResetValueTaskSource<T>[]> _writeQueue;
        private readonly Queue<ManualResetValueTaskSource<T>[]> _readQueue;
        private ManualResetValueTaskSource<T>[] _currentWrite;
        private ManualResetValueTaskSource<T>[] _currentRead;
        public CircleTaskBuffer()
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
        }

        private int _write_offset;
        private int _read_offset;
        public ManualResetValueTaskSource<T> WriteNext()
        {

            var result = _currentWrite[_write_offset];
            result.Reset();
            //Console.WriteLine(result.GetHashCode());
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
                for (int i = 0; i < ArrayLength; i++)
                {
                    _currentWrite[i] = new ManualResetValueTaskSource<T>();
                }

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
