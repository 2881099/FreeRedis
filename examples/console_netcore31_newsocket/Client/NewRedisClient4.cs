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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace console_netcore31_newsocket
{

    public class NewRedisClient4 : RedisClientBase
    {

       
        private SourceConcurrentQueue2<Task<bool>> _receiverQueue;
        private readonly byte _protocalStart;
        private readonly NewRedisClient9 newRedisClient9;

        public NewRedisClient4()
        {
            _protocalStart = (byte)43;
            newRedisClient9 = new NewRedisClient9();


        }
        public override void CreateConnection(string ip, int port)
        {
            newRedisClient9.CreateConnection(ip, port);
            base.CreateConnection(ip, port);
        }

        protected override void Init()
        {
            _receiverQueue = new SourceConcurrentQueue2<Task<bool>>(_sender);
            base.Init();
        }

        public Task<bool> FlushDBAsync()
        {
            var bytes = Encoding.UTF8.GetBytes($"flushdb\r\n");
            var taskSource = CreateTask(null, TaskCreationOptions.RunContinuationsAsynchronously);
            _receiverQueue.Enqueue(taskSource, bytes);
            return taskSource;
        }
        public Task<bool> AuthAsync(string password)
        {
            var bytes = Encoding.UTF8.GetBytes($"AUTH {password}\r\n");
            var taskSource = CreateTask(null, TaskCreationOptions.RunContinuationsAsynchronously);
            _receiverQueue.Enqueue(taskSource, bytes);
            return taskSource;
        }
        public override Task<bool> SetAsync(string key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes($"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n");
            var taskSource = new TaskCompletionSource<bool>(null, TaskCreationOptions.RunContinuationsAsynchronously).Task;
            if (TryGetSendLock())
            {
                _receiverQueue.Enqueue(taskSource, bytes);
                ReleaseSend();
                return taskSource;
            }
            else
            {
                return newRedisClient9.SetAsync(key, value);
            }
            
            
            
        }

        protected internal override void Handler(in ReadOnlySequence<byte> sequence)
        {
            Task<bool> task;
            foreach (ReadOnlyMemory<byte> segment in sequence)
            {
                var span = segment.Span;
                var position = span.IndexOf(_protocalStart);
                SpinWait wait = default;
                while (position != -1)
                {
                    while (!_receiverQueue.TryDequeue(out task)) { wait.SpinOnce(); }
                    TrySetResult(task, true);
                    span = span.Slice(position+1);
                    position = span.IndexOf(_protocalStart);

                }
            }
        }

    }
}
