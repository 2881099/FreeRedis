using BeetleX.Clients;
using console_netcore31_newsocket.Client.Utils;
//using NetCoreServer;
using System;
using System.Buffers;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{


    public class NewRedisClient28 : AsyncTcpClient
    {

        private readonly byte _protocalStart;
        public readonly CircleTaskBuffer5<bool> _taskBuffer;
        public NewRedisClient28(string ip,int port)
        {
            this.LocalEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            this.Connect(out _);
             _protocalStart = (byte)43;
            _taskBuffer = new CircleTaskBuffer5<bool>();
        }
#if DEBUG
        public void Clear()
        {
            _taskBuffer.Clear();
        }
#endif

        //protected override void OnConnected()
        //{
        //    Console.WriteLine($"Chat TCP client connected a new session with Id {Id}");
        //}
        //protected override void OnError(System.Net.Sockets.SocketError error)
        //{
        //    Console.WriteLine($"Chat TCP client caught an error with code {error}");
        //}
        private int _send_lock_flag;

        public bool IsCompleted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _send_lock_flag != 1;

            }
        }
        public int LockCount;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void LockSend()
        {

            SpinWait wait = default;
            while (Interlocked.CompareExchange(ref _send_lock_flag, 1, 0) != 0)
            {
                wait.SpinOnce();
            }

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetSendLock()
        {
            return Interlocked.CompareExchange(ref _send_lock_flag, 1, 0) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseSend()
        {

            _send_lock_flag = 0;

        }


        public Task<bool> AuthAsync(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return default;
            }
            var bytes = Encoding.UTF8.GetBytes($"AUTH {password}\r\n");
            LockSend();
            this.SendAsync($"AUTH {password}\r\n");
            var task = _taskBuffer.WriteNext();
            ReleaseSend();
            return task.Task;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public Task<bool> SetAsync(string key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes($"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n");
            LockSend();
            this.SendAsync(bytes);
            var task = _taskBuffer.WriteNext();
            ReleaseSend();
            return task.Task;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public Task<bool> SetAsync(byte[] bytes)
        {
            LockSend();
            this.SendAsync(bytes);
            var task = _taskBuffer.WriteNext();
            ReleaseSend();
            return task.Task;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public Task<bool> SetAsyncWithoutLock(byte[] bytes)
        {
            this.SendAsync(bytes);
            var task = _taskBuffer.WriteNext();
            ReleaseSend();
            return task.Task;
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            Handler(buffer.AsSpan().Slice((int)offset,(int)size));
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected internal void Handler(in ReadOnlySequence<byte> sequence)
        {

            foreach (ReadOnlyMemory<byte> segment in sequence)
            {
                var span = segment.Span;
                var position = span.IndexOf(_protocalStart);
                while (position != -1)
                {
                    _taskBuffer.ReadNext(true);
                    if (position == span.Length - 1)
                    {
                        break;
                    }
                    span = span.Slice(position + 1);
                    position = span.IndexOf(_protocalStart);

                }
            }
            //Console.WriteLine(1);

        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected internal void Handler(in ReadOnlySpan<byte> sequence)
        {

            //第一个节点是有用的节点
            var span = sequence;
            var position = span.IndexOf(_protocalStart);
            while (position != -1)
            {

                _taskBuffer.ReadNext(true);
                if (position == span.Length - 1)
                {
                    break;
                }
                span = span.Slice(position + 1);
                position = span.IndexOf(_protocalStart);

            }
            //Console.WriteLine(1);
        }
    }

}
