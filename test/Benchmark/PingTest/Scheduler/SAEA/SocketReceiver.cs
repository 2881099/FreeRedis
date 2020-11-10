
using System;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace console_netcore31_newsocket
{
    internal sealed class SocketReceiver : SocketSenderReceiverBase
    {
        public SocketReceiver(Socket socket, PipeScheduler scheduler) : base(socket, scheduler)
        {
        }

        public SocketAwaitableEventArgs WaitForDataAsync()
        {
            _awaitableEventArgs.SetBuffer(Memory<byte>.Empty);

            if (!_socket.ReceiveAsync(_awaitableEventArgs))
            {
                _awaitableEventArgs.Complete();
            }

            return _awaitableEventArgs;
        }

        public SocketAwaitableEventArgs ReceiveAsync(Memory<byte> buffer)
        {
            _awaitableEventArgs.SetBuffer(buffer);

            if (!_socket.ReceiveAsync(_awaitableEventArgs))
            {
                _awaitableEventArgs.Complete();
            }

            return _awaitableEventArgs;
        }
    }
}