using System;
using System.Collections.Generic;

namespace FreeRedis
{
    partial class RedisClient
	{
		public void Discard() => Call<string>("DISCARD").ThrowOrValue();
		public object[] Exec()
		{
			try
			{
				return Call<object>("EXEC").NewValue(a => a as List<object>).ThrowOrValue().ToArray();
            }
            catch
            {
				SafeReleaseSocket();
				throw;
            }
            finally
            {
				_state = ClientStatus.Normal;
            }
		}
		public void Multi()
		{
			if (_state != ClientStatus.Normal) throw new ArgumentException($"ClientModel current is: {_state}");
			Call<string>("MULTI");
			_state = ClientStatus.Normal;
		}
		public void UnWatch() => Call<string>("UNWATCH").ThrowOrValue();
		public void Watch(params string[] keys) => Call<string>("WATCH".Input(keys).FlagKey(keys)).ThrowOrValue();

		// Pipeline
		public void StartPipe()
		{
			if (_state != ClientStatus.Normal) throw new ArgumentException($"ClientModel current is: {_state}");
			_state = ClientStatus.Pipeline;
        }
		public void EndPipe()
        {
			if (_state != ClientStatus.Pipeline) throw new ArgumentException($"ClientModel current is: {_state}");
			_state = ClientStatus.Normal;
		}
	}
}
