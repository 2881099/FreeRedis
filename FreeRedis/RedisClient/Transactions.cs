using System;
using System.Collections.Generic;

namespace FreeRedis
{
    partial class RedisClient
	{
		public void Discard() => Call<string>("DISCARD".SubCommand(null)).ThrowOrValue();
		public object[] Exec()
		{
			try
			{
				return Call<object>("EXEC".SubCommand(null)).NewValue(a => a as List<object>).ThrowOrValue().ToArray();
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
			Call<string>("MULTI".SubCommand(null));
			_state = ClientStatus.Normal;
		}
		public void UnWatch() => Call<string>("UNWATCH".SubCommand(null)).ThrowOrValue();
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
