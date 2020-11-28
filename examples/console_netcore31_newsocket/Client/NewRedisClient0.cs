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

    public class NewRedisClient0
    {
        //private readonly LinkedList<TaskCompletionSource<bool>> _receiverQueue;
        private TaskLink<bool> _taskLink;
        private TaskLink<bool> _store;
        private TaskLink<bool> _head;
        private readonly byte _protocalStart;
        private readonly ConnectionContext _connection;
        public readonly PipeWriter _sender;
        private readonly PipeReader _reciver;

        static NewRedisClient0()
        {
           

        }
        public NewRedisClient0(string ip, int port) : this(new IPEndPoint(IPAddress.Parse(ip), port))
        {
        }
        public NewRedisClient0(IPEndPoint point)
        {
            _protocalStart = (byte)43;
            _store = new TaskLink<bool>();
            _taskLink = _store;
            _head = _taskLink;
            SocketConnectionFactory client = new SocketConnectionFactory(new SocketTransportOptions());
            _connection = client.ConnectAsync(point).Result;
            _sender = _connection.Transport.Output;
            _reciver = _connection.Transport.Input;
            RunReciver();
            //Task.Run(async () => { 
            //    await Task.Delay(30000);
            //    Console.WriteLine(total);
            //    Console.WriteLine(_receiverQueue.Count);
            //    await Task.Delay(20000);
            //    Console.WriteLine(total);
            //    Console.WriteLine(_receiverQueue.Count);
            //    await Task.Delay(10000);
            //    Console.WriteLine(total);
            //    Console.WriteLine(_receiverQueue.Count);
            //});
            //RunSender();
        }


        private readonly object _lock = new object();
        long total = 0;
        long _locked = 0;
        long _taget = 0;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Wait()
        {
            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _locked, 1, 0) != 0)
            {
                wait.SpinOnce();
            }
        }
        public Task<bool> SetAsync(string key, string value)
        {
            
            var bytes = Encoding.UTF8.GetBytes($"SET {key} {value}\r\n");
            var taskSource = new TaskLink<bool>();
            Wait();
            if (_taskLink == null)
            {
                _taskLink = _store;
                _head = _store;
            }
            _taskLink.Next = taskSource;
            _taskLink = _taskLink.Next;
            _sender.WriteAsync(bytes);
            return taskSource._task;

        }
        private async void RunReciver()
        {

            while (true)
            {

                var result = await _reciver.ReadAsync();
                var buffer = result.Buffer;

                if (buffer.IsSingleSegment)
                {

                    //total += buffer.Length;
                    //Console.WriteLine($"当前剩余 {_taskCount} 个任务未完成,队列中有 {_receiverQueue.Count} 个任务！缓冲区长 {buffer.Length} .");
                    Handler(buffer);
                }
                else
                {

                    //total += buffer.Length;
                    //Console.WriteLine($"当前剩余 {_taskCount} 个任务未完成,队列中有 {_receiverQueue.Count} 个任务！缓冲区长 {buffer.Length} .");
                    Handler(buffer);
                }
                _reciver.AdvanceTo(buffer.End);
                if (result.IsCompleted)
                {
                    return;
                }

            }
        }


        private void Handler(in ReadOnlySequence<byte> sequence)
        {
            TaskCompletionSource<bool> task;
            var reader = new SequenceReader<byte>(sequence);
            //int _deal = 0;
            //79 75
            //if (reader.TryReadTo(out ReadOnlySpan<byte> result, 43, advancePastDelimiter: true))
            //{
            while (reader.TryReadTo(out ReadOnlySpan<byte> _, 43, advancePastDelimiter: true))
            {
                //Wait();
                var temp = _head;
                _head = _head.Next;
                _head.TrySetResult(true);
                temp = null;
                _locked = 0;
                //_deal += 1;
                //Interlocked.Decrement(ref _taskCount);

            }
            // }
            //while (!_receiverQueue.TryDequeue(out task)) { }
            //Interlocked.Increment(ref count);
            //task.SetResult(Encoding.UTF8.GetString(sequence.Slice(reader.Position, sequence.End).ToArray()).Contains("OK"));
        }

        internal class TaskLink<T>
        {
            //public readonly Task<T> _task;
            private readonly static Func<Task<T>, T, bool> _setResult;
            private readonly static Func<Task<T>> _getTask;
            static TaskLink()
            {
                _setResult = typeof(Task<T>)
                    .GetMethod("TrySetResult",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(T) },null)
                    .CreateDelegate<Func<Task<T>, T, bool>>();

               
                var ctor = typeof(Task<T>).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,null, new Type[0], null);

                DynamicMethod dynamicMethod = new DynamicMethod("GETTASK", typeof(Task<T>), new Type[0]);
                var iLGenerator = dynamicMethod.GetILGenerator();
                iLGenerator.Emit(OpCodes.Newobj, ctor);
                iLGenerator.Emit(OpCodes.Ret);
                _getTask = (Func<Task<T>>)dynamicMethod.CreateDelegate(typeof(Func<Task<T>>));
            }

            public readonly Task<T> _task;
            public TaskLink(TaskCreationOptions options = TaskCreationOptions.None)
            {
                _task = _getTask();
            }
            public TaskLink<T> Next;

            public bool TrySetResult(T result)
            {
                bool rval = _setResult(_task, result);
                if (!rval)
                {
                    SpinWait sw = default;
                    while (!_task.IsCompleted)
                    {
                        sw.SpinOnce();
                    }
                }

                return rval;
            }

        } 

    }
}
