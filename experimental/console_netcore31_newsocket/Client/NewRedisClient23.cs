﻿using console_netcore31_newsocket.Client.Utils;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{


    public class NewRedisClient23 : RedisClientBase5
    {

        private readonly byte _protocalStart;
        private readonly CircleTaskBuffer3<bool> _taskBuffer;
        public NewRedisClient23()
        {
            _taskBuffer = new CircleTaskBuffer3<bool>();
            _protocalStart = (byte)43;
        }
        public Task<bool> AuthAsync(string password)
        {
            if (password == null)
            {
                return default;
            }
            var bytes = Encoding.UTF8.GetBytes($"AUTH {password}\r\n");
            LockSend();
            _sender.WriteAsync(bytes);
            var task = _taskBuffer.WriteNext();
            ReleaseSend();
            return task.Task;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public Task<bool> SetAsync(string key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes($"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n");
            LockSend();
            var task = _taskBuffer.WriteNext();
            _sender.WriteAsync(bytes);
            ReleaseSend();
            return task.Task;
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

        }
    }

}