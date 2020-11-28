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
    
    public class NewRedisClient5 : RedisClientBase
    {
        

        private const int TASK_BUFFER_LENGTH = 202400;
        private const int TASK_BUFFER_PRELENGTH = TASK_BUFFER_LENGTH - 1;
        

        private byte _protocalStart;
        private readonly Task<bool>[] _tasks;
        public NewRedisClient5()
        {
            _protocalStart = 43;
            _tasks = new Task<bool>[TASK_BUFFER_LENGTH];
        }


        long _receiver_locked = 0;
        private long _sendIndex = 0;
        private long _receiverIndex = 0;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WaitHandler()
        {
            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _receiver_locked, 1, 0) != 0)
            {
                wait.SpinOnce();
            }
        }

        public override Task<bool> SetAsync(string key, string value)
        {
            var task = CreateTask();
            var bytes = Encoding.UTF8.GetBytes($"SET {key} {value}\r\n");
            LockSend();
            _tasks[_sendIndex] = task;
            if (_sendIndex == TASK_BUFFER_PRELENGTH)
            {

                _sendIndex = 0;
            }
            else
            {
                _sendIndex += 1;
            }
            _sender.WriteAsync(bytes);
            ReleaseSend();
            return task;

        }
       


        protected internal override void Handler(in ReadOnlySequence<byte> sequence)
        {
            var reader = new SequenceReader<byte>(sequence);
            WaitHandler();
            while (reader.TryReadTo(out ReadOnlySpan<byte> _, 43, advancePastDelimiter: true))
            {

                TrySetResult(_tasks[_receiverIndex], true);
                if (_receiverIndex == TASK_BUFFER_PRELENGTH)
                {
                    _receiverIndex = 0;
                }
                else
                {
                    _receiverIndex += 1;
                }
            }
            _receiver_locked = 0;
        }
    }
}
