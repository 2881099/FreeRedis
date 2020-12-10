using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{



    public class NewRedisClient19 : RedisClientBase2
    {

        private readonly byte _protocalStart;
        private readonly SingleLinks2<bool> _taskBuffer;
        private readonly SingleLinks2<bool> _tempBuffer;
        private readonly SingleLinkNode2<bool> _head;
        private readonly MemoryStream _memeory;
        private const int BackupBufferLength = 4 * 1024 * 1024;
        public NewRedisClient19()
        {
            _memeory = new MemoryStream(BackupBufferLength);
            _taskBuffer = new SingleLinks2<bool>();
            _tempBuffer = new SingleLinks2<bool>();
            _head = _taskBuffer.Head;
            _protocalStart = (byte)43;
        }
        public Task<bool> AuthAsync(string password)
        {
            var bytes = Encoding.UTF8.GetBytes($"AUTH {password}\r\n");
            var task = CreateTask(null, TaskCreationOptions.RunContinuationsAsynchronously);
            LockSend();
            _taskBuffer.Append(task);
            _sender.WriteAsync(bytes);
            ReleaseSend();
            return task;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override Task<bool> SetAsync(string key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes($"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n");
            var task = CreateTask(null, TaskCreationOptions.RunContinuationsAsynchronously);
            SpinWait wait = default;
            if (!TryGetSendLock())
            {
                wait.SpinOnce();
                if (!TryGetSendLock())
                {
                    if (bytes.Length + _memeory.Position < BackupBufferLength)
                    {
                        _memeory.Write(bytes);
                        _tempBuffer.Append(task);
                    }
                    return task;
                }

            }
            if (_memeory.Position > 0)
            {
                var array = _memeory.GetBuffer().AsSpan().Slice(0, (int)_memeory.Position).ToArray();
                _sender.WriteAsync(new ReadOnlyMemory<byte>(array));
                _memeory.Position = 0;
            }
            _taskBuffer.Append(task);
            _sender.WriteAsync(bytes);


            ReleaseSend();
            return task;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void SetAsync(byte[] bytes, Task<bool> task)
        {
            _taskBuffer.Append(task);
            _sender.WriteAsync(bytes);
            ReleaseSend();
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void SetAndWaitAsync(byte[] bytes, Task<bool> task)
        {

            LockSend();
            _taskBuffer.Append(task);
            _sender.WriteAsync(bytes);
            ReleaseSend();

        }


        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected internal override void Handler(in ReadOnlySequence<byte> sequence)
        {

            //第一个节点是有用的节点

            var tempTail = _head;
            if (sequence.Length < 60)
            {

                foreach (ReadOnlyMemory<byte> segment in sequence)
                {
                    var span = segment.Span;
                    var position = span.IndexOf(_protocalStart);
                    while (position != -1)
                    {

                        tempTail = tempTail.Next;
                        tempTail.Completed(true);

                        if (position == span.Length - 1)
                        {
                            break;
                        }
                        span = span.Slice(position + 1);
                        position = span.IndexOf(_protocalStart);

                    }
                }
                LockSend();
                _taskBuffer.ClearBefore(tempTail);
                ReleaseSend();
            }
            else
            {
                var tempParameters = _head.Next;
                int step = 0;

                foreach (ReadOnlyMemory<byte> segment in sequence)
                {
                    var span = segment.Span;
                    var position = span.IndexOf(_protocalStart);
                    while (position != -1)
                    {

                        step += 1;
                        tempTail = tempTail.Next;
                        tempTail.Result = true;

                        if (position == span.Length - 1)
                        {
                            break;
                        }
                        span = span.Slice(position + 1);
                        position = span.IndexOf(_protocalStart);

                    }
                }

                if (step == 0)
                {
                    return;
                }

                LockSend();
                /*
                 * Keep 'tempTail' is the final result.
                 * 
                 * Head                        Tail       Tail
                 *  ↓                           ↓1         ↓2       
                 * Head -> node1 -> ..... -> tempTail ... End ....   
                 *         |________________________|
                 *
                 * ------------------------------------------
                 * |                Tail1                   |
                 * |                                        |
                 * |  Head Tail                             |
                 * |      ↓                                 |
                 * |     Head -> node1 -> ..... -> tempTail |  
                 * |             |________________________| |
                 * |                                        |
                 * |----------------------------------------|             
                 * |                Tail2                   |
                 * |                                        |
                 * |    Head                          Tail  |
                 * |     ↓                              ↓   |
                 * |    Head -> node1 ->  tempTail ... End  |
                 * |            |________________|          |
                 * |                                        |
                 * |________________________________________|
                */
                _taskBuffer.ClearBefore(tempTail);
                ReleaseSend();

                ThreadPool.QueueUserWorkItem(new WaitCallback((state) => { HandlerResult(); }));
                void HandlerResult()
                {
                    while (step != 0)
                    {
                        tempParameters.Completed();
                        tempParameters = tempParameters.Next;
                        step -= 1;
                    }
                }

            }


        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected internal override void Handler(in ReadOnlySpan<byte> sequence)
        {

            //第一个节点是有用的节点

            var tempTail = _head;
            if (sequence.Length < 60)
            {


                var span = sequence;
                var position = span.IndexOf(_protocalStart);
                while (position != -1)
                {

                    tempTail = tempTail.Next;
                    tempTail.Completed(true);

                    if (position == span.Length - 1)
                    {
                        break;
                    }
                    span = span.Slice(position + 1);
                    position = span.IndexOf(_protocalStart);

                }
                LockSend();
                _taskBuffer.ClearBefore(tempTail);
                ReleaseSend();
            }
            else
            {
                var tempParameters = _head.Next;
                int step = 0;

                var span = sequence;
                var position = span.IndexOf(_protocalStart);
                while (position != -1)
                {

                    step += 1;
                    tempTail = tempTail.Next;
                    tempTail.Result = true;

                    if (position == span.Length - 1)
                    {
                        break;
                    }
                    span = span.Slice(position + 1);
                    position = span.IndexOf(_protocalStart);

                }

                if (step == 0)
                {
                    return;
                }

                LockSend();
                /*
                 * Keep 'tempTail' is the final result.
                 * 
                 * Head                        Tail       Tail
                 *  ↓                           ↓1         ↓2       
                 * Head -> node1 -> ..... -> tempTail ... End ....   
                 *         |________________________|
                 *
                 * ------------------------------------------
                 * |                Tail1                   |
                 * |                                        |
                 * |  Head Tail                             |
                 * |      ↓                                 |
                 * |     Head -> node1 -> ..... -> tempTail |  
                 * |             |________________________| |
                 * |                                        |
                 * |----------------------------------------|             
                 * |                Tail2                   |
                 * |                                        |
                 * |    Head                          Tail  |
                 * |     ↓                              ↓   |
                 * |    Head -> node1 ->  tempTail ... End  |
                 * |            |________________|          |
                 * |                                        |
                 * |________________________________________|
                */
                _taskBuffer.ClearBefore(tempTail);
                ReleaseSend();

                ThreadPool.QueueUserWorkItem(new WaitCallback((state) => { HandlerResult(); }));
                void HandlerResult()
                {
                    while (step != 0)
                    {
                        tempParameters.Completed();
                        tempParameters = tempParameters.Next;
                        step -= 1;
                    }
                }

            }


        }
    }



}
