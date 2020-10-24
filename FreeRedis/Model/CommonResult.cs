using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeRedis.Model
{
    static class RedisResultNewValueExtensions
    {
        public static RoleResult ThrowOrValueToRole(this RedisResult rt) =>
            rt.ThrowOrValue((a, _) =>
            {
                if (a?.Any() != true) return null;
                var role = new RoleResult { role = a[0].ConvertTo<RoleType>() };
                switch (role.role)
                {
                    case RoleType.Master:
                        role.data = new RoleResult.MasterInfo
                        {
                            _replication_offset = a[1].ConvertTo<long>(),
                            _slaves = (a[2] as object[])?.Select(x =>
                            {
                                var xs = x as object[];
                                return new RoleResult.MasterInfo.SlaveInfo
                                {
                                    ip = xs[0].ConvertTo<string>(),
                                    port = xs[1].ConvertTo<int>(),
                                    slave_offset = xs[2].ConvertTo<long>()
                                };
                            }).ToArray()
                        };
                        break;
                    case RoleType.Slave:
                        role.data = new RoleResult.SlaveInfo
                        {
                            master_ip = a[1].ConvertTo<string>(),
                            master_port = a[2].ConvertTo<int>(),
                            replication_state = a[3].ConvertTo<string>(),
                            data_received = a[4].ConvertTo<long>()
                        };
                        break;
                    case RoleType.Sentinel:
                        role.data = a[1].ConvertTo<string[]>();
                        break;
                }
                return role;
            });
    }

    //1) "master"
    //2) (integer) 15891
    //3) 1) 1) "127.0.0.1"
    //      2) "6380"
    //      3) "15617"

    //1) "slave"
    //2) "127.0.0.1"
    //3) (integer) 6381
    //4) "connected"
    //5) (integer) 74933

    //1) "sentinel"
    //2) 1) "mymaster"
    public class RoleResult
    {
        public RoleType role;
        public object data;

        public class MasterInfo
        {
            public long _replication_offset;
            public SlaveInfo[] _slaves;

            public class SlaveInfo
            {
                public string ip;
                public int port;
                public long slave_offset;
            }
        }
        public class SlaveInfo
        {
            public string master_ip;
            public int master_port;
            public string replication_state;
            public long data_received;
        }
    }
    public enum RoleType { Master, Slave, Sentinel }

    public class ScanResult<T>
    {
        public readonly long cursor;
        public readonly T[] items;
        public readonly long length;
        public ScanResult(long cursor, T[] items) { this.cursor = cursor; this.items = items; this.length = items.LongLength; }
    }
}
