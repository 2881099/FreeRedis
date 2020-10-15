using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests
{
    public class ConnectionTests : TestBase
    {
        [Fact]
        public void Auth()
        {
            cli.Auth("123456");
            cli.Auth("default", "123456");
        }

        [Fact]
        public void ClientCaching()
        {
            cli.ClientCaching(Confirm.Yes);
            cli.ClientCaching(Confirm.No);
        }

        [Fact]
        public void ClientGetName()
        {
            Assert.Null(cli.ClientGetName());
            cli.ClientSetName("xxx-test001");
            Assert.Equal("xxx-test001", cli.ClientGetName());
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
                cli.ClientKill("localhost", 1, ClientType.Master, "default", "127.0.0.1:50618", Confirm.Yes);
                cli.ClientKill("localhost", 1, ClientType.Normal, "default", "127.0.0.1:50618", Confirm.Yes);
                cli.ClientKill("localhost", 1, ClientType.PubSub, "default", "127.0.0.1:50618", Confirm.Yes);
                cli.ClientKill("localhost", 1, ClientType.Slave, "default", "127.0.0.1:50618", Confirm.Yes);
            }
            catch (RedisException ex)
            {
                Assert.Equal("ERR No such client", ex.Message);
            }
        }

        [Fact]
        public void ClientList()
        {
            var r1 = cli.ClientList();
            var r2 = cli.ClientList(ClientType.Master);
            var r3 = cli.ClientList(ClientType.Normal);
            var r4 = cli.ClientList(ClientType.PubSub);
            var r5 = cli.ClientList(ClientType.Slave);
        }

        [Fact]
        public void ClientPause()
        {
            cli.ClientPause(1000);
        }

        [Fact]
        public void ClientReply()
        {
            //cli.ClientReply(ClientReplyType.On);
            cli.ClientReply(ClientReplyType.Off);
            cli.ClientReply(ClientReplyType.Skip);
            cli.ClientReply(ClientReplyType.On);

            cli.SetGetTest();

            cli.ClientReply(ClientReplyType.Off);

            var key = Guid.NewGuid().ToString();
            cli.Set(key, key);
            Assert.Null(cli.Get(key));
        }

        [Fact]
        public void ClientSetName()
        {
            Assert.Null(cli.ClientGetName());
            cli.ClientSetName("xxx-test002");
            Assert.Equal("xxx-test002", cli.ClientGetName());
        }

        [Fact]
        public void ClientTracking()
        {
            cli.ClientTracking(true, null, null, false, false, false, false);
            cli.ClientTracking(false, null, null, false, false, false, false);
        }

        [Fact]
        public void ClientUnBlock()
        {
            var r1 = cli.ClientUnBlock(1);
            var r2 = cli.ClientUnBlock(11);
            var r3 = cli.ClientUnBlock(11, ClientUnBlockType.Error);
            var r4 = cli.ClientUnBlock(11, ClientUnBlockType.Timeout);
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
            var r1 = cli.Hello("3");
            var r2 = cli.Hello("3", "default", "123456", "myname-client");

            Assert.Equal("myname-client", cli.ClientGetName());

            var r3 = cli.Hello("2");
        }

        [Fact]
        public void Ping()
        {
            Assert.Equal("PONG", cli.Ping());
            var txt = Guid.NewGuid().ToString();
            Assert.Equal(txt, cli.Ping(txt));
        }

        [Fact]
        public void Quit()
        {
            Assert.Equal("PONG", cli.Ping());
            cli.Quit();
            Assert.Throws<Exception>(() => cli.Ping());
        }

        [Fact]
        public void Select()
        {
            cli.Select(0);
            cli.SetGetTest();
            Assert.Equal("PONG", cli.Ping());

            var key = Guid.NewGuid().ToString();
            cli.Set(key, key);
            Assert.Equal(key, cli.Get(key));


            cli.Select(1);
            cli.SetGetTest();
            Assert.Equal("PONG", cli.Ping());

            Assert.NotEqual(key, cli.Get(key));
            cli.Set(key, key);
            Assert.Equal(key, cli.Get(key));
        }
    }
}
