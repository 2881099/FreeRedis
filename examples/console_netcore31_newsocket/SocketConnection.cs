
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace console_netcore31_newsocket
{
    internal sealed class SocketConnection : TransportConnection
    {
        private static readonly int MinAllocBufferSize = SlabMemoryPool.BlockSize / 2;
        private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static readonly bool IsMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        private readonly Socket _socket;
        private readonly SocketReceiver _receiver;
        private readonly SocketSender _sender;
        private readonly CancellationTokenSource _connectionClosedTokenSource = new CancellationTokenSource();

        private readonly object _shutdownLock = new object();
        private volatile bool _socketDisposed;
        private volatile Exception _shutdownReason;
        private Task _processingTask;
        private readonly TaskCompletionSource<object> _waitForConnectionClosedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _connectionClosed;

        internal SocketConnection(Socket socket,
                                  MemoryPool<byte> memoryPool,
                                  PipeScheduler scheduler,
                                  long? maxReadBufferSize = null,
                                  long? maxWriteBufferSize = null)
        {
            Debug.Assert(memoryPool != null);

            _socket = socket;
            MemoryPool = memoryPool;

            LocalEndPoint = _socket.LocalEndPoint;
            RemoteEndPoint = _socket.RemoteEndPoint;

            ConnectionClosed = _connectionClosedTokenSource.Token;

            // On *nix platforms, Sockets already dispatches to the ThreadPool.
            // Yes, the IOQueues are still used for the PipeSchedulers. This is intentional.
            // https://github.com/aspnet/KestrelHttpServer/issues/2573
            var awaiterScheduler = IsWindows ? scheduler : PipeScheduler.Inline;

            _receiver = new SocketReceiver(_socket, awaiterScheduler);
            _sender = new SocketSender(_socket, awaiterScheduler);

            maxReadBufferSize ??= 0;
            maxWriteBufferSize ??= 0;

            var inputOptions = new PipeOptions(MemoryPool, PipeScheduler.ThreadPool, scheduler, maxReadBufferSize.Value, maxReadBufferSize.Value / 2, useSynchronizationContext: false);
            var outputOptions = new PipeOptions(MemoryPool, scheduler, PipeScheduler.ThreadPool, maxWriteBufferSize.Value, maxWriteBufferSize.Value / 2, useSynchronizationContext: false);

            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

            // Set the transport and connection id
            Transport = pair.Transport;
            Application = pair.Application;
        }

        public PipeWriter Input => Application.Output;

        public PipeReader Output => Application.Input;

        public override MemoryPool<byte> MemoryPool { get; }

        public void Run()
        {
            _processingTask = RunAsync();
        }

        private async Task RunAsync()
        {
            try
            {
                // Spawn send and receive logic
                var receiveTask = DoReceive();
                var sendTask = DoSend();

                // Now wait for both to complete
                await receiveTask;
                await sendTask;

                _receiver.Dispose();
                _sender.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        // Only called after connection middleware is complete which means the ConnectionClosed token has fired.
        public async ValueTask DisposeAsync()
        {
            Transport.Input.Complete();
            Transport.Output.Complete();

            if (_processingTask != null)
            {
                await _processingTask;
            }

            _connectionClosedTokenSource.Dispose();
        }

        private async Task DoReceive()
        {
            Exception error = null;

            try
            {
                await ProcessReceives();
            }
            catch (SocketException ex) when (IsConnectionResetError(ex.SocketErrorCode))
            {
                Console.WriteLine(ex);
            }
            catch (Exception ex)
                when ((ex is SocketException socketEx && IsConnectionAbortError(socketEx.SocketErrorCode)) ||
                       ex is ObjectDisposedException)
            {
                // This exception should always be ignored because _shutdownReason should be set.
                error = ex;

                if (!_socketDisposed)
                {
                    // This is unexpected if the socket hasn't been disposed yet.
                }
            }
            catch (Exception ex)
            {
                // This is unexpected.
                error = ex;
            }
            finally
            {
                // If Shutdown() has already bee called, assume that was the reason ProcessReceives() exited.
                Input.Complete(_shutdownReason ?? error);

                FireConnectionClosed();

                await _waitForConnectionClosedTcs.Task;
            }
        }

        private async Task ProcessReceives()
        {
            // Resolve `input` PipeWriter via the IDuplexPipe interface prior to loop start for performance.
            var input = Input;
            while (true)
            {
                // Wait for data before allocating a buffer.
                var saea = _receiver.WaitForDataAsync();
                await saea;
                // Ensure we have some reasonable amount of buffer space
                var buffer = input.GetMemory(MinAllocBufferSize);
                var bytesReceived = await _receiver.ReceiveAsync(buffer);
                saea.ProtocalAnalysis(buffer);
                if (bytesReceived == 0)
                {
                    // FIN
                    break;
                }

                input.Advance(bytesReceived);

                var flushTask = input.FlushAsync();

                var paused = !flushTask.IsCompleted;

                var result = await flushTask;

                if (result.IsCompleted || result.IsCanceled)
                {
                    // Pipe consumer is shut down, do we stop writing
                    break;
                }
            }
        }

        private async Task DoSend()
        {
            Exception shutdownReason = null;
            Exception unexpectedError = null;

            try
            {
                await ProcessSends();
            }
            catch (SocketException ex) when (IsConnectionResetError(ex.SocketErrorCode))
            {
                Console.WriteLine(ex);
            }
            catch (Exception ex)
                when ((ex is SocketException socketEx && IsConnectionAbortError(socketEx.SocketErrorCode)) ||
                       ex is ObjectDisposedException)
            {
                // This should always be ignored since Shutdown() must have already been called by Abort().
                shutdownReason = ex;
            }
            catch (Exception ex)
            {
                shutdownReason = ex;
                unexpectedError = ex;
            }
            finally
            {
                Shutdown(shutdownReason);

                // Complete the output after disposing the socket
                Output.Complete(unexpectedError);

                // Cancel any pending flushes so that the input loop is un-paused
                Input.CancelPendingFlush();
            }
        }

        private async Task ProcessSends()
        {
            // Resolve `output` PipeReader via the IDuplexPipe interface prior to loop start for performance.
            var output = Output;
            while (true)
            {
                var result = await output.ReadAsync();

                if (result.IsCanceled)
                {
                    break;
                }

                var buffer = result.Buffer;

                var end = buffer.End;
                var isCompleted = result.IsCompleted;
                if (!buffer.IsEmpty)
                {
                    await _sender.SendAsync(buffer);
                }

                output.AdvanceTo(end);

                if (isCompleted)
                {
                    break;
                }
            }
        }

        private void FireConnectionClosed()
        {
            // Guard against scheduling this multiple times
            if (_connectionClosed)
            {
                return;
            }

            _connectionClosed = true;

            ThreadPool.UnsafeQueueUserWorkItem(state =>
            {
                state.CancelConnectionClosedToken();

                state._waitForConnectionClosedTcs.TrySetResult(null);
            },
            this,
            preferLocal: false);
        }

        private void Shutdown(Exception shutdownReason)
        {

            lock (_shutdownLock)
            {

                if (_socketDisposed)
                {
                    return;
                }

                // Make sure to close the connection only after the _aborted flag is set.
                // Without this, the RequestsCanBeAbortedMidRead test will sometimes fail when
                // a BadHttpRequestException is thrown instead of a TaskCanceledException.
                _socketDisposed = true;

                try
                {
                    // Try to gracefully close the socket even for aborts to match libuv behavior.
                    _socket.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                    // Ignore any errors from Socket.Shutdown() since we're tearing down the connection anyway.
                }

                _socket.Dispose();
            }
        }

        private void CancelConnectionClosedToken()
        {
            try
            {
                _connectionClosedTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static bool IsConnectionResetError(SocketError errorCode)
        {
            // A connection reset can be reported as SocketError.ConnectionAborted on Windows.
            // ProtocolType can be removed once https://github.com/dotnet/corefx/issues/31927 is fixed.
            return errorCode == SocketError.ConnectionReset ||
                   errorCode == SocketError.Shutdown ||
                   (errorCode == SocketError.ConnectionAborted && IsWindows) ||
                   (errorCode == SocketError.ProtocolType && IsMacOS);
        }

        private static bool IsConnectionAbortError(SocketError errorCode)
        {
            // Calling Dispose after ReceiveAsync can cause an "InvalidArgument" error on *nix.
            return errorCode == SocketError.OperationAborted ||
                   errorCode == SocketError.Interrupted ||
                   (errorCode == SocketError.InvalidArgument && !IsWindows);
        }
    }


    internal abstract partial class TransportConnection
    {     
        public TransportConnection()
        {
            
        }

        public EndPoint LocalEndPoint { get; set; }
        public EndPoint RemoteEndPoint { get; set; }

        public virtual MemoryPool<byte> MemoryPool { get; }

        public IDuplexPipe Transport { get; set; }

        public IDuplexPipe Application { get; set; }


        public CancellationToken ConnectionClosed { get; set; }

    }

    internal class DuplexPipe : IDuplexPipe
    {
        public DuplexPipe(PipeReader reader, PipeWriter writer)
        {
            Input = reader;
            Output = writer;
        }

        public PipeReader Input { get; }

        public PipeWriter Output { get; }

        public static DuplexPipePair CreateConnectionPair(PipeOptions inputOptions, PipeOptions outputOptions)
        {
            var input = new Pipe(inputOptions);
            var output = new Pipe(outputOptions);

            var transportToApplication = new DuplexPipe(output.Reader, input.Writer);
            var applicationToTransport = new DuplexPipe(input.Reader, output.Writer);

            return new DuplexPipePair(applicationToTransport, transportToApplication);
        }

        // This class exists to work around issues with value tuple on .NET Framework
        public readonly struct DuplexPipePair
        {
            public IDuplexPipe Transport { get; }
            public IDuplexPipe Application { get; }

            public DuplexPipePair(IDuplexPipe transport, IDuplexPipe application)
            {
                Transport = transport;
                Application = application;
            }
        }
    }
}