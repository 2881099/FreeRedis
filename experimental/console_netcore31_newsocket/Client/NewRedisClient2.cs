using Microsoft.AspNetCore.Connections;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace console_netcore31_newsocket
{

    public class NewRedisClient2
    {

        private readonly Queue<TaskCompletionSource<bool>> _receiverQueue;
        private readonly byte _protocalStart;
        private readonly ConnectionContext _connection;
        public readonly PipeWriter _sender;
        private readonly PipeReader _reciver;
        public NewRedisClient2(string ip, int port) : this(new IPEndPoint(IPAddress.Parse(ip), port))
        {
        }
        public NewRedisClient2(IPEndPoint point)
        {
            _protocalStart = (byte)43;
            _receiverQueue = new Queue<TaskCompletionSource<bool>>();
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
        public Task<bool> SetAsync(string key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes($"SET {key} {value}\r\n");
            var taskSource = new TaskCompletionSource<bool>();
            lock (_lock)
            {
                _sender.WriteAsync(bytes);
                _receiverQueue.Enqueue(taskSource);

            }
            return taskSource.Task;
        }
        private async void RunReciver()
        {

            while (true)
            {

                var result = await _reciver.ReadAsync().ConfigureAwait(false);
                var buffer = result.Buffer;

                Handler(buffer);
                //if (buffer.IsSingleSegment)
                //{

                //    //total += buffer.Length;
                //    //Console.WriteLine($"当前剩余 {_taskCount} 个任务未完成,队列中有 {_receiverQueue.Count} 个任务！缓冲区长 {buffer.Length} .");
                //    Handler(buffer);
                //}
                //else
                //{

                //    //total += buffer.Length;
                //    //Console.WriteLine($"当前剩余 {_taskCount} 个任务未完成,队列中有 {_receiverQueue.Count} 个任务！缓冲区长 {buffer.Length} .");
                //    Handler(buffer);
                //}
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
                lock (_lock)
                {
                    _receiverQueue.Dequeue().SetResult(true);
                }

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
                while (!_receiverQueue.TryDequeue(out task)) { }

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
