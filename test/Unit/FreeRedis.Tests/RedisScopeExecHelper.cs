using System;

namespace FreeRedis.Tests
{
    public static class RedisScopeExecHelper
    {
        public static void ExecScope(string connectionString, Action<RedisClient> func)
        {
            using (var scope = new RedisClient(RedisEnvironmentHelper.GetHost(connectionString)))
            {
                func.Invoke(scope);
            }
        }

        public static void ExecScope(ConnectionStringBuilder connectionStringBuilder, Action<RedisClient> func)
        {
            using (var scope = new RedisClient(connectionStringBuilder))
            {
                func.Invoke(scope);
            }
        }
    }
}