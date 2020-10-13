using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public RedisResult<string> ClusterAddSlots(params int[] slot) => Call<string>("CLUSTER".SubCommand("ADDSLOTS").Input(slot));
		public RedisResult<string> ClusterBumpEpoch() => Call<string>("CLUSTER".SubCommand("BUMPEPOCH"));
		public RedisResult<long> ClusterCountFailureReports(string nodeid) => Call<long>("CLUSTER".SubCommand("COUNT-FAILURE-REPORTS").InputRaw(nodeid));
		public RedisResult<long> ClusterCountKeysInSlot(int slot) => Call<long>("CLUSTER".SubCommand("COUNTKEYSINSLOT").InputRaw(slot));
		public RedisResult<string> ClusterDelSlots(params int[] slot) => Call<string>("CLUSTER".SubCommand("DELSLOTS").Input(slot));
		public RedisResult<string> ClusterFailOver(ClusterFailOverType type) => Call<string>("CLUSTER".SubCommand("FAILOVER").InputRaw(type));
		public RedisResult<string> ClusterFlushSlots() => Call<string>("CLUSTER".SubCommand("FLUSHSLOTS"));
		public RedisResult<long> ClusterForget(string nodeid) => Call<long>("CLUSTER".SubCommand("FORGET").InputRaw(nodeid));
		public RedisResult<string[]> ClusterGetKeysInSlot(int slot) => Call<string[]>("CLUSTER".SubCommand("GETKEYSINSLOT").InputRaw(slot));
		public RedisResult<Dictionary<string, string>> ClusterInfo() => Call<string[]>("CLUSTER".SubCommand("INFO")).NewValue(a => a.MapToHash<string>(Encoding));
		public RedisResult<int> ClusterKeySlot(string key) => Call<int>("CLUSTER".SubCommand("KEYSLOT").InputRaw(key));
		public RedisResult<string> ClusterMeet(string ip, int port) => Call<string>("CLUSTER".SubCommand("MEET").Input(ip, port));
		public RedisResult<string> ClusterMyId() => Call<string>("CLUSTER".SubCommand("MYID"));
		public RedisResult<string> ClusterNodes() => Call<string>("CLUSTER".SubCommand("NODES"));
		public RedisResult<string> ClusterReplicas(string nodeid) => Call<string>("CLUSTER".SubCommand("REPLICAS").InputRaw(nodeid));
		public RedisResult<string> ClusterReplicate(string nodeid) => Call<string>("CLUSTER".SubCommand("REPLICATE").InputRaw(nodeid));
		public RedisResult<string> ClusterReset(ClusterResetType type) => Call<string>("CLUSTER".SubCommand("RESET").InputRaw(type));
		public RedisResult<string> ClusterSaveConfig() => Call<string>("CLUSTER".SubCommand("SAVECONFIG"));
		public RedisResult<string> ClusterSetConfigEpoch(string epoch) => Call<string>("CLUSTER".SubCommand("SET-CONFIG-EPOCH").InputRaw(epoch));
		public RedisResult<string[]> ClusterSetSlot(int slot, ClusterSetSlotType type, string nodeid = null) => Call<string[]>("CLUSTER".SubCommand("SETSLOT")
			.Input(slot, type)
			.InputIf(!string.IsNullOrWhiteSpace(nodeid), nodeid));
		public RedisResult<string> ClusterSlaves(string nodeid) => Call<string>("CLUSTER".SubCommand("SLAVES").InputRaw(nodeid));
		public RedisResult<object> ClusterSlots() => Call<object>("CLUSTER".SubCommand("SLOTS"));
		public RedisResult<string> ReadOnly() => Call<string>("READONLY");
		public RedisResult<string> ReadWrite() => Call<string>("READWRITE");
    }
}
