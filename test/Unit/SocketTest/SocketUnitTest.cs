using console_netcore31_newsocket;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SocketTest
{
    public class SocketUnitTest
    {

        public static string Result;
        [Fact(DisplayName ="测试 SOCKET 在平台的可用性")]
        public void Test1()
        {
            var point = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 18989);
            StartServer(point);
            StartClient(point);

        }

        private async void StartServer(IPEndPoint point)
        {
            TcpListener listener = new TcpListener(point);
            listener.Start();
            Console.WriteLine("Server: Start Accept!");
            var client = await listener.AcceptTcpClientAsync();
            Console.WriteLine("Server: Accept One!");
            byte[] bytes = new byte[1024];

            int count = 0;
            while (count<20)
            {
                await Task.Delay(1000);
                Console.WriteLine("Server: Loop!");
                NetworkStream stream = client.GetStream();
                int i;
                if ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    var data = Encoding.UTF8.GetString(bytes, 0, i);
                    Assert.Equal("test", data);
                    client.Client.Send(bytes);
                    Console.WriteLine("Server: Data has been send!");
                    if (data == "test")
                    {
                        stream.Dispose();
                        client.Dispose();
                        return;
                    }
                }
            }
        }

        

        private async void StartClient(IPEndPoint point)
        {
            await Task.Delay(3000);
            SocketConnectionFactory client = new SocketConnectionFactory(new SocketTransportOptions());
            var connection = client.ConnectAsync(point).Result;
            Console.WriteLine("Client: Connected!");
            var buffer = Encoding.UTF8.GetBytes("test");
            Task.Run(async () =>
            {
                var result = await connection.Transport.Input.ReadAsync();
                Result = Encoding.UTF8.GetString(result.Buffer.FirstSpan);
            });
            await connection.Transport.Output.WriteAsync(buffer);
            Console.WriteLine("Client: Data has been send!");
            while (Result == default)
            {
                Thread.Sleep(1000);
                Console.WriteLine("Client: Loop!");
            }
            Assert.Contains("test", Result);
            await client.DisposeAsync();
        }
    }
}
