using System;
using System.Collections.Generic;

namespace FreeRedis
{
    partial class RedisClient
	{
        public TransactionHook Multi()
        {
            CheckUseTypeOrThrow(UseType.Pooling, UseType.Cluster, UseType.Sentinel, UseType.SingleInside, UseType.SingleTemp);
            return new TransactionHook(new TransactionAdapter(Adapter.TopOwner));
        }
        public class TransactionHook : RedisClient
        {
            internal TransactionHook(TransactionAdapter adapter) : base(adapter) { }
            public void Discard() => (Adapter as TransactionAdapter).Discard();
            public object[] Exec() => (Adapter as TransactionAdapter).Exec();
            public void UnWatch() => (Adapter as TransactionAdapter).UnWatch();
            public void Watch(params string[] keys) => (Adapter as TransactionAdapter).Watch(keys);

            ~TransactionHook()
            {
                (Adapter as TransactionAdapter).Dispose();
            }
        }



        // Pipeline
        public PipelineHook StartPipe()
        {
            CheckUseTypeOrThrow(UseType.Pooling, UseType.Cluster, UseType.Sentinel, UseType.SingleInside, UseType.SingleTemp);
            return new PipelineHook(new PipelineAdapter(Adapter.TopOwner));
        }
        public class PipelineHook : RedisClient
        {
            internal PipelineHook(PipelineAdapter adapter) : base(adapter) { }
            public object[] EndPipe() => (Adapter as PipelineAdapter).EndPipe();

            ~PipelineHook()
            {
                (Adapter as PipelineAdapter).Dispose();
            }
        }



        // GetShareClient
        public ShareClientHook GetShareClient()
        {
            CheckUseTypeOrThrow(UseType.Pooling, UseType.Sentinel, UseType.SingleInside);
            var rds = Adapter.GetRedisSocket(null);
            return new ShareClientHook(new SingleTempAdapter(Adapter.TopOwner, rds, () => rds.Dispose()));
        }
        public class ShareClientHook: RedisClient
        {
            internal ShareClientHook(BaseAdapter adapter) : base(adapter) { }

            ~ShareClientHook()
            {
                (Adapter as SingleTempAdapter).Dispose();
            }
        }
    }
}
