using FreeRedis.RediSearch;
using System.Collections.Generic;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
    {
        public string[] Ft_List() => Call("FT._LIST", rt => rt.ThrowOrValue<string[]>());
        public AggregateBuilder FtAggregate(string index, string query) => new AggregateBuilder(this, index, query);

        public void FtAliasAdd(string alias, string index) => Call("FT.ALIASADD".Input(alias, index), rt => rt.ThrowOrValue<string>() == "OK");
        public void FtAliasDel(string alias) => Call("FT.ALIASDEL".Input(alias), rt => rt.ThrowOrValue<string>() == "OK");
        public void FtAliasUpdate(string alias, string index) => Call("FT.ALIASUPDATE".Input(alias, index), rt => rt.ThrowOrValue<string>() == "OK");

        public AlterBuilder FtAlter(string index) => new AlterBuilder(this, index);

        public Dictionary<string, object> FtConfigGet(string option, string value) => Call("FT.CONFIG".SubCommand("GET").Input(option, value), rt => rt.ThrowOrValue((a, _) => a.MapToHash<object>(rt.Encoding)));
        public void FtConfigSet(string option, string value) => Call("FT.CONFIG".SubCommand("SET").Input(option, value), rt => rt.ThrowOrValue<string>() == "OK");

        public CreateBuilder FtCreate(string index) => new CreateBuilder(this, index);

        public void FtCursorDel(string index, long cursor_id) => Call("FT.CURSOR".SubCommand("DEL").Input(index, cursor_id), rt => rt.ThrowOrValue<string>() == "OK");
        public AggregationResult FtCursorRead(string index, long cursorId, int count = 0) => Call("FT.CURSOR".SubCommand("READ")
            .Input(index, cursorId)
            .InputIf(count > 0, "COUNT", count), rt => rt.ThrowOrValue((a, _) => new AggregationResult(a[0], a[1].ConvertTo<long>())));

        public long FtDictAdd(string dict, params string[] terms) => Call("FT.DICTADD".Input(dict).Input(terms.Select(a => (object)a).ToArray()), rt => rt.ThrowOrValue<long>());
        public long FtDictDel(string dict, params string[] terms) => Call("FT.DICTDEL".Input(dict).Input(terms.Select(a => (object)a).ToArray()), rt => rt.ThrowOrValue<long>());
        public string[] FtDictDump(string dict) => Call("FT.DICTDUMP".Input(dict), rt => rt.ThrowOrValue<string[]>());

        public void FtDropIndex(string index, bool dd = false) => Call("FT.DROPINDEX".Input(index).InputIf(dd, "DD"), rt => rt.ThrowOrValue<string>() == "OK");

        public string FtExplain(string index, string query, string dialect = null) => Call("FT.EXPLAIN".Input(index, query).InputIf(!string.IsNullOrEmpty(dialect), "DIALECT", dialect), rt => rt.ThrowOrValue<string>());
        //public object FtInfo(string index) => Call("FT.INFO".Input(index), rt => rt.ThrowOrValue<object>());

        //public object FtProfile(string index) => Call("FT.PROFILE".Input(index), rt => rt.ThrowOrValue<object>());
        public SearchBuilder FtSearch(string index, string query) => new SearchBuilder(this, index, query);
        public Dictionary<string, Dictionary<string, double>> FtSpellCheck(string index, string query, int distance = 1, int? dialect = null) => Call("FT.SPELLCHECK".Input(index, query)
            .InputIf(distance > 1, "DISTANCE", distance)
            .InputIf((dialect ?? ConnectionString.FtDialect) > 0, "DIALECT", dialect), rt => rt.ThrowOrValueToFtSpellCheckResult());

        public Dictionary<string, string[]> FtSynDump(string index) => Call("FT.SYNDUMP".Input(index), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string[]>(rt.Encoding)));
        public void FtSynUpdate(string index, string synonymGroupId, bool skipInitialScan, params string[] terms) => Call("FT.SYNUPDATE"
            .Input(index, synonymGroupId)
            .InputIf(skipInitialScan, "SKIPINITIALSCAN")
            .Input(terms.Select(a => (object)a).ToArray()), rt => rt.ThrowOrValue<string>() == "OK");

        public long FtSugAdd(string key, string str, double score, bool incr = false, string payload = null) => Call("FT.SUGADD".InputKey(key).Input(str, score)
            .InputIf(incr, "INCR")
            .InputIf(payload != null, "PAYLOAD", payload), rt => rt.ThrowOrValue<long>());
        public void FtSugDel(string key, string str) => Call("FT.SUGDEL".InputKey(key).Input(str), rt => rt.ThrowOrValue<long>());
        public string[] FtSugGet(string key, string prefix, bool fuzzy = false, bool withScores = false, bool withPayloads = false, int? max = null) => Call("FT.SUGGET".InputKey(key)
            .Input(prefix)
            .InputIf(fuzzy, "FUZZY")
            .InputIf(withScores, "WITHSCORES")
            .InputIf(withPayloads, "WITHPAYLOADS")
            .InputIf(max != null, "MAX", max), rt => rt.ThrowOrValue<string[]>());
        public void FtSugLen(string key) => Call("FT.SUGLEN".InputKey(key), rt => rt.ThrowOrValue<long>());

        public string[] FtTagVals(string index, string fieldName) => Call("FT.TAGVALS".Input(index, fieldName), rt => rt.ThrowOrValue<string[]>());
    }

    static partial class RedisResultThrowOrValueExtensions
    {
        public static Dictionary<string, Dictionary<string, double>> ThrowOrValueToFtSpellCheckResult(this RedisResult rt) =>
           rt.ThrowOrValue((rawTerms, _) =>
           {
               var returnTerms = new Dictionary<string, Dictionary<string, double>>(rawTerms.Length);
               foreach (var term in rawTerms)
               {
                   var rawElements = term as object[];
                   string termValue = rawElements[1].ConvertTo<string>();

                   var list = rawElements[2] as object[];
                   var entries = new Dictionary<string, double>(list.Length);
                   foreach (var entry in list)
                   {
                       var entryElements = entry as object[];
                       string suggestion = entryElements[1].ConvertTo<string>();
                       double score = entryElements[0].ConvertTo<double>();
                       entries.Add(suggestion, score);
                   }
                   returnTerms.Add(termValue, entries);
               }

               return returnTerms;
           });
        public static object ThrowOrValueToFtCursorRead(this RedisResult rt) =>
           rt.ThrowOrValue((a, _) =>
           {
               return a;
           });
    }
}