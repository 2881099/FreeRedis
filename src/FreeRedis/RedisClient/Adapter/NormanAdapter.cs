using FreeRedis;
using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
        class NormanAdapter : BaseAdapter
        {
            internal readonly IdleBus<RedisClientPool> _ib;
            readonly ConnectionStringBuilder[] _connectionStrings;
            readonly Func<string, string> _redirectRule;

            public NormanAdapter(RedisClient topOwner, ConnectionStringBuilder[] connectionStrings, Func<string, string> redirectRule)
            {
                UseType = UseType.Cluster;
                TopOwner = topOwner;

                if (connectionStrings.Any() != true)
                    throw new ArgumentNullException(nameof(connectionStrings));

                _connectionStrings = connectionStrings.ToArray();
                _ib = new IdleBus<RedisClientPool>(TimeSpan.FromMinutes(10));
                foreach (var connectionString in _connectionStrings)
                    RegisterClusterNode(connectionString);

                _redirectRule = redirectRule;
            }
            public override void Dispose()
            {
                _ib.Dispose();
            }

            public override void Refersh(IRedisSocket redisSocket)
            {
                var tmprds = redisSocket as DefaultRedisSocket.TempProxyRedisSocket;
                if (tmprds != null) _ib.Get(tmprds._poolkey);
            }
            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                string[] poolkeys = null;
                if (_redirectRule == null)
                {
                    //crc16
                    var slots = cmd?._keyIndexes.Select(a => ClusterAdapter.GetClusterSlot(cmd._input[a].ToInvariantCultureToString())).Distinct().ToArray();
                    poolkeys = slots?.Select(a => _connectionStrings[a % _connectionStrings.Length]).Select(a => $"{a.Host}/{a.Database}").Distinct().ToArray();
                }
                else
                {
                    poolkeys = cmd?._keyIndexes.Select(a => _redirectRule(cmd._input[a].ToInvariantCultureToString())).Distinct().ToArray();
                }

                if (poolkeys.Length > 1) throw new RedisClientException($"CROSSSLOT Keys in request don't hash to the same slot: {cmd}");
                var poolkey = poolkeys?.FirstOrDefault() ?? $"{_connectionStrings[0].Host}/{_connectionStrings[0].Database}";
                var pool = _ib.Get(poolkey);
                var cli = pool.Get();
                var rds = cli.Value.Adapter.GetRedisSocket(null);
                var rdsproxy = DefaultRedisSocket.CreateTempProxy(rds, () => pool.Return(cli));
                rdsproxy._poolkey = poolkey;
                rdsproxy._pool = pool;
                return rdsproxy;
            }
            public override TValue AdapterCall<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                if (cmd._keyIndexes.Count > 1) //Multiple key slot values not equal
                {
                    switch (cmd._command)
                    {
                        case "DEL":
                        case "UNLINK":
                            return cmd._keyIndexes.Select((_, idx) => AdapterCall(new CommandPacket(cmd._command).InputKey(cmd.GetKey(idx)), parse)).Sum(a => a.ConvertTo<long>()).ConvertTo<TValue>();
                        case "MSET":
                            cmd._keyIndexes.ForEach(idx => AdapterCall(new CommandPacket(cmd._command).InputKey(cmd._input[idx].ToInvariantCultureToString()).InputRaw(cmd._input[idx + 1]), parse));
                            return default;
                        case "MGET":
                            return cmd._keyIndexes.Select((_, idx) => AdapterCall(new CommandPacket(cmd._command).InputKey(cmd.GetKey(idx)), parse).ConvertTo<object[]>().First()).ToArray().ConvertTo<TValue>();
                        case "PFCOUNT":
                            return cmd._keyIndexes.Select((_, idx) => AdapterCall(new CommandPacket(cmd._command).InputKey(cmd.GetKey(idx)), parse)).Sum(a => a.ConvertTo<long>()).ConvertTo<TValue>();
                    }
                }
                return TopOwner.LogCall(cmd, () =>
                {
                    RedisResult rt = null;
                    RedisClientPool pool = null; 
                    var protocolRetry = false;
                    using (var rds = GetRedisSocket(cmd))
                    {
                        try
                        {
                            rds.Write(cmd);
                            rt = rds.Read(cmd);
                        }
                        catch (ProtocolViolationException)
                        {
                            rds.ReleaseSocket();
                            if (cmd.IsReadOnlyCommand() == false || ++cmd._protocolErrorTryCount > 1) throw;
                            protocolRetry = true;
                        }
                        catch (Exception ex)
                        {
                            pool = (rds as DefaultRedisSocket.TempProxyRedisSocket)._pool;
                            if (pool?.SetUnavailable(ex) == true)
                            {
                            }
                            throw;
                        }
                    }
                    if (protocolRetry) return AdapterCall(cmd, parse);
                    return parse(rt);
                });
            }

#if isasync
            public override Task<TValue> AdapterCallAsync<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                //Single socket not support Async Multiplexing
                return Task.FromResult(AdapterCall<TValue>(cmd, parse));
            }
#endif

            //closure connectionString
            void RegisterClusterNode(ConnectionStringBuilder connectionString)
            {
                _ib.TryRegister($"{connectionString.Host}/{connectionString.Database}", () => new RedisClientPool(connectionString, null, TopOwner));
            }
        }

    }
}
