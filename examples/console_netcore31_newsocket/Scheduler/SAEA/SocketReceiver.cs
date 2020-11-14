
using System;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace console_netcore31_newsocket
{
    internal sealed class SocketReceiver : SocketSenderReceiverBase
    {
        public SocketReceiver(Socket socket, PipeScheduler scheduler) : base(socket, scheduler, "receiver")
        {
        }

        public SocketAwaitableEventArgs WaitForDataAsync()
        {
            _awaitableEventArgs.SetBuffer(Memory<byte>.Empty);

            if (!_socket.ReceiveAsync(_awaitableEventArgs))
            {
                //Console.WriteLine("Receiver Post Succeed！");
                _awaitableEventArgs.Complete();
            }
            else
            {
                //Console.WriteLine("Receiver Post has been exist！");
            }

            return _awaitableEventArgs;
        }

        public SocketAwaitableEventArgs ReceiveAsync(Memory<byte> buffer)
        {
            _awaitableEventArgs.SetBuffer(buffer);

            if (!_socket.ReceiveAsync(_awaitableEventArgs))
            {
                //Console.WriteLine("Receiver Post Succeed！");
                _awaitableEventArgs.Complete();
            }
            else
            {
                //Console.WriteLine("Receiver Post has been exist！");
            }


            return _awaitableEventArgs;
        }
    }
}