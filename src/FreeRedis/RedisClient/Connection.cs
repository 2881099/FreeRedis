using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
    {
        public void Auth(string password) => Call("AUTH".Input(password), rt => rt.ThrowOrValue());
        public void Auth(string username, string password) => Call("AUTH".SubCommand(null)
            .InputIf(!string.IsNullOrWhiteSpace(username), username)
            .InputRaw(password), rt => rt.ThrowOrValue());

        public void ClientCaching(Confirm confirm) => Call("CLIENT".SubCommand("CACHING").InputRaw(confirm), rt => rt.ThrowOrValue());
        public string ClientGetName() => Call("CLIENT".SubCommand("GETNAME"), rt => rt.ThrowOrValue<string>());
        public long ClientGetRedir() => Call("CLIENT".SubCommand("GETREDIR"), rt => rt.ThrowOrValue<long>());
        public long ClientId() => Call("CLIENT".SubCommand("ID"), rt => rt.ThrowOrValue<long>());

        public void ClientKill(string ipport) => Call("CLIENT"
            .SubCommand("KILL")
            .InputIf(!string.IsNullOrWhiteSpace(ipport), ipport), rt => rt.ThrowOrValue<long>());
        public long ClientKill(string ipport, long? clientid, ClientType? type, string username, string addr, Confirm? skipme) => Call("CLIENT"
            .SubCommand("KILL")
            .InputIf(!string.IsNullOrWhiteSpace(ipport), ipport)
            .InputIf(clientid != null, "ID", clientid)
            .InputIf(type != null, "TYPE", type)
            .InputIf(!string.IsNullOrWhiteSpace(username), "USER", username)
            .InputIf(!string.IsNullOrWhiteSpace(addr), "ADDR", addr)
            .InputIf(skipme != null, "SKIPME", skipme), rt => rt.ThrowOrValue<long>());

        public string ClientList(ClientType? type = null) => Call("CLIENT".SubCommand("LIST")
            .InputIf(type != null, "TYPE", type), rt => rt.ThrowOrValue<string>());
        public void ClientPause(long timeoutMilliseconds) => Call("CLIENT".SubCommand("PAUSE").InputRaw(timeoutMilliseconds), rt => rt.ThrowOrValue());

        public void ClientReply(ClientReplyType type)
        {
            var cmd = "CLIENT".SubCommand("REPLY").InputRaw(type);
            Adapter.TopOwner.LogCall(cmd, () =>
            {
                using (var rds = Adapter.GetRedisSocket(null))
                {
                    rds.Write(cmd);
                    switch (type)
                    {
                        case ClientReplyType.off:
                            break;
                        case ClientReplyType.on:
                            rds.Read(cmd).ThrowOrNothing();
                            break;
                        case ClientReplyType.skip:
                            break;
                    }
                }
                return default(string);
            });
        }
        public void ClientSetName(string connectionName) => Call("CLIENT".SubCommand("SETNAME").InputRaw(connectionName), rt => rt.ThrowOrNothing());
        public void ClientTracking(bool on_off, long? redirect, string[] prefix, bool bcast, bool optin, bool optout, bool noloop) => Call("CLIENT"
            .SubCommand("TRACKING")
            .InputIf(on_off, "ON")
            .InputIf(!on_off, "OFF")
            .InputIf(redirect != null, "REDIRECT", redirect)
            .InputIf(prefix?.Any() == true, prefix?.Select(a => new[] { "PREFIX", a }).SelectMany(a => a).ToArray())
            .InputIf(bcast, "BCAST")
            .InputIf(optin, "OPTIN")
            .InputIf(optout, "OPTOUT")
            .InputIf(noloop, "NOLOOP"), rt => rt.ThrowOrNothing());
        public bool ClientUnBlock(long clientid, ClientUnBlockType? type = null) => Call("CLIENT"
            .SubCommand("UNBLOCK")
            .InputRaw(clientid)
            .InputIf(type != null, type), rt => rt.ThrowOrValue<bool>());

        public string Echo(string message) => Call("ECHO".SubCommand(null)
            .InputIf(!string.IsNullOrEmpty(message), message), rt => rt.ThrowOrValue<string>());

        public Dictionary<string, object> Hello(string protover, string username = null, string password = null, string clientname = null) => Call("HELLO"
            .Input(protover)
            .InputIf(!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password), "AUTH", username, password)
            .InputIf(!string.IsNullOrWhiteSpace(clientname), "SETNAME", clientname), rt => rt
            .ThrowOrValue((a, _) => a.MapToHash<object>(rt.Encoding)));

        public string Ping(string message = null)
        {
            if (Adapter.UseType == UseType.SingleInside && 
                _pubsubPriv?.IsSubscribed == true && 
                this == Adapter.TopOwner)
            {
                _pubsub.Call("PING");
                return message ?? "PONG";
            }

            return Call("PING".SubCommand(null)
                .InputIf(!string.IsNullOrEmpty(message), message), rt => rt.ThrowOrValue(a =>
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
               }));
        }

        public void Quit() => Call("QUIT", rt => rt.ThrowOrValue());
        public void Select(int index) => Call("SELECT".Input(index), rt => rt.ThrowOrValue());
    }
}
