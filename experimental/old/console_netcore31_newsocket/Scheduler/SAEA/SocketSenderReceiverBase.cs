using System;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace console_netcore31_newsocket
{
    internal abstract class SocketSenderReceiverBase : IDisposable
    {
        protected readonly Socket _socket;
        protected readonly SocketAwaitableEventArgs _awaitableEventArgs;

        protected SocketSenderReceiverBase(Socket socket, PipeScheduler scheduler, string name)
        {
            _socket = socket;
            _awaitableEventArgs = new SocketAwaitableEventArgs(scheduler, name);
        }

        public void Dispose() => _awaitableEventArgs.Dispose();
    }
}