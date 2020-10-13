using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	public partial class RedisSentinelClient : RedisClientBase, IDisposable
	{
		public RedisSentinelClient(string host) : base(host, false) { }

		public void Dispose()
		{
			base.SafeReleaseSocket();
		}

		public RedisResult<string> Ping() => Call<string>("PING");
		public RedisResult<string> Info() => Call<string>("INFO");
		public RedisResult<object> Role() => Call<object>("ROLE");

		public RedisResult<object> SentinelMasters() => Call<object>("SENTINEL".SubCommand("MASTERS"));
		public RedisResult<object> SentinelMaster(string masterName) => Call<object>("SENTINEL".SubCommand("MASTER").InputRaw(masterName));
		public RedisResult<object> SentinelReplicas(string masterName) => Call<object>("SENTINEL".SubCommand("REPLICAS").InputRaw(masterName));
		public RedisResult<object> SentinelSentinels(string masterName) => Call<object>("SENTINEL".SubCommand("SENTINELS").InputRaw(masterName));
		public RedisResult<string> SentinelGetMasterAddrByName(string masterName) => Call<string[]>("SENTINEL".SubCommand("get-master-addr-by-name").InputRaw(masterName)).NewValue(a => $"{a[0]}:{a[1]}");
		public RedisResult<bool> SentinelIsMasterDownByAddr(string ip, int port, long currentEpoch, string runid) => Call<bool>("SENTINEL".SubCommand("IS-MASTER-DOWN-BY-ADDR").Input(ip, port, currentEpoch, runid));

		public RedisResult<object> SentinelReset(string pattern) => Call<object>("SENTINEL".SubCommand("RESET").InputRaw(pattern));
		public RedisResult<object> SentinelFailover(string masterName) => Call<object>("SENTINEL".SubCommand("FAILOVER").InputRaw(masterName));
		public RedisResult<object> SentinelPendingScripts() => Call<object>("SENTINEL".SubCommand("PENDING-SCRIPTS"));
		public RedisResult<object> SentinelMonitor(string name, string ip, int port, int quorum) => Call<object>("SENTINEL".SubCommand("MONITOR").Input(name, ip, port, quorum));
		public RedisResult<object> SentinelFlushConfig() => Call<object>("SENTINEL".SubCommand("FLUSHCONFIG"));
		public RedisResult<object> SentinelRemove(string masterName) => Call<object>("SENTINEL".SubCommand("REMOVE").InputRaw(masterName));
		public RedisResult<object> SentinelCkQuorum(string masterName) => Call<object>("SENTINEL".SubCommand("CKQUORUM").InputRaw(masterName));
		public RedisResult<object> SentinelSet(string masterName, string option, string value) => Call<object>("SENTINEL".SubCommand("SET").Input(masterName, option, value));
		public RedisResult<object> SentinelInfoCache(string masterName) => Call<object>("SENTINEL".SubCommand("INFO-CACHE").InputRaw(masterName));
		public RedisResult<object> SentinelSimulateFailure(bool crashAfterElection, bool crashAfterPromotion) => Call<object>("SENTINEL"
			.SubCommand("SIMULATE-FAILURE")
			.InputIf(crashAfterElection, "crash-after-election")
			.InputIf(crashAfterPromotion, "crash-after-promotion"));
	}
}
