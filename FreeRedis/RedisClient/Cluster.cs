using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<string> ClusterAddSlots(params int[] slot) => Call<string>("CLUSTER", "ADDSLOTS", "".AddIf(true, slot).ToArray());
		public RedisResult<string> ClusterBumpEpoch() => Call<string>("CLUSTER", "BUMPEPOCH");
		public RedisResult<long> ClusterCountFailureReports(string nodeid) => Call<long>("CLUSTER", "COUNT-FAILURE-REPORTS", nodeid);
		public RedisResult<long> ClusterCountKeysInSlot(int slot) => Call<long>("CLUSTER", "COUNTKEYSINSLOT", slot);
		public RedisResult<string> ClusterDelSlots(params int[] slot) => Call<string>("CLUSTER", "DELSLOTS", "".AddIf(true, slot).ToArray());
		public RedisResult<string> ClusterFailOver(ClusterFailOverType type) => Call<string>("CLUSTER", "FAILOVER", type);
		public RedisResult<string> ClusterFlushSlots() => Call<string>("CLUSTER", "FLUSHSLOTS");
		public RedisResult<long> ClusterForget(string nodeid) => Call<long>("CLUSTER", "FORGET", nodeid);
		public RedisResult<string[]> ClusterGetKeysInSlot(int slot) => Call<string[]>("CLUSTER", "GETKEYSINSLOT", slot);
		public RedisResult<Dictionary<string, string>> ClusterInfo() => Call<string[]>("CLUSTER", "INFO").NewValue(a => a.MapToHash<string>(Encoding));
		public RedisResult<int> ClusterKeySlot(string key) => Call<int>("CLUSTER", "KEYSLOT", key);
		public RedisResult<string> ClusterMeet(string ip, int port) => Call<string>("CLUSTER", "MEET", ip, port);
		public RedisResult<string> ClusterMyId() => Call<string>("CLUSTER", "MYID");
		public RedisResult<string> ClusterNodes() => Call<string>("CLUSTER", "NODES");
		public RedisResult<string> ClusterReplicas(string nodeid) => Call<string>("CLUSTER", "REPLICAS", nodeid);
		public RedisResult<string> ClusterReplicate(string nodeid) => Call<string>("CLUSTER", "REPLICATE", nodeid);
		public RedisResult<string> ClusterReset(ClusterResetType type) => Call<string>("CLUSTER", "RESET", type);
		public RedisResult<string> ClusterSaveConfig() => Call<string>("CLUSTER", "SAVECONFIG");
		public RedisResult<string> ClusterSetConfigEpoch(string epoch) => Call<string>("CLUSTER", "SET-CONFIG-EPOCH", epoch);
		public RedisResult<string[]> ClusterSetSlot(int slot, ClusterSetSlotType type, string nodeid = null) => Call<string[]>("CLUSTER", "SETSLOT", ""
			.AddIf(true, slot, type)
			.AddIf(!string.IsNullOrWhiteSpace(nodeid), nodeid)
			.ToArray());
		public RedisResult<string> ClusterSlaves(string nodeid) => Call<string>("CLUSTER", "SLAVES", nodeid);
		public RedisResult<object> ClusterSlots() => Call<object>("CLUSTER", "SLOTS");
		public RedisResult<string> ReadOnly() => Call<string>("READONLY");
		public RedisResult<string> ReadWrite() => Call<string>("READWRITE");
    }
}
