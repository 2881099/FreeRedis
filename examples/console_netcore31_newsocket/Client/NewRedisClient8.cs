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

    public class NewRedisClient8 :RedisClientBase
    {
        
        private List<Task<bool>> _currentTaskBuffer;
        private readonly byte _protocalStart;


        public NewRedisClient8()
        {
            _currentTaskBuffer = new List<Task<bool>>(30000);
            taskBuffer = new Queue<Task<bool>[]>();
            _protocalStart = (byte)43;
        }




        public override Task<bool> SetAsync(string key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes($"SET {key} {value}\r\n");
            var taskSource = CreateTask(null, TaskCreationOptions.RunContinuationsAsynchronously);
            LockSend();
            _currentTaskBuffer.Add(taskSource);
            _sender.WriteAsync(bytes);
            ReleaseSend();
            return taskSource;
        }


        private void GetTaskSpan()
        {

            LockSend();
            if (_currentTaskBuffer.Count != 0)
            {
                //Console.WriteLine(_currentTaskBuffer.Count);
                taskBuffer.Enqueue(_currentTaskBuffer.ToArray());
                _currentTaskBuffer = new List<Task<bool>>(20000);

            }
            ReleaseSend();

        }
        private int taskBufferIndex = 0;
        private int _currentReceiverBufferLength;
        private Task<bool>[] _currentReceiverBuffer;
        private readonly Queue<Task<bool>[]> taskBuffer;

      
        protected internal override void Handler(in ReadOnlySequence<byte> sequence)
        {
            //SpinWait wait = default;
            //while (taskBuffer.Count == 0)
            //{
            //    GetTaskSpan();
            //    wait.SpinOnce(8);
            //}
            GetTaskSpan();
            if (_currentReceiverBuffer == null)
            {
                _currentReceiverBuffer = taskBuffer.Dequeue();
                _currentReceiverBufferLength = _currentReceiverBuffer.Length;
            }
            foreach (ReadOnlyMemory<byte> segment in sequence)
            {
                var span = segment.Span;
                var position = span.IndexOf(_protocalStart);
                while (position != -1)
                {

                    if (taskBufferIndex == _currentReceiverBufferLength)
                    {
                        _currentReceiverBuffer = taskBuffer.Dequeue();
                        _currentReceiverBufferLength = _currentReceiverBuffer.Length;
                        taskBufferIndex = 0;
                    }
                    TrySetResult(_currentReceiverBuffer[taskBufferIndex],true);
                    taskBufferIndex += 1;
                    span = span.Slice(position + 1);
                    position = span.IndexOf(_protocalStart);
                }
            }
        }


    }
}
