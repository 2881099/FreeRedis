using System.Collections.Generic;
using System.Linq;

namespace FreeRedis.RediSearch
{
    public class AlterBuilder : SchemaBuilder<AlterBuilder>
    {
        RedisClient _redis;
        string _index;
        internal AlterBuilder(RedisClient redis, string index)
        {
            _redis = redis;
            _index = index;
        }
        public void Execute()
        {
            var cmd = "FT.ALTER".Input(_index)
                .InputIf(_skipInitialScan, "SKIPINITIALSCAN")
                .Input("SCHEMA")
                .Input(_schemaArgs.ToArray());
            _redis.Call(cmd, rt => rt.ThrowOrValue<string>() == "OK");
        }

        private bool _skipInitialScan;
        public AlterBuilder SkipInitialScan(bool value = true)
        {
            _skipInitialScan = value;
            return this;
        }
    }

    public class CreateBuilder : SchemaBuilder<CreateBuilder>
    {
        RedisClient _redis;
        string _index;
        internal CreateBuilder(RedisClient redis, string index)
        {
            _redis = redis;
            _index = index;
            _language = redis.ConnectionString.FtLanguage;
        }
        public void Execute()
        {
            var cmd = "FT.CREATE".Input(_index).InputIf(_on.HasValue, "ON", _on.ToString().ToUpper());
            if (_prefix?.Any() == true) cmd.Input("PREFIX", _prefix.Length).Input(_prefix.Select(a => (object)a).ToArray());
            cmd
                .InputIf(!string.IsNullOrWhiteSpace(_filter), "FILTER", _filter)
                .InputIf(!string.IsNullOrWhiteSpace(_language), "LANGUAGE", _language)
                .InputIf(!string.IsNullOrWhiteSpace(_languageField), "LANGUAGE_FIELD", _languageField)
                .InputIf(_score > 0, "SCORE", _score)
                .InputIf(_scoreField > 0, "SCORE_FIELD", _scoreField)
                .InputIf(!string.IsNullOrWhiteSpace(_payloadField), "PAYLOAD_FIELD", _payloadField)
                .InputIf(_maxTextFields, "MAXTEXTFIELDS")
                .InputIf(_temporary > 0, "TEMPORARY", _temporary)
                .InputIf(_noOffsets, "NOOFFSETS")
                .InputIf(_noHL, "NOHL")
                .InputIf(_noFields, "NOFIELDS")
                .InputIf(_noFreqs, "NOFREQS");
            if (_stopwords?.Any() == true) cmd.Input("STOPWORDS", _stopwords.Length).Input(_stopwords.Select(a => (object)a).ToArray());
            cmd.InputIf(_skipInitialScan, "SKIPINITIALSCAN")
                .Input("SCHEMA")
                .Input(_schemaArgs.ToArray());
            _redis.Call(cmd, rt => rt.ThrowOrValue<string>() == "OK");
        }

        private IndexDataType? _on = IndexDataType.Hash;
        private string[] _prefix;
        private string _filter;
        private string _language;
        private string _languageField;
        private decimal _score;
        private decimal _scoreField;
        private string _payloadField;
        private bool _maxTextFields;
        private long _temporary;
        private bool _noOffsets;
        private bool _noHL;
        private bool _noFields;
        private bool _noFreqs;
        private string[] _stopwords;
        private bool _skipInitialScan;
        public CreateBuilder On(IndexDataType value)
        {
            _on = value;
            return this;
        }
        public CreateBuilder Prefix(params string[] value)
        {
            _prefix = value;
            return this;
        }
        public CreateBuilder Filter(string value)
        {
            _filter = value;
            return this;
        }
        public CreateBuilder Language(string value)
        {
            _language = value;
            return this;
        }
        public CreateBuilder LanguageField(string value)
        {
            _languageField = value;
            return this;
        }
        public CreateBuilder Score(decimal value)
        {
            _score = value;
            return this;
        }
        public CreateBuilder ScoreField(decimal value)
        {
            _scoreField = value;
            return this;
        }
        public CreateBuilder PayloadField(string value)
        {
            _payloadField = value;
            return this;
        }
        public CreateBuilder MaxTextFields(bool value = true)
        {
            _maxTextFields = value;
            return this;
        }
        public CreateBuilder Temporary(long seconds)
        {
            _temporary = seconds;
            return this;
        }
        public CreateBuilder NoOffsets(bool value = true)
        {
            _noOffsets = value;
            return this;
        }
        public CreateBuilder NoHL(bool value = true)
        {
            _noHL = value;
            return this;
        }
        public CreateBuilder NoFields(bool value = true)
        {
            _noFields = value;
            return this;
        }
        public CreateBuilder NoFreqs(bool value = true)
        {
            _noFreqs = value;
            return this;
        }
        public CreateBuilder Stopwords(params string[] value)
        {
            _stopwords = value;
            return this;
        }
        public CreateBuilder SkipInitialScan(bool value = true)
        {
            _skipInitialScan = value;
            return this;
        }
    }

    public class SchemaBuilder<TBuilder> where TBuilder : class
    {
        protected List<object> _schemaArgs = new List<object>();
        public TBuilder AddTextField(string name, string alias = null, double weight = 1.0, bool sortable = false, bool unf = false, bool noStem = false,
            string phonetic = null, bool noIndex = false, bool withSuffixTrie = false, bool missingIndex = false, bool emptyIndex = false) => AddTextField(name, new TextFieldOptions
            {
                Alias = alias,
                Weight = weight,
                Sortable = sortable,
                Unf = unf,
                NoStem = noStem,
                Phonetic = phonetic,
                NoIndex = noIndex,
                WithSuffixTrie = withSuffixTrie,
                MissingIndex = missingIndex,
                EmptyIndex = emptyIndex
            });
        public TBuilder AddTextField(string name, TextFieldOptions options)
        {
            _schemaArgs.Add(name);
            if (options != null)
            {
                if (!string.IsNullOrWhiteSpace(options.Alias))
                {
                    _schemaArgs.Add("AS");
                    _schemaArgs.Add(options.Alias);
                }
                _schemaArgs.Add("TEXT");
                if (options.NoStem) _schemaArgs.Add("NOSTEM");
                if (options.NoIndex) _schemaArgs.Add("NOINDEX");
                if (options.Phonetic != null)
                {
                    _schemaArgs.Add("PHONETIC");
                    _schemaArgs.Add(options.Phonetic);
                }
                if (options.Weight != 1.0)
                {
                    _schemaArgs.Add("WEIGHT");
                    _schemaArgs.Add(options.Weight);
                }
                if (options.WithSuffixTrie) _schemaArgs.Add("WITHSUFFIXTRIE");
                if (options.Sortable) _schemaArgs.Add("SORTABLE");
                if (options.Unf) _schemaArgs.Add("UNF");
                if (options.MissingIndex) _schemaArgs.Add("INDEXMISSING");
                if (options.EmptyIndex) _schemaArgs.Add("INDEXEMPTY");
            }
            return this as TBuilder;
        }
        public TBuilder AddTagField(string name, string alias = null, bool sortable = false, bool unf = false, bool noIndex = false, string separator = ",",
            bool caseSensitive = false, bool withSuffixTrie = false, bool missingIndex = false, bool emptyIndex = false) => AddTagField(name, new TagFieldOptions
            {
                Alias = alias,
                Sortable = sortable,
                Unf = unf,
                NoIndex = noIndex,
                Separator = separator,
                CaseSensitive = caseSensitive,
                WithSuffixTrie = withSuffixTrie,
                MissingIndex = missingIndex,
                EmptyIndex = emptyIndex
            });
        public TBuilder AddTagField(string name, TagFieldOptions options)
        {
            _schemaArgs.Add(name);
            if (options != null)
            {
                if (!string.IsNullOrWhiteSpace(options.Alias))
                {
                    _schemaArgs.Add("AS");
                    _schemaArgs.Add(options.Alias);
                }
                _schemaArgs.Add("TAG");
                if (options.NoIndex) _schemaArgs.Add("NOINDEX");
                if (options.WithSuffixTrie) _schemaArgs.Add("WITHSUFFIXTRIE");
                if (options.Separator != ",")
                {

                    _schemaArgs.Add("SEPARATOR");
                    _schemaArgs.Add(options.Separator);
                }
                if (options.CaseSensitive) _schemaArgs.Add("CASESENSITIVE");
                if (options.Sortable) _schemaArgs.Add("SORTABLE");
                if (options.Unf) _schemaArgs.Add("UNF");
                if (options.MissingIndex) _schemaArgs.Add("INDEXMISSING");
                if (options.EmptyIndex) _schemaArgs.Add("INDEXEMPTY");
            }
            return this as TBuilder;
        }
        public TBuilder AddNumericField(string name, string alias = null, bool sortable = false, bool noIndex = false, bool missingIndex = false) => AddNumericField(name, new NumbericFieldOptions
        {
            Alias = alias,
            Sortable = sortable,
            NoIndex = noIndex,
            MissingIndex = missingIndex
        });
        public TBuilder AddNumericField(string name, NumbericFieldOptions options)
        {
            _schemaArgs.Add(name);
            if (options != null)
            {
                if (!string.IsNullOrWhiteSpace(options.Alias))
                {
                    _schemaArgs.Add("AS");
                    _schemaArgs.Add(options.Alias);
                }
                _schemaArgs.Add("NUMERIC");
                if (options.NoIndex) _schemaArgs.Add("NOINDEX");
                if (options.Sortable) _schemaArgs.Add("SORTABLE");
                if (options.MissingIndex) _schemaArgs.Add("INDEXMISSING");
            }
            return this as TBuilder;
        }
        public TBuilder AddGeoField(string name, string alias = null, bool sortable = false, bool noIndex = false, bool missingIndex = false) => AddGeoField(name, new GeoFieldOptions
        {
            Alias = alias,
            Sortable = sortable,
            NoIndex = noIndex,
            MissingIndex = missingIndex
        });
        public TBuilder AddGeoField(string name, GeoFieldOptions options)
        {
            _schemaArgs.Add(name);
            if (options != null)
            {
                if (!string.IsNullOrWhiteSpace(options.Alias))
                {
                    _schemaArgs.Add("AS");
                    _schemaArgs.Add(options.Alias);
                }
                _schemaArgs.Add("GEO");
                if (options.NoIndex) _schemaArgs.Add("NOINDEX");
                if (options.Sortable) _schemaArgs.Add("SORTABLE");
                if (options.MissingIndex) _schemaArgs.Add("INDEXMISSING");
            }
            return this as TBuilder;
        }
        public TBuilder AddGeoShapeField(string name, string alias = null, CoordinateSystem system = CoordinateSystem.FLAT, bool missingIndex = false) => AddGeoShapeField(name, new GeoShapeFieldOptions
        {
            Alias = alias,
            System = system,
            MissingIndex = missingIndex
        });
        public TBuilder AddGeoShapeField(string name, GeoShapeFieldOptions options)
        {
            _schemaArgs.Add(name);
            if (options != null)
            {
                if (!string.IsNullOrWhiteSpace(options.Alias))
                {
                    _schemaArgs.Add("AS");
                    _schemaArgs.Add(options.Alias);
                }
                _schemaArgs.Add("GEOSHAPE");
                _schemaArgs.Add(options.System.ToString().ToUpper());
                if (options.MissingIndex) _schemaArgs.Add("INDEXMISSING");
            }
            return this as TBuilder;
        }
        public TBuilder AddVectorField(string name, string alias = null, VectorAlgo algorithm = VectorAlgo.FLAT, Dictionary<string, object> attributes = null, bool missingIndex = false) => AddVectorField(name, new VectorFieldOptions
        {
            Alias = alias,
            Algorithm = algorithm,
            Attributes = attributes,
            MissingIndex = missingIndex
        });
        public TBuilder AddVectorField(string name, VectorFieldOptions options)
        {
            _schemaArgs.Add(name);
            if (options != null)
            {
                if (!string.IsNullOrWhiteSpace(options.Alias))
                {
                    _schemaArgs.Add("AS");
                    _schemaArgs.Add(options.Alias);
                }
                _schemaArgs.Add("VECTOR");
                _schemaArgs.Add(options.Algorithm.ToString().ToUpper());
                if (options.Attributes != null)
                {
                    _schemaArgs.Add(options.Attributes.Count * 2);
                    foreach (var attribute in options.Attributes)
                    {
                        _schemaArgs.Add(attribute.Key);
                        _schemaArgs.Add(attribute.Value);
                    }
                }
                if (options.MissingIndex) _schemaArgs.Add("INDEXMISSING");
            }
            return this as TBuilder;
        }
    }

    public enum IndexDataType { Hash, Json }
    public enum FieldType { Text, Tag, Numeric, Geo, Vector, GeoShape }
    public class TextFieldOptions
    {
        public string Alias { get; set; }
        public double Weight { get; set; } = 1.0;
        public bool NoStem { get; set; }
        public string Phonetic { get; set; }
        public bool Sortable { get; set; }
        public bool Unf { get; set; }
        public bool NoIndex { get; set; }
        public bool WithSuffixTrie { get; set; }
        public bool MissingIndex { get; set; }
        public bool EmptyIndex { get; set; }
    }
    public class TagFieldOptions
    {
        public string Alias { get; set; }
        public bool Sortable { get; set; }
        public bool Unf { get; set; }
        public bool NoIndex { get; set; }
        public string Separator { get; set; } = ",";
        public bool CaseSensitive { get; set; }
        public bool WithSuffixTrie { get; set; }
        public bool MissingIndex { get; set; }
        public bool EmptyIndex { get; set; }
    }
    public class NumbericFieldOptions
    {
        public string Alias { get; set; }
        public bool Sortable { get; set; }
        public bool NoIndex { get; set; }
        public bool MissingIndex { get; set; }
    }
    public class GeoFieldOptions
    {
        public string Alias { get; set; }
        public bool Sortable { get; set; }
        public bool NoIndex { get; set; }
        public bool MissingIndex { get; set; }
    }
    public enum CoordinateSystem { FLAT, SPHERICAL }
    public class GeoShapeFieldOptions
    {
        public string Alias { get; set; }
        public CoordinateSystem System { get; set; }
        public bool MissingIndex { get; set; }
    }
    public enum VectorAlgo { FLAT, HNSW }
    public class VectorFieldOptions
    {
        public string Alias { get; set; }
        public VectorAlgo Algorithm { get; set; }
        public Dictionary<string, object> Attributes { get; set; }
        public bool MissingIndex { get; set; }
    }
}
