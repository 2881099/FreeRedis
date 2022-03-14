using console_netcore31_newsocket.Client.Utils;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{


    public class NewRedisClient25 : RedisClientBase5
    {

        private readonly byte _protocalStart;
        public CircleTaskBuffer4<bool> _taskBuffer;
        public NewRedisClient25()
        {
           
            _protocalStart = (byte)43;
        }
        public void Clear()
        {
            _taskBuffer.Clear();
        }
        protected override void Init()
        {
            _taskBuffer = new CircleTaskBuffer4<bool>();
        }

        public bool Pause;

        public ValueTask<bool> AuthAsync(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return default;
            }
            var bytes = Encoding.UTF8.GetBytes($"AUTH {password}\r\n");
            LockSend();
            _sender.WriteAsync(bytes);
            var task = _taskBuffer.WriteNext();
            ReleaseSend();
            return task.AwaitableTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public ValueTask<bool> SetAsync(string key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes($"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n");
            LockSend();
            _sender.WriteAsync(bytes);
            var task = _taskBuffer.WriteNext();
            ReleaseSend();
            return task.AwaitableTask;
        }


        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected internal override void Handler(in ReadOnlySequence<byte> sequence)
        {
            
            foreach (ReadOnlyMemory<byte> segment in sequence)
            {
                if (Pause)
                {
                    Console.WriteLine("出现未处理的数据！");
                }
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
