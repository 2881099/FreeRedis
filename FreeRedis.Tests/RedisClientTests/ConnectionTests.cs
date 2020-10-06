using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests
{
    public class ConnectionTests
    {
        [Fact]
        public void Auth()
        {
            using (var cli = Util.GetRedisClient())
            {
                cli.Auth("123456");
                cli.Auth("default", "123456");
            }
        }

        [Fact]
        public void ClientCaching()
        {

        }

        [Fact]
        public void ClientGetName()
        {

        }

        [Fact]
        public void ClientGetRedir()
        {

        }

        [Fact]
        public void ClientId()
        {

        }

        [Fact]
        public void ClientKill()
        {

        }

        [Fact]
        public void ClientList()
        {

        }

        [Fact]
        public void ClientPaush()
        {

        }

        [Fact]
        public void ClientReply()
        {

        }

        [Fact]
        public void ClientSetName()
        {

        }

        [Fact]
        public void ClientTracking()
        {

        }

        [Fact]
        public void ClientUnBlock()
        {

        }

        [Fact]
        public void Echo()
        {

        }

        [Fact]
        public void Hello()
        {

        }

        [Fact]
        public void Ping()
        {

        }

        [Fact]
        public void Quit()
        {

        }

        [Fact]
        public void Select()
        {

        }
    }
}
