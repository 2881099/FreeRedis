using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests
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
            Assert.Equal("ERR Unknown category 'testcategory'", Assert.Throws<RedisException>(() => cli.AclCat("testcategory"))?.Message);
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
            Assert.Equal("keys", r1.keys[0]);

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
        public void AclList()
        {
            var key1 = "AclList1";
            Assert.True(cli.AclList().Length > 0);
        }

        [Fact]
        public void AclLoad()
        {
            var key1 = "AclLoad1";
        }

        [Fact]
        public void AclLog()
        {
            var key1 = "AclLog1";
        }

        [Fact]
        public void AclSave()
        {
            var key1 = "AclSave1";
        }

        [Fact]
        public void AclSetUser()
        {
            var key1 = "AclSetUser1";
        }

        [Fact]
        public void AclUsers()
        {
            var key1 = "AclUsers1";
            var r1 = cli.AclUsers();
            Assert.True(r1.Length > 0);
            Assert.Equal("default", r1[0]);

            cli.AclSetUser(key1);
            Assert.Equal(2, cli.AclUsers().Length);
            var r2 = cli.AclUsers();

            Assert.Equal("default", r2[0]);
            Assert.Equal(key1, r2[1]);

            Assert.Equal(1, cli.AclDelUser(key1));
        }

        [Fact]
        public void AclWhoami()
        {
            var key1 = "AclWhoami1";
            Assert.Equal("default", cli.AclWhoami());
        }

    }
}
