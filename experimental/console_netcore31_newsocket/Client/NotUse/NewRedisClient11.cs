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

    public class NewRedisClient11 : RedisClientBase
    {

        protected SingleLinks<Task<bool>> _currentTaskBuffer;
        private readonly byte _protocalStart;
        private readonly SingleLinks<Task<bool>> _taskBuffer;
        private readonly SingleLinks<bool> _resultBuffer;

        public NewRedisClient11()
        {
            _currentTaskBuffer = new SingleLinks<Task<bool>>();
            _taskBuffer = new SingleLinks<Task<bool>>();
            _resultBuffer = new SingleLinks<bool>();
            _protocalStart = (byte)43;
            _handlerResultTask = new TaskCompletionSource<long>();
            _tempResultLink = new SingleLinks<bool>();
            ResultDispatcher();
        }



        private int sendCount;
        public override Task<bool> SetAsync(string key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes($"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n");
            var taskSource = CreateTask(null, TaskCreationOptions.RunContinuationsAsynchronously);
            LockSend();
            _currentTaskBuffer.Append(taskSource);
            _sender.WriteAsync(bytes);
            ReleaseSend();
            return taskSource;
        }

        public virtual Task<bool> SetAsync(byte[] bytes)
        {
            var taskSource = CreateTask(null, TaskCreationOptions.RunContinuationsAsynchronously);
            LockSend();
            _currentTaskBuffer.Append(taskSource);
            _sender.WriteAsync(bytes);
            ReleaseSend();
            return taskSource;
        }


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
        private long _handlerCount = 0;
        protected internal override void Handler(in ReadOnlySequence<byte> sequence)
        {

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
                if (_handlerCount > 0)
                {

                    _tempResultLink.Append(result);
                    _resultBuffer.Append(_tempResultLink);
                    _tempResultLink = new SingleLinks<bool>();
                    _handlerResultTask.SetResult(_handlerCount);
                }
                else
                {

                    _resultBuffer.Append(result);
                    _handlerResultTask.SetResult(_handlerCount);

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
                var last = await _handlerResultTask.Task;
                var firstResult = _resultBuffer.Head;
                var firstTask = _taskBuffer.Head;
                _handlerCount = 0;
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
