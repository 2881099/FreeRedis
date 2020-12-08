using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{



    public class NewRedisClient15 : RedisClientBase
    {

        private readonly byte _protocalStart;
        private readonly SingleLink3<bool> _taskBuffer;
        private readonly SingleLinkNode3<bool> _head;
        private readonly SemaphoreSlim _lock;
        public NewRedisClient15()
        {
            _taskBuffer = new SingleLink3<bool>();
            _lock = new SemaphoreSlim(1);
            _head = _taskBuffer.Head;
            _preNode = _head;
            _protocalStart = (byte)43;
        }
        public Task<bool> AuthAsync(string password)
        {
            var bytes = Encoding.UTF8.GetBytes($"AUTH {password}\r\n");
            var task = new TaskCompletionSource<bool>(null, TaskCreationOptions.RunContinuationsAsynchronously);
            LockSend();
            _taskBuffer.Append(task);
            _sender.WriteAsync(bytes);
            _lock.Release();
            return task.Task;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override Task<bool> SetAsync(string key, string value)
        {
            var bytes = Encoding.UTF8.GetBytes($"*3\r\n$3\r\nSET\r\n${key.Length}\r\n{key}\r\n${value.Length}\r\n{value}\r\n");
            var task = new TaskCompletionSource<bool>(null, TaskCreationOptions.RunContinuationsAsynchronously);
            LockSend();
            _taskBuffer.Append(task);
            _sender.WriteAsync(bytes);
            ReleaseSend();
            return task.Task;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void SetAsync(byte[] bytes, TaskCompletionSource<bool> task)
        {
            _taskBuffer.Append(task);
            _sender.WriteAsync(bytes);
            ReleaseSend();
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void SetAndWaitAsync(byte[] bytes, TaskCompletionSource<bool> task)
        {

            LockSend();
            _taskBuffer.Append(task);
            _sender.WriteAsync(bytes);
            ReleaseSend();

        }

        private int count = -1;
        private int dealCount = -1;
        private string last;
        private int _linkLockCount;
        private SingleLinkNode3<bool> _preNode;
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected internal override void Handler(in ReadOnlySequence<byte> sequence)
        {
            //Interlocked.Increment(ref dealCount);
            //HandlerCount += 1;
            //GetTaskSpan();
            //Console.WriteLine(sequence.Length);
            int step = 0;
            //第一个节点是有用的节点
            var tempParameters = _preNode.Next;
            var tempTail = _preNode;
            //int t = 0;
            foreach (ReadOnlyMemory<byte> segment in sequence)
            {
                //t += 1;
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
            //if (count < t)
            //{
            //    count = t;
            //    Console.WriteLine(t);
            //}
           
            //}
            //catch (Exception)
            //{
            //    File.WriteAllText("1.txt", $"Step{ step }, Content:{ sequence.ToArray() }");
            //    Console.ReadKey();
            //}

            //if (step * 5 != sequence.Length)
            //{
            //    Console.WriteLine($"Protocl Length: {sequence.Length} : { step * 5 == sequence.Length} Now Need to handle {step}!");
            //    //Console.WriteLine($"RandomContent:{ Encoding.UTF8.GetString(sequence.Slice(sequence.Length / 2, sequence.Length / 2 - 1).ToArray()) }");
            //    Console.WriteLine($"Last Six Content:{ Encoding.UTF8.GetString(sequence.Slice(sequence.Length - 6, 6).ToArray()) }");
            //    Console.ReadKey();
            //}
            if (step == 0)
            {
                // nianbao
                //Console.WriteLine(last);
                //var array = sequence.ToArray();
                //for (int i = 0; i < sequence.Length; i++)
                //{

                //    Console.Write(array[i]);

                //}
                //Console.ReadKey();
                return;
            }
            //last = Encoding.UTF8.GetString(sequence.Slice(sequence.Length - 4, 4).ToArray());
            if (_linkLockCount > 10000)
            {
                _linkLockCount = 0;
                LockSend();
                _taskBuffer.ClearBefore(tempTail);
                ReleaseSend();
                _preNode = _head;
            }
            else
            {
                
                if (TryGetSendLock())
                {
                    _linkLockCount = 0;
                    _taskBuffer.ClearBefore(tempTail);
                    _preNode = _head;
                    ReleaseSend();
                }
                else
                {
                    _linkLockCount += step;
                    _preNode = tempTail;
                }

            }

            //LockSend();
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
            //_taskBuffer.ClearBefore(tempTail);
            //ReleaseSend();
            if (step < 4)
            {
                //Console.WriteLine("InStep:"+step);
                //try
                //{
                //if (step == 0)
                //{
                //    Console.WriteLine(sequence.Length);
                //    Console.WriteLine(step);
                //    var array = sequence.ToArray();
                //    Console.WriteLine($"15 Content:{ Encoding.UTF8.GetString(sequence.Slice(0, 15).ToArray()) }");
                //    Console.ReadKey();
                //}
                //while (temp != 0)
                //{
                //    //Interlocked.Increment(ref count);
                //    tempParameters.Completed();
                //    tempParameters = tempParameters.Next;
                //    temp -= 1;
                //}
                //return;
                //  p
                //  ↓
                //  * -> * -> * -> *
                //Interlocked.Increment(ref count);
                //Console.WriteLine("Do!");
                tempParameters.Completed();
                step -= 1;
                while (step != 0)
                {
                    // Console.WriteLine($"Do!{step}");
                    //       p →
                    //       ↓
                    //  * -> * -> * -> *
                    tempParameters = tempParameters.Next;
                    tempParameters.Completed();
                    //Interlocked.Increment(ref count);
                    step -= 1;
                }
                //Console.WriteLine(tempParameters.Next == null);
                //}
                //catch (Exception ex)
                //{

                //    File.WriteAllText("1.txt", $"Length {sequence.Length },Step{ step }, Content:{ Encoding.UTF8.GetString(sequence.ToArray()) }");
                //    if (sequence.Length == 1)
                //    {
                //        Console.WriteLine(sequence.ToArray()[0]);
                //    }
                //    Console.WriteLine("出错" + Encoding.UTF8.GetString(sequence.ToArray()));
                //    Console.ReadKey();
                //}

            }
            else
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback((state) => { HandlerResult(tempParameters, step); }));
                void HandlerResult(SingleLinkNode3<bool> head, int top)
                {
                    while (top != 0)
                    {
                        head.Completed();
                        head = head.Next;
                        top -= 1;
                    }
                }
            }



        }


    }



}
