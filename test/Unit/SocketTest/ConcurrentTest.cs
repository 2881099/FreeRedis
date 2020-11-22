using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SocketTest
{
    public class ConcurrentTest
    {
        [Fact(DisplayName = "测试 新并发队列 同步场景准确性")]
        public void Test()
        {

            NewConcurrentQueue<int> newQueue = new NewConcurrentQueue<int>(default);
            for (int i = 0; i < 100; i++)
            {
                newQueue.Enqueue(i);
            }
            Assert.Equal(100, newQueue.Count);
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(i, newQueue.Dequeue());
            }

        }
        [Fact(DisplayName = "测试 新并发队列 并发长江准确性")]
        public void Test2()
        {
            ConcurrentQueue<int> queue = new ConcurrentQueue<int>();
            NewConcurrentQueue<int> newQueue = new NewConcurrentQueue<int>(default);
            Parallel.For(0,1000,(i)=>{
                queue.Enqueue(i);
                newQueue.Enqueue(i);
            });
            Assert.Equal(1000, queue.Count);
            Assert.Equal(1000, newQueue.Count);
            Parallel.For(0, 1000, (i) => {
                queue.TryDequeue(out var _);
                newQueue.Dequeue();
            });
            Assert.Equal(0, queue.Count);
            Assert.Equal(0, newQueue.Count);
            //for (int i = 0; i < 100; i++)
            //{
            //    queue.TryDequeue(out int result);
            //    Assert.Equal(result, newQueue.Dequeue());
            //}
        }
    }
}
