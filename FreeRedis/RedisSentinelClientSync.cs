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

		public RedisResult<object> SentinelMasters() => Call<object>("SENTINEL", "MASTERS");
		public RedisResult<object> SentinelMaster(string masterName) => Call<object>("SENTINEL", "MASTER", masterName);
		public RedisResult<object> SentinelReplicas(string masterName) => Call<object>("SENTINEL", "REPLICAS", masterName);
		public RedisResult<object> SentinelSentinels(string masterName) => Call<object>("SENTINEL", "SENTINELS", masterName);
		public RedisResult<string> SentinelGetMasterAddrByName(string masterName) => Call<string[]>("SENTINEL", "get-master-addr-by-name", masterName).NewValue(a => $"{a[0]}:{a[1]}");
		public RedisResult<bool> SentinelIsMasterDownByAddr(string ip, int port, long currentEpoch, string runid) => Call<bool>("SENTINEL", "IS-MASTER-DOWN-BY-ADDR", ip, port, currentEpoch, runid);
		public RedisResult<object> SentinelReset(string pattern) => Call<object>("SENTINEL", "RESET", pattern);
		public RedisResult<object> SentinelFailover(string masterName) => Call<object>("SENTINEL", "FAILOVER", masterName);
		public RedisResult<object> SentinelPendingScripts() => Call<object>("SENTINEL", "PENDING-SCRIPTS");
		public RedisResult<object> SentinelMonitor(string name, string ip, int port, int quorum) => Call<object>("SENTINEL", "MONITOR", name, ip, port, quorum);
		public RedisResult<object> SentinelFlushConfig() => Call<object>("SENTINEL", "FLUSHCONFIG");
		public RedisResult<object> SentinelRemove(string masterName) => Call<object>("SENTINEL", "REMOVE", masterName);
		public RedisResult<object> SentinelCkQuorum(string masterName) => Call<object>("SENTINEL", "CKQUORUM", masterName);
		public RedisResult<object> SentinelSet(string masterName, string option, string value) => Call<object>("SENTINEL", "SET", masterName, option, value);
		public RedisResult<object> SentinelInfoCache(string masterName) => Call<object>("SENTINEL", "INFO-CACHE", masterName);
		public RedisResult<object> SentinelSimulateFailure(bool crashAfterElection, bool crashAfterPromotion) => Call<object>("SENTINEL", "SIMULATE-FAILURE", ""
			.AddIf(crashAfterElection, "crash-after-election")
			.AddIf(crashAfterElection, "crash-after-promotion")
			.ToArray());
	}
}
