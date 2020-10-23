using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests
{
    public class StreamsTests : TestBase
    {
        [Fact]
        public void XAck()
        {
            var key1 = "XAck1";
            cli.Del(key1);
        }

        [Fact]
        public void XAdd()
        {
            var key1 = "XAdd1";
            cli.Del(key1);
        }

        [Fact]
        public void XClaim()
        {
            var key1 = "XClaim1";
            cli.Del(key1);
        }

        [Fact]
        public void XClaimJustId()
        {
            var key1 = "XClaimJustId1";
            cli.Del(key1);
        }

        [Fact]
        public void XDel()
        {
            var key1 = "XDel1";
            cli.Del(key1);
        }

        [Fact]
        public void XGroupCreate()
        {
            var key1 = "XGroupCreate1";
            cli.Del(key1);
        }

        [Fact]
        public void XGroupSetId()
        {
            var key1 = "XGroupSetId1";
            cli.Del(key1);
        }

        [Fact]
        public void XGroupDestroy()
        {
            var key1 = "XGroupDestroy1";
            cli.Del(key1);
        }

        [Fact]
        public void XGroupDelConsumer()
        {
            var key1 = "XGroupDelConsumer1";
            cli.Del(key1);
        }

        [Fact]
        public void XInfoStream()
        {
            var key1 = "XInfoStream1";
            cli.Del(key1);
        }

        [Fact]
        public void XInfoGroups()
        {
            var key1 = "XInfoGroups1";
            cli.Del(key1);
        }

        [Fact]
        public void XInfoConsumers()
        {
            var key1 = "XInfoConsumers1";
            cli.Del(key1);
        }

        [Fact]
        public void XLen()
        {
            var key1 = "XLen1";
            cli.Del(key1);
        }

        [Fact]
        public void XPending()
        {
            var key1 = "XPending1";
            cli.Del(key1);
        }

        [Fact]
        public void XRange()
        {
            var key1 = "XRange1";
            cli.Del(key1);
        }

        [Fact]
        public void XRevRange()
        {
            var key1 = "XRevRange1";
            cli.Del(key1);
        }

        [Fact]
        public void XRead()
        {
            var key1 = "XRead1";
            cli.Del(key1);
        }

        [Fact]
        public void XReadGroup()
        {
            var key1 = "XReadGroup1";
            cli.Del(key1);
        }

        [Fact]
        public void XTrim()
        {
            var key1 = "XTrim1";
            cli.Del(key1);
        }
    }
}
