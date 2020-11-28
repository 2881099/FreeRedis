using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{
    public class ClientPool1<T> where T: RedisClientBase,new()
    {
        private string _ip;
        private int _port;
        private T[] _clients;
        public T _node;
        public ClientPool1(string ip,int port)
        {
            _ip = ip;
            _port = port;
            _clients = new T[4];
            for (int i = 0; i < 4; i++)
            {
                var temp = new T();
                temp.CreateConnection(ip, port);
                _clients[i] = temp;
            }

            _node = new T();
            _node.CreateConnection(ip, port);
        }

        public Task<bool> SetAsync(string key,string value)
        {
            for (int i = 0; i < _clients.Length; i++)
            {
                if (_clients[i].IsCompleted)
                {
                    return _clients[i].SetAsync(key, value);
                }
            }
            return _node.SetAsync(key, value);
        }


    }
}
