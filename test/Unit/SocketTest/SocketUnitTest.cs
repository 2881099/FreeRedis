using console_netcore31_newsocket;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SocketTest
{
    public class SocketUnitTest
    {

        public static string Result;
        [Fact]
        public void Test1()
        {
            var point = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8989);
            StartServer(point);
            StartClient(point);

        }

        private async void StartServer(IPEndPoint point)
        {
            TcpListener listener = new TcpListener(point);
            listener.Start();

            var client = await listener.AcceptTcpClientAsync();
            byte[] bytes = new byte[1024];
            while (client.Connected)
            {

                NetworkStream stream = client.GetStream();
                int i;
                if ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    var data = System.Text.Encoding.UTF8.GetString(bytes, 0, i);
                    Assert.Equal("test", data);
                    client.Client.Send(bytes);
                    //Console.WriteLine("客户端发来：" + data);
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
            await Task.Delay(1000);
            var endpoit = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8989);
            SocketConnectionFactory client = new SocketConnectionFactory(new SocketTransportOptions());
            var connection = client.ConnectAsync(endpoit).Result;
            var buffer = Encoding.UTF8.GetBytes("test");
            await connection.Transport.Output.WriteAsync(buffer);
            await Task.Delay(1000);
            Assert.Contains("test", Result);

        }
    }
}
