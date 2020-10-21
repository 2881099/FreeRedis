using System;
using System.Collections.Generic;

namespace FreeRedis
{
    partial class RedisClient
	{
        public TransactionHook Multi()
        {
            CheckUseTypeOrThrow(UseType.Pooling, UseType.Cluster, UseType.Sentinel, UseType.SingleInside);
            return new TransactionHook(this);
        }
        public class TransactionHook : RedisClient
        {
            internal TransactionHook(RedisClient cli) : base(new TransactionAdapter(cli))
            {
                this.Serialize = cli.Serialize;
                this.Deserialize = cli.Deserialize;
            }
            public void Discard() => (_adapter as TransactionAdapter).Discard();
            public object[] Exec() => (_adapter as TransactionAdapter).Exec();
            public void UnWatch() => (_adapter as TransactionAdapter).UnWatch();
            public void Watch(params string[] keys) => (_adapter as TransactionAdapter).Watch(keys);
        }



        // Pipeline
        public PipelineHook StartPipe()
        {
            CheckUseTypeOrThrow(UseType.Pooling, UseType.Cluster, UseType.Sentinel, UseType.SingleInside);
            return new PipelineHook(this);
        }
        public class PipelineHook : RedisClient
        {
            internal PipelineHook(RedisClient cli) : base(new PipelineAdapter(cli))
            {
                this.Serialize = cli.Serialize;
                this.Deserialize = cli.Deserialize;
            }
            public object[] EndPipe() => (_adapter as PipelineAdapter).EndPipe();
        }
    }
}
