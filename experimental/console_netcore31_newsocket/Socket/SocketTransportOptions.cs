﻿using System;
using System.Buffers;

namespace console_netcore31_newsocket
{
    /// <summary>
    /// Options for socket based transports.
    /// </summary>
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
        /// Wait until there is data available to allocate a buffer. Setting this to false can increase throughput at the cost of increased memory usage.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool WaitForDataBeforeAllocatingBuffer { get; set; } = true;

        /// <summary>
        /// Set to false to enable Nagle's algorithm for all connections.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool NoDelay { get; set; } = false;

        /// <summary>
        /// The maximum length of the pending connection queue.
        /// </summary>
        /// <remarks>
        /// Defaults to 512.
        /// </remarks>
        public int Backlog { get; set; } = 512;

        /// <summary>
        /// Gets or sets the maximum unconsumed incoming bytes the transport will buffer.
        /// </summary>
        public long? MaxReadBufferSize { get; set; } = 4L * 1024 * 1024 * 1024;

        /// <summary>
        /// Gets or sets the maximum outgoing bytes the transport will buffer before applying write backpressure.
        /// </summary>
        public long? MaxWriteBufferSize { get; set; } = 512 * 1024;

        /// <summary>
        /// Inline application and transport continuations instead of dispatching to the threadpool.
        /// </summary>
        /// <remarks>
        /// This will run application code on the IO thread which is why this is unsafe.
        /// It is recommended to set the DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS environment variable to '1' when using this setting to also inline the completions
        /// at the runtime layer as well.
        /// This setting can make performance worse if there is expensive work that will end up holding onto the IO thread for longer than needed.
        /// Test to make sure this setting helps performance.
        /// </remarks>
        public bool UnsafePreferInlineScheduling { get; set; }

        internal Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } =()=>new SlabMemoryPool();
    }
}
