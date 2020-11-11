using console_netcore31_newsocket.Scheduler;
using FreeRedis;
using Microsoft.AspNetCore.Connections;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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
        static void Main(string[] args)
        {
            //FreeRedisTest();
            //
            //var endpoit = new IPEndPoint(IPAddress.Parse(),);
            var endpoit = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9379);
            //NewSocketTest(endpoit);

            Server(endpoit);
            Test(endpoit);
            Console.ReadKey();

        }

        public static async void TestTask()
        {
            //await new MyScheduler(100);
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
                await Task.Delay(1000);
                Console.WriteLine("Server: Loop!");
                NetworkStream stream = client.GetStream();
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

            Console.WriteLine("Run Output!");
            while (true)
            {

                Console.WriteLine("");
                var temp = Console.ReadLine();
                if (temp != null)
                {
                    var buffer = Encoding.UTF8.GetBytes(temp + "\r\n");
                    await connection.Transport.Output.WriteAsync(buffer);
                    //connection.Transport.Output.Advance(result.);
                    //Console.WriteLine("发送数据！");
                    //connection.Transport.Output.Complete();
                }

            }
        }
        public static async void Input(ConnectionContext connection)
        {
            Console.WriteLine("Run Input!");
            while (true)
            {

                //Console.WriteLine("Input!");
                var result = await connection.Transport.Input.ReadAsync();
                //Console.WriteLine("GetData!");
                var data = Encoding.UTF8.GetString(result.Buffer.ToArray());
                Console.WriteLine(data);
                connection.Transport.Input.AdvanceTo(result.Buffer.End);
                //await result.Buffer.
                //connection.Transport.Input.Complete();

            }

        }

        private static readonly Stopwatch sw = new Stopwatch();
        private const int frequence = 20000;
        


        private static int count = 0;

        #region newSocket

        public static async void NewSocketTest(IPEndPoint endpoit)
        {
            //ResultDict = new ConcurrentDictionary<string, string>();
            SocketConnectionFactory client = new SocketConnectionFactory(new SocketTransportOptions());
            var connection = client.ConnectAsync(endpoit).Result;
            connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("AUTH \r\n"));
            var result = connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("SELECT 15\r\n")).Result;
            Thread.Sleep(3000);
            var readResult = connection.Transport.Input.ReadAsync().Result;
            var data = Encoding.UTF8.GetString(readResult.Buffer.FirstSpan);
            Console.WriteLine(data);
            connection.Transport.Input.AdvanceTo(readResult.Buffer.End);
            SendPing(connection);
            GetPong(connection);
            while (count != frequence)
            {
                Thread.Sleep(1000);
            }
            SendPing(connection);
            while (count != frequence)
            {
                Thread.Sleep(1000);
            }
            SendPing(connection);
        }
        public static async void SendPing(ConnectionContext connection)
        {
            count = 0;
            sw.Restart();
            int index = 0;
            while (index < frequence)
            {
                await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("PING\r\n"));
                index += 1;
            }
        }

        public static async void GetPong(ConnectionContext connection)
        {
            while (true)
            {

                var result = await connection.Transport.Input.ReadAsync();
                AddCount(result.Buffer.ToArray());
                //connection.Transport.Input.AsStream().Flush();
                connection.Transport.Input.AdvanceTo(result.Buffer.End);

            }

        }

        public static async void AddCount(byte[] buffer)
        {
            var data = Encoding.UTF8.GetString(buffer);
            Interlocked.Add(ref count, data.Split('+').Length - 1);
            if (count == frequence)
            {
                sw.Stop();
                Console.WriteLine("NewSocketRedis:" + sw.ElapsedMilliseconds + "ms");
            }
        }

        private ConcurrentDictionary<string, string> ResultDict;
        //private Concur
        //public static async string Set(string key, string value)
        //{

        //}
        #endregion


        #region CSRedis
        public static async void FreeRedisTest()
        {

            var client = new RedisClient("");
            SendPing(client);
            while (count != frequence)
            {
                Thread.Sleep(1000);
            }
            SendPing(client);
            while (count != frequence)
            {
                Thread.Sleep(1000);
            }
            SendPing(client);
        }
        public static async void SendPing(RedisClient client)
        {
            count = 0;
            sw.Restart();
            Parallel.For(0, frequence, (state) =>
            {
                var data = client.Ping();
                //Console.WriteLine(data);
                Interlocked.Add(ref count, data.Split('N').Length - 1);
                if (count == frequence)
                {
                    sw.Stop();
                    Console.WriteLine("FreeRedis:"+sw.ElapsedMilliseconds + "ms");
                } });
            
        }
        #endregion
    }
}
