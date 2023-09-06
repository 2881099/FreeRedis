using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static FreeRedis.FtCreateParams;

namespace FreeRedis
{
    partial class RedisClient
    {
        public string[] Ft_List() => Call("FT._LIST", rt => rt.ThrowOrValue<string[]>());

        //public string[] FtAggregate() => Call("FT._LIST", rt => rt.ThrowOrValue<string[]>());

        public void FtAliasAdd(string alias, string index) => Call("FT.ALIASADD".Input(alias, index), rt => rt.ThrowOrValue<string>() == "OK");
        public void FtAliasDel(string alias) => Call("FT.ALIASDEL".Input(alias), rt => rt.ThrowOrValue<string>() == "OK");
        public void FtAliasUpdate(string alias, string index) => Call("FT.ALIASUPDATE".Input(alias, index), rt => rt.ThrowOrValue<string>() == "OK");

        //public void FtAlter (string alias, string index) => Call("FT.ALTER".Input(alias, index), rt => rt.ThrowOrValue<string>() == "OK");

        public Dictionary<string, object> FtConfigGet(string option, string value) => Call("FT.CONFIG".SubCommand("GET").Input(option, value), rt => rt.ThrowOrValue((a, _) => a.MapToHash<object>(rt.Encoding)));
        public void FtConfigSet(string option, string value) => Call("FT.CONFIG".SubCommand("SET").Input(option, value), rt => rt.ThrowOrValue<string>() == "OK");

        public void FtCreate(string index, Field[] schema, FtCreateParams param) => Call(FtCreateParams.ToCommandPacket(index, schema, param), rt => rt.ThrowOrValue<string>() == "OK");

        public void FtCursorDel(string index, long cursor_id) => Call("FT.CURSOR".SubCommand("DEL").Input(index, cursor_id), rt => rt.ThrowOrValue<string>() == "OK");
        public object FtCursorRead(string index, long cursor_id, int count = 0) => Call("FT.CURSOR".SubCommand("READ")
            .Input(index, cursor_id)
            .InputIf(count > 0, "COUNT", count), rt => rt.ThrowOrValue<object>());

        public long FtDictAdd(string dict, params string[] terms) => Call("FT.DICTADD".Input(dict).Input(terms.Select(a => (object)a).ToArray()), rt => rt.ThrowOrValue<long>());
        public long FtDictDel(string dict, params string[] terms) => Call("FT.DICTDEL".Input(dict).Input(terms.Select(a => (object)a).ToArray()), rt => rt.ThrowOrValue<long>());
        public string[] FtDictDump(string dict) => Call("FT.DICTDUMP".Input(dict), rt => rt.ThrowOrValue<string[]>());

        public void FtDropIndex(string index, bool dd = false) => Call("FT.DROPINDEX".Input(index).InputIf(dd, "DD"), rt => rt.ThrowOrValue<string>() == "OK");

        public string FtExplain(string index, string query, string dialect = null) => Call("FT.EXPLAIN".Input(index, query).InputIf(!string.IsNullOrEmpty(dialect), "DIALECT", dialect), rt => rt.ThrowOrValue<string>());
        public object FtInfo(string index) => Call("FT.INFO".Input(index), rt => rt.ThrowOrValue<object>());

        //public object FtProfile(string index) => Call("FT.PROFILE".Input(index), rt => rt.ThrowOrValue<object>());
        public object FtSearch(string index, string query) => Call("FT.SEARCH".Input(index), rt => rt.ThrowOrValue<object>());
        //public object FtSpellCheck(string index) => Call("FT.SPELLCHECK".Input(index), rt => rt.ThrowOrValue<object>());

        public Dictionary<string, string[]> FtSynDump(string index) => Call("FT.SYNDUMP".Input(index), rt => rt.ThrowOrValue((a, _) => a.MapToHash<string[]>(rt.Encoding)));
        public void FtSynUpdate(string index, string synonym_group_id, bool skipInitialScan, params string[] terms) => Call("FT.SYNUPDATE"
            .Input(index, synonym_group_id)
            .InputIf(skipInitialScan, "SKIPINITIALSCAN")
            .Input(terms.Select(a => (object)a).ToArray()), rt => rt.ThrowOrValue<string>() == "OK");
        public string[] FtTagVals(string index, string field_name) => Call("FT.TAGVALS".Input(index, field_name), rt => rt.ThrowOrValue<string[]>());


    }

    public class FtSearchBuilder
    {
        private bool _noContent, _verbatim, _noStopWords, _withScores, _withPayLoads, _withSortKeys;
        private List<object[]> _filter = new List<object[]>();
        private List<object[]> _geoFilter = new List<object[]>();
        private List<string> _inKeys = new List<string>();
        private List<string> _inFields = new List<string>();
        private List<string[]> _return = new List<string[]>();
        private bool _sumarize;
        private string[] _sumarizeFields;
        private long _sumarizeFrags = -1;
        private long _sumarizeLen = -1;
        private string _sumarizeSeparator;
        private bool _highLight;
        private List<string> _highLightFields = new List<string>();
        private string[] _highLightTags;
        private decimal _slop = -1;
        private long _timeout;
        private bool _inOrder;
        private string _language, _expander, _scorer;
        private bool _explainScore;
        private string _payLoad;
        private string _sortBy;
        private bool _sortByDesc;
        private bool _sortByWithCount;
        private long _limitOffset, _limitNum;
        private List<object> _params = new List<object>();
        private int _dialect;

        public FtSearchBuilder NoContent(bool value = true)
        {
            _noContent = value;
            return this;
        }
        public FtSearchBuilder Verbatim(bool value = true)
        {
            _verbatim = value;
            return this;
        }
        public FtSearchBuilder NoStopWords(bool value = true)
        {
            _noStopWords = value;
            return this;
        }
        public FtSearchBuilder WithScores(bool value = true)
        {
            _withScores = value;
            return this;
        }
        public FtSearchBuilder WithPayLoads(bool value = true)
        {
            _withPayLoads = value;
            return this;
        }
        public FtSearchBuilder WithSortKeys(bool value = true)
        {
            _withSortKeys = value;
            return this;
        }
        public FtSearchBuilder Filter(string field, string min, string max)
        {
            _filter.Add(new object[] { field, min, max });
            return this;
        }
        public FtSearchBuilder GeoFilter(string field, string lon, string lat, decimal radius, GeoUnit unit = GeoUnit.m)
        {
            _geoFilter.Add(new object[] { field, lon, lat, radius, unit });
            return this;
        }
        public FtSearchBuilder InKeys(params string[] key)
        {
            if (key?.Any() == true) _inKeys.AddRange(key);
            return this;
        }
        public FtSearchBuilder InFields(params string[] field)
        {
            if (field?.Any() == true) _inFields.AddRange(field);
            return this;
        }

        public FtSearchBuilder Return(params string[] identifier)
        {
            if (identifier?.Any() == true) _return.AddRange(identifier.Select(a => new[] {a, null}));
            return this;
        }
        public FtSearchBuilder Return(Dictionary<string, string> identifier)
        {
            if (identifier?.Any() == true) _return.AddRange(identifier.Select(a => new[] { a.Key, a.Value }));
            return this;
        }
        public FtSearchBuilder Sumarize(string[] fields, long frags, long len, string separator)
        {
            _sumarize = true;
            _sumarizeFields = fields;
            _sumarizeFrags = frags;
            _sumarizeLen = len;
            _sumarizeSeparator = separator;
            return this;
        }
        public FtSearchBuilder HighLight(string[] fields, string tagsOpen, string tagsClose)
        {
            _highLight = true;
            _highLightTags = new[] { tagsOpen, tagsClose }; 
            return this;
        }
        public FtSearchBuilder Slop(decimal value)
        {
            _slop = value;
            return this;
        }
        public FtSearchBuilder Timeout(long milliseconds)
        {
            _timeout = milliseconds;
            return this;
        }
        public FtSearchBuilder InOrder(bool value = true)
        {
            _inOrder = value;
            return this;
        }
        public FtSearchBuilder Language(string value)
        {
            _language = value;
            return this;
        }
        public FtSearchBuilder Expander(string value)
        {
            _expander = value;
            return this;
        }
        public FtSearchBuilder Scorer(string value)
        {
            _scorer = value;
            return this;
        }
        public FtSearchBuilder ExplainScore(bool value)
        {
            _explainScore = value;
            return this;
        }
        public FtSearchBuilder PayLoad(string value)
        {
            _payLoad = value;
            return this;
        }
        public FtSearchBuilder SortBy(string sortBy, bool desc = false, bool withCount = false)
        {
            _sortBy = sortBy;
            _sortByDesc = desc;
            _sortByWithCount = withCount;
            return this;
        }
        public FtSearchBuilder Limit(long offset, long num)
        {
            _limitOffset = offset;
            _limitNum = num;
            return this;
        }
        public FtSearchBuilder Params(string name, string value)
        {
            _params.Add(name);
            _params.Add(value);
            return this;
        }
        public FtSearchBuilder Dialect(int value)
        {
            _dialect = value;
            return this;
        }
    }
    public class FtCreateParams
    {
        public enum OnType { Hash, Json }
        public enum FieldType { Text, Tag, Numeric, Geo, Vector, GeoShape }
        public class Field
        {
            public string Name { get; set; }
            public string Alias { get; set; }
            public FieldType Type { get; set; }
            public bool Sortable { get; set; }
            public bool Unf { get; set; }
            public bool NoIndex { get; set; }
        }

        public OnType On { get; set; } = OnType.Hash;
        public string[] Prefix { get; set; }
        public string Filter { get; set; }
        public string Language { get; set; }
        public string LanguageField { get; set; }
        public decimal Score { get; set; }
        public decimal ScoreField { get; set; }
        public string PayLoadField { get; set; }
        public bool MaxTextFields { get; set; }
        public long Temporary { get; set; }
        public bool NoOffsets { get; set; }
        public bool NoHL { get; set; }
        public bool NoFields { get; set; }
        public bool NoFreqs { get; set; }
        public string[] Stopwords { get; set; }
        public bool SkipInitialScan { get; set; }

        public static CommandPacket ToCommandPacket(string index, Field[] schema, FtCreateParams param)
        {
            var on = param?.On ?? OnType.Hash;
            var cmd = "FT.CREATE".Input(index).Input("ON", on.ToString().ToUpper());
            if (param != null)
            {
                if (param.Prefix?.Any() == true) cmd.Input("PREFIX", param.Prefix.Length).Input(param.Prefix.Select(a => (object)a).ToArray());
                cmd
                    .InputIf(!string.IsNullOrWhiteSpace(param.Filter), "FILTER", param.Filter)
                    .InputIf(!string.IsNullOrWhiteSpace(param.Language), "LANGUAGE", param.Language)
                    .InputIf(!string.IsNullOrWhiteSpace(param.LanguageField), "LANGUAGE_FIELD", param.LanguageField)
                    .InputIf(param.Score > 0, "SCORE", param.Score)
                    .InputIf(param.ScoreField > 0, "SCORE_FIELD", param.ScoreField)
                    .InputIf(!string.IsNullOrWhiteSpace(param.PayLoadField), "PAYLOAD_FIELD", param.PayLoadField)
                    .InputIf(param.MaxTextFields, "MAXTEXTFIELDS")
                    .InputIf(param.Temporary > 0, "TEMPORARY", param.Temporary)
                    .InputIf(param.NoOffsets, "NOOFFSETS")
                    .InputIf(param.NoHL, "NOHL")
                    .InputIf(param.NoFields, "NOFIELDS")
                    .InputIf(param.NoFreqs, "NOFREQS");
                if (param.Stopwords?.Any() == true) cmd.Input("STOPWORDS", param.Stopwords.Length).Input(param.Stopwords.Select(a => (object)a).ToArray());
                cmd
                    .InputIf(param.SkipInitialScan, "SKIPINITIALSCAN");
            }
            if (schema != null)
            {
                cmd.Input("SCHEMA");
                foreach(var field in schema)
                {
                    cmd.Input(field.Name)
                        .InputIf(!string.IsNullOrWhiteSpace(field.Alias), "AS", field.Alias)
                        .Input(field.Type.ToString().ToUpper())
                        .InputIf(field.Sortable, "SORTABLE")
                        .InputIf(field.Sortable && field.Unf, "UNF")
                        .InputIf(field.NoIndex, "NOINDEX");
                }
            }
            return cmd;
        }
    }
}
