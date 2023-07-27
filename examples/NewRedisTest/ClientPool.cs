using FreeRedis.Engine;
using FreeRedis.NewClient.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewRedisTest
{
    internal class ClientPool
    {
        public readonly FreeRedisClient Client1;
        public readonly FreeRedisClient Client2;
        private FreeRedisClient _current_client;
        public ClientPool(string connectionString)
        {
            Client1 = new(connectionString, msg => Console.WriteLine(msg));
            Client2 = new(connectionString, msg => Console.WriteLine(msg));
            _current_client = Client1;
        }


        public async ValueTask<bool> SetAsync(string key, string value)
        {
            if (!Client1.IsCompleted)
            {
                return await Client2.SetAsync5(key, value);
            }
           return await Client1.SetAsync5(key, value);
        }



    }
}
