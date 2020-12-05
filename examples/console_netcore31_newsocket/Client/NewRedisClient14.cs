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



    public class NewRedisClient14 : RedisClientBase
    {

        private readonly byte _protocalStart;
        private readonly SingleLinks2<bool> _taskBuffer;
        private readonly SingleLinkNode2<bool> _head;
        public NewRedisClient14()
        {
            _taskBuffer = new SingleLinks2<bool>();
            _head = _taskBuffer.Head;
            _protocalStart = (byte)43;
        }
        public Task<bool> AuthAsync(string password)
        {
            var bytes = Encoding.UTF8.GetBytes($"AUTH {password}\r\n");
            var task = CreateTask(null, TaskCreationOptions.RunContinuationsAsynchronously);
            LockSend();
            _taskBuffer.Append(task);
            _sender.WriteAsync(bytes);
            ReleaseSend();
            return task;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override Task<bool> SetAsync(string key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes($"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n");
            var task = CreateTask(null, TaskCreationOptions.RunContinuationsAsynchronously);
            LockSend();
            _taskBuffer.Append(task);
            _sender.WriteAsync(bytes);
            ReleaseSend();
            return task;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void SetAsync(byte[] bytes,Task<bool> task)
        {
            _taskBuffer.Append(task);
            _sender.WriteAsync(bytes);
            ReleaseSend();
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void SetAndWaitAsync(byte[] bytes, Task<bool> task)
        {

            LockSend();
            _taskBuffer.Append(task);
            _sender.WriteAsync(bytes);
            ReleaseSend();

        }

        private int count =-1;
        private int dealCount =-1;
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected internal override void Handler(in ReadOnlySequence<byte> sequence)
        {
            //Interlocked.Increment(ref dealCount);
            //HandlerCount += 1;
            //GetTaskSpan();

            int step = 0;
            //第一个节点是有用的节点
            var tempParameters = _head.Next;
            var tempTail = _head;
            foreach (ReadOnlyMemory<byte> segment in sequence)
            {
                var span = segment.Span;
                var position = span.IndexOf(_protocalStart);
                while (position != -1)
                {

                    step += 1;
                    tempTail = tempTail.Next;
                    tempTail.Result = true;


                    span = span.Slice(position + 1);
                    position = span.IndexOf(_protocalStart);

                }
            }

            LockSend();
            _taskBuffer.ClearBefore(tempTail);
            ReleaseSend();

            if (step < 4)
            {

                tempParameters.Completed();
                step -= 1;
                while (step!=0)
                {
                    tempParameters = tempParameters.Next;
                    tempParameters.Completed();
                    step -= 1;
                }

            }
            else
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback((state) => { HandlerResult(tempParameters); }));
                void HandlerResult(SingleLinkNode2<bool> head)
                {
                    for (int i = 0; i < step; i+=1)
                    {
                        head.Completed();
                        head = head.Next;
                    }

                }
            }
   
        }
        

    }



}
