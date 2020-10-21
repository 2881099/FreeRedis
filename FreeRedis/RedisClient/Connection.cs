using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public void Auth(string password) => _adapter.CheckSingle(() => Call<string>("AUTH".Input(password), rt => rt.ThrowOrValue()));
		public void Auth(string username, string password) => _adapter.CheckSingle(() => Call<string>("AUTH".SubCommand(null)
			.InputIf(!string.IsNullOrWhiteSpace(username), username)
			.InputRaw(password), rt => rt.ThrowOrValue()));

		public void ClientCaching(Confirm confirm) => _adapter.CheckSingle(() => Call<string>("CLIENT".SubCommand("CACHING").InputRaw(confirm), rt => rt.ThrowOrValue()));
		public string ClientGetName() => _adapter.CheckSingle(() => Call<string>("CLIENT".SubCommand("GETNAME"), rt => rt.ThrowOrValue()));
		public long ClientGetRedir() => _adapter.CheckSingle(() => Call<long>("CLIENT".SubCommand("GETREDIR"), rt => rt.ThrowOrValue()));
		public long ClientId() => _adapter.CheckSingle(() => Call<long>("CLIENT".SubCommand("ID"), rt => rt.ThrowOrValue()));

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

		public void ClientPause(long timeoutMilliseconds) => _adapter.CheckSingle(() => Call<string>("CLIENT"
			.SubCommand("PAUSE")
			.InputRaw(timeoutMilliseconds), rt => rt.ThrowOrValue()));

		public void ClientReply(ClientReplyType type) => _adapter.CheckSingle<int>(() =>
		{
			var cmd = "CLIENT".SubCommand("REPLY").InputRaw(type);
			using (var rds = _adapter.GetRedisSocket(cmd))
			{
				rds.Write(cmd);
				switch (type)
				{
					case ClientReplyType.Off:
						break;
					case ClientReplyType.On:
						cmd.Read<string>();
						break;
					case ClientReplyType.Skip:
						break;
				}
			}
			return 0;
		});
		public void ClientSetName(string connectionName) => _adapter.CheckSingle(() => Call<string>("CLIENT"
			.SubCommand("SETNAME")
			.InputRaw(connectionName), rt => rt.ThrowOrValue()));
		public void ClientTracking(bool on_off, long? redirect, string[] prefix, bool bcast, bool optin, bool optout, bool noloop) => _adapter.CheckSingle(() => Call<string>("CLIENT"
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

		public Dictionary<string, object> Hello(string protover, string username = null, string password = null, string clientname = null) => _adapter.CheckSingle(() => Call<object[], Dictionary<string, object>>("HELLO"
			.Input(protover)
			.InputIf(!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password), "AUTH", username, password)
			.InputIf(!string.IsNullOrWhiteSpace(clientname), "SETNAME", clientname), rt => rt
			.NewValue(a => a.MapToHash<object>(rt.Encoding)).ThrowOrValue()));

		public string Ping(string message = null) => Call<string>("PING".SubCommand(null)
			.InputIf(!string.IsNullOrEmpty(message), message), rt => rt.ThrowOrValue());

		public void Quit() => _adapter.CheckSingle(() => Call<string>("QUIT", rt => rt.ThrowOrValue()));
		public void Select(int index) => _adapter.CheckSingle(() => Call<string>("SELECT".Input(index), rt => rt.ThrowOrValue()));
	}
}
