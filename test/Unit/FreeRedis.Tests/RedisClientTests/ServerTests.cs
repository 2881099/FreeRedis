using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests.Other
{
    public class ServerTests : TestBase
    {
        [Fact]
        public void AclCat()
        {
            var r1 = cli.AclCat();
            Assert.NotEmpty(r1);

            var r2 = cli.AclCat("scripting");
            Assert.NotEmpty(r2);
            Assert.Equal("ERR Unknown category 'testcategory'", Assert.Throws<RedisServerException>(() => cli.AclCat("testcategory"))?.Message);
        }

        [Fact]
        public void AclDelUser()
        {
            var key1 = "AclDelUser1";
            var key2 = "AclDelUser2";
            cli.AclSetUser(key1);
            cli.AclSetUser(key2);

            var r1 = cli.AclList();
            var r2 = cli.AclDelUser(key1, key2);
            Assert.Equal(2, r2);
            var r3 = cli.AclList();

            Assert.Equal(r1.Length - 2, r3.Length);
        }

        [Fact]
        public void AclGenPass()
        {
            Assert.Equal("dd721260bfe1b3d9601e7fbab36de6d04e2e67b0ef1c53de59d45950db0dd3cc".Length, cli.AclGenPass().Length);
            Assert.Equal("355ef3dd".Length, cli.AclGenPass(32).Length);
            Assert.Equal("90".Length, cli.AclGenPass(5).Length);
        }

        [Fact]
        public void AclGetUser()
        {
            var key1 = "AclGetUser1";

            var r1 = cli.AclGetUser();
            Assert.NotNull(r1);
            Assert.NotEmpty(r1.flags);
            Assert.True(!string.IsNullOrWhiteSpace(r1.commands));
            Assert.NotEmpty(r1.keys);

            cli.AclDelUser(key1);
            cli.AclSetUser(key1);
            var r2 = cli.AclGetUser(key1);
            Assert.NotNull(r1);
            Assert.Single(r2.flags);
            Assert.Equal("off", r2.flags[0]);
            Assert.Empty(r2.passwords);
            Assert.Empty(r2.keys);

            cli.AclSetUser(key1, "reset", "+@all", "~*", "-@string", "+incr", "-debug", "+debug|digest");
            var r3 = cli.AclGetUser(key1);
            Assert.NotNull(r3);
            Assert.Contains("+@all", r3.commands);
            Assert.Contains("-@string", r3.commands);
            Assert.Contains("+debug|digest", r3.commands);

            cli.AclDelUser(key1);
        }

        [Fact]
        public void AclSetUser()
        {
            var key1 = "AclSetUser1";
            cli.AclDelUser(key1);
            cli.AclSetUser(key1, ">123456");

            using (var sh = cli.GetDatabase())
            {
                Assert.Equal("WRONGPASS invalid username-password pair", Assert.Throws<RedisServerException>(() => sh.Auth(key1, "123456"))?.Message);
            }

            cli.AclSetUser(key1, "on", "+acl");

            using (var sh = cli.GetDatabase())
            {
                sh.Auth(key1, "123456");
                var r1 = sh.AclWhoami();
                Assert.Equal(key1, r1);

                sh.Quit();
            }

            cli.AclSetUser(key1, "<123456");
            cli.AclDelUser(key1);
        }

        [Fact]
        public void AclUsers()
        {
            var key1 = "AclUsers1";
            var r1 = cli.AclUsers();
            Assert.True(r1.Length > 0);

            cli.AclSetUser(key1);
            Assert.Equal(2, cli.AclUsers().Length);
            var r2 = cli.AclUsers();

            Assert.Contains(r2, a => a == key1);

            Assert.Equal(1, cli.AclDelUser(key1));
        }

        [Fact]
        public void AclWhoami()
        {
            Assert.Equal("default", cli.AclWhoami());
        }


        [Fact]
        public void BgRewriteAof()
        {
            var r1 = cli.BgRewriteAof();
        }

        [Fact]
        public void BgSave()
        {
            //var r1 = cli.BgSave();
        }

        [Fact]
        public void Command()
        {
            var r1 = cli.Command();
        }

        [Fact]
        public void CommandCount()
        {
            var r1 = cli.CommandCount();
            Assert.True(r1 > 0);
        }

        [Fact]
        public void CommandGetKeys()
        {
            var r1 = cli.CommandGetKeys("set", "key1", "val1");
            Assert.Single(r1);
            Assert.Equal("key1", r1[0]);
        }

        [Fact]
        public void CommandInfo()
        {
            var r1 = cli.CommandInfo("get", "set", "hset");
            Assert.Equal(3, r1.Length);
        }

        [Fact]
        public void ConfigGet()
        {
            var r1 = cli.ConfigGet("*max-*-entries*");
            Assert.NotEmpty(r1);
        }

        [Fact]
        public void ConfigResetStat()
        {
            cli.ConfigResetStat();
        }

        [Fact]
        public void ConfigRewrite()
        {
            //cli.ConfigRewrite();
        }

        [Fact]
        public void ConfigSet()
        {
            //cli.ConfigSet("hash-max-zipmap-entries", 512);
        }

        [Fact]
        public void DbSize()
        {
            var key1 = "DbSize1";
            cli.Set(key1, Guid.NewGuid());

            Assert.True(cli.DbSize() > 0);
        }

        [Fact]
        public void DebugObject()
        {
            var key1 = "DebugObject1";
            cli.Set(key1, Guid.NewGuid());

            Assert.NotNull(cli.DebugObject(key1));
        }

        [Fact]
        public void DebugSegfault()
        {
            //using (var sh = cli.GetShareClient())
            //{
            //    cli.DebugSegfault();
            //    var r1 = sh.AclWhoami();
            //}
        }

        [Fact]
        public void FlushAll()
        {
            RedisScopeExecHelper.ExecScope("redis_flush", (cli) =>
            {
                using (var sh = cli.GetDatabase(7))
                {
                    cli.FlushAll(true);
                    cli.FlushAll(false);
                }
            });
        }

        [Fact]
        public void FlushDb()
        {
            //using (var sh = cli.GetDatabase(7))
            //{
            //    cli.FlushDb(true);
            //    cli.FlushDb(false);
            //}
        }

        [Fact]
        public void Info()
        {
            var r1 = cli.Info();
            var r2 = cli.Info("server");
            Assert.NotNull(r1);
            Assert.NotNull(r2);
        }

        [Fact]
        public void LastSave()
        {
            cli.Save();
            var r1 = cli.LastSave();
        }

        [Fact]
        public void LatencyDoctor()
        {
            var r1 = cli.LatencyDoctor();
            Assert.NotNull(r1);
        }

        [Fact]
        public void MemoryDoctor()
        {
            var r1 = cli.LatencyDoctor();
            Assert.NotNull(r1);
        }

        [Fact]
        public void MemoryMallocStats()
        {
            var r1 = cli.LatencyDoctor();
            Assert.NotNull(r1);
        }

        [Fact]
        public void MemoryPurge()
        {
            cli.MemoryPurge();
        }

        [Fact]
        public void MemoryStats()
        {
            var r1 = cli.MemoryStats();
            Assert.NotEmpty(r1);
        }

        [Fact]
        public void MemoryUsage()
        {
            var key1 = "MemoryUsage1";
            cli.Set(key1, "123");
            var r1 = cli.MemoryUsage(key1);
            Assert.True(r1 > 0);
        }

        [Fact]
        public void Role()
        {
            var r1 = cli.Role();
            Assert.NotNull(r1);
            //Assert.Equal(RoleType.Master, r1.role);
        }
    }
}
