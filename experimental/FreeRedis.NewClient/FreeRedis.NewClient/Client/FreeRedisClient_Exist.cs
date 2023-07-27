using FreeRedis.Engine;
using FreeRedis.NewClient.Protocols;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace FreeRedis.NewClient.Client
{
    public sealed partial class FreeRedisClient : FreeRedisClientBase
    {
        public Task<long> ExistAsync(string key)
        {

            var chars1 = key.AsSpan();
            TaskCompletionSource<long> result = new(TaskCreationOptions.RunContinuationsAsynchronously);
            ProtocolContinueResult delegateMethod(ref SequenceReader<byte> reader) { return NumberProtocol.HandleBytes(result, ref reader); }
            if (chars1.Length + 9 <= 1048576)
            {
                // The input span is small enough where we can one-shot this.

                int byteCount1 = _utf8.GetByteCount(chars1);
                var length = byteCount1 + _exist_command_length + 2;

                WaitSendLock();
                _taskBuffer.Enqueue(delegateMethod);

                Span<byte> scratchBuffer = _sender.GetSpan(length);
                _exist_command_buffer.CopyTo(scratchBuffer);

                _utf8.GetBytes(chars1, scratchBuffer.Slice(_exist_command_length, byteCount1));
                scratchBuffer[length - 2] = 13;
                scratchBuffer[length - 1] = 10;
                _sender.Advance(length);

            }
            else
            {
                WaitSendLock();

                _taskBuffer.Enqueue(delegateMethod);
                //Allocate a stateful Encoder instance and chunk this.
                _utf8_encoder.Convert($"EXISTS {key}\r\n", _sender, true, out _, out _);
            }

            if (_sender.CanGetUnflushedBytes && _sender.UnflushedBytes > 2048)
            {
                _sender.FlushAsync().ConfigureAwait(false);
            }

            ReleaseSendLock();

            if (_sender.CanGetUnflushedBytes && _sender.UnflushedBytes > 0 && TryGetSendLock())
            {
                _sender.FlushAsync().ConfigureAwait(false);
                ReleaseSendLock();
            }
            return result.Task;
        }

    }

}
