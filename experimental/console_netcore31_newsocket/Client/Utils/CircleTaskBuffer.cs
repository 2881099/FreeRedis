using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket.Client.Utils
{
    public class SingleLinks5<T>
    {
        private int _lock = 0;
        public bool InReading;
        public readonly ManualResetValueTaskSource<T>[] Buffer;
        public SingleLinks5()
        {
            Buffer = new ManualResetValueTaskSource<T>[10240];
            for (int i = 0; i < 10240; i++)
            {
                Buffer[i] = new ManualResetValueTaskSource<T>();
            }
        }

        public SingleLinks5<T> Next;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetLock()
        {
            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0)
            {
                wait.SpinOnce();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseLock()
        {
            _lock = 0;
        }
        public SingleLinks5<T> AppendNew()
        {

            var temp = new SingleLinks5<T>();
            GetLock();
            temp.Next = Next;
            Next = temp;
            ReleaseLock();
            return temp;

        }
    }

    public class CircleTaskBuffer<T>
    {
        private int _readLock = 0;
        private int _writeLock = 0;
        public int ArrayLength = 10240;
        private SingleLinks5<T> _writePtr;
        private SingleLinks5<T> _readPtr;
        private ManualResetValueTaskSource<T>[] _currentWrite;
        private ManualResetValueTaskSource<T>[] _currentRead;
        public CircleTaskBuffer()
        {
            var first = new SingleLinks5<T>();
            first.InReading = true;
            first.Next = first;
            _writePtr = first;
            _readPtr = first;
            _currentWrite = first.Buffer;
            _currentRead = first.Buffer;
        }

        private int _write_offset;
        private int _read_offset;
        public ManualResetValueTaskSource<T> WriteNext()
        {

            var result = _currentWrite[_write_offset];
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
            _writePtr.Next.GetLock();
            bool shut = _writePtr.Next.InReading;
            _writePtr.Next.ReleaseLock();
            if (shut)
            {
                _writePtr = _writePtr.AppendNew();
            }
            else
            {
                _writePtr = _writePtr.Next;
            }
            _write_offset = 0;
            _currentWrite = _writePtr.Buffer;

        }


        public void ReadNext(T value)
        {

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

            var pre = _readPtr;
            pre.GetLock();
            pre.InReading = false;
            _readPtr = _readPtr.Next;
            pre.ReleaseLock();
            _readPtr.InReading = true;
            _read_offset = 0;
            _currentRead = _readPtr.Buffer;

        }
    }
}
