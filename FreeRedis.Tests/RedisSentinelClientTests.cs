using FreeRedis.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests
{
    public class SentinelTests
    {
        public static RedisSentinelClient GetClient() => new RedisSentinelClient("127.0.0.1:21479");

        [Fact]
        public void Ping()
        {
            using (var cli = GetClient())
            {
                Assert.Equal("PONG", cli.Ping());
            }
        }

        [Fact]
        public void Info()
        {
            using (var cli = GetClient())
            {
                var rt = cli.Info();
            }
        }

        [Fact]
        public void Role()
        {
            using (var cli = GetClient())
            {
                var rt = cli.Role();
                Assert.Equal(RoleType.Sentinel, rt.role);
                Assert.True(rt.masters.Any());
                Assert.Equal("mymaster", rt.masters.FirstOrDefault());
            }
        }

        [Fact]
        public void Masters()
        {
            using (var cli = GetClient())
            {
                var rt = cli.Masters();
                Assert.True(rt.Any());
                Assert.Equal("mymaster", rt[0].name);
            }
        }

        [Fact]
        public void Master()
        {
            using (var cli = GetClient())
            {
                var rt = cli.Master("mymaster");
                Assert.NotNull(rt);
                Assert.Equal("mymaster", rt.name);

                Assert.Equal("ERR No such master with that name",
                    Assert.Throws<RedisException>(() => cli.Master("mymaster222")).Message);
            }
        }

        [Fact]
        public void Salves()
        {
            using (var cli = GetClient())
            {
                var rt = cli.Salves("mymaster");
                Assert.True(rt.Any());
                Assert.Equal("ok", rt[0].master_link_status);
            }
        }

        [Fact]
        public void Sentinels()
        {
            using (var cli = GetClient())
            {
                var rt = cli.Sentinels("mymaster");
                Assert.True(rt.Any());
                Assert.Equal("127.0.0.1", rt[0].ip);
            }
        }

        [Fact]
        public void GetMasterAddrByName()
        {
            using (var cli = GetClient())
            {
                var rt = cli.GetMasterAddrByName("mymaster");
                Assert.False(string.IsNullOrEmpty(rt));
            }
        }

        [Fact]
        public void IsMasterDownByAddr()
        {
            using (var cli = GetClient())
            {
                var st = cli.Sentinels("mymaster");
                Assert.True(st.Any());
                var rt = cli.IsMasterDownByAddr(st[0].name, st[0].port, st[0].voted_leader_epoch, st[0].runid);
                Assert.NotNull(rt);
                Assert.False(rt.down_state);
                Assert.Equal("*", rt.leader);
                Assert.Equal(st[0].voted_leader_epoch, rt.vote_epoch);
            }
        }

        [Fact]
        public void Reset()
        {
            using (var cli = GetClient())
            {
                var rt = cli.Reset("*");
                Assert.True(rt > 0);
            }
        }

        [Fact]
        public void Failover()
        {
            using (var cli = GetClient())
            {
                cli.Failover("mymaster");

                Assert.Equal("ERR No such master with that name",
                    Assert.Throws<RedisException>(() => cli.Failover("mymaster222")).Message);
            }
        }

        [Fact]
        public void PendingScripts()
        {
            using (var cli = GetClient())
            {
                var rt = cli.PendingScripts();
            }
        }

        [Fact]
        public void FlushConfig()
        {
            using (var cli = GetClient())
            {
                cli.FlushConfig();
            }
        }

        [Fact]
        public void Remove()
        {
            using (var cli = GetClient())
            {
                //cli.Remove("mymaster");

                Assert.Equal("ERR No such master with that name",
                    Assert.Throws<RedisException>(() => cli.Remove("mymaster222")).Message);
            }
        }

        [Fact]
        public void CkQuorum()
        {
            using (var cli = GetClient())
            {
                var rt = cli.CkQuorum("mymaster");

                Assert.Equal("ERR No such master with that name",
                    Assert.Throws<RedisException>(() => cli.CkQuorum("mymaster222")).Message);
            }
        }

        [Fact]
        public void Set()
        {
            using (var cli = GetClient())
            {
                cli.Set("mymaster", "down-after-milliseconds", "5000");
            }
        }

        [Fact]
        public void InfoCache()
        {
            using (var cli = GetClient())
            {
                var rt = cli.InfoCache("mymaster");
            }
        }

        [Fact]
        public void SimulateFailure()
        {
            using (var cli = GetClient())
            {
                cli.SimulateFailure(true, true);
            }
        }
    }
}
