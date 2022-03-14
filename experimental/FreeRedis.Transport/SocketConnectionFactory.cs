// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace FreeRedis.Transport;

public class SocketConnectionFactory : IAsyncDisposable
{
    private readonly SocketTransportOptions _options;
    private readonly MemoryPool<byte> _memoryPool;
    private readonly PipeOptions _inputOptions;
    private readonly PipeOptions _outputOptions;
    private readonly SocketSenderPool _socketSenderPool;
    public SocketConnectionFactory(SocketTransportOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _options = options;
        _memoryPool = options.MemoryPoolFactory();

        var maxReadBufferSize = _options.MaxReadBufferSize ?? 0;
        var maxWriteBufferSize = _options.MaxWriteBufferSize ?? 0;

        // These are the same, it's either the thread pool or inline
        var applicationScheduler = _options.UnsafePreferInlineScheduling ? PipeScheduler.Inline : PipeScheduler.ThreadPool;
        var transportScheduler = applicationScheduler;
        // https://github.com/aspnet/KestrelHttpServer/issues/2573
        var awaiterScheduler = OperatingSystem.IsWindows() ? transportScheduler : PipeScheduler.Inline;

        _inputOptions = new PipeOptions(_memoryPool, applicationScheduler, transportScheduler, maxReadBufferSize, maxReadBufferSize / 2, useSynchronizationContext: false);
        _outputOptions = new PipeOptions(_memoryPool, transportScheduler, applicationScheduler, maxWriteBufferSize, maxWriteBufferSize / 2, useSynchronizationContext: false);
        _socketSenderPool = new SocketSenderPool(awaiterScheduler);
    }

    public async ValueTask<SocketConnection> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        var ipEndPoint = endpoint as IPEndPoint;

        if (ipEndPoint is null)
        {
            throw new NotSupportedException("The SocketConnectionFactory only supports IPEndPoints for now.");
        }

        var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = _options.NoDelay
        };

        await socket.ConnectAsync(ipEndPoint);

        var socketConnection = new SocketConnection(
            socket,
            _memoryPool,
            _inputOptions.ReaderScheduler, // This is either threadpool or inline
            _socketSenderPool,
            _inputOptions,
            _outputOptions,
            _options.WaitForDataBeforeAllocatingBuffer);

        socketConnection.Start();
        return socketConnection;
    }

    public ValueTask DisposeAsync()
    {
        _memoryPool.Dispose();
        return default;
    }
}