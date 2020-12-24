using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests.Other
{
    public class ConnectionTests : TestBase
    {
        [Fact]
        public void Auth()
        {
            using (var db = cli.GetDatabase())
            {
                db.Auth("123456");
                db.Auth("default", "123456");
            }
        }

        [Fact(Skip = "Need special environment")]
        public void ClientCaching()
        {
            cli.ClientCaching(Confirm.yes);
            cli.ClientCaching(Confirm.no);
        }

        [Fact]
        public void ClientGetName()
        {
            using (var db = cli.GetDatabase())
            {
                db.ClientSetName("xxx-test001");
                Assert.Equal("xxx-test001", db.ClientGetName());
            }
        }

        [Fact]
        public void ClientGetRedir()
        {
            var r1 = cli.ClientGetRedir();
        }

        [Fact]
        public void ClientId()
        {
            var r1 = cli.ClientId();
        }

        [Fact]
        public void ClientKill()
        {
            try
            {
                cli.ClientKill("localhost");
                cli.ClientKill("localhost", 1, ClientType.master, "default", "127.0.0.1:50618", Confirm.yes);
                cli.ClientKill("localhost", 1, ClientType.normal, "default", "127.0.0.1:50618", Confirm.yes);
                cli.ClientKill("localhost", 1, ClientType.pubsub, "default", "127.0.0.1:50618", Confirm.yes);
                cli.ClientKill("localhost", 1, ClientType.slave, "default", "127.0.0.1:50618", Confirm.yes);
            }
            catch (RedisServerException ex)
            {
                Assert.Equal("ERR No such client", ex.Message);
            }
        }

        [Fact]
        public void ClientList()
        {
            var r1 = cli.ClientList();
            var r2 = cli.ClientList(ClientType.master);
            var r3 = cli.ClientList(ClientType.normal);
            var r4 = cli.ClientList(ClientType.pubsub);
            var r5 = cli.ClientList(ClientType.slave);
        }

        [Fact]
        public void ClientPause()
        {
            cli.ClientPause(1000);
        }

        [Fact]
        public void ClientReply()
        {
            using (var db = cli.GetDatabase())
            {
                //db.ClientReply(ClientReplyType.On);
                db.ClientReply(ClientReplyType.off);
                db.ClientReply(ClientReplyType.skip);
                db.ClientReply(ClientReplyType.on);
                db.SetGetTest();

                db.ClientReply(ClientReplyType.off);
                var key = Guid.NewGuid().ToString();
                db.Set(key, key);
                Assert.Null(db.Get(key));

                db.ClientReply(ClientReplyType.on);
                db.SetGetTest();
            }
        }

        [Fact]
        public void ClientSetName()
        {
            using (var db = cli.GetDatabase())
            {
                db.ClientSetName("xxx-test002");
                Assert.Equal("xxx-test002", db.ClientGetName());
            }
        }

        [Fact]
        public void ClientTracking()
        {
            using (var db = cli.GetDatabase())
            {
                db.ClientTracking(true, null, null, false, false, false, false);
                db.ClientTracking(false, null, null, false, false, false, false);
            }
        }

        [Fact]
        public void ClientUnBlock()
        {
            var r1 = cli.ClientUnBlock(1);
            var r2 = cli.ClientUnBlock(11);
            var r3 = cli.ClientUnBlock(11, ClientUnBlockType.error);
            var r4 = cli.ClientUnBlock(11, ClientUnBlockType.timeout);
        }

        [Fact]
        public void Echo()
        {
            var txt = Guid.NewGuid().ToString();
            Assert.Equal(txt, cli.Echo(txt));
        }

        [Fact]
        public void Hello()
        {
            RedisScopeExecHelper.ExecScope(new ConnectionStringBuilder()
            {
                Host = RedisEnvironmentHelper.GetHost("redis_single"),
                Password = "123456",
                MaxPoolSize = 1
            }, (cli) =>
            {
                var r1 = cli.Hello("3");
                var r2 = cli.Hello("3", "default", "123456", "myname-client");

                Assert.Equal("myname-client", cli.ClientGetName());

                var r3 = cli.Hello("2");
            });
        }

        [Fact]
        public void Ping()
        {
            Assert.Equal("PONG", cli.Ping());
            var txt = Guid.NewGuid().ToString();
            Assert.Equal(txt, cli.Ping(txt));
        }

        [Fact(Skip = "Connection pool skip")]
        public void Select()
        {
            using (var db = cli.GetDatabase())
            {
                db.Select(1);
                db.SetGetTest();
                Assert.Equal("PONG", db.Ping());

                var key = Guid.NewGuid().ToString();
                db.Set(key, key);
                Assert.Equal(key, db.Get(key));


                db.Select(1);
                db.SetGetTest();
                Assert.Equal("PONG", db.Ping());

                Assert.NotEqual(key, db.Get(key));
                db.Set(key, key);
                Assert.Equal(key, db.Get(key));
            }
        }
    }
}
