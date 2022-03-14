using FreeRedis.Client.Protocal;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace FreeRedis.Engine
{


    public class FreeRedisClient : FreeRedisClientBase
    {

        private readonly byte _protocalStart;
        public FreeRedisClient(string connectionString)
        {
           

            try
            {
                //Todo Analysis connectionString
                string ip = default!;
                int port = default!;
                this.CreateConnection(ip, port);

                string? password = default!;
                if (password != null)
                {
                    var result = this.AuthAsync(password).Result;
                    if (!result)
                    {
                        throw new Exception("Reids服务器密码不正确!");
                    }
                }
            }
            catch (Exception ex)
            {

                throw new Exception($"创建链接遇到问题:{ex.Message!}");
            }
            finally
            {
                this.DisposeAsync();
            }
             _protocalStart = (byte)43;
        }
#if DEBUG
        //public void Clear()
        //{
        //    _taskBuffer.Clear();
        //}
#endif
        //protected override void Init()
        //{
        //    _taskBuffer = new();
        //}


        public Task<bool> AuthAsync(string password)
        {
            var authHandler = new AuthProtocal(password);
            return SendProtocal(authHandler);
        }

        public Task<bool> SetAsync(string key, string value)
        {
            var setHandler = new SetProtocal(key, value);
            return SendProtocal(setHandler);
        }

       

        //private int _revc_offset;
        ////从返回流中分割获取 Redis 结果.
        //[MethodImpl(MethodImplOptions.AggressiveOptimization)]
        //protected override void Handler(in ReadOnlySequence<byte> sequence)
        //{
        //    _taskBuffer.ReadNext(in sequence, ref _revc_offset);
        //    foreach (ReadOnlyMemory<byte> segment in sequence)
        //    {
        //        _taskBuffer.ReadNext(ReadOnlyMemory<byte>)
        //        var span = segment.Span;
        //        //var position = span.IndexOf(_protocalStart);
        //        //while (position != -1)
        //        //{
        //        //    _taskBuffer.ReadNext(null);
        //        //    if (position == span.Length - 1)
        //        //    {
        //        //        break;
        //        //    }
        //        //    span = span.Slice(position + 1);
        //        //    position = span.IndexOf(_protocalStart);

        //        //}
        //    }
        //    //Console.WriteLine(1);

        //}

        //[MethodImpl(MethodImplOptions.AggressiveOptimization)]
        //protected internal override void Handler(in ReadOnlySpan<byte> sequence)
        //{

        //    //第一个节点是有用的节点
        //    var span = sequence;
        //    var position = span.IndexOf(_protocalStart);
        //    while (position != -1)
        //    {

        //        _taskBuffer.ReadNext(null);
        //        if (position == span.Length - 1)
        //        {
        //            return;
        //        }
        //        span = span.Slice(position + 1);
        //        position = span.IndexOf(_protocalStart);

        //    }
        //    //Console.WriteLine(1);
        //}
    }

}
