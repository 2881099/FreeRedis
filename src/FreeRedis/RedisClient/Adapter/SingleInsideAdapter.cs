﻿using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
        internal class SingleInsideAdapter : BaseAdapter
        {
            readonly IRedisSocket _redisSocket;

            public SingleInsideAdapter(RedisClient topOwner, RedisClient owner, string host, 
                bool ssl, RemoteCertificateValidationCallback certificateValidation, LocalCertificateSelectionCallback certificateSelection,
                TimeSpan connectTimeout, TimeSpan receiveTimeout, TimeSpan sendTimeout, 
                Action<RedisClient> connected, Action<RedisClient> disconnected)
            {
                UseType = UseType.SingleInside;
                TopOwner = topOwner;
                _redisSocket = new DefaultRedisSocket(host, ssl, certificateValidation, certificateSelection);
                _redisSocket.Connected += (s, e) => connected?.Invoke(owner);
                _redisSocket.Disconnected += (s, e) => disconnected?.Invoke(owner);
                _redisSocket.ConnectTimeout = connectTimeout;
                _redisSocket.ReceiveTimeout = receiveTimeout;
                _redisSocket.SendTimeout = sendTimeout;
            }

            public override void Dispose()
            {
                _redisSocket.Dispose();
            }

            public override void Refersh(IRedisSocket redisSocket)
            {
            }
            public override IRedisSocket GetRedisSocket(CommandPacket cmd)
            {
                return DefaultRedisSocket.CreateTempProxy(_redisSocket, null);
            }
            public override TValue AdapterCall<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                return TopOwner.LogCall(cmd, () =>
                {
                    _redisSocket.Write(cmd);
                    var rt = _redisSocket.Read(cmd);
                    if (cmd._command == "QUIT") _redisSocket.ReleaseSocket();
                    return parse(rt);
                });
            }
#if isasync
            public override Task<TValue> AdapterCallAsync<TValue>(CommandPacket cmd, Func<RedisResult, TValue> parse)
            {
                return TopOwner.LogCallAsync(cmd, async () =>
                {
                    await _redisSocket.WriteAsync(cmd);
                    var rt = await _redisSocket.ReadAsync(cmd);
                    if (cmd._command == "QUIT") _redisSocket.ReleaseSocket();
                    return parse(rt);
                });
            }
#endif

        }
    }
}