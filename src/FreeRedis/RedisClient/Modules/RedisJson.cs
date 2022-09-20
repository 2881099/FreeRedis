using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
        public string JsonGet(string key, string indent = default, string newline = default, string space = default, params string[] paths)
            => Call("JSON.GET".InputKey(key)
                .InputIf(indent != null, "INDENT", indent)
                .InputIf(newline != null, "NEWLINE", newline)
                .InputIf(space != null, "SPACE", space)
                .Input(paths), rt => rt.ThrowOrValue<string>());

        public string[] JsonMGet(string[] keys, string path = "$") => Call("JSON.MGET".InputKey(keys).Input(path), rt => rt.ThrowOrValue<string[]>());

        public bool JsonSet(string key, string value, string path = "$", bool nx = false, bool xx = false)
        {
            return Call("JSON.SET".InputKey(key, path)
                .InputRaw(value)
                .InputIf(nx, "NX")
                .InputIf(xx, "XX"), rt => rt.ThrowOrValue<string>() == "OK");
        }

        public long JsonDel(string key, string path = "$") => Call("JSON.DEL".InputKey(key, path), rt => rt.ThrowOrValue<long>());

        public long[] JsonArrInsert(string key, string path, long index = 0, params object[] values) => Call("JSON.ARRINSERT".InputKey(key, path, index).Input(values.Select(SerializeRedisValue).ToArray()), rt => rt.ThrowOrValue<long[]>());

        public long[] JsonArrAppend(string key, string path, params object[] values) => Call("JSON.ARRAPPEND".InputKey(key, path).Input(values.Select(SerializeRedisValue).ToArray()), rt => rt.ThrowOrValue<long[]>());

        public long[] JsonArrIndex<T>(string key, string path, T value) where T : struct => Call("JSON.ARRINDEX".InputKey(key, path).InputRaw(SerializeRedisValue(value)), rt => rt.ThrowOrValue<long[]>());

        public long[] JsonArrLen(string key, string path) => Call("JSON.ARRLEN".InputKey(key, path), rt => rt.ThrowOrValue<long[]>());

        public object[] JsonArrPop(string key, string path, int index = -1) => HReadArray<object>("JSON.ARRPOP".InputKey(key, path, index));

        public long[] JsonArrTrim(string key, string path, int start, int stop) => Call("JSON.ARRTRIM".InputKey(key, path).Input(start, stop), rt => rt.ThrowOrValue<long[]>());
        public long[] JsonClear(string key, string path = "$") => Call("JSON.CLEAR".InputKey(key, path), rt => rt.ThrowOrValue<long[]>());
        public long[] JsonDebugMemory(string key, string path = "$") => Call("JSON.DEBUG".InputKey("MEMORY", key, path), rt => rt.ThrowOrValue<long[]>());

        public long JsonForget(string key, string path = "$") => Call("JSON.FORGET".InputKey(key, path), rt => rt.ThrowOrValue<long>());
        public string JsonNumIncrBy(string key, string path, double value) => Call("JSON.NUMINCRBY".InputKey(key, path).Input(value), rt => rt.ThrowOrValue<string>());
        public string JsonNumMultBy(string key, string path, double value) => Call("JSON.NUMMULTBY".InputKey(key, path).Input(value), rt => rt.ThrowOrValue<string>());

        public string[][] JsonObjKeys(string key, string path = "$") => Call("JSON.OBJKEYS".InputKey(key, path), rt => rt.ThrowOrValue<string[][]>());
        public long[] JsonObjLen(string key, string path = "$") => Call("JSON.OBJLEN".InputKey(key, path), rt => rt.ThrowOrValue<long[]>());
        public object[][] JsonResp(string key, string path = "$") => Call("JSON.RESP".InputKey(key, path), rt => rt.ThrowOrValue<object[][]>());
        public long[] JsonStrAppend(string key, string value, string path = "$") => Call("JSON.STRAPPEND".InputKey(key, path).Input($"\"{value}\""), rt => rt.ThrowOrValue<long[]>());
        public long[] JsonStrLen(string key, string path = "$") => Call("JSON.STRLEN".InputKey(key, path), rt => rt.ThrowOrValue<long[]>());
        public bool[] JsonToggle(string key, string path = "$") => Call("JSON.TOGGLE".InputKey(key, path), rt => rt.ThrowOrValue<bool[]>());
        public string[] JsonType(string key, string path = "$") => Call("JSON.TYPE".InputKey(key, path), rt => rt.ThrowOrValue<string[]>());

    }
}
