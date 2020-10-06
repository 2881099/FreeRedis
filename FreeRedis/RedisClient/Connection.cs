using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public void Auth(string password) => Call<string>("AUTH", null, password).ThrowOrVoid();
		public void Auth(string username, string password) => Call<string>("AUTH", null, ""
			.AddIf(!string.IsNullOrWhiteSpace(username), username)
			.AddIf(true, password)
			.ToArray()).ThrowOrVoid();
		public RedisResult<string> ClientCaching(Confirm confirm) => Call<string>("CLIENT", "CACHING", confirm);
		public RedisResult<string> ClientGetName() => Call<string>("CLIENT", "GETNAME");
		public RedisResult<long> ClientGetRedir() => Call<long>("CLIENT", "GETREDIR");
		public RedisResult<long> ClientId() => Call<long>("CLIENT", "ID");
		public RedisResult<long> ClientKill(string ipport, long? clientid) => Call<long>("CLIENT", "KILL", ""
			.AddIf(!string.IsNullOrWhiteSpace(ipport), ipport)
			.AddIf(clientid != null, clientid)
			.ToArray());
		public RedisResult<long> ClientKill(string ipport, long? clientid, ClientType? type, string username, string addr, Confirm? skipme) => Call<long>("CLIENT", "KILL", ""
			.AddIf(!string.IsNullOrWhiteSpace(ipport), ipport)
			.AddIf(clientid != null, clientid)
			.AddIf(type != null, "TYPE", type)
			.AddIf(!string.IsNullOrWhiteSpace(username), "USER", username)
			.AddIf(!string.IsNullOrWhiteSpace(addr), "ADDR", addr)
			.AddIf(skipme != null, "SKIPME", skipme)
			.ToArray());
		public RedisResult<string[]> ClientList(ClientType? type) => Call<string[]>("CLIENT", "LIST", ""
			.AddIf(type != null, "TYPE", type)
			.ToArray());
		public RedisResult<string> ClientPaush(long timeoutMilliseconds) => Call<string>("CLIENT", "PAUSE", timeoutMilliseconds);
		public RedisResult<string> ClientReply(ClientReplyType type) => Call<string>("CLIENT", "REPLY", type);
		public RedisResult<string> ClientSetName(string connectionName) => Call<string>("CLIENT", "SETNAME", connectionName);
		public RedisResult<string> ClientTracking(bool on_off, long? redirect, string[] prefix, bool bcast, bool optin, bool optout, bool noloop) => Call<string>("CLIENT", "TRACKING", ""
			.AddIf(on_off, "ON")
			.AddIf(!on_off, "OFF")
			.AddIf(redirect != null, "REDIRECT", redirect)
			.AddIf(prefix?.Any() == true, prefix.Select(a => new[] { "PREFIX", a }).SelectMany(a => a).ToArray())
			.AddIf(bcast, "BCAST")
			.AddIf(optin, "OPTIN")
			.AddIf(optout, "OPTOUT")
			.AddIf(noloop, "NOLOOP")
			.ToArray());
		public RedisResult<bool> ClientUnBlock(long clientid, ClientUnBlockType? type = null) => Call<bool>("CLIENT", "UNBLOCK", ""
			.AddIf(true, clientid)
			.AddIf(type != null, type)
			.ToArray());
		public RedisResult<string> Echo(string message) => Call<string>("ECHO", null, message);
		public RedisResult<object> Hello(decimal protover, string username, string password, string clientname) => Call<object>("HELLO", null, ""
			.AddIf(true, protover)
			.AddIf(!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password), "AUTH", username, password)
			.AddIf(!string.IsNullOrWhiteSpace(clientname), "SETNAME", clientname)
			.ToArray());
		public RedisResult<string> Ping(string message = null) => Call<string>("PING", null, message);
		public RedisResult<string> Quit() => Call<string>("QUIT");
		public RedisResult<string> Select(int index) => Call<string>("SELECT", null, index);
    }
}
