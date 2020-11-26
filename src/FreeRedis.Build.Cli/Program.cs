using System;
using System.Threading.Tasks;

namespace FreeRedis.Build.Cli
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var instance = RedisClientBuilder.Builder();
            //await instance.OutputIntefaceAsync("IFreeRedisContext.cs");
            //await instance.OutputProxyAsync("FreeRedisContext.cs");
            await instance.OutputRedisHelperAsync("FreeRedisHelper.cs");

            Console.ReadLine();
        }
    }
}
