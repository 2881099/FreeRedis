using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeRedis
{
    partial class RedisClient
    {
        class ClusterAdapter : BaseAdapter
        {
            readonly RedisClient _cli;
            readonly IdleBus<RedisClientPool> _ib;

            public ClusterAdapter(RedisClient cli, ConnectionStringBuilder[] clusterConnectionStrings)
            {
                UseType = UseType.Cluster;
                _cli = cli;
                _ib = new IdleBus<RedisClientPool>();
                _ib.Notice += new EventHandler<IdleBus<string, RedisClientPool>.NoticeEventArgs>((_, e) => { });
            }

            public override void Dispose()
            {
                _ib.Dispose();
            }

            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                throw new NotImplementedException();
            }
            public override T2 AdapaterCall<T1, T2>(CommandPacket cmd, Func<RedisResult<T1>, T2> parse)
            {
                throw new NotImplementedException();
            }

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
                        ismoved = simpleError.StartsWith("MOVED "),
                        isask = simpleError.StartsWith("ASK ")
                    };
                    if (ret.ismoved == false && ret.isask == false) return null;
                    var parts = simpleError.Split(new string[] { "\r\n" }, StringSplitOptions.None).FirstOrDefault().Split(new[] { ' ' }, 3);
                    if (parts.Length != 3 ||
                        ushort.TryParse(parts[1], out ret.slot) == false) return null;
                    ret.endpoint = parts[2];
                    return ret;
                }
            }
        }
    }
}
