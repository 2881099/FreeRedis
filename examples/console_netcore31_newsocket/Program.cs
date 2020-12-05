using FreeRedis;
using Microsoft.AspNetCore.Connections;
using StackExchange.Redis;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{

    /// <summary>
    /// 移除了 SocketTrace
    /// 缓冲池等设计保留原样，该功能尚未测试
    /// </summary>
    class Program
    {
        private static bool _useDelay;
        private static int _delayCount;
        private static int port;
        private static string ip;
        private static string pwd;
        private const int frequence = 300000;

        private static RedisClient _freeRedisClient;
        private static BeetleX.Redis.RedisDB _beetleClient;
        //private static NewRedisClient0 _redisClient0;
        private static NewRedisClient1 _redisClient1;
        private static NewRedisClient2 _redisClient2;
        private static NewRedisClient5 _redisClient5;
        private static NewRedisClient7 _redisClient7;
        private static NewRedisClient3 _redisClient3;
        private static NewRedisClient4 _redisClient4;
        private static NewRedisClient8 _redisClient8;
        private static NewRedisClient9 _redisClient9;
        private static NewRedisClient12 _redisClient12;
        private static NewRedisClient14 _redisClient14;
        private static ClientPool3 _pool10;
        private static ClientPool4 _pool13;
        private static ClientPool5 _pool14;
        private static NewLife.Caching.Redis _newLifeRedis;

        private static IDatabase _stackExnchangeClient;


        private static ClientPool1<NewRedisClient9> _pool9;
        private static ClientPool1<NewRedisClient7> _pool7;
        private static ClientPool1<NewRedisClient4> _pool4;
        private static ClientPool1<NewRedisClient5> _pool5;

        private static ClientPool2<NewRedisClient7> _pool27;
        private static ClientPool2<NewRedisClient4> _pool24;
        private static ClientPool2<NewRedisClient5> _pool25;

        private static Action<string> _beforeSw;
        public static ConnectionMultiplexer seredis;
        static void Main(string[] args)
        {
            Console.WriteLine("按键开始！");
            //Console.ReadKey();
            Configuration();
            //Thread.Sleep(3000);

            //for (int i = 0; i < 100; i++)
            //{
            //    RunTest();
            //}
            RunTest();
            //Console.WriteLine("====== 以上预热 =======");
            //RunTest();

            //_pool10.ShowHandlerCount();
            //CheckPool();
            //Console.WriteLine(_pool10.GetLockCount1());
            //Console.WriteLine(_pool10.GetLockCount2());
            //Console.WriteLine(_pool10.GetLockCount3());
            Console.ReadKey();

        }


        private static void Configuration()
        {

            //_useDelay = true;
            //_delayCount = 20000;
            //Notice : Please use "//" comment "/*".

            ///*
            using (StreamReader stream = new StreamReader("Redis.rsf"))
            {
                ip = stream.ReadLine();
                port = int.Parse(stream.ReadLine());
                pwd = stream.ReadLine();
            }//*/
            /*
            ip = "127.0.0.1";
            port = 6379;
            //*/

            // NewLife.Redis
            // _newLifeRedis = new NewLife.Caching.Redis($"{ip}:{port}",null, 1);
            //var result = newLifeRedis.Set("1", "1");
            //Console.WriteLine(result);
            //Console.ReadKey();
            //_beetleClient = new BeetleX.Redis.RedisDB(0);
            //var host = _beetleClient.Host.AddWriteHost(ip, port);
            //host.MaxConnections = 1000;
            //host.QueueMaxLength = 512;
            //_freeRedisClient = new RedisClient($"{ip}:{port},database=0,min pool size=100");
            //_redisClient0 = new NewRedisClient0(ip, port);

            //_pool4 = new ClientPool1<NewRedisClient4>(ip, port);
            //_pool5 = new ClientPool1<NewRedisClient5>(ip, port);
            //_pool7 = new ClientPool1<NewRedisClient7>(ip, port);
            //_pool9 = new ClientPool1<NewRedisClient9>(ip, port);
            //_pool27 = new ClientPool2<NewRedisClient7>(ip, port);
            //_pool25 = new ClientPool2<NewRedisClient5>(ip, port);
            //_pool24 = new ClientPool2<NewRedisClient4>(ip, port);
            //_redisClient1 = new NewRedisClient1(ip, port);
            //_redisClient2 = new NewRedisClient2(ip, port);
            //_redisClient3 = new NewRedisClient3(ip, port);
            //_redisClient12 = new NewRedisClient12();
            //_redisClient12.CreateConnection(ip, port);
            _redisClient14 = new NewRedisClient14();
            _redisClient14.CreateConnection(ip, port);
            //for (int i = 0; i < 100; i++)
            //{
            //    Parallel.For(0, 10, (i) =>
            //    {
            //        var key = i.ToString();
            //        _redisClient14.SetAsync(key, key);
            //    });
            //}
            
           
            //_redisClient14.AuthAsync(pwd);
            _redisClient4 = new NewRedisClient4();
            _redisClient4.CreateConnection(ip, port);
            //_redisClient4.AuthAsync(pwd);
            //_redisClient4.AuthAsync(pwd);
            //_redisClient5 = _pool5._node;
            //_redisClient7 = _pool7._node;
            //_redisClient8 = new NewRedisClient8();
            //_redisClient8.CreateConnection(ip, port);
            //_redisClient9 = new NewRedisClient9();
            //_redisClient9.CreateConnection(ip, port);
            //_redisClient9.AuthAsync(pwd);
            ///_pool13 = new ClientPool4(ip, port);
            //_pool13.AuthAsync(pwd);
            //_pool10 = new ClientPool3(ip, port);
            //_pool10.AuthAsync(pwd);
            //_pool14 = new ClientPool5(ip, port);
            //_pool14.AuthAsync(pwd);
            //_redisClient5.SetAsync("a", "a");
            seredis = ConnectionMultiplexer.Connect($"{ip}:{port}");
            _stackExnchangeClient = seredis.GetDatabase(0);


            //_beforeSw = (str) =>
            //{
            //    if (str.Contains("Pool10"))
            //    {
            //        _pool10.Start();
            //    }
            //};


        }


        public static void RunTest()
        {
            //Thread.Sleep(3000);
            //FreeRedisSetTest();
            //StackExchangeRedisSetTest();
            //StackExchangeRedisSetTest();
            //StackExchangeRedisSetTest();
            //StackExchangeRedisSetTest();
            //StackExchangeRedisSetTest();
            //StackExchangeRedisSetTest();
            StackExchangeRedisSetTest();
            StackExchangeRedisSetTest();
            NewSocketRedis14SetTest();
            NewSocketRedis14SetTest();
            //NewSocketRedis14SetTest();
            //NewSocketRedis14SetTest();
            //NewSocketRedis14SetTest();
            //NewSocketRedis14SetTest();
            //NewSocketRedis14SetTest();
            //Pool514SetTest();
            //Pool514SetTest();
            //Pool514SetTest();
            //Pool514SetTest();
            //Pool514SetTest();
            //Pool514SetTest();
            //Pool310SetTest();
            //Pool310SetTest();
            //Pool310SetTest();
            //Pool310SetTest();
            //Pool310SetTest();
            //Pool310SetTest();
            //seredis.Close();
            //seredis.Dispose();
            //NewSocketRedis4SetTest();
            //NewSocketRedis4SetTest();
            //NewSocketRedis4SetTest();
            //NewSocketRedis4SetTest();
            //NewSocketRedis4SetTest();
            //NewSocketRedis4SetTest();
            //NewSocketRedis9SetTest();
            //NewSocketRedis9SetTest();
            //NewSocketRedis14SetTest();
            //NewSocketRedis14SetTest();
            //Pool514SetTest(); Pool514SetTest();
            //NewSocketRedis12SetTest();
            //Pool310SetTest(); Pool310SetTest();
            //NewSocketRedis0SetTest();
            //NewSocketRedis1SetTest();
            //NewSocketRedis2SetTest();
            //NewSocketRedis3SetTest();
            //NewSocketRedis7SetTest();
            //NewSocketRedis5SetTest();
            //Pool14SetTest();
            //Pool19SetTest();
            //Pool15SetTest();
            //Pool17SetTest();
            //Pool24SetTest();
            //Pool25SetTest();
            //Pool27SetTest();
            //BeetleXRedisSetTest();
            //NewLifeRedisSetTest();
        }

        #region CheckPool
        public static void CheckPool()
        {
            CheckPool(_pool9);
            CheckPool(_pool4);
            //CheckPool(_pool7);
            //CheckPool(_pool24);
            //CheckPool(_pool27);
        }

        public static void CheckPool<T>(ClientPool1<T> value) where T : RedisClientBase, new()
        {
            Console.WriteLine($"===========Poo1 : {typeof(T).Name}==========");
            for (int i = 0; i < value.CallCounter.Length; i++)
            {
                Console.WriteLine($"No.{i} used {value.CallCounter[i]}");
            }

        }
        public static void CheckPool<T>(ClientPool2<T> value) where T : RedisClientBase, new()
        {
            Console.WriteLine($"===========Poo1 : {typeof(T).Name}==========");
            for (int i = 0; i < value.CallCounter.Length; i++)
            {
                Console.WriteLine($"No.{i} used {value.CallCounter[i]}");
            }

        }
        #endregion


        #region TestNewSocket

        public static async void RunInOutTest(IPEndPoint point)
        {
            Server(point);
            Test(point);
        }

        public static async void Server(IPEndPoint point)
        {
            TcpListener listener = new TcpListener(point);
            listener.Start();
            Console.WriteLine("Server: Start Accept!");
            var client = await listener.AcceptTcpClientAsync();
            Console.WriteLine("Server: Accept One!");
            byte[] bytes = new byte[1024];

            int count = 0;
            while (count < 20)
            {
                //await Task.Delay(10);
                NetworkStream stream = client.GetStream();
                if (stream.CanRead)
                {
                    int i;
                    if ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        var data = Encoding.UTF8.GetString(bytes, 0, i);
                        client.Client.Send(bytes);
                        //Console.WriteLine("Server: Data has been send!");
                        if (data == "test")
                        {
                            stream.Dispose();
                            client.Dispose();
                            return;
                        }
                    }
                }

            }
        }
        public static async void Test(IPEndPoint endpoit)
        {


            // SAEA 被接口封装为异步状态机 详见：https://sharplab.io/#v2:D4AQTAjAsAUCAMACEECsBuWDkQHQCUBXAOwBcBLAWwFNcBhAe0oAdyAbagJwGUuA3cgGNqAZ0xwkKXABUAFp2oBDACbliAc3EgAzMjCI6iAN6xEZnADZkAFkQBZRWoAUKeAG0AuokWd1IgJSm5sEmMEHBEYgAosTKTv7ikZEoAJxOAEQgVrKKIogipD6k1MoAhOkJ4UlmjMQiDBwESsoA0tQAnvGJSQC+VcE6lsgAHDbRsfH95qHV5iApiMTUAO6I3AyCANbUpACCy46FAEYcUXzUZLu+Il1TZn1hMA+wsGrFnMSKbHprG9t7B3Ix1O50u10QAC4zABJOicIFCL4AOQYFAAZu1GCwOBQGMQqqE7ohmPC+IpikMFCo8Wx2jh9AB9QRfNhHRRbLHMHElRAAXkQ8T5AD5jIgeuIXjBqiTyGSKShGcy2Kz2ZsJY9qoN1lsdvtDooTtQzhc9uCAOK6wHvQW8kWkWTkMREwZHBgNRDQkSc7nKYWIfDUNFcC7CKIAR0IXxuTJZbK2ABpEDHlXHNt6diVKhqkoM3ogLaQAyJCGxSJMpUlQiAAOyICDdSLPbPJXQgWwAeWI6eKcQViEEeIoxEjuOIgQrkUJE9m5DRAoDQYUxFDEajTmTKoTSaVm7TTC5GeU/kQwGAROqC+Dy6Nq7YN2hZC4bD+JXo+58RoAHoIchpqE4FDnDdU0TAcyDUEdyDxRNh2VfxE2A1Vu0zcdZmmc8cwsAgSCcMCh0gvEszQ+5JWqJtNVbWwAFU6kUINO2Q3sIH0PCIPJKCxyJGZiIY/cfVwwc2NHIjelInNKIMPiM3LapuLQ3jsUPW5p2CcikhJVFqEEHsxgUg8exkysxLQslOH7QTh3YvE+Q9R9OGfHVlFwKJv1/dR/0A7dY1VBCd1TRiRLQ2cBVYyzR0QUp+VgthUOIuTiLMDDIgAemS1IMkAUuNADZTQAsf/SRAAGpxgHVQNFwKjpAAMWGXAC24Uh4Q0Jx7UdXAACFCDRRd/EC2YHjIyUqh6IA= 
            // 其中 continuation 为异步状态机的 MoveNext 方法。

            SocketConnectionFactory client = new SocketConnectionFactory(new SocketTransportOptions());
            var connection = client.ConnectAsync(endpoit).Result;
            Input(connection);
            Output(connection);

        }


       

        public static async void Output(ConnectionContext connection)
        {

            Console.WriteLine("Run Sender!");
            while (true)
            {

                var temp = Console.ReadLine();
                if (temp != null)
                {
                    var buffer = Encoding.UTF8.GetBytes(temp);
                    await connection.Transport.Output.WriteAsync(buffer);
                }

            }
        }
        public static async void Input(ConnectionContext connection)
        {
            Console.WriteLine("Run Receiver!");
            while (true)
            {

                //Console.WriteLine("Input!");
                var result = await connection.Transport.Input.ReadAsync();
                var buffer = result.Buffer;
                try
                {
                    Console.WriteLine();
                    if (!buffer.IsEmpty)
                    {
                        if (!buffer.IsSingleSegment)
                        {
                            var data = Encoding.UTF8.GetString(buffer.FirstSpan);
                            Console.WriteLine("Receive : " + data);
                            Console.WriteLine("-----------");
                            connection.Transport.Input.AdvanceTo(buffer.End);
                        }
                        else
                        {
                            var data = Encoding.UTF8.GetString(buffer.ToArray());
                            Console.WriteLine("Receive : " + data);
                            Console.WriteLine("==============");
                            connection.Transport.Input.AdvanceTo(buffer.End);
                        }
                    }
                    else if (result.IsCompleted)
                    {
                        break;
                    }
                    //await connection.Transport.Input.CompleteAsync();
                }
                finally
                {
                    Console.WriteLine("==============");
                    Console.Write("Send:");
                }

            }

        }
        #endregion

        #region RedisTest

        private static void RunAction(Func<string,Task> action,string title)
        {
            int count = 0;
            Console.WriteLine("=========================");
            var result = _redisClient4.FlushDBAsync().Result;
            Console.WriteLine($"Clear DB 0 - [{(result?"SUCCEED":"FAILED")}]!");
            if (_useDelay)
            {
                Thread.Sleep(_delayCount);
            }
            var tasks = new Task[frequence];
            Stopwatch sw = new Stopwatch();
            //_beforeSw?.Invoke(title);
            Console.WriteLine("Start Run:");
            Thread.Sleep(0);
            Thread.Sleep(1000);
            sw.Start();
            for (var a = 0; a < frequence; a += 1)
            {
                tasks[a] = action(a.ToString());
            }
            Task.WaitAll(tasks);
            sw.Stop();
            //for (var a = 0; a < frequence; a += 1)
            //{
            //    var key = a.ToString();
            //    tasks[a] = Task.Run(() =>
            //    {
            //        var result = _stackExnchangeClient.StringGet(key);
            //        if (result != key)
            //        {
            //            Interlocked.Increment(ref count);
            //        }
            //    });
            //}
            //Task.WaitAll(tasks);
            //Console.WriteLine($"{title} (0-{frequence / 10000}W) : {sw.ElapsedTicks} SPAN! ");
            Console.WriteLine($"{title} (0-{frequence / 10000}W) : {sw.ElapsedMilliseconds}ms! ");
            //Console.WriteLine($"Errors : {count} !");
            //if (count>0)
            //{
            //    Thread.Sleep(1000);
            //    for (var a = 0; a < frequence; a += 1)
            //    {
            //        var key = a.ToString();
            //        tasks[a] = Task.Run(() =>
            //        {
            //            var result = _stackExnchangeClient.StringGet(key);
            //            if (result != key)
            //            {
            //                Interlocked.Increment(ref count);
            //            }
            //        });
            //    }
            //    Task.WaitAll(tasks);
            //    Console.WriteLine($"Rechecking Errors : {count} !");
            //}
            Console.WriteLine("=========================\r\n");
        }


        #region BeetleXRedis - SET
        public static void BeetleXRedisSetTest()
        {
            var tasks = new Task[frequence];
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (var a = 0; a < frequence; a += 1)
            {
                tasks[a] = Task.Run(async () =>
                {
                    var key = a.ToString();
                    var result =await _beetleClient.Set(key, key);
                    if (result != "OK")
                    {
                        throw new Exception("not equal");
                    }
                });
            }
            Task.WaitAll(tasks);
            sw.Stop();
            Console.WriteLine($"BeetleXRedis(0-{frequence}): {sw.ElapsedMilliseconds}ms");
        }
        #endregion

        //#region NewSocketRedis0 - SET
        //public static void NewSocketRedis0SetTest()
        //{
        //    RunAction((key) =>
        //    {

        //        return _redisClient0.SetAsync(key, key);

        //    }, "NewRedis0");

        //}
        //#endregion

        #region NewSocketRedis1 - SET
        public static void NewSocketRedis1SetTest()
        {
            RunAction((key) =>
            {

                return _redisClient1.SetAsync(key, key);

            }, "NewRedis1");

        }
        #endregion

        #region NewSocketRedis2 - SET
        public static void NewSocketRedis2SetTest()
        {
            RunAction((key) =>
            {

                return _redisClient2.SetAsync(key, key);

            }, "NewRedis2");
            
        }
        #endregion

        #region NewSocketRedis3 - SET
        public static void NewSocketRedis3SetTest()
        {
            RunAction((key) =>
            {

                return _redisClient3.SetAsync(key, key);

            }, "NewRedis3");

        }
        #endregion

        #region NewSocketRedis4 - SET
        public static void NewSocketRedis4SetTest()
        {
            RunAction((key) =>
            {

                return _redisClient4.SetAsync(key, key);

            }, "NewRedis4");
        }
        #endregion

        #region NewSocketRedis5 - SET
        public static void NewSocketRedis5SetTest()
        {
            RunAction((key) =>
            {

                return _redisClient5.SetAsync(key, key);

            }, "NewRedis5");

        }
        #endregion

        #region NewSocketRedis7 - SET
        public static void NewSocketRedis7SetTest()
        {
            RunAction((key) =>
            {

                return _redisClient7.SetAsync(key, key);

            }, "NewRedis7");

        }
        #endregion

        #region NewSocketRedis8 - SET
        public static void NewSocketRedis8SetTest()
        {
            RunAction((key) =>
            {

                return _redisClient8.SetAsync(key, key);

            }, "NewRedis8");

        }
        #endregion

        #region NewSocketRedis9 - SET
        public static void NewSocketRedis9SetTest()
        {
            RunAction((key) =>
            {

                return _redisClient9.SetAsync(key, key);

            }, "NewRedis9");

        }
        #endregion

        #region NewSocketRedis12 - SET
        public static void NewSocketRedis12SetTest()
        {
            RunAction((key) =>
            {

                return _redisClient12.SetAsync(key, key);

            }, "NewRedis12");

        }
        #endregion

        #region NewSocketRedis14 - SET
        public static void NewSocketRedis14SetTest()
        {
            RunAction((key) =>
            {

                return _redisClient14.SetAsync(key, key);

            }, "NewRedis14");

        }
        #endregion

        #region FreeRedis - SET
        public static void FreeRedisSetTest()
        {
            RunAction((key) =>
            {

                return _freeRedisClient.SetAsync(key, key);

            }, "FreeRedisClient");
            
        }
        #endregion

        #region StackExchangeRedis - SET
        public static void StackExchangeRedisSetTest()
        {

            RunAction((key) =>
            {

                return _stackExnchangeClient.StringSetAsync(key, key);

            }, "StackExchange");
            
        }

        #endregion

        #region NewlifeRedis - SET
        public static void NewLifeRedisSetTest()
        {
            var tasks = new Task[frequence];
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (var a = 0; a < frequence; a += 1)
            {
                tasks[a] = Task.Run(async () =>
                {
                    var key = a.ToString();
                    var result = _newLifeRedis.Set(key, key);
                    if (!result)
                    {
                        throw new Exception("not equal");
                    }
                });
            }
            Task.WaitAll(tasks);
            sw.Stop();
            Console.WriteLine($"NewlifeRedis(0-{frequence}): {sw.ElapsedMilliseconds}ms");
        }
        #endregion



        #endregion

        #region PoolTest
        #region Pool1-4 - SET
        public static void Pool14SetTest()
        {
            RunAction((key) =>
            {

                return _pool4.SetAsync(key, key);

            }, "NewRedisWithPool1-4");
        }
        #endregion

        #region Pool1-5 - SET
        public static void Pool15SetTest()
        {
            RunAction((key) =>
            {

                return _pool5.SetAsync(key, key);

            }, "NewRedisWithPool1-5");

        }
        #endregion

        #region Pool1-7 - SET
        public static void Pool17SetTest()
        {
            RunAction((key) =>
            {

                return _pool7.SetAsync(key, key);

            }, "NewRedisWithPool1-7");

        }
        #endregion

        #region Pool1-9 - SET
        public static void Pool19SetTest()
        {
            RunAction((key) =>
            {

                return _pool9.SetAsync(key, key);

            }, "NewRedisWithPool1-9");
        }
        #endregion

        #region Pool2-4 - SET
        public static void Pool24SetTest()
        {
            RunAction((key) =>
            {

                return _pool24.SetAsync(key, key);

            }, "NewRedisWithPool2-4");
        }
        #endregion

        #region Pool2-5 - SET
        public static void Pool25SetTest()
        {
            RunAction((key) =>
            {

                return _pool25.SetAsync(key, key);

            }, "NewRedisWithPool2-5");

        }
        #endregion

        #region Pool2-7 - SET
        public static void Pool27SetTest()
        {
            RunAction((key) =>
            {

                return _pool27.SetAsync(key, key);

            }, "NewRedisWithPool2-7");

        }
        #endregion

        #region Pool3-10 - SET
        public static void Pool310SetTest()
        {
            RunAction((key) =>
            {

                return _pool10.SetAsync(key, key);

            }, "NewRedisWithPool10");

        }
        #endregion


        #region Pool4-10 - SET
        public static void Pool410SetTest()
        {
            RunAction((key) =>
            {

                return _pool13.SetAsync(key, key);

            }, "NewRedisWithPool13");

        }
        #endregion

        #region Pool5-14 - SET
        public static void Pool514SetTest()
        {
            RunAction((key) =>
            {

                return _pool14.SetAsync(key, key);

            }, "NewRedisWithPool14");

        }
        #endregion

        #endregion
    }

}
