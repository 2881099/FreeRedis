using System.Collections.Generic;
using System.Linq;

namespace FreeRedis.RediSearch
{
    public class AggregationResult
    {
        public long CursorId { get; }
        public Dictionary<string, object>[] Results { get; }

        public AggregationResult(object result, long cursorId = -1)
        {
            var arr = result as object[];
            Results = new Dictionary<string, object>[arr.Length - 1];
            for (int i = 1; i < arr.Length; i++)
            {
                var raw = arr[i] as object[];
                var cur = new Dictionary<string, object>();
                for (int j = 0; j < raw.Length;)
                {
                    var key = raw[j++].ConvertTo<string>();
                    var val = raw[j++];
                    cur.Add(key, val);
                }
                Results[i - 1] = cur;
            }
            CursorId = cursorId;
        }
    }

    public class AggregateBuilder
    {
        RedisClient _redis;
        string _index;
        string _query;
        internal AggregateBuilder(RedisClient redis, string index, string query)
        {
            _redis = redis;
            _index = index;
            _query = query;
            _dialect = redis.ConnectionString.FtDialect;
        }
        public AggregationResult Execute()
        {
            var cmd = "FT.SEARCH".Input(_index).Input(_query)
                .InputIf(_verbatim, "VERBATIM");
            if (_load.Any()) cmd.Input("LOAD", _load.Count).Input(_load.ToArray());
            cmd.InputIf(_timeout >= 0, "TIMEOUT", _timeout);
            if (_groupBy.Any())
            {
                cmd.Input("GROUPBY", _groupBy.Count).Input(_groupBy.ToArray());
                foreach (var reduce in _groupByReduces)
                    cmd.Input("REDUCE", reduce.Function, reduce.Arguments?.Length ?? 0).InputIf(!string.IsNullOrWhiteSpace(reduce.Alias), reduce.Alias);
            }
            if (_sortBy.Any()) cmd.InputIf(_sortBy.Any(), "SORTBY").Input(_sortBy.ToArray()).InputIf(_sortByMax > 0, "MAX", _sortByMax);
            cmd.InputIf(_applies.Any(), _applies.ToArray())
                .InputIf(_limitOffset > 0 || _limitNum != 10, "LIMIT", _limitOffset, _limitNum)
                .InputIf(!string.IsNullOrWhiteSpace(_filter), "FILTER", _filter);
            if (_withCursor) cmd.Input("WITHCURSOR").InputIf(_withCursorCount != -1, "COUNT", _withCursorCount).InputIf(_withCursorMaxIdle != -1, "MAXIDLE", _withCursorMaxIdle);
            if (_params.Any()) cmd.Input("PARAMS", _params.Count).Input(_params);
            cmd
               .InputIf(_dialect > 0, "DIALECT", _dialect);
            return _redis.Call(cmd, rt => rt.ThrowOrValue((a, _) =>
            {
                if (_withCursor) return new AggregationResult(a[0], a[1].ConvertTo<long>());
                else return new AggregationResult(a);
            }));
        }

        private bool _verbatim;
        private List<object> _load = new List<object>();
        private long _timeout = -1;
        private List<object> _groupBy = new List<object>();
        private List<AggregateReduce> _groupByReduces = new List<AggregateReduce>();
        private List<object> _sortBy = new List<object>();
        private int _sortByMax;
        private List<object> _applies = new List<object>();
        private long _limitOffset, _limitNum = 10;
        private string _filter;
        private bool _withCursor;
        private int _withCursorCount = -1;
        private long _withCursorMaxIdle = -1;
        private List<object> _params = new List<object>();
        private int _dialect;

        public AggregateBuilder Verbatim(bool value = true)
        {
            _verbatim = value;
            return this;
        }
        public AggregateBuilder Load(params string[] fields)
        {
            if (fields?.Any() == true) _load.AddRange(fields);
            return this;
        }
        public AggregateBuilder Timeout(long milliseconds)
        {
            _timeout = milliseconds;
            return this;
        }
        public AggregateBuilder GroupBy(params string[] properties)
        {
            if (properties?.Any() == true) _groupBy.AddRange(properties);
            return this;
        }
        public AggregateBuilder GroupBy(string[] properties = null, params AggregateReduce[] reduces)
        {
            if (properties?.Any() == true) _groupBy.AddRange(properties);
            if (reduces?.Any() == true) _groupByReduces.AddRange(reduces);
            return this;
        }
        public AggregateBuilder SortBy(string property, bool desc = false)
        {
            if (!string.IsNullOrWhiteSpace(property))
            {
                _sortBy.Add(property);
                if (desc) _sortBy.Add("DESC");
            }
            return this;
        }
        public AggregateBuilder SortBy(string[] properties, bool[] desc, int max = 0)
        {
            if (properties != null)
            {
                for (var a = 0; a < properties.Length; a++)
                {
                    if (!string.IsNullOrWhiteSpace(properties[a]))
                    {
                        _sortBy.Add(properties[a]);
                        if (desc != null && a < desc.Length && desc[a]) _sortBy.Add("DESC");
                    }
                }
            }
            _sortByMax = max;
            return this;
        }
        public AggregateBuilder Apply(string expression, string alias)
        {
            _applies.Add("APPLY");
            _applies.Add(expression);
            _applies.Add("AS");
            _applies.Add(alias);
            return this;
        }
        public AggregateBuilder Limit(long offset, long num)
        {
            _limitOffset = offset;
            _limitNum = num;
            return this;
        }
        public AggregateBuilder Filter(string value)
        {
            _filter = value;
            return this;
        }
        public AggregateBuilder WithCursor(int count = -1, long maxIdle = -1)
        {
            _withCursor = true;
            _withCursorCount = count;
            _withCursorMaxIdle = maxIdle;
            return this;
        }
        public AggregateBuilder Params(string name, string value)
        {
            _params.Add(name);
            _params.Add(value);
            return this;
        }
        public AggregateBuilder Dialect(int value)
        {
            _dialect = value;
            return this;
        }
    }

    public class AggregateReduce
    {
        public string Function { get; set; }
        public object[] Arguments { get; set; }
        public string Alias { get; set; }
    }
}
