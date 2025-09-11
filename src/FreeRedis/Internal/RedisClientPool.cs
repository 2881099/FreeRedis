using FreeRedis.Internal.ObjectPool;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FreeRedis.Internal
{
    public class RedisClientPool : ObjectPool<RedisClient>, IDisposable
    {
        public RedisClientPool(ConnectionStringBuilder connectionString, RedisClient topOwner) : base(null)
        {
            _policy = new RedisClientPoolPolicy
            {
                _pool = this
            };
            _policy.Connected += (s, o) =>
            {
                var cli = s as RedisClient;
                var rds = cli.Adapter.GetRedisSocket(null);
                var adapter = cli.Adapter as RedisClient.SingleInsideAdapter;
                rds.Socket.ReceiveTimeout = (int)connectionString.ReceiveTimeout.TotalMilliseconds;
                rds.Socket.SendTimeout = (int)connectionString.SendTimeout.TotalMilliseconds;
                rds.Encoding = connectionString.Encoding;
                var isIgnoreAop = rds.LastCommand.IsIgnoreAop;

                var cmds = new List<CommandPacket>();
                if (connectionString.Protocol == RedisProtocol.RESP3)
                {
                    //未开启 ACL 的情况
                    if (string.IsNullOrWhiteSpace(connectionString.User) && !string.IsNullOrWhiteSpace(connectionString.Password))
                        cmds.Add("AUTH".SubCommand(null)
                             .Input(connectionString.Password)
                             .OnData(rt =>
                             {
                                 if (rt.IsError && rt.SimpleError != "ERR Client sent AUTH, but no password is set")
                                     rt.ThrowOrNothing();
                                 rds.Protocol = RedisProtocol.RESP3;
                             }));
                    cmds.Add("HELLO"
                        .Input(3)
                        .InputIf(!string.IsNullOrWhiteSpace(connectionString.User) && !string.IsNullOrWhiteSpace(connectionString.Password), "AUTH", connectionString.User, connectionString.Password)
                        .InputIf(!string.IsNullOrWhiteSpace(connectionString.ClientName), "SETNAME", connectionString.ClientName)
                        .OnData(rt =>
                        {
                            rt.ThrowOrNothing();
                            rds.Protocol = RedisProtocol.RESP3;
                        }));
                }
                else if (!string.IsNullOrEmpty(connectionString.User) && !string.IsNullOrEmpty(connectionString.Password))
                    cmds.Add("AUTH".SubCommand(null)
                        .InputIf(!string.IsNullOrWhiteSpace(connectionString.User), connectionString.User)
                        .Input(connectionString.Password)
                        .OnData(rt =>
                        {
                            rt.ThrowOrNothing();
                        }));
                else if (!string.IsNullOrEmpty(connectionString.Password))
                    cmds.Add("AUTH".SubCommand(null)
                        .Input(connectionString.Password)
                        .OnData(rt =>
                        {
                            if (rt.IsError && rt.SimpleError != "ERR Client sent AUTH, but no password is set")
                                rt.ThrowOrNothing();
                        }));

                if (connectionString.Database > 0)
                    cmds.Add("SELECT".Input(connectionString.Database)
                        .OnData(rt =>
                        {
                            if (rt.IsError)
                            {
                                if (rt.SimpleError == "ERR SELECT is not allowed in cluster mode" ||
                                    rt.SimpleError == "ERR invalid database index.") //Garnet
                                    connectionString.Database = 0;
                                else
                                rt.ThrowOrNothing();
                            }
                            (rds as IRedisSocketModify).SetDatabase(connectionString.Database);
                        }));

                if (!string.IsNullOrEmpty(connectionString.ClientName) && connectionString.Protocol == RedisProtocol.RESP2)
                    cmds.Add("CLIENT".SubCommand("SETNAME").InputRaw(connectionString.ClientName)
                        .OnData(rt =>
                        {
                            rt.ThrowOrNothing();
                        }));

                cmds.Add("CLIENT".SubCommand("ID")
                    .OnData(rt =>
                    {
                        if (rt.IsError) return; //ERR Syntax error, try CLIENT (LIST | KILL | GETNAME | SETNAME | PAUSE | REPLY)
                        (rds as IRedisSocketModify).SetClientId(rt.ThrowOrValue<long>());
                    }));

                cmds.ForEach(cmd =>
                {
                    topOwner.LogCallCtrl(cmd, () => 1, true, false); // aop before
                });
                using (var ms = new MemoryStream()) {
                    var writer = new RespHelper.Resp3Writer(ms, rds.Encoding, RedisProtocol.RESP2);
                    cmds.ForEach(cmd =>
                    {
                        cmd.WriteTarget = $"{rds.Host}/{rds.Database}";
                        writer.WriteCommand(cmd);
                    });

                    ms.Position = 0;
                    ms.CopyTo(rds.Stream);
                    ms.Close();
                    ms.Dispose();
                }
                cmds.ForEach(cmd =>
                {
                    topOwner.LogCallCtrl(cmd, () =>
                    {
                        var rt = rds.Read(cmd);
                        return rt.Value;
                    }, false, true); //aop after
                });

                topOwner?.OnConnected(TopOwner, new ConnectedEventArgs(connectionString.Host, this, cli));
                if (isIgnoreAop == false)
                    topOwner?.OnNotice(TopOwner, new NoticeEventArgs(NoticeType.Info, null, $"{connectionString.Host.PadRight(21)} > Connected, ClientId: {rds.ClientId}, Database: {rds.Database}, Pool: {_freeObjects.Count}/{_allObjects.Count}", cli));
            };
            this.Policy = _policy;
            this.TopOwner = topOwner;
            _policy._connectionStringBuilder = connectionString;
            if (connectionString.MinPoolSize > 0)
                RedisClientPoolPolicy.PrevReheatConnectionPool(this, connectionString.MinPoolSize);
        }

        internal bool CheckAvailable() => base.LiveCheckAvailable();

        internal RedisClientPoolPolicy _policy;
        public string Key => _policy.Key;
        public string Prefix => _policy._connectionStringBuilder.Prefix;
        internal RedisClient TopOwner;
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
        public bool IsAutoDisposeWithSystem { get => _connectionStringBuilder.ExitAutoDisposePool; set => _connectionStringBuilder.ExitAutoDisposePool = value; }
        public TimeSpan AvailableCheckInterval { get; set; } = TimeSpan.FromSeconds(3);
        public TimeSpan ToleranceWindow { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan ToleranceCheckInterval { get; set; } = TimeSpan.FromSeconds(1);

        public bool OnCheckAvailable(Object<RedisClient> obj)
        {
            obj.ResetValue();
            CommandPacket cmd = "PING";
            cmd.IsIgnoreAop = true;
            return obj.Value.Call(cmd) as string == "PONG";
        }

        public RedisClient OnCreate()
        {
            return new RedisClient(_pool.TopOwner, _connectionStringBuilder.Host, 
                _connectionStringBuilder.Ssl, _connectionStringBuilder.CertificateValidation, _connectionStringBuilder.CertificateSelection,
                _connectionStringBuilder.ConnectTimeout,
                _connectionStringBuilder.ReceiveTimeout, _connectionStringBuilder.SendTimeout, 
                cli => Connected(cli, new EventArgs()), 
                cli =>
                {
                    _pool.TopOwner?.OnDisconnected(_pool.TopOwner, new DisconnectedEventArgs(_connectionStringBuilder.Host, _pool, cli));
                });
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
						CommandPacket cmd = "PING";
						cmd.IsIgnoreAop = true;
						obj.Value.Call(cmd);
					}
                    catch
                    {
                        obj.ResetValue();
                    }
                }
            }
        }
#if !NET40
        async public Task OnGetAsync(Object<RedisClient> obj)
        {
			if (_pool.IsAvailable)
			{
				if (DateTime.Now.Subtract(obj.LastReturnTime).TotalSeconds > 60 || obj.Value.Adapter.GetRedisSocket(null).IsConnected == false)
				{
					try
					{
						CommandPacket cmd = "PING";
						cmd.IsIgnoreAop = true;
						await obj.Value.CallAsync(cmd);
					}
					catch
					{
						obj.ResetValue();
					}
				}
			}
		}
#endif

        public void OnGetTimeout() { }
        public void OnAvailable() { }
        public void OnUnavailable()
        {
            _pool.TopOwner?.OnUnavailable(_pool.TopOwner, new UnavailableEventArgs(_connectionStringBuilder.Host, _pool));
            _pool.TopOwner?.OnNotice(_pool.TopOwner, new NoticeEventArgs(NoticeType.Info, null, $"{_connectionStringBuilder.Host.PadRight(21)} > Unavailable", null));
        }

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
				CommandPacket cmd = "PING";
				cmd.IsIgnoreAop = true;
				conn.Value.Call(cmd);
			}
            catch (Exception ex)
            {
                initTestOk = false; //预热一次失败，后面将不进行
                pool.SetUnavailable(ex, DateTime.Now);
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
