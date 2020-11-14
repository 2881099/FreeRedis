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
        private readonly PipeWriter _sender;
        private readonly PipeReader _reciver;
        public NewRedisClient(string ip, int port) :this(new IPEndPoint(IPAddress.Parse(ip), port))
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
                var array = result.Buffer.ToArray();
                Handler(array.AsSpan());
                _reciver.AdvanceTo(result.Buffer.End);
            }
        }


        private void Handler(ReadOnlySpan<byte> span)
        {
            TaskCompletionSource<string> task;
            var offset = span.IndexOf((byte)43);
            while (offset != -1)
            {
                if (offset != 0)
                {
                    while (!_taskQueue.TryDequeue(out task)) { }
                    task.SetResult(Encoding.UTF8.GetString(span.Slice(0, offset)));
                }
                span = span.Slice(offset + 1, span.Length - offset - 1);
                offset = span.IndexOf((byte)43);
            }
            while (!_taskQueue.TryDequeue(out task)) { }
            task.SetResult(Encoding.UTF8.GetString(span.Slice(0, span.Length)));
        }



        private Task<string> SendAsync(string commond)
        {
            var content = Encoding.UTF8.GetBytes(commond);
            var taskSource = new TaskCompletionSource<string>();
            AddSendAndReciverTask(content, taskSource);
            return taskSource.Task;
        }
        private Task<string> SendAsync(object[] command)
        {
            var taskSource = new TaskCompletionSource<string>();
            using (var ms = new MemoryStream())
            {
                new FreeRedis.RespHelper.Resp3Writer(ms, null, FreeRedis.RedisProtocol.RESP2);
                AddSendAndReciverTask(ms.ToArray(), taskSource);
                ms.Close();
            }
            return taskSource.Task;
        }

        private async void AddSendAndReciverTask(byte[] content, TaskCompletionSource<string> task)
        {
            await _sender.WriteAsync(content);
            _taskQueue.Enqueue(task);
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
        public async Task<bool> Set(string key,string value)
        {
            var result = await SendAsync(new object[] { "SET", key, value });
            return result == "OK\r\n";
            //var result = await SendAsync($"SET {key} {value}\r\n");
            //return result == "OK\r\n";
        }
        public async Task<string> Get(string key)
        {
            var result = await SendAsync(new object[] { "GET", key });
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
