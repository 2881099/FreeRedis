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

    public class NewRedisClient7
    {
        private readonly static Func<Task<bool>, bool, bool> _setResult;
        private readonly static Func<Task<bool>> _getTask;
        static NewRedisClient7()
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

        private readonly Queue<Task<bool>> _receiverQueue;
        private readonly byte _protocalStart;
        private readonly ConnectionContext _connection;
        public readonly PipeWriter _sender;
        private readonly PipeReader _reciver;
        public NewRedisClient7(string ip, int port) : this(new IPEndPoint(IPAddress.Parse(ip), port))
        {
        }
        public NewRedisClient7(IPEndPoint point)
        {

            _protocalStart = (byte)43;
            _receiverQueue = new Queue<Task<bool>>();
            SocketConnectionFactory client = new SocketConnectionFactory(new SocketTransportOptions());
            _connection = client.ConnectAsync(point).Result;
            _sender = _connection.Transport.Output;
            _reciver = _connection.Transport.Input;
            RunReciver();

        }

        private long _locked = 0;

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
            var taskSource = _getTask();
            Wait();
            _receiverQueue.Enqueue(taskSource);
            _sender.WriteAsync(bytes);
            _locked = 0;
            return taskSource;
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

            var reader = new SequenceReader<byte>(sequence);
            //int _deal = 0;
            //79 75
            //if (reader.TryReadTo(out ReadOnlySpan<byte> result, 43, advancePastDelimiter: true))
            //{
            Wait();
            while (reader.TryReadTo(out ReadOnlySpan<byte> _, 43, advancePastDelimiter: true))
            {
                
                TrySetResult(_receiverQueue.Dequeue(), true);
               
                //_deal += 1;
                //Interlocked.Decrement(ref _taskCount);

            }
            _locked = 0;
            // }
            //while (!_receiverQueue.TryDequeue(out task)) { }
            //Interlocked.Increment(ref count);
            //task.SetResult(Encoding.UTF8.GetString(sequence.Slice(reader.Position, sequence.End).ToArray()).Contains("OK"));
        }
        public bool TrySetResult(Task<bool> task, bool result)
        {
            bool rval = _setResult(task, result);
            if (!rval)
            {
                SpinWait sw = default;
                while (!task.IsCompleted)
                {
                    sw.SpinOnce();
                }
            }

            return rval;
        }

    }
}
