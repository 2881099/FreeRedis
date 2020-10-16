using System;
using System.Collections.Generic;
using System.Text;

namespace FreeRedis.Tests
{
    class AutoMake
    {

        //admin, noscript, loading, stale, skip_slowlog
        //@admin, @slow, @dangerous
        public void Acl(string arg1) { }


        //write, denyoom, fast
        //@write, @string, @fast
        public void Append(string key, string arg2) { }


        //fast
        //@keyspace, @fast
        public void Asking() { }


        //noscript, loading, stale, skip_monitor, skip_slowlog, fast, no_auth
        //@fast, @connection
        public void Auth(string arg1) { }


        //admin, noscript
        //@admin, @slow, @dangerous
        public void Bgrewriteaof() { }


        //admin, noscript
        //@admin, @slow, @dangerous
        public void Bgsave() { }


        //readonly
        //@read, @bitmap, @slow
        public void Bitcount(string key) { }


        //write, denyoom
        //@write, @bitmap, @slow
        public void Bitfield(string key) { }


        //readonly, fast
        //@read, @bitmap, @fast
        public void Bitfield_ro(string key) { }


        //write, denyoom
        //@write, @bitmap, @slow
        public void Bitop(string[] arg1, string[] keys, string[] arg3) { }


        //readonly
        //@read, @bitmap, @slow
        public void Bitpos(string key, string arg2) { }


        //write, noscript
        //@write, @list, @slow, @blocking
        public void Blpop(string[] keys, string[] arg2) { }


        //write, noscript
        //@write, @list, @slow, @blocking
        public void Brpop(string[] keys, string[] arg2) { }


        //write, denyoom, noscript
        //@write, @list, @slow, @blocking
        public void Brpoplpush(string key, string arg2, string arg3) { }


        //write, noscript, fast
        //@write, @sortedset, @fast, @blocking
        public void Bzpopmax(string[] keys, string[] arg2) { }


        //write, noscript, fast
        //@write, @sortedset, @fast, @blocking
        public void Bzpopmin(string[] keys, string[] arg2) { }


        //admin, noscript, random, loading, stale
        //@admin, @slow, @dangerous, @connection
        public void Client(string arg1) { }


        //admin, random, stale
        //@admin, @slow, @dangerous
        public void Cluster(string arg1) { }


        //random, loading, stale
        //@slow, @connection
        public void Command() { }


        //admin, noscript, loading, stale
        //@admin, @slow, @dangerous
        public void Config(string arg1) { }


        //readonly, fast
        //@keyspace, @read, @fast
        public void Dbsize() { }


        //admin, noscript, loading, stale
        //@admin, @slow, @dangerous
        public void Debug(string arg1) { }


        //write, denyoom, fast
        //@write, @string, @fast
        public void Decr(string key) { }


        //write, denyoom, fast
        //@write, @string, @fast
        public void Decrby(string key, string arg2) { }


        //write
        //@keyspace, @write, @slow
        public void Del(string[] keys) { }


        //noscript, loading, stale, fast
        //@fast, @transaction
        public void Discard() { }


        //readonly, random
        //@keyspace, @read, @slow
        public void Dump(string key) { }


        //readonly, fast
        //@read, @fast, @connection
        public void Echo(string arg1) { }


        //noscript, movablekeys
        //@slow, @scripting
        public void Eval(string arg1, string arg2) { }


        //noscript, movablekeys
        //@slow, @scripting
        public void Evalsha(string arg1, string arg2) { }


        //noscript, loading, stale, skip_monitor, skip_slowlog
        //@slow, @transaction
        public void Exec() { }


        //readonly, fast
        //@keyspace, @read, @fast
        public void Exists(string[] keys) { }


        //write, fast
        //@keyspace, @write, @fast
        public void Expire(string key, string arg2) { }


        //write, fast
        //@keyspace, @write, @fast
        public void Expireat(string key, string arg2) { }


        //write
        //@keyspace, @write, @slow, @dangerous
        public void Flushall() { }


        //write
        //@keyspace, @write, @slow, @dangerous
        public void Flushdb() { }


        //write, denyoom
        //@write, @geo, @slow
        public void Geoadd(string key, string arg2, string arg3, string arg4) { }


        //readonly
        //@read, @geo, @slow
        public void Geodist(string key, string arg2, string arg3) { }


        //readonly
        //@read, @geo, @slow
        public void Geohash(string key) { }


        //readonly
        //@read, @geo, @slow
        public void Geopos(string key) { }


        //write, movablekeys
        //@write, @geo, @slow
        public void Georadius(string key, string arg2, string arg3, string arg4, string arg5) { }


        //readonly, movablekeys
        //@read, @geo, @slow
        public void Georadius_ro(string key, string arg2, string arg3, string arg4, string arg5) { }


        //write, movablekeys
        //@write, @geo, @slow
        public void Georadiusbymember(string key, string arg2, string arg3, string arg4) { }


        //readonly, movablekeys
        //@read, @geo, @slow
        public void Georadiusbymember_ro(string key, string arg2, string arg3, string arg4) { }


        //readonly, fast
        //@read, @string, @fast
        public void Get(string key) { }


        //readonly, fast
        //@read, @bitmap, @fast
        public void Getbit(string key, string arg2) { }


        //readonly
        //@read, @string, @slow
        public void Getrange(string key, string arg2, string arg3) { }


        //write, denyoom, fast
        //@write, @string, @fast
        public void Getset(string key, string arg2) { }


        //write, fast
        //@write, @hash, @fast
        public void Hdel(string key, string arg2) { }


        //noscript, loading, stale, skip_monitor, skip_slowlog, fast, no_auth
        //@fast, @connection
        public void Hello(string arg1) { }


        //readonly, fast
        //@read, @hash, @fast
        public void Hexists(string key, string arg2) { }


        //readonly, fast
        //@read, @hash, @fast
        public void Hget(string key, string arg2) { }


        //readonly, random
        //@read, @hash, @slow
        public void Hgetall(string key) { }


        //write, denyoom, fast
        //@write, @hash, @fast
        public void Hincrby(string key, string arg2, string arg3) { }


        //write, denyoom, fast
        //@write, @hash, @fast
        public void Hincrbyfloat(string key, string arg2, string arg3) { }


        //readonly, sort_for_script
        //@read, @hash, @slow
        public void Hkeys(string key) { }


        //readonly, fast
        //@read, @hash, @fast
        public void Hlen(string key) { }


        //readonly, fast
        //@read, @hash, @fast
        public void Hmget(string key, string arg2) { }


        //write, denyoom, fast
        //@write, @hash, @fast
        public void Hmset(string key, string arg2, string arg3) { }


        //readonly, loading, stale
        //@read, @slow
        //public void Host:() { }


        //readonly, random
        //@read, @hash, @slow
        public void Hscan(string key, string arg2) { }


        //write, denyoom, fast
        //@write, @hash, @fast
        public void Hset(string key, string arg2, string arg3) { }


        //write, denyoom, fast
        //@write, @hash, @fast
        public void Hsetnx(string key, string arg2, string arg3) { }


        //readonly, fast
        //@read, @hash, @fast
        public void Hstrlen(string key, string arg2) { }


        //readonly, sort_for_script
        //@read, @hash, @slow
        public void Hvals(string key) { }


        //write, denyoom, fast
        //@write, @string, @fast
        public void Incr(string key) { }


        //write, denyoom, fast
        //@write, @string, @fast
        public void Incrby(string key, string arg2) { }


        //write, denyoom, fast
        //@write, @string, @fast
        public void Incrbyfloat(string key, string arg2) { }


        //random, loading, stale
        //@slow, @dangerous
        public void Info() { }


        //readonly, sort_for_script
        //@keyspace, @read, @slow, @dangerous
        public void Keys(string arg1) { }


        //readonly, random, loading, stale, fast
        //@read, @admin, @fast, @dangerous
        public void Lastsave() { }


        //admin, noscript, loading, stale
        //@admin, @slow, @dangerous
        public void Latency(string arg1) { }


        //readonly
        //@read, @list, @slow
        public void Lindex(string key, string arg2) { }


        //write, denyoom
        //@write, @list, @slow
        public void Linsert(string key, string arg2, string arg3, string arg4) { }


        //readonly, fast
        //@read, @list, @fast
        public void Llen(string key) { }


        //readonly, fast
        //@read, @fast
        public void Lolwut() { }


        //write, fast
        //@write, @list, @fast
        public void Lpop(string key) { }


        //write, denyoom, fast
        //@write, @list, @fast
        public void Lpush(string key, string arg2) { }


        //write, denyoom, fast
        //@write, @list, @fast
        public void Lpushx(string key, string arg2) { }


        //readonly
        //@read, @list, @slow
        public void Lrange(string key, string arg2, string arg3) { }


        //write
        //@write, @list, @slow
        public void Lrem(string key, string arg2, string arg3) { }


        //write, denyoom
        //@write, @list, @slow
        public void Lset(string key, string arg2, string arg3) { }


        //write
        //@write, @list, @slow
        public void Ltrim(string key, string arg2, string arg3) { }


        //readonly, random, movablekeys
        //@read, @slow
        public void Memory(string arg1) { }


        //readonly, fast
        //@read, @string, @fast
        public void Mget(string[] keys) { }


        //write, random, movablekeys
        //@keyspace, @write, @slow, @dangerous
        public void Migrate(string arg1, string arg2, string arg3, string arg4, string arg5) { }


        //admin, noscript
        //@admin, @slow, @dangerous
        public void Module(string arg1) { }


        //admin, noscript, loading, stale
        //@admin, @slow, @dangerous
        public void Monitor() { }


        //write, fast
        //@keyspace, @write, @fast
        public void Move(string key, string arg2) { }


        //write, denyoom
        //@write, @string, @slow
        public void Mset(string[] keys, string[] arg2) { }


        //write, denyoom
        //@write, @string, @slow
        public void Msetnx(string[] keys, string[] arg2) { }


        //noscript, loading, stale, fast
        //@fast, @transaction
        public void Multi() { }


        //readonly, random
        //@keyspace, @read, @slow
        public void Object(string arg1) { }


        //write, fast
        //@keyspace, @write, @fast
        public void Persist(string key) { }


        //write, fast
        //@keyspace, @write, @fast
        public void Pexpire(string key, string arg2) { }


        //write, fast
        //@keyspace, @write, @fast
        public void Pexpireat(string key, string arg2) { }


        //write, denyoom, fast
        //@write, @hyperloglog, @fast
        public void Pfadd(string key) { }


        //readonly
        //@read, @hyperloglog, @slow
        public void Pfcount(string[] keys) { }


        //write, admin
        //@write, @admin, @slow, @dangerous
        public void Pfdebug(string arg1, string arg2) { }


        //write, denyoom
        //@write, @hyperloglog, @slow
        public void Pfmerge(string[] keys) { }


        //admin
        //@hyperloglog, @admin, @slow, @dangerous
        public void Pfselftest() { }


        //stale, fast
        //@fast, @connection
        public void Ping() { }


        //readonly, loading, stale
        //@read, @slow
        public void Post() { }


        //write, denyoom
        //@write, @string, @slow
        public void Psetex(string key, string arg2, string arg3) { }


        //pubsub, noscript, loading, stale
        //@pubsub, @slow
        public void Psubscribe(string arg1) { }


        //admin, noscript
        //@admin, @slow, @dangerous
        public void Psync(string arg1, string arg2) { }


        //readonly, random, fast
        //@keyspace, @read, @fast
        public void Pttl(string key) { }


        //pubsub, loading, stale, fast
        //@pubsub, @fast
        public void Publish(string arg1, string arg2) { }


        //pubsub, random, loading, stale
        //@pubsub, @slow
        public void Pubsub(string arg1) { }


        //pubsub, noscript, loading, stale
        //@pubsub, @slow
        public void Punsubscribe() { }


        //readonly, random
        //@keyspace, @read, @slow
        public void Randomkey() { }


        //fast
        //@keyspace, @fast
        public void Readonly() { }


        //fast
        //@keyspace, @fast
        public void Readwrite() { }


        //write
        //@keyspace, @write, @slow
        public void Rename(string key, string arg2) { }


        //write, fast
        //@keyspace, @write, @fast
        public void Renamenx(string key, string arg2) { }


        //admin, noscript, loading, stale
        //@admin, @slow, @dangerous
        public void Replconf() { }


        //admin, noscript, stale
        //@admin, @slow, @dangerous
        public void Replicaof(string arg1, string arg2) { }


        //write, denyoom
        //@keyspace, @write, @slow, @dangerous
        public void Restore(string key, string arg2, string arg3) { }


        //write, denyoom, asking
        //@keyspace, @write, @slow, @dangerous
        //public void Restore-asking(string key, string arg2, string arg3) { }


        //readonly, noscript, loading, stale, fast
        //@read, @fast, @dangerous
        public void Role() { }


        //write, fast
        //@write, @list, @fast
        public void Rpop(string key) { }


        //write, denyoom
        //@write, @list, @slow
        public void Rpoplpush(string key, string arg2) { }


        //write, denyoom, fast
        //@write, @list, @fast
        public void Rpush(string key, string arg2) { }


        //write, denyoom, fast
        //@write, @list, @fast
        public void Rpushx(string key, string arg2) { }


        //write, denyoom, fast
        //@write, @set, @fast
        public void Sadd(string key, string arg2) { }


        //admin, noscript
        //@admin, @slow, @dangerous
        public void Save() { }


        //readonly, random
        //@keyspace, @read, @slow
        public void Scan(string arg1) { }


        //readonly, fast
        //@read, @set, @fast
        public void Scard(string key) { }


        //noscript
        //@slow, @scripting
        public void Script(string arg1) { }


        //readonly, sort_for_script
        //@read, @set, @slow
        public void Sdiff(string[] keys) { }


        //write, denyoom
        //@write, @set, @slow
        public void Sdiffstore(string[] keys, string[] arg2) { }


        //loading, stale, fast
        //@keyspace, @fast
        public void Select(string arg1) { }


        //write, denyoom
        //@write, @string, @slow
        public void Set(string key, string arg2) { }


        //write, denyoom
        //@write, @bitmap, @slow
        public void Setbit(string key, string arg2, string arg3) { }


        //write, denyoom
        //@write, @string, @slow
        public void Setex(string key, string arg2, string arg3) { }


        //write, denyoom, fast
        //@write, @string, @fast
        public void Setnx(string key, string arg2) { }


        //write, denyoom
        //@write, @string, @slow
        public void Setrange(string key, string arg2, string arg3) { }


        //admin, noscript, loading, stale
        //@admin, @slow, @dangerous
        public void Shutdown() { }


        //readonly, sort_for_script
        //@read, @set, @slow
        public void Sinter(string[] keys) { }


        //write, denyoom
        //@write, @set, @slow
        public void Sinterstore(string[] keys, string[] arg2) { }


        //readonly, fast
        //@read, @set, @fast
        public void Sismember(string key, string arg2) { }


        //admin, noscript, stale
        //@admin, @slow, @dangerous
        public void Slaveof(string arg1, string arg2) { }


        //admin, random, loading, stale
        //@admin, @slow, @dangerous
        public void Slowlog(string arg1) { }


        //readonly, sort_for_script
        //@read, @set, @slow
        public void Smembers(string key) { }


        //write, fast
        //@write, @set, @fast
        public void Smove(string key, string arg2, string arg3) { }


        //write, denyoom, movablekeys
        //@write, @set, @sortedset, @list, @slow, @dangerous
        public void Sort(string key) { }


        //write, random, fast
        //@write, @set, @fast
        public void Spop(string key) { }


        //readonly, random
        //@read, @set, @slow
        public void Srandmember(string key) { }


        //write, fast
        //@write, @set, @fast
        public void Srem(string key, string arg2) { }


        //readonly, random
        //@read, @set, @slow
        public void Sscan(string key, string arg2) { }


        //readonly, movablekeys
        //@read, @string, @slow
        public void Stralgo(string arg1) { }


        //readonly, fast
        //@read, @string, @fast
        public void Strlen(string key) { }


        //pubsub, noscript, loading, stale
        //@pubsub, @slow
        public void Subscribe(string arg1) { }


        //readonly
        //@read, @string, @slow
        public void Substr(string key, string arg2, string arg3) { }


        //readonly, sort_for_script
        //@read, @set, @slow
        public void Sunion(string[] keys) { }


        //write, denyoom
        //@write, @set, @slow
        public void Sunionstore(string[] keys, string[] arg2) { }


        //write, fast
        //@keyspace, @write, @fast, @dangerous
        public void Swapdb(string arg1, string arg2) { }


        //admin, noscript
        //@admin, @slow, @dangerous
        public void Sync() { }


        //readonly, random, loading, stale, fast
        //@read, @fast
        public void Time() { }


        //readonly, fast
        //@keyspace, @read, @fast
        public void Touch(string[] keys) { }


        //readonly, random, fast
        //@keyspace, @read, @fast
        public void Ttl(string key) { }


        //readonly, fast
        //@keyspace, @read, @fast
        public void Type(string key) { }


        //write, fast
        //@keyspace, @write, @fast
        public void Unlink(string[] keys) { }


        //pubsub, noscript, loading, stale
        //@pubsub, @slow
        public void Unsubscribe() { }


        //noscript, fast
        //@fast, @transaction
        public void Unwatch() { }


        //noscript
        //@keyspace, @slow
        public void Wait(string arg1, string arg2) { }


        //noscript, fast
        //@fast, @transaction
        public void Watch(string[] keys) { }


        //write, random, fast
        //@write, @stream, @fast
        public void Xack(string key, string arg2, string arg3) { }


        //write, denyoom, random, fast
        //@write, @stream, @fast
        public void Xadd(string key, string arg2, string arg3, string arg4) { }


        //write, random, fast
        //@write, @stream, @fast
        public void Xclaim(string key, string arg2, string arg3, string arg4, string arg5) { }


        //write, fast
        //@write, @stream, @fast
        public void Xdel(string key, string arg2) { }


        //write, denyoom
        //@write, @stream, @slow
        public void Xgroup(string arg1) { }


        //readonly, random
        //@read, @stream, @slow
        public void Xinfo(string arg1) { }


        //readonly, fast
        //@read, @stream, @fast
        public void Xlen(string key) { }


        //readonly, random
        //@read, @stream, @slow
        public void Xpending(string key, string arg2) { }


        //readonly
        //@read, @stream, @slow
        public void Xrange(string key, string arg2, string arg3) { }


        //readonly, movablekeys
        //@read, @stream, @slow, @blocking
        public void Xread(string key, string arg2, string arg3) { }


        //write, movablekeys
        //@write, @stream, @slow, @blocking
        public void Xreadgroup(string key, string arg2, string arg3, string arg4, string arg5, string arg6) { }


        //readonly
        //@read, @stream, @slow
        public void Xrevrange(string key, string arg2, string arg3) { }


        //write, denyoom, fast
        //@write, @stream, @fast
        public void Xsetid(string key, string arg2) { }


        //write, random
        //@write, @stream, @slow
        public void Xtrim(string key) { }


        //write, denyoom, fast
        //@write, @sortedset, @fast
        public void Zadd(string key, string arg2, string arg3) { }


        //readonly, fast
        //@read, @sortedset, @fast
        public void Zcard(string key) { }


        //readonly, fast
        //@read, @sortedset, @fast
        public void Zcount(string key, string arg2, string arg3) { }


        //write, denyoom, fast
        //@write, @sortedset, @fast
        public void Zincrby(string key, string arg2, string arg3) { }


        //write, denyoom, movablekeys
        //@write, @sortedset, @slow
        public void Zinterstore(string arg1, string arg2, string arg3) { }


        //readonly, fast
        //@read, @sortedset, @fast
        public void Zlexcount(string key, string arg2, string arg3) { }


        //write, fast
        //@write, @sortedset, @fast
        public void Zpopmax(string key) { }


        //write, fast
        //@write, @sortedset, @fast
        public void Zpopmin(string key) { }


        //readonly
        //@read, @sortedset, @slow
        public void Zrange(string key, string arg2, string arg3) { }


        //readonly
        //@read, @sortedset, @slow
        public void Zrangebylex(string key, string arg2, string arg3) { }


        //readonly
        //@read, @sortedset, @slow
        public void Zrangebyscore(string key, string arg2, string arg3) { }


        //readonly, fast
        //@read, @sortedset, @fast
        public void Zrank(string key, string arg2) { }


        //write, fast
        //@write, @sortedset, @fast
        public void Zrem(string key, string arg2) { }


        //write
        //@write, @sortedset, @slow
        public void Zremrangebylex(string key, string arg2, string arg3) { }


        //write
        //@write, @sortedset, @slow
        public void Zremrangebyrank(string key, string arg2, string arg3) { }


        //write
        //@write, @sortedset, @slow
        public void Zremrangebyscore(string key, string arg2, string arg3) { }


        //readonly
        //@read, @sortedset, @slow
        public void Zrevrange(string key, string arg2, string arg3) { }


        //readonly
        //@read, @sortedset, @slow
        public void Zrevrangebylex(string key, string arg2, string arg3) { }


        //readonly
        //@read, @sortedset, @slow
        public void Zrevrangebyscore(string key, string arg2, string arg3) { }


        //readonly, fast
        //@read, @sortedset, @fast
        public void Zrevrank(string key, string arg2) { }


        //readonly, random
        //@read, @sortedset, @slow
        public void Zscan(string key, string arg2) { }


        //readonly, fast
        //@read, @sortedset, @fast
        public void Zscore(string key, string arg2) { }


        //write, denyoom, movablekeys
        //@write, @sortedset, @slow
        public void Zunionstore(string arg1, string arg2, string arg3) { }
    }
}
