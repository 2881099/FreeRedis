using FreeRedis.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
        class ClusterAdapter : BaseAdapter
        {
            internal readonly IdleBus<RedisClientPool> _ib;
            readonly ConnectionStringBuilder[] _clusterConnectionStrings;

            public ClusterAdapter(RedisClient topOwner, ConnectionStringBuilder[] clusterConnectionStrings)
            {
                UseType = UseType.Cluster;
                TopOwner = topOwner;

                if (clusterConnectionStrings.Any() != true)
                    throw new ArgumentNullException(nameof(clusterConnectionStrings));

                _clusterConnectionStrings = clusterConnectionStrings.ToArray();
                _ib = new IdleBus<RedisClientPool>(TimeSpan.FromMinutes(10));
                RefershClusterNodes();
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
                var slots = cmd?._keyIndexes.Select(a => GetClusterSlot(cmd._input[a].ToInvariantCultureToString())).Distinct().ToArray();
                var poolkeys = slots?.Select(a => _slotCache.TryGetValue(a, out var trykey) ? trykey : null).Distinct().Where(a => a != null).ToArray();
                //if (poolkeys.Length > 1) throw new RedisClientException($"CROSSSLOT Keys in request don't hash to the same slot: {cmd}");
                var poolkey = poolkeys?.FirstOrDefault();
            goto_getrndkey:
                if (string.IsNullOrEmpty(poolkey))
                {
                    var rndkeys = _ib.GetKeys(v => v == null || v.IsAvailable);
                    if (rndkeys.Any() == false) throw new RedisClientException($"All nodes of the cluster failed to connect");
                    poolkey = rndkeys[_rnd.Value.Next(0, rndkeys.Length)];
                }
                var pool = _ib.Get(poolkey);
                if (pool.IsAvailable == false)
                {
                    poolkey = null;
                    goto goto_getrndkey;
                }
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
                            return cmd._keyIndexes.Select((_, idx) => AdapterCall(cmd._command.InputKey(cmd.GetKey(idx)), parse)).Sum(a => a.ConvertTo<long>()).ConvertTo<TValue>();
                        case "MSET":
                            cmd._keyIndexes.ForEach(idx => AdapterCall(cmd._command.InputKey(cmd._input[idx].ToInvariantCultureToString()).InputRaw(cmd._input[idx + 1]), parse));
                            return default;
                        case "MGET":
                            return cmd._keyIndexes.Select((_, idx) => AdapterCall(cmd._command.InputKey(cmd.GetKey(idx)), parse).ConvertTo<object[]>().First()).ToArray().ConvertTo<TValue>();
                        case "PFCOUNT":
                            return cmd._keyIndexes.Select((_, idx) => AdapterCall(cmd._command.InputKey(cmd.GetKey(idx)), parse)).Sum(a => a.ConvertTo<long>()).ConvertTo<TValue>();
                    }
                }
                return TopOwner.LogCall(cmd, () =>
                {
                    RedisResult rt = null;
                    RedisClientPool pool = null;
                    using (var rds = GetRedisSocket(cmd))
                    {
                        pool = (rds as DefaultRedisSocket.TempProxyRedisSocket)._pool;
                        try
                        {
                            if (cmd._clusterMovedAsking)
                            {
                                cmd._clusterMovedAsking = false;
                                var askingCmd = "ASKING".SubCommand(null).FlagReadbytes(false);
                                rds.Write(askingCmd);
                                rds.Read(askingCmd);
                            }
                            rds.Write(cmd);
                            rt = rds.Read(cmd);
                        }
                        catch (Exception ex)
                        {
                            if (pool?.SetUnavailable(ex) == true)
                            {
                            }
                            throw ex;
                        }
                    }
                    if (rt.IsError && pool != null)
                    {
                        var moved = ClusterMoved.ParseSimpleError(rt.SimpleError);
                        if (moved != null && cmd._clusterMovedTryCount < 3)
                        {
                            cmd._clusterMovedTryCount++;

                            if (moved.endpoint.StartsWith("127.0.0.1"))
                                moved.endpoint = $"{DefaultRedisSocket.SplitHost(pool._policy._connectionStringBuilder.Host).Key}:{moved.endpoint.Substring(10)}";
                            else if (moved.endpoint.StartsWith("localhost", StringComparison.CurrentCultureIgnoreCase))
                                moved.endpoint = $"{DefaultRedisSocket.SplitHost(pool._policy._connectionStringBuilder.Host).Key}:{moved.endpoint.Substring(10)}";

                            ConnectionStringBuilder connectionString = pool._policy._connectionStringBuilder.ToString();
                            connectionString.Host = moved.endpoint;
                            RegisterClusterNode(connectionString);

                            if (moved.ismoved)
                                _slotCache.AddOrUpdate(moved.slot, connectionString.Host, (k1, v1) => connectionString.Host);

                            if (moved.isask)
                                cmd._clusterMovedAsking = true;

                            TopOwner.OnNotice(null, new NoticeEventArgs(NoticeType.Info, null, $"{(cmd.WriteHost ?? "Not connected").PadRight(21)} > {cmd}\r\n{rt.SimpleError} ", null));
                            return AdapterCall(cmd, parse);
                        }
                    }
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

            void RefershClusterNodes()
            {
                foreach (var testConnection in _clusterConnectionStrings)
                {
                    RegisterClusterNode(testConnection);
                    //尝试求出其他节点，并缓存slot
                    try
                    {
                        var cnodes = AdapterCall<string>("CLUSTER".SubCommand("NODES"), rt => rt.ThrowOrValue<string>()).Split('\n');
                        foreach (var cnode in cnodes)
                        {
                            if (string.IsNullOrEmpty(cnode)) continue;
                            var dt = cnode.Trim().Split(' ');
                            if (dt.Length < 9) continue;
                            if (!dt[2].StartsWith("master") && !dt[2].EndsWith("master")) continue;
                            if (dt[7] != "connected") continue;

                            var endpoint = dt[1];
                            var at40 = endpoint.IndexOf('@');
                            if (at40 != -1) endpoint = endpoint.Remove(at40);

                            if (endpoint.StartsWith("127.0.0.1"))
                                endpoint = $"{DefaultRedisSocket.SplitHost(testConnection.Host).Key}:{endpoint.Substring(10)}";
                            else if (endpoint.StartsWith("localhost", StringComparison.CurrentCultureIgnoreCase))
                                endpoint = $"{DefaultRedisSocket.SplitHost(testConnection.Host).Key}:{endpoint.Substring(10)}";
                            ConnectionStringBuilder connectionString = testConnection.ToString();
                            connectionString.Host = endpoint;
                            RegisterClusterNode(connectionString);

                            for (var slotIndex = 8; slotIndex < dt.Length; slotIndex++)
                            {
                                var slots = dt[slotIndex].Split('-');
                                if (ushort.TryParse(slots[0], out var tryslotStart) &&
                                    ushort.TryParse(slots[1], out var tryslotEnd))
                                {
                                    for (var slot = tryslotStart; slot <= tryslotEnd; slot++)
                                        _slotCache.AddOrUpdate(slot, connectionString.Host, (k1, v1) => connectionString.Host);
                                }
                            }
                        }
                        break;
                    }
                    catch
                    {
                        _ib.TryRemove(testConnection.Host, true);
                    }
                }

                if (_ib.GetKeys().Length == 0)
                    throw new RedisClientException($"All \"clusterConnectionStrings\" failed to connect");
            }
            //closure connectionString
            void RegisterClusterNode(ConnectionStringBuilder connectionString)
            {
                _ib.TryRegister(connectionString.Host, () => new RedisClientPool(connectionString, null, TopOwner));
            }

            ConcurrentDictionary<ushort, string> _slotCache = new ConcurrentDictionary<ushort, string>();
            class ClusterMoved
            {
                public bool ismoved;
                public bool isask;
                public ushort slot;
                public string endpoint;
                public static ClusterMoved ParseSimpleError(string simpleError)
                {
                    if (string.IsNullOrWhiteSpace(simpleError)) return null;
                    var ret = new ClusterMoved
                    {
                        ismoved = simpleError.StartsWith("MOVED "), //永久定向
                        isask = simpleError.StartsWith("ASK ") //临时性一次定向
                    };
                    if (ret.ismoved == false && ret.isask == false) return null;
                    var parts = simpleError.Split(new string[] { "\r\n" }, StringSplitOptions.None).FirstOrDefault().Split(new[] { ' ' }, 3);
                    if (parts.Length != 3 ||
                        ushort.TryParse(parts[1], out ret.slot) == false) return null;
                    ret.endpoint = parts[2];
                    return ret;
                }
            }

            #region crc16
            private static readonly ushort[] crc16tab = {
                0x0000,0x1021,0x2042,0x3063,0x4084,0x50a5,0x60c6,0x70e7,
                0x8108,0x9129,0xa14a,0xb16b,0xc18c,0xd1ad,0xe1ce,0xf1ef,
                0x1231,0x0210,0x3273,0x2252,0x52b5,0x4294,0x72f7,0x62d6,
                0x9339,0x8318,0xb37b,0xa35a,0xd3bd,0xc39c,0xf3ff,0xe3de,
                0x2462,0x3443,0x0420,0x1401,0x64e6,0x74c7,0x44a4,0x5485,
                0xa56a,0xb54b,0x8528,0x9509,0xe5ee,0xf5cf,0xc5ac,0xd58d,
                0x3653,0x2672,0x1611,0x0630,0x76d7,0x66f6,0x5695,0x46b4,
                0xb75b,0xa77a,0x9719,0x8738,0xf7df,0xe7fe,0xd79d,0xc7bc,
                0x48c4,0x58e5,0x6886,0x78a7,0x0840,0x1861,0x2802,0x3823,
                0xc9cc,0xd9ed,0xe98e,0xf9af,0x8948,0x9969,0xa90a,0xb92b,
                0x5af5,0x4ad4,0x7ab7,0x6a96,0x1a71,0x0a50,0x3a33,0x2a12,
                0xdbfd,0xcbdc,0xfbbf,0xeb9e,0x9b79,0x8b58,0xbb3b,0xab1a,
                0x6ca6,0x7c87,0x4ce4,0x5cc5,0x2c22,0x3c03,0x0c60,0x1c41,
                0xedae,0xfd8f,0xcdec,0xddcd,0xad2a,0xbd0b,0x8d68,0x9d49,
                0x7e97,0x6eb6,0x5ed5,0x4ef4,0x3e13,0x2e32,0x1e51,0x0e70,
                0xff9f,0xefbe,0xdfdd,0xcffc,0xbf1b,0xaf3a,0x9f59,0x8f78,
                0x9188,0x81a9,0xb1ca,0xa1eb,0xd10c,0xc12d,0xf14e,0xe16f,
                0x1080,0x00a1,0x30c2,0x20e3,0x5004,0x4025,0x7046,0x6067,
                0x83b9,0x9398,0xa3fb,0xb3da,0xc33d,0xd31c,0xe37f,0xf35e,
                0x02b1,0x1290,0x22f3,0x32d2,0x4235,0x5214,0x6277,0x7256,
                0xb5ea,0xa5cb,0x95a8,0x8589,0xf56e,0xe54f,0xd52c,0xc50d,
                0x34e2,0x24c3,0x14a0,0x0481,0x7466,0x6447,0x5424,0x4405,
                0xa7db,0xb7fa,0x8799,0x97b8,0xe75f,0xf77e,0xc71d,0xd73c,
                0x26d3,0x36f2,0x0691,0x16b0,0x6657,0x7676,0x4615,0x5634,
                0xd94c,0xc96d,0xf90e,0xe92f,0x99c8,0x89e9,0xb98a,0xa9ab,
                0x5844,0x4865,0x7806,0x6827,0x18c0,0x08e1,0x3882,0x28a3,
                0xcb7d,0xdb5c,0xeb3f,0xfb1e,0x8bf9,0x9bd8,0xabbb,0xbb9a,
                0x4a75,0x5a54,0x6a37,0x7a16,0x0af1,0x1ad0,0x2ab3,0x3a92,
                0xfd2e,0xed0f,0xdd6c,0xcd4d,0xbdaa,0xad8b,0x9de8,0x8dc9,
                0x7c26,0x6c07,0x5c64,0x4c45,0x3ca2,0x2c83,0x1ce0,0x0cc1,
                0xef1f,0xff3e,0xcf5d,0xdf7c,0xaf9b,0xbfba,0x8fd9,0x9ff8,
                0x6e17,0x7e36,0x4e55,0x5e74,0x2e93,0x3eb2,0x0ed1,0x1ef0
            };
            public static ushort GetClusterSlot(string key)
            {
                //HASH_SLOT = CRC16(key) mod 16384
                var blob = Encoding.ASCII.GetBytes(key);
                int offset = 0, count = blob.Length, start = -1, end = -1;
                byte lt = (byte)'{', rt = (byte)'}';
                for (int a = 0; a < count - 1; a++)
                    if (blob[a] == lt)
                    {
                        start = a;
                        break;
                    }
                if (start >= 0)
                {
                    for (int a = start + 1; a < count; a++)
                        if (blob[a] == rt)
                        {
                            end = a;
                            break;
                        }
                }

                if (start >= 0
                    && end >= 0
                    && --end != start)
                {
                    offset = start + 1;
                    count = end - start;
                }

                uint crc = 0;
                for (int i = 0; i < count; i++)
                    crc = ((crc << 8) ^ crc16tab[((crc >> 8) ^ blob[offset++]) & 0x00FF]) & 0x0000FFFF;
                return (ushort)(crc % 16384);
            }
            #endregion
        }
    }
}
