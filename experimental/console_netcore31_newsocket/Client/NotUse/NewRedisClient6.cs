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

    public class NewRedisClient6
    {
        private readonly static Func<Task<bool>, bool, bool> _setResult;
        private readonly static Func<Task<bool>> _getTask;
        static NewRedisClient6()
        {
            _setResult = typeof(Task<bool>)
                .GetMethod("TrySetResult",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(bool) }, null)
                .CreateDelegate<Func<Task<bool>, bool, bool>>();


            var ctor = typeof(Task<bool>).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[0], null);

            DynamicMethod dynamicMethod = new DynamicMethod("GETTASK", typeof(Task<bool>), new Type[0]);
            var iLGenerator = dynamicMethod.GetILGenerator();
            iLGenerator.Emit(OpCodes.Newobj, ctor);
            iLGenerator.Emit(OpCodes.Ret);
            _getTask = (Func<Task<bool>>)dynamicMethod.CreateDelegate(typeof(Func<Task<bool>>));
        }

        private const int TASK_BUFFER_LENGTH = 1024;
        private const int TASK_BUFFER_PRELENGTH = TASK_BUFFER_LENGTH - 1;
        
        private readonly byte _protocalStart;
        private readonly ConnectionContext _connection;
        public readonly PipeWriter _sender;
        private readonly PipeReader _reciver;
        private readonly Task<bool>[] _array;


        public NewRedisClient6(string ip, int port) : this(new IPEndPoint(IPAddress.Parse(ip), port))
        {
        }
        public NewRedisClient6(IPEndPoint point)
        {
            _protocalStart = (byte)43;
            _array = new Task<bool>[TASK_BUFFER_LENGTH];
            for (int i = 0; i < TASK_BUFFER_LENGTH; i++)
            {
                _array[i] = _getTask();
            }
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
        private int _taskCount;
        long total = 0;
        //volatile int _locked = 0;
        long _taget = 0;
        private int _fetch = -1;
        private volatile int _sendIndex = -1;
        private long _receiverIndex = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Wait()
        {
            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _sendIndex, _fetch + 1, _fetch) != _fetch)
            {
                wait.SpinOnce();
            }
        }
        public Task<bool> SetAsync(string key, string value)
        {
            
            var bytes = Encoding.UTF8.GetBytes($"SET {key} {value}\r\n");
            //var taskSource = new TaskCompletionSource<bool>(null, TaskCreationOptions.None);
            Wait();
            //_receiverQueue.Enqueue(taskSource);
            if (_sendIndex == TASK_BUFFER_LENGTH)
            {
                _sendIndex = -1;
                _fetch = -1;
            }
            _fetch += 1;
            _sender.WriteAsync(bytes);
            return _array[_sendIndex];

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
                Wait();
                _setResult(_array[_receiverIndex],true);
                _array[_receiverIndex] = _getTask();
                if (_receiverIndex == TASK_BUFFER_PRELENGTH)
                {
                    _receiverIndex = 0;
                }
                else
                {
                    _receiverIndex += 1;
                }
                //_receiverQueue.Dequeue().SetResult(true);
                //_locked = 0;
                //_deal += 1;
                //Interlocked.Decrement(ref _taskCount);

            }
            // }
            //while (!_receiverQueue.TryDequeue(out task)) { }
            //Interlocked.Increment(ref count);
            //task.SetResult(Encoding.UTF8.GetString(sequence.Slice(reader.Position, sequence.End).ToArray()).Contains("OK"));
        }
        private int count;
        private void Handler(in ReadOnlySpan<byte> span)
        {

            var tempSpan = span;
            TaskCompletionSource<bool> task = default;
            //var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(span));
            //var offset = tempSpan.IndexOf(_protocalStart);
            int offset;
            //int _deal = 0;
            while ((offset = tempSpan.IndexOf(_protocalStart)) != -1)
            {

                tempSpan = tempSpan.Slice(offset + 1, tempSpan.Length - offset - 1);
                //while (!_receiverQueue.TryDequeue(out task)) { }

                //if (task != default)
                //{

                //_deal += 1;
                //Interlocked.Decrement(ref _taskCount);
                task.SetResult(true);
                //task = default;
                //}

            }
            //Console.WriteLine($"本次完成 {_deal} 个任务! 剩余 {_taskCount} 个任务！");
        }


    }
}
