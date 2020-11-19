using Microsoft.AspNetCore.Connections;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{
    public class NewRedisClient
    {

        private readonly ConcurrentQueue<TaskCompletionSource<string>> _taskQueue;
        private readonly ConnectionContext _connection;
        public readonly PipeWriter _sender;
        private readonly PipeReader _reciver;
        public NewRedisClient(string ip, int port) : this(new IPEndPoint(IPAddress.Parse(ip), port))
        {
        }
        public NewRedisClient(IPEndPoint point)
        {
            _taskQueue = new ConcurrentQueue<TaskCompletionSource<string>>();
            SocketConnectionFactory client = new SocketConnectionFactory(new SocketTransportOptions());
            _connection = client.ConnectAsync(point).Result;
            _sender = _connection.Transport.Output;
            _reciver = _connection.Transport.Input;
            RunReciver();
        }

        private async void RunReciver()
        {

            while (true)
            {
                var result = await _reciver.ReadAsync();
                var buffer = result.Buffer;
                if (!buffer.IsSingleSegment)
                {
                    Handler(buffer.FirstSpan);
                }
                else
                {
                    Handler(result.Buffer);
                }
                _reciver.AdvanceTo(result.Buffer.End);
            }
        }


        private void Handler(in ReadOnlySequence<byte> sequence)
        {
            TaskCompletionSource<string> task;
            var reader = new SequenceReader<byte>(sequence);
            if (reader.TryReadTo(out ReadOnlySpan<byte> result, 43, advancePastDelimiter: true))
            {
                while (reader.TryReadTo(out result, 43, advancePastDelimiter: true))
                {

                    while (!_taskQueue.TryDequeue(out task)) { }
                    task.SetResult(Encoding.UTF8.GetString(result));
                }
            }
            while (!_taskQueue.TryDequeue(out task)) { }
            task.SetResult(Encoding.UTF8.GetString(sequence.Slice(reader.Position, sequence.End).ToArray()));
        }

        private void Handler(in ReadOnlySpan<byte> span)
        {
            var tempSpan = span;
            TaskCompletionSource<string> task;
            //var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(span));
            var offset = tempSpan.IndexOf((byte)43);
            while (offset != -1)
            {
                if (offset != 0)
                {
                    while (!_taskQueue.TryDequeue(out task)) { }
                    task.SetResult(Encoding.UTF8.GetString(tempSpan.Slice(0, offset)));
                }
                tempSpan = tempSpan.Slice(offset + 1, tempSpan.Length - offset - 1);
                offset = tempSpan.IndexOf((byte)43);
            }
            while (!_taskQueue.TryDequeue(out task)) { }
            task.SetResult(Encoding.UTF8.GetString(tempSpan.Slice(0, tempSpan.Length)));

        }



        private Task<string> SendAsync(string commond)
        {
            //lock (_lock)
            //{
                var content = Encoding.UTF8.GetBytes(commond);
                var taskSource = new TaskCompletionSource<string>();
                AddSendAndReciverTask(content, taskSource);
                return taskSource.Task;
            //}


        }
        private Task<string> SendAsync(List<object> command)
        {
            //lock (_lock)
            //{
                var taskSource = new TaskCompletionSource<string>();
                using (var ms = new MemoryStream())
                {
                    new FreeRedis.RespHelper.Resp3Writer(ms, null, FreeRedis.RedisProtocol.RESP2).WriteCommand(command);
                    AddSendAndReciverTask(ms.ToArray(), taskSource);
                    ms.Close();
                }
                return taskSource.Task;
            //}
        }
        private readonly object _lock = new object();
        private async void AddSendAndReciverTask(byte[] content, TaskCompletionSource<string> task)
        {
            //try
            //{

                await _sender.WriteAsync(content);
                _taskQueue.Enqueue(task);
                //_sender.Advance(content.Length);

            //}
            //catch (Exception ex)
            //{
                //File.WriteAllText("1.txt", ex.StackTrace);
                //Console.WriteLine(ex);
            //}

        }

        public async Task<bool> AuthAsync(string password)
        {
            var result = await SendAsync($"AUTH {password}\r\n");
            return result == "OK\r\n";
        }

        public async Task<bool> SelectDB(int dbIndex)
        {
            var result = await SendAsync($"SELECT {dbIndex}\r\n");
            return result == "OK\r\n";
        }
        public async Task<bool> Set(string key, string value)
        {
            var result = await SendAsync(new List<object> { "SET", key, value });
            return result == "OK\r\n";
            //var result = await SendAsync($"SET {key} {value}\r\n");
            //return result == "OK\r\n";
        }
        public async Task<string> Get(string key)
        {
            var result = await SendAsync(new List<object> { "GET", key });
            return result;
            //var result = await SendAsync($"SET {key} {value}\r\n");
            //return result == "OK\r\n";
        }
        public async Task<bool> PingAsync()
        {
            return await SendAsync($"PING\r\n") == "PONG\r\n";
        }
    }
}
