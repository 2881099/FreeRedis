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



    public class NewRedisClient15 : RedisClientBase
    {

        protected SingleLinks2<bool> _currentTaskBuffer;
        private readonly byte _protocalStart;
        private readonly SingleLinks2<bool> _taskBuffer;
        public NewRedisClient15()
        {
            _currentTaskBuffer = new SingleLinks2<bool>();
            _taskBuffer = new SingleLinks2<bool>();
            _protocalStart = (byte)43;
            //Task.Run(() =>
            //{
            //    for (int i = 0; i < 20; i++)
            //    {
            //        Thread.Sleep(3000);
            //        Console.WriteLine(count);
            //        Console.WriteLine("Deal Times:"+ dealCount);
            //    }
            //});
        }
        public Task<bool> AuthAsync(string password)
        {
            var bytes = Encoding.UTF8.GetBytes($"AUTH {password}\r\n");
            var task = CreateTask(null, TaskCreationOptions.RunContinuationsAsynchronously);
            LockSend();
            _currentTaskBuffer.Append(task);
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
            _currentTaskBuffer.Append(task);
            _sender.WriteAsync(bytes);
            ReleaseSend();
            return task;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void SetAsync(byte[] bytes,Task<bool> task)
        {
            _currentTaskBuffer.Append(task);
            _sender.WriteAsync(bytes);
            ReleaseSend();
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void SetAndWaitAsync(byte[] bytes, Task<bool> task)
        {

            LockSend();
            _currentTaskBuffer.Append(task);
            _sender.WriteAsync(bytes);
            ReleaseSend();

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetTaskSpan()
        {

            LockSend();
            if (_currentTaskBuffer.Head.Next!=null)
            {
                _taskBuffer.Append(_currentTaskBuffer);
                _currentTaskBuffer = new SingleLinks2<bool>();
            }
            ReleaseSend();

        }
        private int count =-1;
        private int dealCount =-1;
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected internal override void Handler(in ReadOnlySequence<byte> sequence)
        {
            //Interlocked.Increment(ref dealCount);
            //HandlerCount += 1;
            GetTaskSpan();
            int step = 0;
            //第一个节点是有用的节点
            SingleLinkNode2<bool> tempHead = _taskBuffer.Head;
            var tempParameters = tempHead.Next;
            var tempTail = tempHead;
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

            if (step == 1)
            {
                tempParameters.Completed();
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
