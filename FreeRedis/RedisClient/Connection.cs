using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public void Auth(string password) => Call<string>("AUTH".Input(password)).ThrowOrValue();
		public void Auth(string username, string password) => Call<string>("AUTH".SubCommand(null)
			.InputIf(!string.IsNullOrWhiteSpace(username), username)
			.InputRaw(password)).ThrowOrValue();

		public void ClientCaching(Confirm confirm) => Call<string>("CLIENT".SubCommand("CACHING").InputRaw(confirm)).ThrowOrValue();
		public string ClientGetName() => Call<string>("CLIENT".SubCommand("GETNAME")).ThrowOrValue();
		public long ClientGetRedir() => Call<long>("CLIENT".SubCommand("GETREDIR")).ThrowOrValue();
		public long ClientId() => Call<long>("CLIENT".SubCommand("ID")).ThrowOrValue();
		public void ClientKill(string ipport) => Call<long>("CLIENT"
			.SubCommand("KILL")
			.InputIf(!string.IsNullOrWhiteSpace(ipport), ipport)).ThrowOrValue();
		public long ClientKill(string ipport, long? clientid, ClientType? type, string username, string addr, Confirm? skipme) => Call<long>("CLIENT"
			.SubCommand("KILL")
			.InputIf(!string.IsNullOrWhiteSpace(ipport), ipport)
			.InputIf(clientid != null, "ID", clientid)
			.InputIf(type != null, "TYPE", type)
			.InputIf(!string.IsNullOrWhiteSpace(username), "USER", username)
			.InputIf(!string.IsNullOrWhiteSpace(addr), "ADDR", addr)
			.InputIf(skipme != null, "SKIPME", skipme)).ThrowOrValue();
		public string ClientList(ClientType? type = null) => Call<string>("CLIENT"
			.SubCommand("LIST")
			.InputIf(type != null, "TYPE", type)).ThrowOrValue();
		public void ClientPause(long timeoutMilliseconds) => Call<string>("CLIENT"
			.SubCommand("PAUSE")
			.InputRaw(timeoutMilliseconds)).ThrowOrValue();
		public void ClientReply(ClientReplyType type)
		{
			switch (type)
			{
				case ClientReplyType.Off:
					CallWriteOnly("CLIENT".SubCommand("REPLY").InputRaw(type));
					_state = ClientStatus.ClientReplyOff;
					break;
				case ClientReplyType.On:
					Call<string>("CLIENT".SubCommand("REPLY").InputRaw(type)).ThrowOrValue();
					_state = ClientStatus.Normal;
					break;
				case ClientReplyType.Skip:
					CallWriteOnly("CLIENT".SubCommand("REPLY").InputRaw(type));
					_state = ClientStatus.ClientReplySkip;
					break;
			}
		}
		public void ClientSetName(string connectionName) => Call<string>("CLIENT"
			.SubCommand("SETNAME")
			.InputRaw(connectionName)).ThrowOrValue();
		public void ClientTracking(bool on_off, long? redirect, string[] prefix, bool bcast, bool optin, bool optout, bool noloop) => Call<string>("CLIENT"
			.SubCommand("TRACKING")
			.InputIf(on_off, "ON")
			.InputIf(!on_off, "OFF")
			.InputIf(redirect != null, "REDIRECT", redirect)
			.InputIf(prefix?.Any() == true, prefix?.Select(a => new[] { "PREFIX", a }).SelectMany(a => a).ToArray())
			.InputIf(bcast, "BCAST")
			.InputIf(optin, "OPTIN")
			.InputIf(optout, "OPTOUT")
			.InputIf(noloop, "NOLOOP")).ThrowOrValue();
		public bool ClientUnBlock(long clientid, ClientUnBlockType? type = null) => Call<bool>("CLIENT"
			.SubCommand("UNBLOCK")
			.InputRaw(clientid)
			.InputIf(type != null, type)).ThrowOrValue();

		public string Echo(string message) => Call<string>("ECHO".SubCommand(null)
			.InputIf(!string.IsNullOrEmpty(message), message)).ThrowOrValue();
		public Dictionary<string, object> Hello(string protover, string username = null, string password = null, string clientname = null) => Call<object[]>("HELLO"
			.Input(protover)
			.InputIf(!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password), "AUTH", username, password)
			.InputIf(!string.IsNullOrWhiteSpace(clientname), "SETNAME", clientname))
			.NewValue(a => a.MapToHash<object>(Encoding)).ThrowOrValue();
		public string Ping(string message = null) => Call<string>("PING".SubCommand(null)
			.InputIf(!string.IsNullOrEmpty(message), message)).ThrowOrValue();
		public void Quit() => Call<string>("QUIT").ThrowOrValue();
		public void Select(int index) => Call<string>("SELECT".Input(index)).ThrowOrValue();
	}
}
