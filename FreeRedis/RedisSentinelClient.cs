using FreeRedis.Internal;
using FreeRedis.Model;
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
		public SentinelInfoResult Info() => Call<string, SentinelInfoResult>("INFO", rt => rt.NewValue(a => new SentinelInfoResult(a)).ThrowOrValue());
		public SentinelRoleResult Role() => Call<object, SentinelRoleResult>("ROLE", rt => rt.NewValue(a =>
		{
			var objs = a as List<object>;
			if (objs.Any()) return new SentinelRoleResult { Role = objs[0].ConvertTo<SentinelRoleType>(), Masters = objs[1].ConvertTo<string[]>() };
			return null;
		}).ThrowOrValue());

		public SentinelMasterResult[] SentinelMasters() => Call<object, SentinelMasterResult[]>("SENTINEL".SubCommand("MASTERS"), rt => rt.NewValue (a =>
		{
			var objs = a as List<object>;
			return objs.Select(x => x.ConvertTo<string[]>().MapToClass<SentinelMasterResult>(rt.Encoding)).ToArray();
		}).ThrowOrValue());
		public SentinelMasterResult SentinelMaster(string masterName) => Call<string[], SentinelMasterResult>("SENTINEL".SubCommand("MASTER").InputRaw(masterName), rt => rt.NewValue(a => a.MapToClass<SentinelMasterResult>(rt.Encoding)).ThrowOrValue());
		public SentinelSalveResult[] SentinelSalves(string masterName) => Call<object, SentinelSalveResult[]>("SENTINEL".SubCommand("SLAVES").InputRaw(masterName), rt => rt.NewValue(a =>
		{
			var objs = a as List<object>;
			return objs.Select(x => x.ConvertTo<string[]>().MapToClass<SentinelSalveResult>(rt.Encoding)).ToArray();
		}).ThrowOrValue());
		public SentinelSentinelResult[] SentinelSentinels(string masterName) => Call<object, SentinelSentinelResult[]>("SENTINEL".SubCommand("SENTINELS").InputRaw(masterName), rt => rt.NewValue(a =>
		{
			var objs = a as List<object>;
			return objs.Select(x => x.ConvertTo<string[]>().MapToClass<SentinelSentinelResult>(rt.Encoding)).ToArray();
		}).ThrowOrValue());
		public string SentinelGetMasterAddrByName(string masterName) => Call<string[], string>("SENTINEL".SubCommand("GET-MASTER-ADDR-BY-NAME").InputRaw(masterName), rt => rt.NewValue(a => $"{a[0]}:{a[1]}").ThrowOrValue());
		public SentinelIsMaterDownByAddrResult SentinelIsMasterDownByAddr(string ip, int port, long currentEpoch, string runid) => Call<string[], SentinelIsMaterDownByAddrResult>("SENTINEL".SubCommand("IS-MASTER-DOWN-BY-ADDR").Input(ip, port, currentEpoch, runid), 
			rt => rt.NewValue(a => new SentinelIsMaterDownByAddrResult { down_state = a[0].ConvertTo<bool>(), leader = a[1], vote_epoch = a[1].ConvertTo<long>() }).ThrowOrValue());

		public long SentinelReset(string pattern) => Call<long>("SENTINEL".SubCommand("RESET").InputRaw(pattern), rt => rt.ThrowOrValue());
		public void SentinelFailover(string masterName) => Call<object>("SENTINEL".SubCommand("FAILOVER").InputRaw(masterName), rt => rt.ThrowOrValue());



		public object SentinelPendingScripts() => Call<object>("SENTINEL".SubCommand("PENDING-SCRIPTS"), rt => rt.ThrowOrValue());
		public object SentinelMonitor(string name, string ip, int port, int quorum) => Call<object>("SENTINEL".SubCommand("MONITOR").Input(name, ip, port, quorum), rt => rt.ThrowOrValue());



		public void SentinelFlushConfig() => Call<object>("SENTINEL".SubCommand("FLUSHCONFIG"), rt => rt.ThrowOrValue());
		public void SentinelRemove(string masterName) => Call<object>("SENTINEL".SubCommand("REMOVE").InputRaw(masterName), rt => rt.ThrowOrValue());
		public string SentinelCkQuorum(string masterName) => Call<string>("SENTINEL".SubCommand("CKQUORUM").InputRaw(masterName), rt => rt.ThrowOrValue());
		public void SentinelSet(string masterName, string option, string value) => Call<object>("SENTINEL".SubCommand("SET").Input(masterName, option, value), rt => rt.ThrowOrValue());



		public object SentinelInfoCache(string masterName) => Call<object>("SENTINEL".SubCommand("INFO-CACHE").InputRaw(masterName), rt => rt.ThrowOrValue());
		public void SentinelSimulateFailure(bool crashAfterElection, bool crashAfterPromotion) => Call<object>("SENTINEL"
			.SubCommand("SIMULATE-FAILURE")
			.InputIf(crashAfterElection, "crash-after-election")
			.InputIf(crashAfterPromotion, "crash-after-promotion"), rt => rt.ThrowOrValue());
	}
}
