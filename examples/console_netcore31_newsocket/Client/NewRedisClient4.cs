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


        public NewRedisClient4()
        {
            _protocalStart = (byte)43;
            
        }

        protected override void Init()
        {
            _receiverQueue = new SourceConcurrentQueue2<Task<bool>>(_sender);
            base.Init();
        }

        public Task<bool> FlushDBAsync()
        {
            var bytes = Encoding.UTF8.GetBytes($"flushdb\r\n");
            var taskSource = CreateTask();
            _receiverQueue.Enqueue(taskSource, bytes);
            return taskSource;
        }

        public override Task<bool> SetAsync(string key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes($"SET {key} {value}\r\n");
            var taskSource = CreateTask();
            _receiverQueue.Enqueue(taskSource, bytes);
            return taskSource;
        }

        protected internal override void Handler(in ReadOnlySequence<byte> sequence)
        {
            Task<bool> task;
            var reader = new SequenceReader<byte>(sequence);

            while (reader.TryReadTo(out ReadOnlySpan<byte> _, 43, advancePastDelimiter: true))
            {

                while (!_receiverQueue.TryDequeue(out task)) { }

                TrySetResult(task, true);
            }
        }

    }
}
