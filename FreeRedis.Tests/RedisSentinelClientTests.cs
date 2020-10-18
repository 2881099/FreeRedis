using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace FreeRedis.Tests
{
    public class RedisSentinelClientTests
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
                Assert.Equal(Model.SentinelRoleType.Sentinel, rt.Role);
                Assert.True(rt.Masters.Any());
                Assert.Equal("mymaster", rt.Masters.FirstOrDefault());
            }
        }

        [Fact]
        public void SentinelMasters()
        {
            using (var cli = GetClient())
            {
                var rt = cli.SentinelMasters();
                Assert.True(rt.Any());
                Assert.Equal("mymaster", rt[0].name);
            }
        }

        [Fact]
        public void SentinelMaster()
        {
            using (var cli = GetClient())
            {
                var rt = cli.SentinelMaster("mymaster");
                Assert.NotNull(rt);
                Assert.Equal("mymaster", rt.name);

                Assert.Equal("ERR No such master with that name",
                    Assert.Throws<RedisException>(() => cli.SentinelMaster("mymaster222")).Message);
            }
        }

        [Fact]
        public void SentinelSalves()
        {
            using (var cli = GetClient())
            {
                var rt = cli.SentinelSalves("mymaster");
                Assert.True(rt.Any());
                Assert.Equal("ok", rt[0].master_link_status);
            }
        }

        [Fact]
        public void SentinelSentinels()
        {
            using (var cli = GetClient())
            {
                var rt = cli.SentinelSentinels("mymaster");
                Assert.True(rt.Any());
                Assert.Equal("127.0.0.1", rt[0].ip);
            }
        }

        [Fact]
        public void SentinelGetMasterAddrByName()
        {
            using (var cli = GetClient())
            {
                var rt = cli.SentinelGetMasterAddrByName("mymaster");
                Assert.False(string.IsNullOrEmpty(rt));
            }
        }

        [Fact]
        public void SentinelIsMasterDownByAddr()
        {
            using (var cli = GetClient())
            {
                var st = cli.SentinelSentinels("mymaster");
                Assert.True(st.Any());
                var rt = cli.SentinelIsMasterDownByAddr(st[0].name, st[0].port, st[0].voted_leader_epoch, st[0].runid);
                Assert.NotNull(rt);
                Assert.False(rt.down_state);
                Assert.Equal("*", rt.leader);
                Assert.Equal(st[0].voted_leader_epoch, rt.vote_epoch);
            }
        }

        [Fact]
        public void SentinelReset()
        {
            using (var cli = GetClient())
            {
                var rt = cli.SentinelReset("*");
                Assert.True(rt > 0);
            }
        }

        [Fact]
        public void SentinelFailover()
        {
            using (var cli = GetClient())
            {
                cli.SentinelFailover("mymaster");

                Assert.Equal("ERR No such master with that name",
                    Assert.Throws<RedisException>(() => cli.SentinelFailover("mymaster222")).Message);
            }
        }

        [Fact]
        public void SentinelPendingScripts()
        {
            using (var cli = GetClient())
            {
                var rt = cli.SentinelPendingScripts();
            }
        }

        [Fact]
        public void SentinelFlushConfig()
        {
            using (var cli = GetClient())
            {
                cli.SentinelFlushConfig();
            }
        }

        [Fact]
        public void SentinelRemove()
        {
            using (var cli = GetClient())
            {
                //cli.SentinelRemove("mymaster");

                Assert.Equal("ERR No such master with that name",
                    Assert.Throws<RedisException>(() => cli.SentinelRemove("mymaster222")).Message);
            }
        }

        [Fact]
        public void SentinelCkQuorum()
        {
            using (var cli = GetClient())
            {
                var rt = cli.SentinelCkQuorum("mymaster");

                Assert.Equal("ERR No such master with that name",
                    Assert.Throws<RedisException>(() => cli.SentinelCkQuorum("mymaster222")).Message);
            }
        }

        [Fact]
        public void SentinelSet()
        {
            using (var cli = GetClient())
            {
                cli.SentinelSet("mymaster", "down-after-milliseconds", "5000");
            }
        }

        [Fact]
        public void SentinelInfoCache()
        {
            using (var cli = GetClient())
            {
                var rt = cli.SentinelInfoCache("mymaster");
            }
        }

        [Fact]
        public void SentinelSimulateFailure()
        {
            using (var cli = GetClient())
            {
                cli.SentinelSimulateFailure(true, true);
            }
        }
    }
}
