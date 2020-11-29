using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{
    public class ClientPool1<T> where T: RedisClientBase,new()
    {
        private string _ip;
        private int _port;
        private T[] _clients;
        public T _node;
        public int[] CallCounter;
        private long[] _locks;
        private int _length;
        public ClientPool1(string ip,int port)
        {
            _ip = ip;
            _port = port;
            _length = 5;
            _clients = new T[_length];
            CallCounter = new int[_length];
            _locks = new long[_length];
            for (int i = 0; i < _length; i++)
            {
                var temp = new T();
                temp.CreateConnection(ip, port);
                _clients[i] = temp;
            }

            _node = new T();
            _node.CreateConnection(ip, port);
        }

        private volatile int _index;
        public Task<bool> SetAsync(string key,string value)
        {
            for (int i = _index; i < _length; i+=1)
            {

                if (Interlocked.CompareExchange(ref _locks[i], 1, 0) == 0)
                {
                    _index = i + 1;
                    if (_index == _length)
                    {
                        _index = 0;
                    }
                    //Interlocked.Increment(ref CallCounter[i]);
                    var task = _clients[i].SetAsync(key, value);
                    _locks[i] = 0;
                    return task;
                }
                
            }
            return _node.SetAsync(key, value);
        }


    }
}
