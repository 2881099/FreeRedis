using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace console_netcore31_cluster
{
    public class StackExchangeRedis
    {
        static ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("180.102.130.181:7001,180.102.130.184:7001,180.102.130.181:7002");
        static IDatabase db => redis.GetDatabase();
        public void Start()
        {
            for (var k = 0; k < 1; k++)
            {
                new Thread(() =>
                {
                    for (var a = 0; a < 10000; a++)
                    {
                        try
                        {
                            db.StringGet(Guid.NewGuid().ToString());
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        Thread.CurrentThread.Join(100);
                    }
                }).Start();
            }
        }
    }
}
