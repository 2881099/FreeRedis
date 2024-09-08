using OpenTelemetry.Trace;
using System;
using System.Diagnostics;

namespace FreeRedis.OpenTelemetry
{

    public static class TracerProviderBuilderExtensions
    {
        public static TracerProviderBuilder AddFreeRedisInstrumentation(this TracerProviderBuilder builder, RedisClient redisClient)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (redisClient == null) throw new ArgumentNullException(nameof(redisClient));

            redisClient.Notice += (s, e) =>
            {
                if (Debugger.IsAttached)
                    Debug.WriteLine(e.Log);
            };

            builder.AddSource(DiagnosticListener.SourceName);

            var instrumentation = new FreeRedisInstrumentation(new DiagnosticListener());

            return builder.AddInstrumentation(() => instrumentation);
        }
    }
}