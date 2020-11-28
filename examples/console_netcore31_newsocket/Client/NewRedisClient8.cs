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

    public class NewRedisClient8
    {
        private readonly static Func<Task<bool>, bool, bool> _setResult;
        private readonly static Func<Task<bool>> _getTask;
        static NewRedisClient8()
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
        private List<Task<bool>> _currentTaskBuffer;
        private readonly byte _protocalStart;
        private readonly ConnectionContext _connection;
        public readonly PipeWriter _sender;
        private readonly PipeReader _reciver;
        public NewRedisClient8(string ip, int port) : this(new IPEndPoint(IPAddress.Parse(ip), port))
        {
        }
        public NewRedisClient8(IPEndPoint point)
        {
            taskBuffer = new Queue<Task<bool>[]>();
            _protocalStart = (byte)43;
            SocketConnectionFactory client = new SocketConnectionFactory(new SocketTransportOptions());
            _connection = client.ConnectAsync(point).Result;
            _sender = _connection.Transport.Output;
            _reciver = _connection.Transport.Input;
            _currentTaskBuffer = new List<Task<bool>>(3000);
            RunReciver();

        }

        private long _locked = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WaitSend()
        {
            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _locked, 1, 0) != 0)
            {
                wait.SpinOnce();
            }
        }

        private long _remainBuffer = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WaitBuffer()
        {
            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _remainBuffer, 1, 0) != 0)
            {
                wait.SpinOnce(8);
            }
        }



        public Task<bool> SetAsync(string key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes($"SET {key} {value}\r\n");
            var taskSource = _getTask();
            WaitSend();
            _currentTaskBuffer.Add(taskSource);
            _sender.WriteAsync(bytes);
            _locked = 0;
            return taskSource;
        }


        private bool GetTaskSpan()
        {

            WaitSend();
            if (_currentTaskBuffer.Count != 0)
            {

                taskBuffer.Enqueue(_currentTaskBuffer.ToArray());
                _currentTaskBuffer = new List<Task<bool>>(3000);
                _locked = 0;
                return true;

            }
            _locked = 0;
            return false;
            


        }
        private int taskBufferIndex = 0;
        private readonly Queue<Task<bool>[]> taskBuffer;
        
        private void Handler(in ReadOnlySequence<byte> sequence)
        {

            WaitBuffer();
            GetTaskSpan();
            var temp = taskBuffer.Peek();
            var reader = new SequenceReader<byte>(sequence);


            while (reader.TryReadTo(out ReadOnlySpan<byte> _, 43, advancePastDelimiter: true))
            {

                if (taskBufferIndex == temp.Length)
                {
                    taskBuffer.Dequeue();
                    temp = taskBuffer.Peek();
                    taskBufferIndex = 0;

                    if (temp.Length == 0)
                    {
                        SpinWait wait = default;
                        while (!GetTaskSpan())
                        {
                            wait.SpinOnce(8);
                        }
                    }
                }

                TrySetResult(temp[taskBufferIndex], true);
                taskBufferIndex += 1;

            }
            _remainBuffer = 0;

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
        private async void RunReciver()
        {

            while (true)
            {

                var result = await _reciver.ReadAsync().ConfigureAwait(false);
                var buffer = result.Buffer;
                Handler(buffer);
                _reciver.AdvanceTo(buffer.End);
                if (result.IsCompleted)
                {
                    return;
                }

            }
        }
    }
}
