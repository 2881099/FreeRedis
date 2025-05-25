using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FreeRedis.RediSearch
{
    public class SearchResult
    {
        public long Total { get; }
        public List<Document> Documents { get; }

        public SearchResult(long total, List<Document> docs)
        {
            Total = total;
            Documents = docs;
        }
    }
    public class Document
    {
        public string Id { get; }
        public double Score { get; set; }
        public byte[] Payload { get; }
        public string[] ScoreExplained { get; private set; }
        public Dictionary<string, object> Body { get; } = new Dictionary<string, object>();
        public object this[string key]
        {
            get => Body.TryGetValue(key, out var val) ? val : null;
            internal set => Body[key] = value;
        }

        public Document(string id, double score, byte[] payload)
        {
            Id = id;
            Score = score;
            Payload = payload;
        }
        public static Document Load(string id, double score, byte[] payload, object[] fieldValues, string[] scoreExplained)
        {
            Document ret = new Document(id, score, payload);
            if (fieldValues != null)
            {
                for (int i = 0; i < fieldValues.Length; i += 2)
                {
                    string fieldName = fieldValues[i].ConvertTo<string>();
                    if (fieldName == "$")
                        ret.Body["json"] = fieldValues[i + 1];
                    else
                        ret.Body[fieldName] = fieldValues[i + 1];
                }
            }
            ret.ScoreExplained = scoreExplained;
            return ret;
        }
    }

    public class SearchBuilder
    {
        RedisClient _redis;
        string _index;
        string _query;
        internal SearchBuilder(RedisClient redis, string index, string query)
        {
            _redis = redis;
            _index = index;
            _query = query;
            _dialect = redis.ConnectionString.FtDialect;
            _language = redis.ConnectionString.FtLanguage;
        }
        internal CommandPacket GetCommandPacket()
        {
            var cmd = "FT.SEARCH".Input(_index).Input(_query)
                .InputIf(_noContent, "NOCONTENT")
                .InputIf(_verbatim, "VERBATIM")
                .InputIf(_noStopwords, "NOSTOPWORDS")
                .InputIf(_withScores, "WITHSCORES")
                .InputIf(_withPayloads, "WITHPAYLOADS")
                .InputIf(_withSortKeys, "WITHSORTKEYS");
            if (_filters.Any()) cmd.Input("FILTER").Input(_filters.ToArray());
            if (_geoFilter.Any()) cmd.Input("GEOFILTER").Input(_geoFilter.ToArray());
            if (_inKeys.Any()) cmd.Input("INKEYS", _inKeys.Count).Input(_inKeys.ToArray());
            if (_inFields.Any()) cmd.Input("INFIELDS", _inFields.Count).Input(_inFields.ToArray());
            if (_return.Any())
            {
                cmd.Input("RETURN", _return.Sum(a => a[1] == null ? 1 : 3));
                foreach (var ret in _return)
                    if (ret[1] == null) cmd.Input(ret[0]);
                    else cmd.Input(ret[0], "AS", ret[1]);
            }
            if (_sumarize)
            {
                cmd.Input("SUMMARIZE");
                if (_sumarizeFields.Any()) cmd.Input("FIELDS", _sumarizeFields.Count).Input(_sumarizeFields.ToArray());
                if (_sumarizeFrags != -1) cmd.Input("FRAGS", _sumarizeFrags);
                if (_sumarizeLen != -1) cmd.Input("LEN", _sumarizeLen);
                if (_sumarizeSeparator != null) cmd.Input("SEPARATOR", _sumarizeSeparator);
            }
            if (_highLight)
            {
                cmd.Input("HIGHLIGHT");
                if (_highLightFields.Any()) cmd.Input("FIELDS", _highLightFields.Count).Input(_highLightFields.ToArray());
                if (_highLightTags != null) cmd.Input("TAGS").Input(_highLightTags);
            }
            cmd
                .InputIf(_slop >= 0, "SLOP", _slop)
                .InputIf(_timeout >= 0, "TIMEOUT", _timeout)
                .InputIf(_inOrder, "INORDER")
                .InputIf(!string.IsNullOrWhiteSpace(_language), "LANGUAGE", _language)
                .InputIf(!string.IsNullOrWhiteSpace(_expander), "EXPANDER", _expander)
                .InputIf(!string.IsNullOrWhiteSpace(_scorer), "SCORER", _scorer)
                .InputIf(_explainScore, "EXPLAINSCORE")
                .InputIf(!string.IsNullOrWhiteSpace(_payLoad), "PAYLOAD", _payLoad)
                .InputIf(!string.IsNullOrWhiteSpace(_sortBy), "SORTBY", _sortBy).InputIf(_sortByDesc, "DESC")
                .InputIf(_limitOffset > 0 || _limitNum != 10, "LIMIT", _limitOffset, _limitNum);
            if (_params.Any())
            {
                cmd.Input("PARAMS", _params.Count);
                _params.ForEach(item => cmd.Input(item));
            }
            cmd
               .InputIf(_dialect > 0, "DIALECT", _dialect);
            return cmd;
        }
        SearchResult FetchResult(RedisResult rt) => rt.ThrowOrValue((a, _) =>
        {
            var ret = new SearchResult(a[0].ConvertTo<long>(), new List<Document>());
            if (a.Any() != true || ret.Total <= 0) return ret;

            int step = 1;
            int scoreOffset = 0;
            int contentOffset = 1;
            int payloadOffset = 0;
            if (_withScores)
            {
                step++;
                scoreOffset = 1;
                contentOffset++;

            }
            if (_noContent == false)
            {
                step++;
                if (_withPayloads)
                {
                    payloadOffset = scoreOffset + 1;
                    step++;
                    contentOffset++;
                }
            }
            for (var x = 1; x < a.Length; x += step)
            {
                var id = a[x].ConvertTo<string>();
                double score = 1.0;
                byte[] payload = null;
                object[] fieldValues = null;
                string[] scoreExplained = null;
                if (_withScores) score = a[x + scoreOffset].ConvertTo<double>();
                if (_withPayloads) payload = a[x + payloadOffset].ConvertTo<byte[]>();
                if (_noContent == false) fieldValues = a[x + contentOffset].ConvertTo<object[]>();
                ret.Documents.Add(Document.Load(id, score, payload, fieldValues, scoreExplained));
            }
            return ret;
        });
        public SearchResult Execute()
        {
            var cmd = GetCommandPacket();
            return _redis.Call(cmd, FetchResult);
        }
#if isasync
        public Task<SearchResult> ExecuteAsync()
        {
            var cmd = GetCommandPacket();
            return _redis.CallAsync(cmd, FetchResult);
        }
#endif

        internal bool _noContent, _verbatim, _noStopwords, _withScores, _withPayloads, _withSortKeys;
        internal List<object> _filters = new List<object>();
        internal List<object> _geoFilter = new List<object>();
        internal List<object> _inKeys = new List<object>();
        internal List<object> _inFields = new List<object>();
        internal List<string[]> _return = new List<string[]>();
        internal bool _sumarize;
        internal List<object> _sumarizeFields = new List<object>();
        internal long _sumarizeFrags = -1;
        internal long _sumarizeLen = -1;
        internal string _sumarizeSeparator;
        internal bool _highLight;
        internal List<object> _highLightFields = new List<object>();
        internal object[] _highLightTags;
        internal decimal _slop = -1;
        internal long _timeout = -1;
        internal bool _inOrder;
        internal string _language;
        internal string _expander, _scorer;
        internal bool _explainScore;
        internal string _payLoad;
        internal string _sortBy;
        internal bool _sortByDesc;
        internal long _limitOffset, _limitNum = 10;
        internal List<string> _params = new List<string>();
        internal int _dialect;

        public SearchBuilder NoContent(bool value = true)
        {
            _noContent = value;
            return this;
        }
        public SearchBuilder Verbatim(bool value = true)
        {
            _verbatim = value;
            return this;
        }
        public SearchBuilder NoStopwords(bool value = true)
        {
            _noStopwords = value;
            return this;
        }
        public SearchBuilder WithScores(bool value = true)
        {
            _withScores = value;
            return this;
        }
        public SearchBuilder WithPayloads(bool value = true)
        {
            _withPayloads = value;
            return this;
        }
        public SearchBuilder WithSortKeys(bool value = true)
        {
            _withSortKeys = value;
            return this;
        }
        public SearchBuilder Filter(string field, object min, object max)
        {
            _filters.AddRange(new object[] { field, min, max });
            return this;
        }
        public SearchBuilder GeoFilter(string field, decimal lon, decimal lat, decimal radius, GeoUnit unit = GeoUnit.m)
        {
            _geoFilter.AddRange(new object[] { field, lon, lat, radius, unit });
            return this;
        }
        public SearchBuilder InKeys(params string[] keys)
        {
            if (keys?.Any() == true) _inKeys.AddRange(keys);
            return this;
        }
        public SearchBuilder InFields(params string[] fields)
        {
            if (fields?.Any() == true) _inFields.AddRange(fields);
            return this;
        }

        public SearchBuilder Return(params string[] identifiers)
        {
            if (identifiers?.Any() == true) _return.AddRange(identifiers.Select(a => new[] { a, null }));
            return this;
        }
        public SearchBuilder Return(params KeyValuePair<string, string>[] identifierProperties)
        {
            if (identifierProperties?.Any() == true) _return.AddRange(identifierProperties.Select(a => new[] { a.Key, a.Value }));
            return this;
        }
        public SearchBuilder Sumarize(string[] fields, long frags = -1, long len = -1, string separator = null)
        {
            _sumarize = true;
            _sumarizeFields.AddRange(fields);
            _sumarizeFrags = frags;
            _sumarizeLen = len;
            _sumarizeSeparator = separator;
            return this;
        }
        public SearchBuilder HighLight(string[] fields, string tagsOpen = null, string tagsClose = null)
        {
            _highLight = true;
            _highLightFields.AddRange(fields);
            _highLightTags = new[] { tagsOpen, tagsClose };
            return this;
        }
        public SearchBuilder Slop(decimal value)
        {
            _slop = value;
            return this;
        }
        public SearchBuilder Timeout(long milliseconds)
        {
            _timeout = milliseconds;
            return this;
        }
        public SearchBuilder InOrder(bool value = true)
        {
            _inOrder = value;
            return this;
        }
        public SearchBuilder Language(string value)
        {
            _language = value;
            return this;
        }
        public SearchBuilder Expander(string value)
        {
            _expander = value;
            return this;
        }
        public SearchBuilder Scorer(string value)
        {
            _scorer = value;
            return this;
        }
        public SearchBuilder ExplainScore(bool value = true)
        {
            _explainScore = value;
            return this;
        }
        public SearchBuilder Payload(string value)
        {
            _payLoad = value;
            return this;
        }
        public SearchBuilder SortBy(string sortBy, bool desc = false)
        {
            _sortBy = sortBy;
            _sortByDesc = desc;
            return this;
        }
        public SearchBuilder Limit(long offset, long num)
        {
            _limitOffset = offset;
            _limitNum = num;
            return this;
        }
        public SearchBuilder Params(string name, string value)
        {
            _params.Add(name);
            _params.Add(value);
            return this;
        }
        public SearchBuilder Dialect(int value)
        {
            _dialect = value;
            return this;
        }
    }

    public static class SearchBuilderStringExtensions
    {
        // 替换为判断field字段是否在以(lon, lat)为圆心，半径为radius的圆内更好，但是只用于Search方法的话还是不用那么复杂的实现了
        public static bool GeoRadius(this string field, decimal lon, decimal lat, decimal radius, GeoUnit unit) => true;
        public static bool ShapeWithin(this string field, string parameterName) => true;
        public static bool ShapeContains(this string field, string parameterName) => true;
        public static bool ShapeIntersects(this string field, string parameterName) => true;
        public static bool ShapeDisjoint(this string field, string parameterName) => true;
    }
}
