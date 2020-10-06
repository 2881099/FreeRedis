using System;
using System.Collections.Generic;
using System.Text;

namespace FreeRedis.Tests
{
    public static class Util
    {
        public static RedisClient GetRedisClient() => new RedisClient("127.0.0.1:6379", false);
    }
}
