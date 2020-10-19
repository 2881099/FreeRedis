using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		T CheckSingle<T>(Func<T> call)
        {
            switch (_usetype)
            {
				case UseType.Pooling:
					if (_ib.Get(_poolingBag.masterHost).Policy.PoolSize == 1) return call();
					throw new RedisException($"RedisClient: Method cannot be used in {_usetype} mode. You can set \"max pool size=1\", but it is not singleton mode.");

				case UseType.Sentinel:
					if (_ib.Get(_poolingBag.masterHost).Policy.PoolSize == 1) return call();
					throw new RedisException($"RedisClient: Method cannot be used in {_usetype} mode. You can set \"max pool size=1\", but it is not singleton mode.");

				case UseType.SingleInsideSocket:
				case UseType.OutsideSocket: return call();
			}
			throw new RedisException($"RedisClient: Method cannot be used in {_usetype} mode.");
		}

		public void Auth(string password) => CheckSingle(() => Call<string>("AUTH".Input(password), rt => rt.ThrowOrValue()));
		public void Auth(string username, string password) => CheckSingle(() => Call<string>("AUTH".SubCommand(null)
			.InputIf(!string.IsNullOrWhiteSpace(username), username)
			.InputRaw(password), rt => rt.ThrowOrValue()));

		public void ClientCaching(Confirm confirm) => CheckSingle(() => Call<string>("CLIENT".SubCommand("CACHING").InputRaw(confirm), rt => rt.ThrowOrValue()));
		public string ClientGetName() => CheckSingle(() => Call<string>("CLIENT".SubCommand("GETNAME"), rt => rt.ThrowOrValue()));
		public long ClientGetRedir() => CheckSingle(() => Call<long>("CLIENT".SubCommand("GETREDIR"), rt => rt.ThrowOrValue()));
		public long ClientId() => CheckSingle(() => Call<long>("CLIENT".SubCommand("ID"), rt => rt.ThrowOrValue()));

		public void ClientKill(string ipport) => Call<long>("CLIENT"
			.SubCommand("KILL")
			.InputIf(!string.IsNullOrWhiteSpace(ipport), ipport), rt => rt.ThrowOrValue());
		public long ClientKill(string ipport, long? clientid, ClientType? type, string username, string addr, Confirm? skipme) => Call<long>("CLIENT"
			.SubCommand("KILL")
			.InputIf(!string.IsNullOrWhiteSpace(ipport), ipport)
			.InputIf(clientid != null, "ID", clientid)
			.InputIf(type != null, "TYPE", type)
			.InputIf(!string.IsNullOrWhiteSpace(username), "USER", username)
			.InputIf(!string.IsNullOrWhiteSpace(addr), "ADDR", addr)
			.InputIf(skipme != null, "SKIPME", skipme), rt => rt.ThrowOrValue());
		public string ClientList(ClientType? type = null) => Call<string>("CLIENT"
			.SubCommand("LIST")
			.InputIf(type != null, "TYPE", type), rt => rt.ThrowOrValue());

		public void ClientPause(long timeoutMilliseconds) => CheckSingle(() => Call<string>("CLIENT"
			.SubCommand("PAUSE")
			.InputRaw(timeoutMilliseconds), rt => rt.ThrowOrValue()));
		public void ClientReply(ClientReplyType type) => CheckSingle<int>(() =>
		{
			switch (type)
			{
				case ClientReplyType.Off:
					_state = ClientStatus.ClientReplyOff;
					var cb1 = "CLIENT".SubCommand("REPLY").InputRaw(type);
					GetRedisSocket(cb1).Write(cb1);
					break;
				case ClientReplyType.On:
					_state = ClientStatus.Normal;
					Call<string>("CLIENT".SubCommand("REPLY").InputRaw(type), rt => rt.ThrowOrValue());
					break;
				case ClientReplyType.Skip:
					_state = ClientStatus.ClientReplySkip;
					var cb3 = "CLIENT".SubCommand("REPLY").InputRaw(type);
					GetRedisSocket(cb3).Write(cb3);
					break;
			}
			return 0;
		});
		public void ClientSetName(string connectionName) => CheckSingle(() => Call<string>("CLIENT"
			.SubCommand("SETNAME")
			.InputRaw(connectionName), rt => rt.ThrowOrValue()));
		public void ClientTracking(bool on_off, long? redirect, string[] prefix, bool bcast, bool optin, bool optout, bool noloop) => CheckSingle(() => Call<string>("CLIENT"
			.SubCommand("TRACKING")
			.InputIf(on_off, "ON")
			.InputIf(!on_off, "OFF")
			.InputIf(redirect != null, "REDIRECT", redirect)
			.InputIf(prefix?.Any() == true, prefix?.Select(a => new[] { "PREFIX", a }).SelectMany(a => a).ToArray())
			.InputIf(bcast, "BCAST")
			.InputIf(optin, "OPTIN")
			.InputIf(optout, "OPTOUT")
			.InputIf(noloop, "NOLOOP"), rt => rt.ThrowOrValue()));
		public bool ClientUnBlock(long clientid, ClientUnBlockType? type = null) => Call<bool>("CLIENT"
			.SubCommand("UNBLOCK")
			.InputRaw(clientid)
			.InputIf(type != null, type), rt => rt.ThrowOrValue());

		public string Echo(string message) => Call<string>("ECHO".SubCommand(null)
			.InputIf(!string.IsNullOrEmpty(message), message), rt => rt.ThrowOrValue());

		public Dictionary<string, object> Hello(string protover, string username = null, string password = null, string clientname = null) => CheckSingle(() => Call<object[], Dictionary<string, object>>("HELLO"
			.Input(protover)
			.InputIf(!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password), "AUTH", username, password)
			.InputIf(!string.IsNullOrWhiteSpace(clientname), "SETNAME", clientname), rt => rt
			.NewValue(a => a.MapToHash<object>(rt.Encoding)).ThrowOrValue()));

		public string Ping(string message = null) => Call<string>("PING".SubCommand(null)
			.InputIf(!string.IsNullOrEmpty(message), message), rt => rt.ThrowOrValue());

		public void Quit() => CheckSingle(() => Call<string>("QUIT", rt => rt.ThrowOrValue()));
		public void Select(int index) => CheckSingle(() => Call<string>("SELECT".Input(index), rt => rt.ThrowOrValue()));
	}
}
