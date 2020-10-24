using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public void Auth(string password) => Call<string>("AUTH".Input(password), rt => rt.ThrowOrValue());
		public void Auth(string username, string password) => Call<string>("AUTH".SubCommand(null)
			.InputIf(!string.IsNullOrWhiteSpace(username), username)
			.InputRaw(password), rt => rt.ThrowOrValue());

		public void ClientCaching(Confirm confirm) => Call<string>("CLIENT".SubCommand("CACHING").InputRaw(confirm), rt => rt.ThrowOrValue());
		public string ClientGetName() => Call<string>("CLIENT".SubCommand("GETNAME"), rt => rt.ThrowOrValue());
		public long ClientGetRedir() => Call<long>("CLIENT".SubCommand("GETREDIR"), rt => rt.ThrowOrValue());
		public long ClientId() => Call<long>("CLIENT".SubCommand("ID"), rt => rt.ThrowOrValue());

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
		public string ClientList(ClientType? type = null) => Call<string>("CLIENT".SubCommand("LIST")
			.InputIf(type != null, "TYPE", type), rt => rt.ThrowOrValue());

		public void ClientPause(long timeoutMilliseconds) => Call<string>("CLIENT".SubCommand("PAUSE").InputRaw(timeoutMilliseconds), rt => rt.ThrowOrValue());

		public void ClientReply(ClientReplyType type)
		{
			var cmd = "CLIENT".SubCommand("REPLY").InputRaw(type);
			LogCall(cmd, () =>
			{
				using (var rds = Adapter.GetRedisSocket(null))
				{
					rds.Write(cmd);
					switch (type)
					{
						case ClientReplyType.off:
							break;
						case ClientReplyType.on:
							cmd.Read<string>().ThrowOrValue();
							break;
						case ClientReplyType.skip:
							break;
					}
				}
				return default(string);
			});
		}
		public void ClientSetName(string connectionName) => Call<string>("CLIENT".SubCommand("SETNAME").InputRaw(connectionName), rt => rt.ThrowOrValue());
		public void ClientTracking(bool on_off, long? redirect, string[] prefix, bool bcast, bool optin, bool optout, bool noloop) => Call<string>("CLIENT"
			.SubCommand("TRACKING")
			.InputIf(on_off, "ON")
			.InputIf(!on_off, "OFF")
			.InputIf(redirect != null, "REDIRECT", redirect)
			.InputIf(prefix?.Any() == true, prefix?.Select(a => new[] { "PREFIX", a }).SelectMany(a => a).ToArray())
			.InputIf(bcast, "BCAST")
			.InputIf(optin, "OPTIN")
			.InputIf(optout, "OPTOUT")
			.InputIf(noloop, "NOLOOP"), rt => rt.ThrowOrValue());
		public bool ClientUnBlock(long clientid, ClientUnBlockType? type = null) => Call<bool>("CLIENT"
			.SubCommand("UNBLOCK")
			.InputRaw(clientid)
			.InputIf(type != null, type), rt => rt.ThrowOrValue());

		public string Echo(string message) => Call<string>("ECHO".SubCommand(null)
			.InputIf(!string.IsNullOrEmpty(message), message), rt => rt.ThrowOrValue());

		public Dictionary<string, object> Hello(string protover, string username = null, string password = null, string clientname = null) => Call<object[], Dictionary<string, object>>("HELLO"
			.Input(protover)
			.InputIf(!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password), "AUTH", username, password)
			.InputIf(!string.IsNullOrWhiteSpace(clientname), "SETNAME", clientname), rt => rt
			.NewValue(a => a.MapToHash<object>(rt.Encoding)).ThrowOrValue());

		public string Ping(string message = null)
		{
			if (Adapter.UseType == UseType.SingleInside && 
				_pubsubPriv?.IsSubscribed == true && 
				this == Adapter.TopOwner)
            {
				_pubsub.Call("PING");
				return message ?? "PONG";
			}

			return Call<object, string>("PING".SubCommand(null)
				.InputIf(!string.IsNullOrEmpty(message), message), rt => rt.NewValue(a =>
			   {
				   if (a is string str) return str;
				   if (a is object[] objs)
				   {
						//If the client is subscribed to a channel or a pattern, 
						//it will instead return a multi-bulk with a "pong" in the first position and an empty bulk in the second position, 
						//unless an argument is provided in which case it returns a copy of the argument.
						var str2 = objs[1].ConvertTo<string>();
					   if (!string.IsNullOrEmpty(str2)) return str2;
					   return objs[0].ConvertTo<string>();
				   }
				   return a.ConvertTo<string>();
			   }).ThrowOrValue());
		}

		public void Quit() => Call<string>("QUIT", rt => rt.ThrowOrValue());
		public void Select(int index) => Call<string>("SELECT".Input(index), rt => rt.ThrowOrValue());
	}
}
