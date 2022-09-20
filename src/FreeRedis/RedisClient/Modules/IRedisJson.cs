using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial interface IRedisClient
    {
        long[] JsonArrAppend(string key, string path, params object[] values);
        long[] JsonArrIndex<T>(string key, string path, T value) where T : struct;
        long[] JsonArrInsert(string key, string path, long index = 0, params object[] values);
        long[] JsonArrLen(string key, string path);
        object[] JsonArrPop(string key, string path, int index = -1);
        long[] JsonArrTrim(string key, string path, int start, int stop);
        long[] JsonClear(string key, string path = "$");
        long JsonDel(string key, string path = "$");
        long JsonForget(string key, string path = "$");
        string JsonGet(string key, string indent = null, string newline = null, string space = null, params string[] paths);
        string[] JsonMGet(string[] keys, string path = "$");
        bool JsonSet(string key, string value, string path = "$", bool nx = false, bool xx = false);
        long[] JsonDebugMemory(string key, string path = "$");

         string JsonNumIncrBy(string key, string path, double value);
         string JsonNumMultBy(string key, string path, double value);

        string[][] JsonObjKeys(string key, string path = "$");
         long[] JsonObjLen(string key, string path = "$");
         object[][] JsonResp(string key, string path = "$");

         long[] JsonStrAppend(string key, string value, string path = "$");
         long[] JsonStrLen(string key, string path = "$");
         bool[] JsonToggle(string key, string path = "$");
         string[] JsonType(string key, string path = "$");

    }
}
