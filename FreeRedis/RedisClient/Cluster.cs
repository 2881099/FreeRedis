using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	partial class RedisClient
	{
		public string ClusterAddSlots(params int[] slot) => Call<string>("CLUSTER".SubCommand("ADDSLOTS").Input(slot), rt => rt.ThrowOrValue());
		public string ClusterBumpEpoch() => Call<string>("CLUSTER".SubCommand("BUMPEPOCH"), rt => rt.ThrowOrValue());
		public long ClusterCountFailureReports(string nodeid) => Call<long>("CLUSTER".SubCommand("COUNT-FAILURE-REPORTS").InputRaw(nodeid), rt => rt.ThrowOrValue());
		public long ClusterCountKeysInSlot(int slot) => Call<long>("CLUSTER".SubCommand("COUNTKEYSINSLOT").InputRaw(slot), rt => rt.ThrowOrValue());
		public string ClusterDelSlots(params int[] slot) => Call<string>("CLUSTER".SubCommand("DELSLOTS").Input(slot), rt => rt.ThrowOrValue());
		public string ClusterFailOver(ClusterFailOverType type) => Call<string>("CLUSTER".SubCommand("FAILOVER").InputRaw(type), rt => rt.ThrowOrValue());
		public string ClusterFlushSlots() => Call<string>("CLUSTER".SubCommand("FLUSHSLOTS"), rt => rt.ThrowOrValue());
		public long ClusterForget(string nodeid) => Call<long>("CLUSTER".SubCommand("FORGET").InputRaw(nodeid), rt => rt.ThrowOrValue());
		public string[] ClusterGetKeysInSlot(int slot) => Call<string[]>("CLUSTER".SubCommand("GETKEYSINSLOT").InputRaw(slot), rt => rt.ThrowOrValue());
		public Dictionary<string, string> ClusterInfo() => Call<string[], Dictionary<string, string>>("CLUSTER".SubCommand("INFO"), rt => rt.NewValue(a => a.MapToHash<string>(rt.Encoding)).ThrowOrValue());
		public int ClusterKeySlot(string key) => Call<int>("CLUSTER".SubCommand("KEYSLOT").InputRaw(key), rt => rt.ThrowOrValue());
		public string ClusterMeet(string ip, int port) => Call<string>("CLUSTER".SubCommand("MEET").Input(ip, port), rt => rt.ThrowOrValue());
		public string ClusterMyId() => Call<string>("CLUSTER".SubCommand("MYID"), rt => rt.ThrowOrValue());
		public string ClusterNodes() => Call<string>("CLUSTER".SubCommand("NODES"), rt => rt.ThrowOrValue());
		public string ClusterReplicas(string nodeid) => Call<string>("CLUSTER".SubCommand("REPLICAS").InputRaw(nodeid), rt => rt.ThrowOrValue());
		public string ClusterReplicate(string nodeid) => Call<string>("CLUSTER".SubCommand("REPLICATE").InputRaw(nodeid), rt => rt.ThrowOrValue());
		public string ClusterReset(ClusterResetType type) => Call<string>("CLUSTER".SubCommand("RESET").InputRaw(type), rt => rt.ThrowOrValue());
		public string ClusterSaveConfig() => Call<string>("CLUSTER".SubCommand("SAVECONFIG"), rt => rt.ThrowOrValue());
		public string ClusterSetConfigEpoch(string epoch) => Call<string>("CLUSTER".SubCommand("SET-CONFIG-EPOCH").InputRaw(epoch), rt => rt.ThrowOrValue());
		public string[] ClusterSetSlot(int slot, ClusterSetSlotType type, string nodeid = null) => Call<string[]>("CLUSTER".SubCommand("SETSLOT")
			.Input(slot, type)
			.InputIf(!string.IsNullOrWhiteSpace(nodeid), nodeid), rt => rt.ThrowOrValue());
		public string ClusterSlaves(string nodeid) => Call<string>("CLUSTER".SubCommand("SLAVES").InputRaw(nodeid), rt => rt.ThrowOrValue());
		public object ClusterSlots() => Call<object>("CLUSTER".SubCommand("SLOTS"), rt => rt.ThrowOrValue());
		public string ReadOnly() => Call<string>("READONLY", rt => rt.ThrowOrValue());
		public string ReadWrite() => Call<string>("READWRITE", rt => rt.ThrowOrValue());
    }
}
