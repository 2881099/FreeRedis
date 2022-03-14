using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{
    public class ClientPool2<T> where T: RedisClientBase,new()
    {
        private string _ip;
        private int _port;
        private T[] _clients;
        private const int _length = 5;
        public int[] CallCounter;
        public ClientPool2(string ip,int port)
        {
            _ip = ip;
            _port = port;
            _clients = new T[_length];
            CallCounter = new int[_length];
            for (int i = 0; i < _length; i++)
            {
                var temp = new T();
                temp.CreateConnection(ip, port);
                _clients[i] = temp;
            }
        }

        public Task<bool> SetAsync(string key,string value)
        {
            var index = Thread.CurrentThread.ManagedThreadId & _length;
            CallCounter[index] += 1;
            return _clients[index].SetAsync(key, value);
        }


    }
}
