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

    public class NewRedisClient5
    {
        private readonly static Func<Task<bool>, bool, bool> _setResult;
        private readonly static Func<Task<bool>> _getTask;
        static NewRedisClient5()
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

        private const int TASK_BUFFER_LENGTH = 4096;
        private const int TASK_BUFFER_PRELENGTH = TASK_BUFFER_LENGTH - 1;
        
        private readonly byte _protocalStart;
        private readonly ConnectionContext _connection;
        public readonly PipeWriter _sender;
        private readonly PipeReader _reciver;
        private readonly Task<bool>[] _array;


        public NewRedisClient5(string ip, int port) : this(new IPEndPoint(IPAddress.Parse(ip), port))
        {
        }
        public NewRedisClient5(IPEndPoint point)
        {
            _protocalStart = (byte)43;
            _array = new Task<bool>[TASK_BUFFER_LENGTH];
            for (int i = 0; i < TASK_BUFFER_LENGTH; i++)
            {
                _array[i] = _getTask();
            }
            Console.WriteLine(_array[0].IsCompleted);
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
        long _send_locked = 0;
        long _receiver_locked = 0;
        long _taget = 0;

        private long _sendIndex = 0;
        private long _receiverIndex = 0;
        private long _topIndex = TASK_BUFFER_PRELENGTH;
        private volatile int _step = TASK_BUFFER_LENGTH;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WaitSend()
        {

            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _send_locked, 1, 0) != 0)
            {
                wait.SpinOnce();
            }
            while (_step == 0)
            {
                wait.SpinOnce();
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WaitHandler()
        {
            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _receiver_locked, 1, 0) != 0)
            {
                wait.SpinOnce();
            }
        }

        public Task<bool> SetAsync(string key, string value)
        {
            
            var bytes = Encoding.UTF8.GetBytes($"SET {key} {value}\r\n");
            //var taskSource = new TaskCompletionSource<bool>(null, TaskCreationOptions.None);
            WaitSend();
            Interlocked.Decrement(ref _step);
            //_receiverQueue.Enqueue(taskSource);
            var temp = _sendIndex;
            if (_sendIndex == _topIndex)
            {

                _sendIndex = 0;
            }
            else
            {
                _sendIndex += 1;
            }
            _sender.WriteAsync(bytes);
            _send_locked = 0;
            return _array[temp];

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
            WaitHandler();
            while (reader.TryReadTo(out ReadOnlySpan<byte> _, 43, advancePastDelimiter: true))
            {

                _setResult(_array[_receiverIndex],true);
                _array[_receiverIndex] = _getTask();
                Interlocked.Increment(ref _step);
                if (_receiverIndex == TASK_BUFFER_PRELENGTH)
                {
                    _receiverIndex = 0;
                }
                else
                {
                    _receiverIndex += 1;
                }
                _receiver_locked = 0;
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
