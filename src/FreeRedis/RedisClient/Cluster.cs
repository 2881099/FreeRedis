using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
    {
        //wait testing

        //public string ClusterAddSlots(params int[] slot) => Call("CLUSTER".SubCommand("ADDSLOTS").Input(slot), rt => rt.ThrowOrValue<string>());
        //public string ClusterBumpEpoch() => Call("CLUSTER".SubCommand("BUMPEPOCH"), rt => rt.ThrowOrValue<string>());
        //public long ClusterCountFailureReports(string nodeid) => Call("CLUSTER".SubCommand("COUNT-FAILURE-REPORTS").InputRaw(nodeid), rt => rt.ThrowOrValue<long>());

        //public long ClusterCountKeysInSlot(int slot) => Call("CLUSTER".SubCommand("COUNTKEYSINSLOT").InputRaw(slot), rt => rt.ThrowOrValue<long>());
        //public string ClusterDelSlots(params int[] slot) => Call("CLUSTER".SubCommand("DELSLOTS").Input(slot), rt => rt.ThrowOrValue<string>());
        //public string ClusterFailOver(ClusterFailOverType type) => Call("CLUSTER".SubCommand("FAILOVER").InputRaw(type), rt => rt.ThrowOrValue<string>());
        //public string ClusterFlushSlots() => Call("CLUSTER".SubCommand("FLUSHSLOTS"), rt => rt.ThrowOrValue<string>());

        //public long ClusterForget(string nodeid) => Call("CLUSTER".SubCommand("FORGET").InputRaw(nodeid), rt => rt.ThrowOrValue<long>());
        //public string[] ClusterGetKeysInSlot(int slot) => Call("CLUSTER".SubCommand("GETKEYSINSLOT").InputRaw(slot), rt => rt.ThrowOrValue<string[]>());
        //public Dictionary<string, string> ClusterInfo() => Call("CLUSTER".SubCommand("INFO"), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string>(rt.Encoding)));

        //public int ClusterKeySlot(string key) => Call("CLUSTER".SubCommand("KEYSLOT").InputRaw(key), rt => rt.ThrowOrValue<int>());
        //public string ClusterMeet(string ip, int port) => Call("CLUSTER".SubCommand("MEET").Input(ip, port), rt => rt.ThrowOrValue<string>());
        //public string ClusterMyId() => Call("CLUSTER".SubCommand("MYID"), rt => rt.ThrowOrValue<string>());
        //public string ClusterNodes() => Call("CLUSTER".SubCommand("NODES"), rt => rt.ThrowOrValue<string>());

        //public string ClusterReplicas(string nodeid) => Call("CLUSTER".SubCommand("REPLICAS").InputRaw(nodeid), rt => rt.ThrowOrValue<string>());
        //public string ClusterReplicate(string nodeid) => Call("CLUSTER".SubCommand("REPLICATE").InputRaw(nodeid), rt => rt.ThrowOrValue<string>());
        //public string ClusterReset(ClusterResetType type) => Call("CLUSTER".SubCommand("RESET").InputRaw(type), rt => rt.ThrowOrValue<string>());
        //public string ClusterSaveConfig() => Call("CLUSTER".SubCommand("SAVECONFIG"), rt => rt.ThrowOrValue<string>());

        //public string ClusterSetConfigEpoch(string epoch) => Call("CLUSTER".SubCommand("SET-CONFIG-EPOCH").InputRaw(epoch), rt => rt.ThrowOrValue<string>());
        //public string[] ClusterSetSlot(int slot, ClusterSetSlotType type, string nodeid = null) => Call("CLUSTER".SubCommand("SETSLOT")
        //    .Input(slot, type)
        //    .InputIf(!string.IsNullOrWhiteSpace(nodeid), nodeid), rt => rt.ThrowOrValue<string[]>());

        //public string ClusterSlaves(string nodeid) => Call("CLUSTER".SubCommand("SLAVES").InputRaw(nodeid), rt => rt.ThrowOrValue<string>());
        //public object ClusterSlots() => Call("CLUSTER".SubCommand("SLOTS"), rt => rt.ThrowOrValue<object>());
        //public string ReadOnly() => Call("READONLY", rt => rt.ThrowOrValue<string>());
        //public string ReadWrite() => Call("READWRITE", rt => rt.ThrowOrValue<string>());
    }
}
