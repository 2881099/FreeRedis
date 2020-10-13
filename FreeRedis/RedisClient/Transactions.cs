using System;
using System.Collections.Generic;

namespace FreeRedis
{
    partial class RedisClient
	{
		public void Discard() => Call<string>("DISCARD", rt => rt.ThrowOrValue());
		public object[] Exec()
		{
			try
			{
				return Call<object, List<object>>("EXEC", rt => rt.NewValue(a => a as List<object>).ThrowOrValue()).ToArray();
            }
            catch
            {
				Release();
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
			Call<string>("MULTI", rt => rt.ThrowOrValue());
			_state = ClientStatus.Normal;
		}
		public void UnWatch() => Call<string>("UNWATCH", rt => rt.ThrowOrValue());
		public void Watch(params string[] keys) => Call<string>("WATCH".Input(keys).FlagKey(keys), rt => rt.ThrowOrValue());

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
