using System;

namespace FreeRedis.OpenTelemetry
{

    public class FreeRedisInstrumentation : IDisposable
    {
        private readonly DiagnosticSourceSubscriber? _diagnosticSourceSubscriber;

        public FreeRedisInstrumentation(DiagnosticListener diagnosticListener)
        {
            _diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(diagnosticListener, null);
            _diagnosticSourceSubscriber.Subscribe();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _diagnosticSourceSubscriber?.Dispose();
        }
    }
}