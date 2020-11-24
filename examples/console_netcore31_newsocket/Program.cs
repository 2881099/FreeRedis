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
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{

    /// <summary>
    /// 移除了 SocketTrace
    /// 缓冲池等设计保留原样，该功能尚未测试
    /// </summary>
    class Program
    {

        private static int port;
        private static string ip;
        private static string pwd;
        private const int frequence = 20000;

        private static RedisClient _freeRedisClient;
        private static NewRedisClient3 _redisClient3;
        private static NewRedisClient4 _redisClient4;
        private static IDatabase _stackExnchangeClient;
       
        private static void InitClient()
        {
            //Notice : Please use "//" comment "/*".

            ///*
            using (StreamReader stream = new StreamReader("Redis.rsf"))
            {
                ip = stream.ReadLine();
                port = int.Parse(stream.ReadLine());
                //pwd = stream.ReadLine();
            }//*/
            /*
            ip = "127.0.0.1";
            port = 6379;
            //*/
            _freeRedisClient = new RedisClient($"{ip}:{port},database=15,min pool size=100");
            _redisClient3 = new NewRedisClient3(ip, port);
            _redisClient4 = new NewRedisClient4(ip, port);
            ConnectionMultiplexer seredis = ConnectionMultiplexer.Connect("127.0.0.1:6379");
            _stackExnchangeClient = seredis.GetDatabase(1);

        }

        
        static void Main(string[] args)
        {

            InitClient();
            FreeRedisSetTest();
            StackExchangeRedisSetTest();
            NewSocketRedis3SetTest();
            NewSocketRedis4SetTest();
            
            Console.WriteLine("====== 以上预热 =======");

            FreeRedisSetTest();
            StackExchangeRedisSetTest();
            NewSocketRedis3SetTest();
            NewSocketRedis4SetTest();

            Console.ReadKey();

        }


        #region TestNewSocket

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

        #region NewSocketRedis3 - SET
        public static void NewSocketRedis3SetTest()
        {
            var tasks = new Task[frequence];
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (var a = 0; a < frequence; a += 1)
            {
                tasks[a] = Task.Run(async () =>
                {
                    var key = a.ToString();
                    var result = await _redisClient3.SetAsync(key, key);
                    if (!result)
                    {
                        throw new Exception("not equal");
                    }
                    //var val = await sedb.StringGetAsync(key); //valid
                    //if (val != key) throw new Exception("not equal");
                });
            }
            Task.WaitAll(tasks);
            sw.Stop();
            Console.WriteLine($"NewRedis3(0-{frequence}): {sw.ElapsedMilliseconds}ms");
        }
        #endregion

        #region NewSocketRedis4 - SET
        public static void NewSocketRedis4SetTest()
        {
            var tasks = new Task[frequence];
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (var a = 0; a < frequence; a += 1)
            {
                tasks[a] = Task.Run(async () =>
                {
                    var key = a.ToString();
                    var result = await _redisClient4.SetAsync(key, key);
                    if (!result)
                    {
                        throw new Exception("not equal");
                    }
                });
            }
            Task.WaitAll(tasks);
            sw.Stop();
            Console.WriteLine($"NewRedis4(0-{frequence}): {sw.ElapsedMilliseconds}ms");
        }
        #endregion

        #region FreeRedis - SET
        public static void FreeRedisSetTest()
        {
            var tasks = new Task[frequence];
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (var a = 0; a < frequence; a += 1)
            {
                tasks[a] = Task.Run(async () =>
                {
                    var key = a.ToString();
                    await _freeRedisClient.SetAsync(key, key);
                });
            }
            Task.WaitAll(tasks);
            sw.Stop();
            Console.WriteLine($"FreeRedis(0-{frequence}): {sw.ElapsedMilliseconds}ms");
        }
        #endregion

        #region StackExchangeRedis - SET
        public static void StackExchangeRedisSetTest()
        {
            var tasks = new Task[frequence];
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (var a = 0; a < frequence; a += 1)
            {
                tasks[a] = Task.Run(async () =>
                {
                    var key = a.ToString();
                    var result = await _stackExnchangeClient.StringSetAsync(key, key);
                    if (!result)
                    {
                        throw new Exception("not equal");
                    }
                });
            }
            Task.WaitAll(tasks);
            sw.Stop();
            Console.WriteLine($"StackExchange(0-{frequence}): {sw.ElapsedMilliseconds}ms");
        }

        #endregion


        #endregion


    }

}
