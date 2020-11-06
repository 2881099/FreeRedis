using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace FreeRedis
{
    partial class RedisClient
    {
        class PoolingAdapter : BaseAdapter
        {
            readonly IdleBus<RedisClientPool> _ib;
            readonly string _masterHost;
            readonly bool _rw_splitting;
            readonly bool _is_single;

            public PoolingAdapter(RedisClient topOwner, ConnectionStringBuilder connectionString, params ConnectionStringBuilder[] slaveConnectionStrings)
            {
                UseType = UseType.Pooling;
                TopOwner = topOwner;
                _masterHost = connectionString.Host;
                _rw_splitting = slaveConnectionStrings?.Any() == true;
                _is_single = !_rw_splitting && connectionString.MaxPoolSize == 1;

                _ib = new IdleBus<RedisClientPool>(TimeSpan.FromMinutes(10));
                _ib.Register(_masterHost, () => new RedisClientPool(connectionString, null, TopOwner));

                if (_rw_splitting)
                    foreach (var slave in slaveConnectionStrings)
                        _ib.TryRegister($"slave_{slave.Host}", () => new RedisClientPool(slave, null, TopOwner));
            }

            public override void Dispose()
            {
                _ib.Dispose();
            }

            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                var poolkey = GetIdleBusKey(cmd);
                var pool = _ib.Get(poolkey);
                var cli = pool.Get();
                var rds = cli.Value.Adapter.GetRedisSocket(null);
                var rdsproxy = DefaultRedisSocket.CreateTempProxy(rds, () => pool.Return(cli));
                rdsproxy._pool = pool;
                return rdsproxy;
            }
            public override TValue AdapterCall<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                return TopOwner.LogCall(cmd, () =>
                {
                    RedisResult rt = null;
                    RedisClientPool pool = null;
                    Exception ioex = null;
                    using (var rds = GetRedisSocket(cmd))
                    {
                        pool = (rds as DefaultRedisSocket.TempProxyRedisSocket)._pool;
                        try
                        {
                            rds.Write(cmd);
                            rt = rds.Read(cmd._flagReadbytes);
                        }
                        catch (Exception ex)
                        {
                            ioex = ex;
                        }
                    }
                    if (ioex != null)
                    {
                        if (pool?.SetUnavailable(ioex) == true)
                        {
                        }
                        throw ioex;
                    }
                    rt.IsErrorThrow = TopOwner._isThrowRedisSimpleError;
                    return parse(rt);
                });
            }
#if isasync
            List<AsyncRedisSocket> _asyncRedisSockets = new List<AsyncRedisSocket>();
            int _asyncRedisSocketsCount = 0;
            long _asyncRedisSocketsConcurrentCounter = 0;
            object _asyncRedisSocketsLock = new object();
            AsyncRedisSocket GetAsyncRedisSocket(CommandPacket cmd)
            {
                AsyncRedisSocket asyncRds = null;
                Interlocked.Increment(ref _asyncRedisSocketsConcurrentCounter);
                for (var limit = 0; limit < 1000; limit += 1)
                {
                    if (_asyncRedisSocketsCount > 0)
                    {
                        lock (_asyncRedisSocketsLock)
                        {
                            if (_asyncRedisSockets.Count > 0)
                            {
                                asyncRds = _asyncRedisSockets[_rnd.Value.Next(_asyncRedisSockets.Count)];
                                Interlocked.Increment(ref asyncRds._writeCounter);
                                Interlocked.Decrement(ref _asyncRedisSocketsConcurrentCounter);
                                return asyncRds;
                            }
                        }
                    }
                    if (limit > 50 && _asyncRedisSocketsConcurrentCounter < 2) break;
                    if (_asyncRedisSocketsCount > 1) Thread.CurrentThread.Join(2);
                }
                NewAsyncRedisSocket();
                //AsyncRedisSocket.sb.AppendLine($"线程{Thread.CurrentThread.ManagedThreadId}：AsyncRedisSockets 数量 {_asyncRedisSocketsCount} {_asyncRedisSocketsConcurrentCounter}");
                Interlocked.Decrement(ref _asyncRedisSocketsConcurrentCounter);
                return asyncRds;

                void NewAsyncRedisSocket()
                {
                    var rds = GetRedisSocket(cmd);
                    var key = Guid.NewGuid();
                    asyncRds = new AsyncRedisSocket(rds, () =>
                    {
                        if (_asyncRedisSocketsConcurrentCounter > 0 || _asyncRedisSocketsCount > 1) Thread.CurrentThread.Join(8);
                        Interlocked.Decrement(ref _asyncRedisSocketsCount);
                        lock (_asyncRedisSocketsLock)
                            _asyncRedisSockets.Remove(asyncRds);

                    }, (innerRds, ioex) =>
                    {
                        if (ioex != null) (rds as DefaultRedisSocket.TempProxyRedisSocket)._pool.SetUnavailable(ioex);
                        innerRds.Dispose();
                    }, () =>
                    {
                        lock (_asyncRedisSocketsLock)
                        {
                            if (asyncRds._writeCounter == 0) return true;
                        }
                        return false;
                    });
                    lock (_asyncRedisSocketsLock)
                    {
                        Interlocked.Increment(ref asyncRds._writeCounter);
                        _asyncRedisSockets.Add(asyncRds);
                    }
                    Interlocked.Increment(ref _asyncRedisSocketsCount);
                }
            }

            //class wpp
            //{
            //    public CommandPacket cmd;
            //    public TaskCompletionSource<object> tcs;
            //    public Func<RedisResult, object> parse;
            //    public int tid;
            //}
            //ConcurrentBag<wpp> _globalAsyncQueue = new ConcurrentBag<wpp>();
            //object _globalAsyncQueueLock = new object();
            //long _globalAsyncStatus = 0;
            //long _globalAsyncTicks;
            public override Task<TValue> AdapterCallAsync<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                return TopOwner.LogCallAsync(cmd, async () =>
                {
                    var asyncRds = GetAsyncRedisSocket(cmd);
                    var rt = await asyncRds.WriteAsync(cmd);
                    rt.IsErrorThrow = TopOwner._isThrowRedisSimpleError;
                    return parse(rt);
                });
                //var tid = Thread.CurrentThread.ManagedThreadId;
                //var ticks = Environment.TickCount;
                //var curq = new wpp
                //{
                //    cmd = cmd,
                //    parse = rt => parse(rt),
                //    tcs = new TaskCompletionSource<object>(),
                //    tid = tid
                //};
                //var localQueue = new Queue<wpp>();
                //lock (_globalAsyncQueueLock)
                //{
                //    _globalAsyncQueue.Add(curq);
                //    if (_globalAsyncQueue.Count >= 100)
                //    {
                //        for (var a = 0; a < 100; a++)
                //            if (_globalAsyncQueue.TryTake(out var tmpq))
                //                localQueue.Enqueue(tmpq);
                //    }
                //}

                //if (localQueue.Any())
                //{
                //    using (var rds = GetRedisSocket(cmd))
                //    {
                //        if (rds.IsConnected == false) rds.Connect();
                //        using (var ms = new MemoryStream())
                //        {
                //            var writer = new RespHelper.Resp3Writer(ms, rds.Encoding, rds.Protocol);
                //            foreach (var q in localQueue)
                //                writer.WriteCommand(q.cmd);
                //            ms.Position = 0;
                //            ms.CopyTo(rds.Stream);
                //        }
                //        AsyncRedisSocket.sb.AppendLine($"线程{Thread.CurrentThread.ManagedThreadId}：写入 {localQueue.Count} 个命令 total:{AsyncRedisSocket.sw.ElapsedMilliseconds} ms");
                //        if (rds.ClientReply == ClientReplyType.on)
                //        {
                //            while (localQueue.Any())
                //            {
                //                var q = localQueue.Dequeue();
                //                var rt = rds.Read(false);
                //                var val = q.parse(rt);
                //                q.tcs.TrySetResult(val);
                //            }
                //        }
                //        else
                //        {
                //            while (localQueue.Any())
                //            {
                //                var q = localQueue.Dequeue();
                //                var val = q.parse(new RedisResult(null, true, RedisMessageType.SimpleString));
                //                q.tcs.TrySetResult(val);
                //            }
                //        }
                //    }

                //}
                //var ret = await curq.tcs.Task;
                //return (TValue)ret;

            }
#endif

            string GetIdleBusKey(CommandPacket cmd)
            {
                if (cmd != null && (_rw_splitting || !_is_single))
                {
                    var cmdset = CommandSets.Get(cmd._command);
                    if (cmdset != null)
                    {
                        if (!_is_single && (cmdset.Status & CommandSets.LocalStatus.check_single) == CommandSets.LocalStatus.check_single)
                            throw new RedisServerException($"RedisClient: Method cannot be used in {UseType} mode. You can set \"max pool size=1\", but it is not singleton mode.");

                        if (_rw_splitting &&
                            ((cmdset.Tag & CommandSets.ServerTag.read) == CommandSets.ServerTag.read ||
                            (cmdset.Flag & CommandSets.ServerFlag.@readonly) == CommandSets.ServerFlag.@readonly))
                        {
                            var rndkeys = _ib.GetKeys(v => v == null || v.IsAvailable && v._policy._connectionStringBuilder.Host != _masterHost);
                            if (rndkeys.Any())
                            {
                                var rndkey = rndkeys[_rnd.Value.Next(0, rndkeys.Length)];
                                return rndkey;

                            }
                        }
                    }
                }
                return _masterHost;
            }
        }
    }
}