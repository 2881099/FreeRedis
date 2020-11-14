using BenchmarkDotNet.Attributes;
using console_netcore31_newsocket;
using Microsoft.AspNetCore.Connections;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PingTest
{
    [MemoryDiagnoser, CoreJob, MarkdownExporter, RPlotExporter]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]

    [MaxWarmupCount(8)]
    [IterationCount(20)]
    [MaxIterationCount(40)]
    [ProcessCount(4)]
    public class PingPongTest
    {
        private readonly ConnectionContext connection;
        private static string ip;
        private static int port;
        private static string pwd;

        public PingPongTest()
        {
            using (StreamReader stream = new StreamReader("Redis.rsf"))
            {
                ip = stream.ReadLine();
                port = int.Parse(stream.ReadLine());
                pwd = stream.ReadLine();
            }
            var endpoit = new IPEndPoint(IPAddress.Parse("127"), 12);
            SocketConnectionFactory client = new SocketConnectionFactory(new SocketTransportOptions());
            connection = client.ConnectAsync(endpoit).Result;
            connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("AUTH \r\n"));
            var result = connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("SELECT 15\r\n")).Result;
            Thread.Sleep(3000);
            var readResult = connection.Transport.Input.ReadAsync().Result;
            var data = Encoding.UTF8.GetString(readResult.Buffer.FirstSpan);
            Console.WriteLine(data);
            connection.Transport.Input.AdvanceTo(readResult.Buffer.End);

        }

        [Benchmark]
        public void Test()
        {
            Send();
            GetResult();
            count = 0;
            //Console.WriteLine(count);
            //connection.DisposeAsync();
        }

        public async void Send()
        {
            int index = 0;
            while (index < 1000)
            {
                await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("PING\r\n"));
                index += 1;
            }
        }

        public async void GetResult()
        {
            while (count < 1000)
            {

                var result = await connection.Transport.Input.ReadAsync();
                AddCount(result.Buffer.ToArray());
                connection.Transport.Input.AsStream().Flush();
                connection.Transport.Input.AdvanceTo(result.Buffer.End);

            }

        }

        public int count;
        public async void AddCount(byte[] buffer)
        {
            var data = Encoding.UTF8.GetString(buffer);
            Interlocked.Add(ref count, data.Split('+').Length - 1);
        }

        //public async Task Run()
        //{
        //    for (var a = 0; a < 10000; a+=1)
        //    {

        //    }
        //}

    }
}
