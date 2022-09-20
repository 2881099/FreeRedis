using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests.Other
{
    public class CRC16Tests
    {
        private static RedisClient cli = new RedisClient(
            new ConnectionStringBuilder[]
            {
                "192.168.0.41:6379", "192.168.0.42:6379", "192.168.0.43:6379", "192.168.0.44:6379", "192.168.0.45:6379",
                "192.168.0.46:6379"
            }
        );

        [Fact]
        public void GetCRC16_1()
        {
            var r = cli.ClusterCRC16.GetCRC16("myKey");
            Assert.Equal(32665, r);
        }
        [Fact]
        public void GetSlot()
        {
            var r = cli.ClusterCRC16.GetSlot("20220809_id");
            Assert.Equal(3628, r);
        }
    }
}