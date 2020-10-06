using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace FreeRedis.Tests
{
    public static class Util
    {
        public static RedisClient GetRedisClient() => new RedisClient("127.0.0.1:6379", false);
        //public static RedisClient GetRedisClient() => new RedisClient("192.168.164.10:6379", false);

        public static void SetGetTest(this RedisClient cli)
        {
            var key = Guid.NewGuid().ToString();
            cli.Set(key, key);
            Assert.Equal(key, cli.Get(key));
        }
    }
}
