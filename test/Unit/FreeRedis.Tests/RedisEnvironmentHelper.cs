using System;

namespace FreeRedis.Tests
{
    public static class RedisEnvironmentHelper
    {
        public static string GetHost(string serviceName)
        {
            return Environment.GetEnvironmentVariable($"DOCKER_HOST_{serviceName}");
        }
    }
}