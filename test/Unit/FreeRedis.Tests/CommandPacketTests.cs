using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using Xunit;

namespace FreeRedis.Tests
{
    public class CommandPacketTests
    {

        [Fact]
        public void Prefix()
        {
            var cmd1 = new CommandPacket("GET").InputKey("key1").Prefix("prefix_");
            Assert.Equal("GET prefix_key1", cmd1.ToString());
            Assert.Equal("GET prefix01_key1", cmd1.Prefix("prefix01_").ToString()); //replace

            var cmd2 = new CommandPacket("MGET").InputKey(new[] { "key1", "key2" }).Prefix("prefix_");
            Assert.Equal("MGET prefix_key1 prefix_key2", cmd2.ToString());
            Assert.Equal("MGET prefix01_key1 prefix01_key2", cmd2.Prefix("prefix01_").ToString()); //replace
        }

        [Fact]
        public void GetKey()
        {
            var cmd1 = new CommandPacket("GET").InputKey("key1").Prefix("prefix_");
            Assert.Equal("prefix_key1", cmd1.GetKey(0));
            Assert.Equal("key1", cmd1.GetKey(0, true));
            Assert.Equal("prefix01_key1", cmd1.Prefix("prefix01_").GetKey(0)); //replace
            Assert.Equal("key1", cmd1.Prefix("prefix01_").GetKey(0, true)); //replace

            var cmd2 = new CommandPacket("MGET").InputKey(new[] { "key1", "key2" }).Prefix("prefix_");
            Assert.Equal("prefix_key1", cmd2.GetKey(0));
            Assert.Equal("key1", cmd2.GetKey(0, true));
            Assert.Equal("prefix_key2", cmd2.GetKey(1));
            Assert.Equal("key2", cmd2.GetKey(1, true));
            Assert.Equal("prefix01_key1", cmd2.Prefix("prefix01_").GetKey(0)); //replace
            Assert.Equal("key1", cmd2.Prefix("prefix01_").GetKey(0, true)); //replace
            Assert.Equal("prefix01_key2", cmd2.Prefix("prefix01_").GetKey(1)); //replace
            Assert.Equal("key2", cmd2.Prefix("prefix01_").GetKey(1, true)); //replace
        }
    }
}
