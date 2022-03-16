using FreeRedis.Client.Protocol;
using FreeRedis.Engine;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace FreeRedis
{


    public class FreeRedisClient : FreeRedisClientBase
    {
        public FreeRedisClient(ConnectionStringBuilder connectionString, Action<string>? logger = null) : base(logger)
        {
            try
            {
                //Todo Analysis connectionString
                var host = connectionString.Host.Split(':');
                string ip = host[0];
                this.CreateConnection(host[0], Convert.ToInt32(host[1]));
                    
                string password = connectionString.Password;
                if (password !=  string.Empty)
                {
                    AuthAsync("123");
                    Thread.Sleep(3000);
                    var result = this.AuthAsync(password).Result;
                    if (!result)
                    {
                        throw new Exception("Reids 服务器密码不正确!");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"创建链接遇到问题:{ex.Message!}");
                DisposeAsync().ConfigureAwait(false);
            }
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
            var authHandler = new AuthProtocol(password, errorLogger);
            return SendProtocal(authHandler);
        }

        public Task<bool> SetObjAsync<T>(string key, T value) 
        {
            return SetAsync(key, JsonSerializer.Serialize(value));
        }
        public Task<bool> SetAsync(string key, string value)
        {
            var setHandler = new SetProtocol(key, value, errorLogger);
            return SendProtocal(setHandler);
        }

        public Task<T?> GetAsync<T>(string key)
        {
            var getHandler = new GetProtocol<T>(key, errorLogger);
            return SendProtocal(getHandler);
        }

        public Task<TryResult<T>> TryGetAsync<T>(string key)
        {
            var getHandler = new TryGetProtocol<T>(key, errorLogger);
            return SendProtocal(getHandler);
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
