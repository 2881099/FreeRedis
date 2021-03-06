﻿using FreeRedis;
using System;

namespace FreeRedis.Build.Cli
{
    public static class TemplateHelper
    {
        private static RedisClient _redisClient;

        public static void AddClient(RedisClient redisClient)
        {
            _redisClient = redisClient;
        }

        public static void CreateInstance(ConnectionStringBuilder sentinelConnectionString, string[] sentinels, bool rw_splitting)
        {
            _redisClient = new RedisClient(sentinelConnectionString, sentinels, rw_splitting);
        }

        public static void CreateInstance(ConnectionStringBuilder[] connectionStrings, Func<string, string> redirectRule)
        {
            _redisClient = new RedisClient(connectionStrings, redirectRule);
        }

        public static void CreateInstance(ConnectionStringBuilder[] clusterConnectionStrings)
        {
            _redisClient = new RedisClient(clusterConnectionStrings);
        }

        public static void CreateInstance(ConnectionStringBuilder connectionString, params ConnectionStringBuilder[] slaveConnectionStrings)
        {
            _redisClient = new RedisClient(connectionString, slaveConnectionStrings);
        }


    }
}
