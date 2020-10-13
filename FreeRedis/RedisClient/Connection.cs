using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public void Auth(string password) => Call<string>("AUTH", null, password).ThrowOrValue();
		public void Auth(string username, string password) => Call<string>("AUTH", null, ""
			.AddIf(!string.IsNullOrWhiteSpace(username), username)
			.AddIf(true, password)
			.ToArray()).ThrowOrValue();
		public void ClientCaching(Confirm confirm) => Call<string>("CLIENT", "CACHING", confirm).ThrowOrValue();
		public string ClientGetName() => Call<string>("CLIENT", "GETNAME").ThrowOrValue();
		public long ClientGetRedir() => Call<long>("CLIENT", "GETREDIR").ThrowOrValue();
		public long ClientId() => Call<long>("CLIENT", "ID").ThrowOrValue();
		public void ClientKill(string ipport) => Call<long>("CLIENT", "KILL", ""
			.AddIf(!string.IsNullOrWhiteSpace(ipport), ipport)
			.ToArray()).ThrowOrValue();
		public long ClientKill(string ipport, long? clientid, ClientType? type, string username, string addr, Confirm? skipme) => Call<long>("CLIENT", "KILL", ""
			.AddIf(!string.IsNullOrWhiteSpace(ipport), ipport)
			.AddIf(clientid != null, "ID", clientid)
			.AddIf(type != null, "TYPE", type)
			.AddIf(!string.IsNullOrWhiteSpace(username), "USER", username)
			.AddIf(!string.IsNullOrWhiteSpace(addr), "ADDR", addr)
			.AddIf(skipme != null, "SKIPME", skipme)
			.ToArray()).ThrowOrValue();
		public string ClientList(ClientType? type = null) => Call<string>("CLIENT", "LIST", ""
			.AddIf(type != null, "TYPE", type)
			.ToArray()).ThrowOrValue();
		public void ClientPause(long timeoutMilliseconds) => Call<string>("CLIENT", "PAUSE", timeoutMilliseconds).ThrowOrValue();
		public void ClientReply(ClientReplyType type)
		{
			switch (type)
			{
				case ClientReplyType.Off:
					CallWriteOnly("CLIENT", "REPLY", type);
					_state = ClientStatus.ClientReplyOff;
					break;
				case ClientReplyType.On:
					Call<string>("CLIENT", "REPLY", type).ThrowOrValue();
					_state = ClientStatus.Normal;
					break;
				case ClientReplyType.Skip:
					CallWriteOnly("CLIENT", "REPLY", type);
					_state = ClientStatus.ClientReplySkip;
					break;
			}
		}
		public void ClientSetName(string connectionName) => Call<string>("CLIENT", "SETNAME", connectionName).ThrowOrValue();
		public void ClientTracking(bool on_off, long? redirect, string[] prefix, bool bcast, bool optin, bool optout, bool noloop) => Call<string>("CLIENT", "TRACKING", ""
			.AddIf(on_off, "ON")
			.AddIf(!on_off, "OFF")
			.AddIf(redirect != null, "REDIRECT", redirect)
			.AddIf(prefix?.Any() == true, prefix?.Select(a => new[] { "PREFIX", a }).SelectMany(a => a).ToArray())
			.AddIf(bcast, "BCAST")
			.AddIf(optin, "OPTIN")
			.AddIf(optout, "OPTOUT")
			.AddIf(noloop, "NOLOOP")
			.ToArray()).ThrowOrValue();
		public bool ClientUnBlock(long clientid, ClientUnBlockType? type = null) => Call<bool>("CLIENT", "UNBLOCK", ""
			.AddIf(true, clientid)
			.AddIf(type != null, type)
			.ToArray()).ThrowOrValue();
		public string Echo(string message) => Call<string>("ECHO", null, message).ThrowOrValue();
		public Dictionary<string, object> Hello(string protover, string username = null, string password = null, string clientname = null) => Call<object[]>("HELLO", null, ""
			.AddIf(true, protover)
			.AddIf(!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password), "AUTH", username, password)
			.AddIf(!string.IsNullOrWhiteSpace(clientname), "SETNAME", clientname)
			.ToArray()).NewValue(a => a.MapToHash<object>(Encoding)).ThrowOrValue();
		public string Ping(string message = null) => Call<string>("PING", message).ThrowOrValue();
		public void Quit() => Call<string>("QUIT").ThrowOrValue();
		public void Select(int index) => Call<string>("SELECT", null, index).ThrowOrValue();
	}
}
