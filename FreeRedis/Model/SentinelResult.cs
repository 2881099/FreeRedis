using System;
using System.Collections.Generic;
using System.Text;

namespace FreeRedis.Model.Sentinel
{
    public class RoleResult
    {
        public static implicit operator RoleResult(Model.RoleResult rt) => new RoleResult { role = rt.role, masters = rt.data as string[] };

        public RoleType role;
        public string[] masters;
    }

    //# Server
    //redis_version:3.2.100
    //redis_git_sha1:00000000
    //redis_git_dirty:0
    //redis_build_id:dd26f1f93c5130ee
    //redis_mode:sentinel
    //os:Windows  
    //arch_bits:64
    //multiplexing_api:WinSock_IOCP
    //process_id:30752
    //run_id:edea67f47ce8089d551b296b8e9cfd7ba6d0b955
    //tcp_port:21479
    //uptime_in_seconds:1305
    //uptime_in_days:0
    //hz:19
    //lru_clock:9191517
    //executable:C:\Users\28810\Desktop\Redis-x64-3.2.100\6384\redis-server.exe
    //config_file:C:\Users\28810\Desktop\Redis-x64-3.2.100\6384\sentinel21479.conf

    //# Clients
    //connected_clients:5
    //client_longest_output_list:0
    //client_biggest_input_buf:0
    //blocked_clients:0

    //# CPU
    //used_cpu_sys:0.19
    //used_cpu_user:0.22
    //used_cpu_sys_children:0.00
    //used_cpu_user_children:0.00

    //# Stats
    //total_connections_received:7
    //total_commands_processed:5671
    //instantaneous_ops_per_sec:2
    //total_net_input_bytes:312817
    //total_net_output_bytes:36918
    //instantaneous_input_kbps:0.12
    //instantaneous_output_kbps:0.02
    //rejected_connections:0
    //sync_full:0
    //sync_partial_ok:0
    //sync_partial_err:0
    //expired_keys:0
    //evicted_keys:0
    //keyspace_hits:0
    //keyspace_misses:0
    //pubsub_channels:0
    //pubsub_patterns:0
    //latest_fork_usec:0
    //migrate_cached_sockets:0

    //# Sentinel
    //sentinel_masters:1
    //sentinel_tilt:0
    //sentinel_running_scripts:0
    //sentinel_scripts_queue_length:0
    //sentinel_simulate_failure_flags:0
    //master0:name=mymaster,status=ok,address=127.0.0.1:6381,slaves=2,sentinels=4
    public class InfoResult
    {
        public string text;
        public InfoResult(string text)
        {
            this.text = text;
        }
    }

    // 1) "name"
    // 2) "mymaster"
    // 3) "ip"
    // 4) "127.0.0.1"
    // 5) "port"
    // 6) "6381"
    // 7) "runid"
    // 8) "380dc0424db52c1ff2d1c094659284de55be10fb"
    // 9) "flags"
    //10) "master"
    //11) "link-pending-commands"
    //12) "0"
    //13) "link-refcount"
    //14) "1"
    //15) "last-ping-sent"
    //16) "0"
    //17) "last-ok-ping-reply"
    //18) "755"
    //19) "last-ping-reply"
    //20) "755"
    //21) "down-after-milliseconds"
    //22) "5000"
    //23) "info-refresh"
    //24) "5375"
    //25) "role-reported"
    //26) "master"
    //27) "role-reported-time"
    //28) "55603"
    //29) "config-epoch"
    //30) "304"
    //31) "num-slaves"
    //32) "2"
    //33) "num-other-sentinels"
    //34) "3"
    //35) "quorum"
    //36) "2"
    //37) "failover-timeout"
    //38) "15000"
    //39) "parallel-syncs"
    //40) "1"
    public class MasterResult
    {
        public string name;
        public string ip;
        public int port;
        public string runid;
        public string flags;
        public long link_pending_commands;
        public long link_refcount;
        public long last_ping_sent;
        public long last_ok_ping_reply;
        public long last_ping_reply;
        public long down_after_milliseconds;
        public long info_refresh;
        public string role_reported;
        public long role_reported_time;
        public long config_epoch;
        public long num_slaves;
        public long num_other_sentinels;
        public long quorum;
        public long failover_timeout;
        public long parallel_syncs;
    }

    // 1) "name"
    // 2) "127.0.0.1:6379"
    // 3) "ip"
    // 4) "127.0.0.1"
    // 5) "port"
    // 6) "6379"
    // 7) "runid"
    // 8) ""
    // 9) "flags"
    //10) "s_down,slave"
    //11) "link-pending-commands"
    //12) "100"
    //13) "link-refcount"
    //14) "1"
    //15) "last-ping-sent"
    //16) "11188943"
    //17) "last-ok-ping-reply"
    //18) "11188943"
    //19) "last-ping-reply"
    //20) "11188943"
    //21) "s-down-time"
    //22) "11183890"
    //23) "down-after-milliseconds"
    //24) "5000"
    //25) "info-refresh"
    //26) "1603036921117"
    //27) "role-reported"
    //28) "slave"
    //29) "role-reported-time"
    //30) "11188943"
    //31) "master-link-down-time"
    //32) "0"
    //33) "master-link-status"
    //34) "err"
    //35) "master-host"
    //36) "?"
    //37) "master-port"
    //38) "0"
    //39) "slave-priority"
    //40) "100"
    //41) "slave-repl-offset"
    //42) "0"
    public class SalveResult
    {
        public string name;
        public string ip;
        public int port;
        public string runid;
        public string flags;
        public long link_pending_commands;
        public long link_refcount;
        public long last_ping_sent;
        public long last_ok_ping_reply;
        public long last_ping_reply;
        public long s_down_time;
        public long down_after_milliseconds;
        public long info_refresh;
        public string role_reported;
        public long role_reported_time;
        public long master_link_down_time;
        public string master_link_status;
        public string master_host;
        public int master_port;
        public long slave_priority;
        public long slave_repl_offset;
    }

    // 1) "name"
    // 2) "311f72064b0a58ee7f9d49dab078dada24a2b95c"
    // 3) "ip"
    // 4) "127.0.0.1"
    // 5) "port"
    // 6) "26479"
    // 7) "runid"
    // 8) "311f72064b0a58ee7f9d49dab078dada24a2b95c"
    // 9) "flags"
    //10) "sentinel"
    //11) "link-pending-commands"
    //12) "0"
    //13) "link-refcount"
    //14) "1"
    //15) "last-ping-sent"
    //16) "0"
    //17) "last-ok-ping-reply"
    //18) "364"
    //19) "last-ping-reply"
    //20) "364"
    //21) "down-after-milliseconds"
    //22) "5000"
    //23) "last-hello-message"
    //24) "325"
    //25) "voted-leader"
    //26) "?"
    //27) "voted-leader-epoch"
    //28) "0"
    public class SentinelResult
    {
        public string name;
        public string ip;
        public int port;
        public string runid;
        public string flags;
        public long link_pending_commands;
        public long link_refcount;
        public long last_ping_sent;
        public long last_ok_ping_reply;
        public long last_ping_reply;
        public long down_after_milliseconds;
        public long last_hello_message;
        public string voted_leader;
        public long voted_leader_epoch;
    }

    public class IsMaterDownByAddrResult
    {
        public bool down_state;
        public string leader;
        public long vote_epoch;
    }
}
