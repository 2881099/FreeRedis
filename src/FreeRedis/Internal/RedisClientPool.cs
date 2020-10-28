using FreeRedis.Internal.ObjectPool;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.IO;

namespace FreeRedis.Internal
{
    public class RedisClientPool : ObjectPool<RedisClient>, IDisposable
    {
        public RedisClientPool(string connectionString, Action<RedisClient> connected, RedisClient topOwner) : base(null)
        {
            _policy = new RedisClientPoolPolicy
            {
                _pool = this
            };
            _policy.Connected += (s, o) =>
            {
                var cli = s as RedisClient;
                var rds = cli.Adapter.GetRedisSocket(null);
                using (cli.NoneRedisSimpleError())
                {
                    rds.Socket.ReceiveTimeout = (int)_policy._connectionStringBuilder.ReceiveTimeout.TotalMilliseconds;
                    rds.Socket.SendTimeout = (int)_policy._connectionStringBuilder.SendTimeout.TotalMilliseconds;
                    rds.Encoding = _policy._connectionStringBuilder.Encoding;

                    if (_policy._connectionStringBuilder.Protocol == RedisProtocol.RESP3)
                    {
                        cli.Hello("3", _policy._connectionStringBuilder.User, _policy._connectionStringBuilder.Password, _policy._connectionStringBuilder.ClientName);
                        if (cli.RedisSimpleError != null)
                            throw cli.RedisSimpleError;
                        rds.Protocol = RedisProtocol.RESP3;
                    }
                    else if (!string.IsNullOrEmpty(_policy._connectionStringBuilder.User) && !string.IsNullOrEmpty(_policy._connectionStringBuilder.Password))
                    {
                        cli.Auth(_policy._connectionStringBuilder.User, _policy._connectionStringBuilder.Password);
                        if (cli.RedisSimpleError != null)
                            throw cli.RedisSimpleError;
                    }
                    else if (!string.IsNullOrEmpty(_policy._connectionStringBuilder.Password))
                    {
                        cli.Auth(_policy._connectionStringBuilder.Password);
                        if (cli.RedisSimpleError != null && cli.RedisSimpleError.Message != "ERR Client sent AUTH, but no password is set")
                            throw cli.RedisSimpleError;
                    }

                    if (_policy._connectionStringBuilder.Database > 0)
                    {
                        cli.Select(_policy._connectionStringBuilder.Database);
                        if (cli.RedisSimpleError != null)
                            throw cli.RedisSimpleError;
                    }
                    if (!string.IsNullOrEmpty(_policy._connectionStringBuilder.ClientName) && _policy._connectionStringBuilder.Protocol == RedisProtocol.RESP2)
                    {
                        cli.ClientSetName(_policy._connectionStringBuilder.ClientName);
                        if (cli.RedisSimpleError != null)
                            throw cli.RedisSimpleError;
                    }
                }
                connected?.Invoke(cli);
            };
            this.Policy = _policy;
            this.TopOwner = topOwner;
            _policy.ConnectionString = connectionString;

#if net40
#else
            if (_policy._connectionStringBuilder.MaxPoolSize > 1)
                AsyncSocket = new AsyncRedisSocket(Get().Value.Adapter.GetRedisSocket(null));
#endif
        }

        internal bool CheckAvailable() => base.LiveCheckAvailable();

        internal RedisClientPoolPolicy _policy;
        public string Key => _policy.Key;
        public string Prefix => _policy._connectionStringBuilder.Prefix;
        internal RedisClient TopOwner;
#if net40
#else
        /// <summary>
        /// Single socket not support Async Multiplexing
        /// </summary>
        internal AsyncRedisSocket AsyncSocket;

        void IDisposable.Dispose()
        {
            AsyncSocket.Dispose();
            base.Dispose();
        }
#endif
    }

    public class RedisClientPoolPolicy : IPolicy<RedisClient>
    {
        internal RedisClientPool _pool;
        internal ConnectionStringBuilder _connectionStringBuilder = new ConnectionStringBuilder();
        internal string Key => $"{_connectionStringBuilder.Host}/{_connectionStringBuilder.Database}";
        public event EventHandler Connected;

        public string Name { get => Key; set => throw new NotSupportedException(); }
        public int PoolSize { get => _connectionStringBuilder.MaxPoolSize; set => _connectionStringBuilder.MaxPoolSize = value; }
        public TimeSpan IdleTimeout { get => _connectionStringBuilder.IdleTimeout; set => _connectionStringBuilder.IdleTimeout = value; }
        public TimeSpan SyncGetTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public int AsyncGetCapacity { get; set; } = 100000;
        public bool IsThrowGetTimeoutException { get; set; } = true;
        public bool IsAutoDisposeWithSystem { get; set; } = true;
        public int CheckAvailableInterval { get; set; } = 5;

        public string ConnectionString
        {
            get => _connectionStringBuilder.ToString();
            set
            {
                _connectionStringBuilder = value;

                if (_connectionStringBuilder.MinPoolSize > 0)
                    PrevReheatConnectionPool(_pool, _connectionStringBuilder.MinPoolSize);
            }
        }

        public bool OnCheckAvailable(Object<RedisClient> obj)
        {
            obj.ResetValue();
            return obj.Value.Ping() == "PONG";
        }

        public RedisClient OnCreate()
        {
            return new RedisClient(_pool.TopOwner, _connectionStringBuilder.Host, _connectionStringBuilder.Ssl, _connectionStringBuilder.ConnectTimeout,
                _connectionStringBuilder.ReceiveTimeout, _connectionStringBuilder.SendTimeout, cli => Connected(cli, new EventArgs()));
        }

        public void OnDestroy(RedisClient obj)
        {
            if (obj != null)
            {
                //if (obj.IsConnected) try { obj.Quit(); } catch { } 此行会导致，服务器主动断开后，执行该命令超时停留10-20秒
                try { obj.Dispose(); } catch { }
            }
        }

        public void OnReturn(Object<RedisClient> obj) { }

        public void OnGet(Object<RedisClient> obj)
        {
            if (_pool.IsAvailable)
            {
                if (DateTime.Now.Subtract(obj.LastReturnTime).TotalSeconds > 60 || obj.Value.Adapter.GetRedisSocket(null).IsConnected == false)
                {
                    try
                    {
                        obj.Value.Ping();
                    }
                    catch
                    {
                        obj.ResetValue();
                    }
                }
            }
        }
#if net40
#else
        public Task OnGetAsync(Object<RedisClient> obj)
        {
            OnGet(obj); //todo
            return Task.FromResult(false);
        }
#endif

        public void OnGetTimeout() { }
        public void OnAvailable() { }
        public void OnUnavailable() { }

        public static void PrevReheatConnectionPool(ObjectPool<RedisClient> pool, int minPoolSize)
        {
            if (minPoolSize <= 0) minPoolSize = Math.Min(5, pool.Policy.PoolSize);
            if (minPoolSize > pool.Policy.PoolSize) minPoolSize = pool.Policy.PoolSize;
            var initTestOk = true;
            var initStartTime = DateTime.Now;
            var initConns = new ConcurrentBag<Object<RedisClient>>();

            try
            {
                var conn = pool.Get();
                initConns.Add(conn);
                conn.Value.Ping();
            }
            catch (Exception ex)
            {
                initTestOk = false; //预热一次失败，后面将不进行
                pool.SetUnavailable(ex);
            }
            for (var a = 1; initTestOk && a < minPoolSize; a += 10)
            {
                if (initStartTime.Subtract(DateTime.Now).TotalSeconds > 3) break; //预热耗时超过3秒，退出
                var b = Math.Min(minPoolSize - a, 10); //每10个预热
                var initTasks = new Task[b];
                for (var c = 0; c < b; c++)
                {
                    initTasks[c] = TaskEx.Run(() =>
                    {
                        try
                        {
                            var conn = pool.Get();
                            initConns.Add(conn);
                        }
                        catch
                        {
                            initTestOk = false;  //有失败，下一组退出预热
                        }
                    });
                }
                Task.WaitAll(initTasks);
            }
            while (initConns.TryTake(out var conn)) pool.Return(conn);
        }
    }
}
