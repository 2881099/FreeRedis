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


        public override Task<bool> SetAsync(string key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes($"SET {key} {value}\r\n");
            var taskSource = CreateTask();
            LockSend();
            _tasks.Enqueue(taskSource);
            _sender.WriteAsync(bytes);
            ReleaseSend();
            return taskSource;
        }




        protected internal override void Handler(in ReadOnlySequence<byte> sequence)
        {

            var reader = new SequenceReader<byte>(sequence);
            LockSend();
            while (reader.TryReadTo(out ReadOnlySpan<byte> _, 43, advancePastDelimiter: true))
            {
                
                TrySetResult(_tasks.Dequeue(), true);


            }
            ReleaseSend();

        }
        

    }
}
