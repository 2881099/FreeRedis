using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{

    public class SocketTransportOptions
    {
        /// <summary>
        /// The number of I/O queues used to process requests. Set to 0 to directly schedule I/O to the ThreadPool.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="Environment.ProcessorCount" /> rounded down and clamped between 1 and 16.
        /// </remarks>
        public int IOQueueCount { get; set; } = Math.Min(Environment.ProcessorCount, 16);

        /// <summary>
        /// Set to false to enable Nagle's algorithm for all connections.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool NoDelay { get; set; } = true;

        public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

        public long? MaxWriteBufferSize { get; set; } = 64 * 1024;

        internal Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = () => new SlabMemoryPool();
    }

    internal sealed class SocketClient
    {
        private readonly MemoryPool<byte> _memoryPool;
        private readonly int _numSchedulers;
        private readonly PipeScheduler[] _schedulers;

        private Socket _listenSocket;
        private int _schedulerIndex;
        private readonly SocketTransportOptions _options;

        public EndPoint EndPoint { get; private set; }

        internal SocketClient(
            EndPoint endpoint,
            SocketTransportOptions options)
        {
            EndPoint = endpoint;
            _options = options;
            _memoryPool = _options.MemoryPoolFactory();
            var ioQueueCount = options.IOQueueCount;

            if (ioQueueCount > 0)
            {
                _numSchedulers = ioQueueCount;
                _schedulers = new IOQueue[_numSchedulers];

                for (var i = 0; i < _numSchedulers; i++)
                {
                    _schedulers[i] = new IOQueue();
                }
            }
            else
            {
                var directScheduler = new PipeScheduler[] { PipeScheduler.ThreadPool };
                _numSchedulers = directScheduler.Length;
                _schedulers = directScheduler;
            }
        }


        public async ValueTask<SocketConnection> Connection(CancellationToken cancellationToken = default)
        {

            try
            {

                Socket client;

                // Unix domain sockets are unspecified
                var protocolType = EndPoint is UnixDomainSocketEndPoint ? ProtocolType.Unspecified : ProtocolType.Tcp;

                client = new Socket(EndPoint.AddressFamily, SocketType.Stream, protocolType);

                // Kestrel expects IPv6Any to bind to both IPv6 and IPv4
                if (EndPoint is IPEndPoint ip && ip.Address == IPAddress.IPv6Any)
                {
                    client.DualMode = true;
                }

                await client.ConnectAsync(EndPoint);
                var connection = new SocketConnection(client, _memoryPool, _schedulers[_schedulerIndex], _options.MaxReadBufferSize, _options.MaxWriteBufferSize);
                connection.Run();
                _schedulerIndex = (_schedulerIndex + 1) % _numSchedulers;
                return connection;

            }
            catch (ObjectDisposedException)
            {
                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                return null;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted)
            {
                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                return null;
            }
            catch (SocketException)
            {
                // The connection got reset while it was in the backlog, so we try again.
                //_trace.ConnectionReset(connectionId: "(null)");
                throw new Exception("AAA");
            }
            // }
        }

        public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
        {
            _listenSocket?.Dispose();
            return default;
        }

        public ValueTask DisposeAsync()
        {
            _listenSocket?.Dispose();
            // Dispose the memory pool
            _memoryPool.Dispose();
            return default;
        }
    }
}