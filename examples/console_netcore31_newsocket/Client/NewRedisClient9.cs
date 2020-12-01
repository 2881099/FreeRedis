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

    public class NewRedisClient9 :RedisClientBase
    {
        
        private SingleLinks<Task<bool>> _currentTaskBuffer;
        private readonly byte _protocalStart;
        private readonly SingleLinks<Task<bool>> _taskBuffer;
        private readonly SingleLinks<bool> _resultBuffer;

        public NewRedisClient9()
        {
            _currentTaskBuffer = new SingleLinks<Task<bool>>();
            _taskBuffer = new SingleLinks<Task<bool>>();
            _resultBuffer = new SingleLinks<bool>();
            _protocalStart = (byte)43;
            _handlerResultTask = new TaskCompletionSource<int>();
            _tempResultLink = new SingleLinks<bool>();
            ResultDispatcher();
        }



        private int sendCount;
        public override Task<bool> SetAsync(string key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes($"SET {key} {value}\r\n");
            var taskSource = CreateTask();
            LockSend();
            sendCount += 1;
            _currentTaskBuffer.Append(taskSource);
            _sender.WriteAsync(bytes);
            ReleaseSend();
            return taskSource;
        }


        private void GetTaskSpan()
        {

            LockSend();
            if (_currentTaskBuffer.Count != 0)
            {
                _taskBuffer.Append(_currentTaskBuffer);
                _currentTaskBuffer = new SingleLinks<Task<bool>>();
            }
            ReleaseSend();
            
        }

        private TaskCompletionSource<int> _handlerResultTask;
        private SingleLinks<bool> _tempResultLink;
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
                    result.Append(true);
                    //TrySetResult(_currentReceiverBuffer[taskBufferIndex],true);
                    span = span.Slice(position + 1);
                    position = span.IndexOf(_protocalStart);
                }
            }
           
            if (TryGetReceiverLock())
            {
                if (_tempResultLink.Count>0)
                {
                    
                    _tempResultLink.Append(result);
                    _resultBuffer.Append(_tempResultLink);
                    _tempResultLink = new SingleLinks<bool>();
                    _handlerResultTask.SetResult(_tempResultLink.Count);
                }
                else
                {

                    _resultBuffer.Append(result);
                    _handlerResultTask.SetResult(result.Count);

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
                //var pre = firstTask;
                LockSend();
                for (int i = 0; i < last; i+=1)
                {
                    
                    firstResult = firstResult.Next;
                    firstTask = firstTask.Next;
                    if (firstTask == null)
                    {
                        Console.WriteLine("chushi:"+i);
                        //Console.WriteLine(_taskBuffer.Count);
                    }

                    TrySetResult(firstTask.Value, firstResult.Value);
                }
                Console.WriteLine("need handle : " + last);
                _taskBuffer.ClearBefore(_taskBuffer.Head,firstTask);
                ReleaseSend();
                _handlerResultTask = new TaskCompletionSource<int>();
                ReleaseReceiver();
                
            }

        }

    }

    public class SingleLinks<T>
    {
        public SingleLinkNode<T> Head;
        public SingleLinkNode<T> Tail;
        private readonly SingleLinkNode<T> _first;
        public SingleLinks()
        {
            _first = new SingleLinkNode<T>(default);
            Head = _first;
            Tail = _first;
        }
        public int Count;
        public void Append(T value)
        {
            Count += 1;
            Tail.Next = new SingleLinkNode<T>(value);
            Tail = Tail.Next;
        }
        public void Append(SingleLinks<T> node)
        {
            //Console.WriteLine("process in Append!");
            if (node._first.Next != null)
            {
                //var temp = node.Head;
                //while (temp.Next!=null)
                //{
                //    Count += 1;
                //    temp = temp.Next;
                    
                //}
                Count += node.Count;
                Tail.Next = node._first.Next;
                Tail = Tail.Next;
            }
        }

        public void Clear()
        {
            _first.Next = null;
            Tail = _first;
        }

        public void ClearBefore(SingleLinkNode<T> head,SingleLinkNode<T> node)
        {
            var count = 1;
            head = head.Next;
            while (head != node)
            {
                count += 1;
                head = head.Next;

            }
            Console.WriteLine("ClearBefore :" + count);
            //Console.WriteLine("process in ClearBefore!");
            _first.Next = node.Next;
            Head = _first;
            if (node.Next == null)
            {
                Tail = _first;
            }
            else
            {
               
                Tail = node.Next;
            }
            
        }

    }

    public class SingleLinkNode<T>
    {
        public readonly T Value;
        public SingleLinkNode<T> Next;
        public SingleLinkNode(T value)
        {
            Value = value;
        }

    }

}
