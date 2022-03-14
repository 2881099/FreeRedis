using console_netcore31_newsocket.Client.Utils;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{


    public class NewRedisClient24 : RedisClientBase5
    {

        private readonly byte _protocalStart;
        private readonly CircleTaskBuffer2<bool> _taskBuffer;
        private readonly NewRedisClient22 _other;
        public NewRedisClient24()
        {
            _taskBuffer = new CircleTaskBuffer2<bool>();
            _protocalStart = (byte)43;
            _other = new NewRedisClient22();


        }
        public override void CreateConnection(string ip, int port)
        {
            _other.CreateConnection(ip, port);
            base.CreateConnection(ip, port);
        }
        public ValueTask<bool> AuthAsync(string password)
        {
            _other.AuthAsync(password);
            if (password == null)
            {
                return default;
            }
            var bytes = Encoding.UTF8.GetBytes($"AUTH {password}\r\n");
            ManualResetValueTaskSource<bool> task = null;
            LockSend();
            _sender.WriteAsync(bytes);
            task = _taskBuffer.WriteNext();
            ReleaseSend();
            return task.AwaitableTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public ValueTask<bool> SetAsync(string key, string value)
        {
            if (TryGetSendLock())
            {
                var bytes = Encoding.UTF8.GetBytes($"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n");
                //LockSend();
                var task = _taskBuffer.WriteNext();
                _sender.WriteAsync(bytes);
                ReleaseSend();
                return task.AwaitableTask;
            }
            else
            {
               return _other.SetAsync(key, value);
            }
           
        }




        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected internal override void Handler(in ReadOnlySequence<byte> sequence)
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
        protected internal override void Handler(in ReadOnlySpan<byte> sequence)
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
