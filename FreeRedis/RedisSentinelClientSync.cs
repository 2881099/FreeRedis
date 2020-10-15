using FreeRedis.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace FreeRedis
{
	public partial class RedisSentinelClient : RedisClientBase, IDisposable
	{
		protected IRedisSocket _redisSocket;

		public RedisSentinelClient(string host)
		{
			_redisSocket = new DefaultRedisSocket(host, false);
		}

		protected override IRedisSocket GetRedisSocket() => _redisSocket;

		~RedisSentinelClient() => this.Dispose();
		int _disposeCounter;
		public void Dispose()
		{
			if (Interlocked.Increment(ref _disposeCounter) != 1) return;
			try
			{
				_redisSocket.Dispose();
				Release();
			}
			finally
			{
				GC.SuppressFinalize(this);
			}
		}

		public string Ping() => Call<string>("PING", rt => rt.ThrowOrValue());
		public string Info() => Call<string>("INFO", rt => rt.ThrowOrValue());
		public object Role() => Call<object>("ROLE", rt => rt.ThrowOrValue());

		public object SentinelMasters() => Call<object>("SENTINEL".SubCommand("MASTERS"), rt => rt.ThrowOrValue());
		public object SentinelMaster(string masterName) => Call<object>("SENTINEL".SubCommand("MASTER").InputRaw(masterName), rt => rt.ThrowOrValue());
		public object SentinelReplicas(string masterName) => Call<object>("SENTINEL".SubCommand("REPLICAS").InputRaw(masterName), rt => rt.ThrowOrValue());
		public object SentinelSentinels(string masterName) => Call<object>("SENTINEL".SubCommand("SENTINELS").InputRaw(masterName), rt => rt.ThrowOrValue());
		public string SentinelGetMasterAddrByName(string masterName) => Call<string[], string>("SENTINEL".SubCommand("get-master-addr-by-name").InputRaw(masterName), rt => rt.NewValue(a => $"{a[0]}:{a[1]}").ThrowOrValue());
		public bool SentinelIsMasterDownByAddr(string ip, int port, long currentEpoch, string runid) => Call<bool>("SENTINEL".SubCommand("IS-MASTER-DOWN-BY-ADDR").Input(ip, port, currentEpoch, runid), rt => rt.ThrowOrValue());

		public object SentinelReset(string pattern) => Call<object>("SENTINEL".SubCommand("RESET").InputRaw(pattern), rt => rt.ThrowOrValue());
		public object SentinelFailover(string masterName) => Call<object>("SENTINEL".SubCommand("FAILOVER").InputRaw(masterName), rt => rt.ThrowOrValue());
		public object SentinelPendingScripts() => Call<object>("SENTINEL".SubCommand("PENDING-SCRIPTS"), rt => rt.ThrowOrValue());
		public object SentinelMonitor(string name, string ip, int port, int quorum) => Call<object>("SENTINEL".SubCommand("MONITOR").Input(name, ip, port, quorum), rt => rt.ThrowOrValue());
		public object SentinelFlushConfig() => Call<object>("SENTINEL".SubCommand("FLUSHCONFIG"), rt => rt.ThrowOrValue());
		public object SentinelRemove(string masterName) => Call<object>("SENTINEL".SubCommand("REMOVE").InputRaw(masterName), rt => rt.ThrowOrValue());
		public object SentinelCkQuorum(string masterName) => Call<object>("SENTINEL".SubCommand("CKQUORUM").InputRaw(masterName), rt => rt.ThrowOrValue());
		public object SentinelSet(string masterName, string option, string value) => Call<object>("SENTINEL".SubCommand("SET").Input(masterName, option, value), rt => rt.ThrowOrValue());
		public object SentinelInfoCache(string masterName) => Call<object>("SENTINEL".SubCommand("INFO-CACHE").InputRaw(masterName), rt => rt.ThrowOrValue());
		public object SentinelSimulateFailure(bool crashAfterElection, bool crashAfterPromotion) => Call<object>("SENTINEL"
			.SubCommand("SIMULATE-FAILURE")
			.InputIf(crashAfterElection, "crash-after-election")
			.InputIf(crashAfterPromotion, "crash-after-promotion"), rt => rt.ThrowOrValue());
	}
}
