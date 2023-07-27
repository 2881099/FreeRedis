using FreeRedis.Client.Protocol;
using FreeRedis.Engine;
using System.Text.Json;

namespace FreeRedis
{


    public sealed class FreeRedisClient2 : FreeRedisClientBase2
    {
        public FreeRedisClient2(ConnectionStringBuilder connectionString, Action<string>? logger = null) : base(connectionString.Ip, connectionString.Port, logger)
        {
            try
            {
   
                string password = connectionString.Password;
                if (password !=  string.Empty)
                {
                    if (!this.AuthAsync(password).Result)
                    {
                        throw new Exception("Reids 服务器密码不正确!");
                    }
                }

                int dbIndex = connectionString.Database;
                if (!this.SelectDbAsync(dbIndex).Result)
                {
                    throw new Exception($"Reids 服务器选择数据库出错,数据库索引{dbIndex}!");
                }

            }
            catch (Exception ex)
            {
                throw new Exception($"创建链接遇到问题:{ex.Message!}");
                DisposeAsync();
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

        
        public Task<bool> SelectDbAsync(int dbIndex)
        {
            var selectHandler = new SelectProtocol(dbIndex, errorLogger);
            return SendProtocal(selectHandler);
        }
        public Task<bool> FlushDbAsync()
        {
            var flushHandler = new FlushProtocol(errorLogger);
            return SendProtocal(flushHandler);
        }
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

        public Task<string?> GetAsync(string key)
        {
            var getHandler = new GetProtocol(key, errorLogger);
            return SendProtocal(getHandler);
        }

        //public Task<TryResult<T>> TryGetAsync<T>(string key)
        //{
        //    var getHandler = new TryGetProtocol<T>(key, errorLogger);
        //    return SendProtocal(getHandler);
        //}

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
