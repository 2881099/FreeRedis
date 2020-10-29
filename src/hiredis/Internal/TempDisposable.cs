using System;
using System.Collections.Generic;
using System.Text;

namespace hiredis.Internal
{
    class TempDisposable : IDisposable
    {
        Action _release;
        public TempDisposable(Action release)
        {
            _release = release;
        }

        public void Dispose() => _release?.Invoke();
    }
}
