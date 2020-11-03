using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace FreeRedis.Tests
{
    public static class Utils
    {
        public static void SetGetTest(this RedisClient cli)
        {
            var key = Guid.NewGuid().ToString();
            cli.Set(key, key);
            Assert.Equal(key, cli.Get(key));
        }
    }
}
