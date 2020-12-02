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

    public class NewRedisClient7 : RedisClientBase
    {

        private byte _protocalStart;
        private readonly Queue<Task<bool>>_tasks;
        public NewRedisClient7()
        {
            _protocalStart = 43;
            _tasks = new Queue<Task<bool>>();
        }

        private int se;
        public override Task<bool> SetAsync(string key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes($"SET {key} {value}\r\n");
            var taskSource = CreateTask(null, TaskCreationOptions.RunContinuationsAsynchronously);
            LockSend();
            _tasks.Enqueue(taskSource);
            _sender.WriteAsync(bytes);
            ReleaseSend();
            return taskSource;
        }



        private int re;
        protected internal override void Handler(in ReadOnlySequence<byte> sequence)
        {
            foreach (ReadOnlyMemory<byte> segment in sequence)
            {
                var span = segment.Span;
                var position = span.IndexOf(_protocalStart);
                while (position != -1)
                {
                    LockSend();
                    if (_tasks.Count != 0)
                    {
                        TrySetResult(_tasks.Dequeue(), true);
                    }
                    ReleaseSend();
                    span = span.Slice(position+1);
                    position = span.IndexOf(_protocalStart);
                }
            }
        }

    }
}
