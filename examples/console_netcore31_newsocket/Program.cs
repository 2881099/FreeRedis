using Microsoft.AspNetCore.Connections;
using System;
using System.Buffers;
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
    /// 修改了 Lisenter 功能为 Client，参照源码可恢复服务端功能
    /// 缓冲池等设计保留原样，该功能尚未测试
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var endpoit = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8989);
            Server(endpoit);
            Test(endpoit);
            Console.ReadKey();

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
        public static async void Test(IPEndPoint endpoit)
        {

     
            SocketConnectionFactory client = new SocketConnectionFactory(new SocketTransportOptions());
            var connection =  client.ConnectAsync(endpoit).Result;
            Input(connection);
            Output(connection);
            //await Input(connection);
            // SAEA 被接口封装为异步状态机 详见：https://sharplab.io/#v2:D4AQTAjAsAUCAMACEECsBuWDkQHQCUBXAOwBcBLAWwFNcBhAe0oAdyAbagJwGUuA3cgGNqAZ0xwkKXABUAFp2oBDACbliAc3EgAzMjCI6iAN6xEZnADZkAFkQBZRWoAUKeAG0AuokWd1IgJSm5sEmMEHBEYgAosTKTv7ikZEoAJxOAEQgVrKKIogipD6k1MoAhOkJ4UlmjMQiDBwESsoA0tQAnvGJSQC+VcE6lsgAHDbRsfH95qHV5iApiMTUAO6I3AyCANbUpACCy46FAEYcUXzUZLu+Il1TZn1hMA+wsGrFnMSKbHprG9t7B3Ix1O50u10QAC4zABJOicIFCL4AOQYFAAZu1GCwOBQGMQqqE7ohmPC+IpikMFCo8Wx2jh9AB9QRfNhHRRbLHMHElRAAXkQ8T5AD5jIgeuIXjBqiTyGSKShGcy2Kz2ZsJY9qoN1lsdvtDooTtQzhc9uCAOK6wHvQW8kWkWTkMREwZHBgNRDQkSc7nKYWIfDUNFcC7CKIAR0IXxuTJZbK2ABpEDHlXHNt6diVKhqkoM3ogLaQAyJCGxSJMpUlQiAAOyICDdSLPbPJXQgWwAeWI6eKcQViEEeIoxEjuOIgQrkUJE9m5DRAoDQYUxFDEajTmTKoTSaVm7TTC5GeU/kQwGAROqC+Dy6Nq7YN2hZC4bD+JXo+58RoAHoIchpqE4FDnDdU0TAcyDUEdyDxRNh2VfxE2A1Vu0zcdZmmc8cwsAgSCcMCh0gvEszQ+5JWqJtNVbWwAFU6kUINO2Q3sIH0PCIPJKCxyJGZiIY/cfVwwc2NHIjelInNKIMPiM3LapuLQ3jsUPW5p2CcikhJVFqEEHsxgUg8exkysxLQslOH7QTh3YvE+Q9R9OGfHVlFwKJv1/dR/0A7dY1VBCd1TRiRLQ2cBVYyzR0QUp+VgthUOIuTiLMDDIgAemS1IMkAUuNADZTQAsf/SRAAGpxgHVQNFwKjpAAMWGXAC24Uh4Q0Jx7UdXAACFCDRRd/EC2YHjIyUqh6IA= 
            // 其中 continuation 为异步状态机的 MoveNext 方法。
            // 接收在 SAEA 类中的 ProtocalAnalysis 方法

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
                    var buffer = Encoding.UTF8.GetBytes(temp);
                    await connection.Transport.Output.WriteAsync(buffer);
                    //connection.Transport.Output.Complete();
                }

            }
        }
        public static async void Input(ConnectionContext connection)
        {
            Console.WriteLine("Run Input!");
            while (true)
            {

                Console.WriteLine("Input!");
                var result = await connection.Transport.Input.ReadAsync();
                Console.WriteLine("GetData!");
                var data = Encoding.UTF8.GetString(result.Buffer.FirstSpan);
                Console.WriteLine(data);
                connection.Transport.Input.AdvanceTo(result.Buffer.End);
                //connection.Transport.Input.Complete();

            }

        }

    }
}
