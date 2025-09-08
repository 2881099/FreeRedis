using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
#if isasync
        #region async (copy from sync)

        public Task<string> JsonGetAsync(string key, string indent = default, string newline = default, string space = default, params string[] paths)
            => CallAsync("JSON.GET".InputKey(key)
                .InputIf(indent != null, "INDENT", indent)
                .InputIf(newline != null, "NEWLINE", newline)
                .InputIf(space != null, "SPACE", space)
                .Input(paths), rt => rt.ThrowOrValue<string>());

        public Task<string[]> JsonMGetAsync(string[] keys, string path = "$") => CallAsync("JSON.MGET".InputKey(keys).Input(path), rt => rt.ThrowOrValue<string[]>());

        async public Task JsonSetAsync(string key, string value, string path = "$", bool nx = false, bool xx = false)
            => await CallAsync("JSON.SET".InputKey(key)
                .Input(path)
                .InputRaw(value)
                .InputIf(nx, "NX")
                .InputIf(xx, "XX"), rt => rt.ThrowOrValue<string>() == "OK");
        async public Task JsonMSetAsync(string[] keys, string[] values, string[] paths)
        {
            if (keys?.Any() != true) throw new ArgumentException($"{nameof(keys)} not is null or empry");
            if (values?.Any() != true) throw new ArgumentException($"{nameof(values)} not is null or empry");
            if (paths?.Any() != true) throw new ArgumentException($"{nameof(paths)} not is null or empry");
            if (keys.Length != values.Length || keys.Length != paths.Length) throw new ArgumentException($"{nameof(keys)}, {nameof(values)}, {nameof(paths)} Length must equals");
            var cmd = new CommandPacket("JSON.MSET");
            for (var a = 0; a < keys.Length; a++)
                cmd = cmd.InputKey(keys[a]).Input(paths[a], values[a]);
            await CallAsync(cmd, rt => rt.ThrowOrValue<string>() == "OK");
        }

        async public Task JsonMergeAsync(string key, string path, string value) => await CallAsync("JSON.MERGE".InputKey(key).Input(path, value), rt => rt.ThrowOrValue<string>() == "OK");

        public Task<long> JsonDelAsync(string key, string path = "$") => CallAsync("JSON.DEL".InputKey(key).Input(path), rt => rt.ThrowOrValue<long>());

        public Task<long[]> JsonArrInsertAsync(string key, string path, long index = 0, params object[] values) => CallAsync("JSON.ARRINSERT".InputKey(key).Input(path, index).Input(values.Select(SerializeRedisValue).ToArray()), rt => rt.ThrowOrValue<long[]>());

        public Task<long[]> JsonArrAppendAsync(string key, string path, params object[] values) => CallAsync("JSON.ARRAPPEND".InputKey(key).Input(path).Input(values.Select(SerializeRedisValue).ToArray()), rt => rt.ThrowOrValue<long[]>());

        public Task<long[]> JsonArrIndexAsync<T>(string key, string path, T value) where T : struct => CallAsync("JSON.ARRINDEX".InputKey(key).Input(path).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<long[]>());

        public Task<long[]> JsonArrLenAsync(string key, string path) => CallAsync("JSON.ARRLEN".InputKey(key).Input(path), rt => rt.ThrowOrValue<long[]>());

        async public Task<object[]> JsonArrPopAsync(string key, string path, int index = -1) => await HReadArrayAsync<object>("JSON.ARRPOP".InputKey(key).Input(path).InputIf(index != -1, index));

        public Task<long[]> JsonArrTrimAsync(string key, string path, int start, int stop) => CallAsync("JSON.ARRTRIM".InputKey(key).Input(path).Input(start, stop), rt => rt.ThrowOrValue<long[]>());
        public Task<long> JsonClearAsync(string key, string path = "$") => CallAsync("JSON.CLEAR".InputKey(key).Input(path), rt => rt.ThrowOrValue<long>());
        public Task<long[]> JsonDebugMemoryAsync(string key, string path = "$") => CallAsync("JSON.DEBUG".SubCommand("MEMORY").InputKey(key).Input(path), rt => rt.ThrowOrValue<long[]>());

        public Task<long> JsonForgetAsync(string key, string path = "$") => CallAsync("JSON.FORGET".InputKey(key).Input(path), rt => rt.ThrowOrValue<long>());
        public Task<string> JsonNumIncrByAsync(string key, string path, double value) => CallAsync("JSON.NUMINCRBY".InputKey(key).Input(path).Input(value), rt => rt.ThrowOrValue<string>());
        public Task<string> JsonNumMultByAsync(string key, string path, double value) => CallAsync("JSON.NUMMULTBY".InputKey(key).Input(path).Input(value), rt => rt.ThrowOrValue<string>());

        public Task<string[][]> JsonObjKeysAsync(string key, string path = "$") => CallAsync("JSON.OBJKEYS".InputKey(key).Input(path), rt => rt.ThrowOrValue<string[][]>());
        public Task<long[]> JsonObjLenAsync(string key, string path = "$") => CallAsync("JSON.OBJLEN".InputKey(key).Input(path), rt => rt.ThrowOrValue<long[]>());
        public Task<object[][]> JsonRespAsync(string key, string path = "$") => CallAsync("JSON.RESP".InputKey(key).Input(path), rt => rt.ThrowOrValue<object[][]>());
        public Task<long[]> JsonStrAppendAsync(string key, string value, string path = "$") => CallAsync("JSON.STRAPPEND".InputKey(key).Input(path).Input(SerializeString(value)), rt => rt.ThrowOrValue<long[]>());
        public Task<long[]> JsonStrLenAsync(string key, string path = "$") => CallAsync("JSON.STRLEN".InputKey(key).Input(path), rt => rt.ThrowOrValue<long[]>());
        public Task<bool[]> JsonToggleAsync(string key, string path = "$") => CallAsync("JSON.TOGGLE".InputKey(key).Input(path), rt => rt.ThrowOrValue<bool[]>());
        public Task<string[]> JsonTypeAsync(string key, string path = "$") => CallAsync("JSON.TYPE".InputKey(key).Input(path), rt => rt.ThrowOrValue<string[]>());

        #endregion
#endif
        public string JsonGet(string key, string indent = default, string newline = default, string space = default, params string[] paths)
            => Call("JSON.GET".InputKey(key)
                .InputIf(indent != null, "INDENT", indent)
                .InputIf(newline != null, "NEWLINE", newline)
                .InputIf(space != null, "SPACE", space)
                .Input(paths), rt => rt.ThrowOrValue<string>());

        public string[] JsonMGet(string[] keys, string path = "$") => Call("JSON.MGET".InputKey(keys).Input(path), rt => rt.ThrowOrValue<string[]>());

        public void JsonSet(string key, string value, string path = "$", bool nx = false, bool xx = false)
            => Call("JSON.SET".InputKey(key).Input(path)
                .InputRaw(value)
                .InputIf(nx, "NX")
                .InputIf(xx, "XX"), rt => rt.ThrowOrValue<string>() == "OK");
        public void JsonMSet(string[] keys, string[] values, string[] paths)
        {
            if (keys?.Any() != true) throw new ArgumentException($"{nameof(keys)} not is null or empry");
            if (values?.Any() != true) throw new ArgumentException($"{nameof(values)} not is null or empry");
            if (paths?.Any() != true) throw new ArgumentException($"{nameof(paths)} not is null or empry");
            if (keys.Length != values.Length || keys.Length != paths.Length) throw new ArgumentException($"{nameof(keys)}, {nameof(values)}, {nameof(paths)} Length must equals");
            var cmd = new CommandPacket("JSON.MSET");
            for (var a = 0; a < keys.Length; a++)
                cmd = cmd.InputKey(keys[a]).Input(paths[a], values[a]);
            Call(cmd, rt => rt.ThrowOrValue<string>() == "OK");
        }

        public void JsonMerge(string key, string path, string value) => Call("JSON.MERGE".InputKey(key).Input(path, value), rt => rt.ThrowOrValue<string>() == "OK");

        public long JsonDel(string key, string path = "$") => Call("JSON.DEL".InputKey(key).Input(path), rt => rt.ThrowOrValue<long>());

        public long[] JsonArrInsert(string key, string path, long index = 0, params object[] values) => Call("JSON.ARRINSERT".InputKey(key).Input(path, index).Input(values.Select(SerializeRedisValue).ToArray()), rt => rt.ThrowOrValue<long[]>());

        public long[] JsonArrAppend(string key, string path, params object[] values) => Call("JSON.ARRAPPEND".InputKey(key).Input(path).Input(values.Select(SerializeRedisValue).ToArray()), rt => rt.ThrowOrValue<long[]>());

        public long[] JsonArrIndex<T>(string key, string path, T value) where T : struct => Call("JSON.ARRINDEX".InputKey(key).Input(path).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<long[]>());

        public long[] JsonArrLen(string key, string path) => Call("JSON.ARRLEN".InputKey(key).Input(path), rt => rt.ThrowOrValue<long[]>());

        public object[] JsonArrPop(string key, string path, int index = -1) => HReadArray<object>("JSON.ARRPOP".InputKey(key).Input(path).InputIf(index != -1, index));

        public long[] JsonArrTrim(string key, string path, int start, int stop) => Call("JSON.ARRTRIM".InputKey(key).Input(path).Input(start, stop), rt => rt.ThrowOrValue<long[]>());
        public long JsonClear(string key, string path = "$") => Call("JSON.CLEAR".InputKey(key).Input(path), rt => rt.ThrowOrValue<long>());
        public long[] JsonDebugMemory(string key, string path = "$") => Call("JSON.DEBUG".SubCommand("MEMORY").InputKey(key).Input(path), rt => rt.ThrowOrValue<long[]>());

        public long JsonForget(string key, string path = "$") => Call("JSON.FORGET".InputKey(key).Input(path), rt => rt.ThrowOrValue<long>());
        public string JsonNumIncrBy(string key, string path, double value) => Call("JSON.NUMINCRBY".InputKey(key).Input(path).Input(value), rt => rt.ThrowOrValue<string>());
        public string JsonNumMultBy(string key, string path, double value) => Call("JSON.NUMMULTBY".InputKey(key).Input(path).Input(value), rt => rt.ThrowOrValue<string>());

        public string[][] JsonObjKeys(string key, string path = "$") => Call("JSON.OBJKEYS".InputKey(key).Input(path), rt => rt.ThrowOrValue<string[][]>());
        public long[] JsonObjLen(string key, string path = "$") => Call("JSON.OBJLEN".InputKey(key).Input(path), rt => rt.ThrowOrValue<long[]>());
        public object[][] JsonResp(string key, string path = "$") => Call("JSON.RESP".InputKey(key).Input(path), rt => rt.ThrowOrValue<object[][]>());
        public long[] JsonStrAppend(string key, string value, string path = "$") => Call("JSON.STRAPPEND".InputKey(key).Input(path).Input(SerializeString(value)), rt => rt.ThrowOrValue<long[]>());
        public long[] JsonStrLen(string key, string path = "$") => Call("JSON.STRLEN".InputKey(key).Input(path), rt => rt.ThrowOrValue<long[]>());
        public bool[] JsonToggle(string key, string path = "$") => Call("JSON.TOGGLE".InputKey(key).Input(path), rt => rt.ThrowOrValue<bool[]>());
        public string[] JsonType(string key, string path = "$") => Call("JSON.TYPE".InputKey(key).Input(path), rt => rt.ThrowOrValue<string[]>());
        private static string SerializeString(string input)
        {
            if (input == null)
                return "null";

            var sb = new StringBuilder();
            sb.Append('"');

            foreach (char c in input)
            {
                switch (c)
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '/':
                        // 正斜杠可以选择性转义，这里为了安全起见进行转义
                        sb.Append("\\/");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        // 根据JSON标准，控制字符(0x00-0x1F)必须转义
                        if (c < 32)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("X4"));
                        }
                        // 对于高位Unicode字符，也进行转义以确保兼容性
                        else if (c > 127)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("X4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            
            sb.Append('"');
            return sb.ToString();
        }
    }
}