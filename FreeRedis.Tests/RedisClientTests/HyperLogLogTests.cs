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
    public class HyperLogLogTests : TestBase
    {
        [Fact]
        public void PfAdd()
        {
            var key1 = "PfAdd1";
            cli.Del(key1);
            Assert.True(cli.PfAdd(key1, "a", "b", "c", "d", "e", "f", "g"));
            Assert.False(cli.PfAdd(key1, "a", "b", "c", "d", "e", "f", "g"));
            Assert.True(cli.PfAdd(key1, "a", "b", "c", "d", "e", "f", "g", "h"));
        }

        [Fact]
        public void PfCount()
        {
            var key1 = "PfCount1";
            cli.Del(key1);
            Assert.True(cli.PfAdd(key1, "a", "b", "c", "d", "e", "f", "g"));
            Assert.Equal(7, cli.PfCount(key1));

            var key2 = "PfCount2";
            cli.Del(key2);
            Assert.True(cli.PfAdd(key2, "foo", "bar", "zap"));
            Assert.False(cli.PfAdd(key2, "zap", "zap", "zap"));
            Assert.False(cli.PfAdd(key2, "foo", "bar"));
            Assert.Equal(3, cli.PfCount(key2));
            Assert.Equal(10, cli.PfCount(key1, key2));
            Assert.Equal(10, cli.PfCount(key1, key2, Guid.NewGuid().ToString()));
            Assert.Equal(10, cli.PfCount(Guid.NewGuid().ToString(), key1, key2, Guid.NewGuid().ToString()));
        }

        [Fact]
        public void PfMerge()
        {
            var key1 = "PfMerge1";
            cli.Del(key1);
            Assert.True(cli.PfAdd(key1, "foo", "bar", "zap", "a"));
            Assert.Equal(4, cli.PfCount(key1));

            var key2 = "PfMerge2";
            cli.Del(key2);
            Assert.True(cli.PfAdd(key2, "a", "b", "c", "foo"));
            Assert.Equal(4, cli.PfCount(key2));

            var key3 = "PfMerge3";
            cli.Del(key3);
            cli.PfMerge(key3, key1, key2);
            Assert.Equal(6, cli.PfCount(key3));
            cli.PfMerge(key3, key1, key2, key3);
            Assert.Equal(6, cli.PfCount(key3));

            cli.Del(key3);
            cli.PfMerge(key3, Guid.NewGuid().ToString());
            Assert.Equal(0, cli.PfCount(key3));

            cli.Del(key3);
            cli.PfMerge(key3, key1);
            Assert.Equal(4, cli.PfCount(key3));

            cli.PfMerge(key3, key2);
            Assert.Equal(6, cli.PfCount(key3));
        }
    }
}
