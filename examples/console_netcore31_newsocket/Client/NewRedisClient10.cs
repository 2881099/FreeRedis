using Microsoft.AspNetCore.Connections;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace console_netcore31_newsocket
{



    public class NewRedisClient10 : RedisClientBase
    {

        protected SingleLinks<Task<bool>> _currentTaskBuffer;
        private readonly byte _protocalStart;
        private readonly SingleLinks<Task<bool>> _taskBuffer;
        private readonly SingleLinks<bool> _resultBuffer;
        public NewRedisClient10()
        {
            _currentTaskBuffer = new SingleLinks<Task<bool>>();
            _taskBuffer = new SingleLinks<Task<bool>>();
            _resultBuffer = new SingleLinks<bool>();
            _protocalStart = (byte)43;
            _handlerResultTask = new TaskCompletionSource<long>();
            _tempResultLink = new SingleLinks<bool>();
            ResultDispatcher();
        }
        
        public override Task<bool> SetAsync(string key, string value)
        {
            throw new NotImplementedException();
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void SetAsync(byte[] bytes,Task<bool> task)
        {
            _currentTaskBuffer.Append(task);
            _sender.WriteAsync(bytes);
            //ReleaseSend();
        }
        public void SetAndWaitAsync(byte[] bytes, Task<bool> task)
        {

            LockSend();
            _currentTaskBuffer.Append(task);
            _sender.WriteAsync(bytes);
            ReleaseSend();

        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void GetTaskSpan()
        {

            LockSend();
            if (_currentTaskBuffer.Head.Next!=null)
            {
                _taskBuffer.Append(_currentTaskBuffer);
                _currentTaskBuffer = new SingleLinks<Task<bool>>();
            }
            ReleaseSend();

        }

        private TaskCompletionSource<long> _handlerResultTask;
        private SingleLinks<bool> _tempResultLink;
        //public long HandlerCount = 0;
        private long _handlerCount = 0;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected internal override void Handler(in ReadOnlySequence<byte> sequence)
        {
            //HandlerCount += 1;
            GetTaskSpan();
            SingleLinks<bool> result = new SingleLinks<bool>();

            foreach (ReadOnlyMemory<byte> segment in sequence)
            {
                var span = segment.Span;
                var position = span.IndexOf(_protocalStart);
                while (position != -1)
                {
                    _handlerCount += 1;
                    result.Append(true);
                    span = span.Slice(position + 1);
                    position = span.IndexOf(_protocalStart);
                }
            }

            if (TryGetReceiverLock())
            {
                var tempCount = _handlerCount;
                _handlerCount = 0;
                if (tempCount > 0)
                {
                    
                    _tempResultLink.Append(result);
                    _resultBuffer.Append(_tempResultLink);
                    _tempResultLink = new SingleLinks<bool>();
                    _handlerResultTask.SetResult(tempCount);
                }
                else
                {
                    _resultBuffer.Append(result);
                    _handlerResultTask.SetResult(tempCount);

                }

            }
            else
            {
                _tempResultLink.Append(result);
            }

        }
        
        public async void ResultDispatcher()
        {
            while (true)
            {
                var last = await _handlerResultTask.Task.ConfigureAwait(false);
                var firstResult = _resultBuffer.Head;
                var firstTask = _taskBuffer.Head;
                for (int i = 0; i < last; i += 1)
                {

                    firstResult = firstResult.Next;
                    firstTask = firstTask.Next;
                    TrySetResult(firstTask.Value, firstResult.Value);

                }
                LockSend();
                _taskBuffer.ClearBefore(firstTask);
                ReleaseSend();
                _resultBuffer.Clear();
                _handlerResultTask = new TaskCompletionSource<long>();
                ReleaseReceiver();

            }

        }

    }



}
