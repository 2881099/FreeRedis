using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeRedis.RediSearch
{
    public class FtDocumentRepository<T>
    {
        static ConcurrentDictionary<Type, DocumentSchemaInfo> _schemaFactories = new ConcurrentDictionary<Type, DocumentSchemaInfo>();
        internal protected class DocumentSchemaInfo
        {
            public Type DocumentType { get; set; }
            public FtDocumentAttribute DocumentAttribute { get; set; }
            public PropertyInfo KeyProperty { get; set; }
            public List<DocumentSchemaFieldInfo> Fields { get; set; } = new List<DocumentSchemaFieldInfo>();
            public Dictionary<string, DocumentSchemaFieldInfo> FieldsMap { get; set; } = new Dictionary<string, DocumentSchemaFieldInfo>();
            public Dictionary<string, DocumentSchemaFieldInfo> FieldsMapRead { get; set; } = new Dictionary<string, DocumentSchemaFieldInfo>();
        }
        internal protected class DocumentSchemaFieldInfo
        {
            public DocumentSchemaInfo DocumentSchema { get; set; }
            public PropertyInfo Property { get; set; }
            public FtFieldAttribute FieldAttribute { get; set; }
            public FieldType FieldType { get; set; }
        }

        internal protected RedisClient _client;
        internal protected DocumentSchemaInfo _schema;
        public FtDocumentRepository(RedisClient client)
        {
            var type = typeof(T);
            _client = client;
            _schema = _schemaFactories.GetOrAdd(type, t =>
            {
                var fieldProprties = type.GetProperties().Select(p => new
                {
                    attribute = p.GetCustomAttributes(false).FirstOrDefault(a => a is FtFieldAttribute) as FtFieldAttribute,
                    property = p
                }).Where(a => a.attribute != null);
                var schema = new DocumentSchemaInfo
                {
                    DocumentType = type,
                };
                foreach (var fieldProperty in fieldProprties)
                {
                    var field = new DocumentSchemaFieldInfo
                    {
                        DocumentSchema = schema,
                        Property = fieldProperty.property,
                        FieldAttribute = fieldProperty.attribute,
                        FieldType = GetMapFieldType(fieldProperty.property, fieldProperty.attribute)
                    };
                    schema.Fields.Add(field);
                    schema.FieldsMap[field.Property.Name] = field;
                    schema.FieldsMapRead[field.FieldAttribute.Name] = field;
                }
                if (schema.Fields.Any() == false) throw new Exception($"Not found: [FtFieldAttribute]");
                schema.DocumentAttribute = type.GetCustomAttributes(false).FirstOrDefault(a => a is FtDocumentAttribute) as FtDocumentAttribute;
                schema.KeyProperty = type.GetProperties().FirstOrDefault(p => p.GetCustomAttributes(false).FirstOrDefault(a => a is FtKeyAttribute) != null);
                return schema;
            });
        }
        protected FieldType GetMapFieldType(PropertyInfo property, FtFieldAttribute ftattr)
        {
            //Text, Tag, Numeric, Geo, Vector, GeoShape
            if (ftattr is FtTextFieldAttribute) return FieldType.Text;
            if (ftattr is FtTagFieldAttribute) return FieldType.Tag;
            if (ftattr is FtNumericFieldAttribute) return FieldType.Numeric;
            if (ftattr is FtGeoFieldAttribute) return FieldType.Geo;
            if (ftattr is FtGeoShapeFieldAttribute) return FieldType.GeoShape;
            return FieldType.Text;
        }

        CreateBuilder GetCreateBuilder()
        {
            var attr = _schema.DocumentAttribute;
            var createBuilder = _client.FtCreate(attr.Name);
            // 组合全局前缀和文档前缀  
            var finalPrefix = _client.ConnectionString.Prefix + attr.Prefix;
            if (!string.IsNullOrWhiteSpace(finalPrefix))
                createBuilder.Prefix(finalPrefix);
            if (!string.IsNullOrWhiteSpace(attr.Filter)) createBuilder.Prefix(attr.Filter);
            if (!string.IsNullOrWhiteSpace(attr.Language)) createBuilder.Language(attr.Language);
            foreach (var field in _schema.Fields)
            {
                switch (field.FieldType)
                {
                    case FieldType.Text:
                        {
                            var ftattr = field.FieldAttribute as FtTextFieldAttribute;
                            createBuilder.AddTextField(ftattr.Name, new TextFieldOptions
                            {
                                Alias = ftattr.Alias,
                                EmptyIndex = ftattr.EmptyIndex,
                                MissingIndex = ftattr.MissingIndex,
                                NoIndex = ftattr.NoIndex,
                                NoStem = ftattr.NoStem,
                                Phonetic = ftattr.Phonetic,
                                Sortable = ftattr.Sortable,
                                Unf = ftattr.Unf,
                                Weight = ftattr.Weight,
                                WithSuffixTrie = ftattr.WithSuffixTrie,
                            });
                        }
                        break;
                    case FieldType.Tag:
                        {
                            var ftattr = field.FieldAttribute as FtTagFieldAttribute;
                            createBuilder.AddTagField(ftattr.Name, new TagFieldOptions
                            {
                                Alias = ftattr.Alias,
                                CaseSensitive = ftattr.CaseSensitive,
                                EmptyIndex = ftattr.EmptyIndex,
                                MissingIndex = ftattr.MissingIndex,
                                NoIndex = ftattr.NoIndex,
                                Separator = ftattr.Separator,
                                Sortable = ftattr.Sortable,
                                Unf = ftattr.Unf,
                                WithSuffixTrie = ftattr.WithSuffixTrie,
                            });
                        }
                        break;
                    case FieldType.Numeric:
                        {
                            var ftattr = field.FieldAttribute as FtNumericFieldAttribute;
                            createBuilder.AddNumericField(ftattr.Name, new NumbericFieldOptions
                            {
                                Alias = ftattr.Alias,
                                MissingIndex = ftattr.MissingIndex,
                                NoIndex = ftattr.NoIndex,
                                Sortable = ftattr.Sortable,
                            });
                        }
                        break;
                    case FieldType.Geo:
                        {
                            var ftattr = field.FieldAttribute as FtGeoFieldAttribute;
                            createBuilder.AddGeoField(ftattr.Name, new GeoFieldOptions
                            {
                                Alias = ftattr.Alias,
                                MissingIndex = ftattr.MissingIndex,
                                NoIndex = ftattr.NoIndex,
                                Sortable = ftattr.Sortable,
                            });

                        }
                        break;
                    case FieldType.GeoShape:
                        {
                            var ftattr = field.FieldAttribute as FtGeoShapeFieldAttribute;
                            createBuilder.AddGeoShapeField(ftattr.Name, new GeoShapeFieldOptions
                            {
                                Alias = ftattr.Alias,
                                System = ftattr.System,
                                MissingIndex = ftattr.MissingIndex
                            });
                        }
                        break;
                }
            }
            return createBuilder;
        }
        public void DropIndex(bool dd = false) => _client.FtDropIndex(_schema.DocumentAttribute.Name, dd);
        public void CreateIndex() => GetCreateBuilder().Execute();

        void Save(T doc, RedisClient.PipelineHook pipe)
        {
            var key = $"{_schema.DocumentAttribute.Prefix}{_schema.KeyProperty.GetValue(doc, null)}";
            var opts = _schema.Fields.Where((a, b) => b > 0).Select((a, b) => new object[]
            {
                a.FieldAttribute.FieldName,
                toRedisValue(a)
            }).SelectMany(a => a).ToArray();
            var field = _schema.Fields[0].FieldAttribute.FieldName;
            var value = _schema.Fields[0].Property.GetValue(doc, null);
            if (pipe != null) pipe.HMSet(key, field, value, opts);
            else _client.HMSet(key, field, value, opts);

            object toRedisValue(DocumentSchemaFieldInfo dsf)
            {
                var val = dsf.Property.GetValue(doc, null);
                if (dsf.FieldType == FieldType.Tag)
                {
                    if (dsf.Property.PropertyType.IsArrayOrList())
                        val = string.Join((dsf.FieldAttribute as FtTagFieldAttribute).Separator ?? ",", typeof(string[]).FromObject(val) as string[]);
                }
                return val;
            }
        }
        public void Save(T doc) => Save(doc, null);
        public void Save(T[] docs) => Save(docs as IEnumerable<T>);
        public void Save(IEnumerable<T> docs)
        {
            if (docs == null) return;
            using (var pipe = _client.StartPipe())
            {
                foreach (var doc in docs)
                    Save(doc, pipe);
                pipe.EndPipe();
            }
        }

        void SaveAll(T doc, RedisClient.PipelineHook pipe)
        {
            var key = $"{_schema.DocumentAttribute.Prefix}{_schema.KeyProperty.GetValue(doc, null)}";

            // 获取所有可读写的属性（排除 Key 属性和索引器属性）
            var allProperties = typeof(T).GetProperties().Where(p =>
                p != _schema.KeyProperty &&
                p.CanRead && p.CanWrite &&
                !p.GetIndexParameters().Any()).ToList();

            if (allProperties.Count == 0) return;

            // 构建字段列表
            var fieldValues = new List<object>();

            foreach (var prop in allProperties)
            {
                var fieldName = GetFieldName(prop);
                var fieldValue = GetFieldValue(prop, doc);
                fieldValues.Add(fieldName);
                fieldValues.Add(fieldValue);
            }

            if (fieldValues.Count >= 2)
            {
                var firstFieldName = (string)fieldValues[0];
                var firstFieldValue = fieldValues[1];
                var opts = fieldValues.Skip(2).ToArray();

                if (pipe != null) pipe.HMSet(key, firstFieldName, firstFieldValue, opts);
                else _client.HMSet(key, firstFieldName, firstFieldValue, opts);
            }

            string GetFieldName(PropertyInfo prop)
            {
                // 如果有 FtFieldAttribute，使用其定义的字段名，否则使用属性名
                if (_schema.FieldsMap.ContainsKey(prop.Name))
                {
                    return _schema.FieldsMap[prop.Name].FieldAttribute.FieldName;
                }
                return prop.Name;
            }

            object GetFieldValue(PropertyInfo prop, T document)
            {
                var val = prop.GetValue(document, null);

                // 如果有对应的 FtFieldAttribute，按其规则处理
                if (_schema.FieldsMap.ContainsKey(prop.Name))
                {
                    var fieldInfo = _schema.FieldsMap[prop.Name];
                    if (fieldInfo.FieldType == FieldType.Tag)
                    {
                        if (prop.PropertyType.IsArrayOrList())
                            val = string.Join((fieldInfo.FieldAttribute as FtTagFieldAttribute).Separator ?? ",", typeof(string[]).FromObject(val) as string[]);
                    }
                }

                return val;
            }
        }

        /// <summary>
        /// 保存文档的所有字段，包括没有标记 FtFieldAttribute 的字段。
        /// 没有 FtFieldAttribute 的字段将直接使用属性名称作为 Redis 字段名称。
        /// </summary>
        /// <param name="doc">要保存的文档</param>
        public void SaveAll(T doc) => SaveAll(doc, null);

        /// <summary>
        /// 保存多个文档的所有字段，包括没有标记 FtFieldAttribute 的字段。
        /// </summary>
        /// <param name="docs">要保存的文档数组</param>
        public void SaveAll(T[] docs) => SaveAll(docs as IEnumerable<T>);

        /// <summary>
        /// 保存多个文档的所有字段，包括没有标记 FtFieldAttribute 的字段。
        /// </summary>
        /// <param name="docs">要保存的文档集合</param>
        public void SaveAll(IEnumerable<T> docs)
        {
            if (docs == null) return;
            using (var pipe = _client.StartPipe())
            {
                foreach (var doc in docs)
                    SaveAll(doc, pipe);
                pipe.EndPipe();
            }
        }

        /// <summary>
        /// 获取文档的所有字段，包括没有标记 FtFieldAttribute 的字段。
        /// 使用与 Search 方法相同的反序列化逻辑。
        /// </summary>
        /// <param name="id">文档ID</param>
        /// <returns>文档对象，如果不存在则返回 null</returns>
        public T Get(object id)
        {
            if (id == null) return default(T);

            var key = $"{_schema.DocumentAttribute.Prefix}{id}";
            var fieldValues = _client.HGetAll(key);

            if (fieldValues == null || fieldValues.Count == 0) return default(T);

            // 转换为与 Search 方法兼容的格式
            var convertedValues = fieldValues.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
            return DeserializeDocumentCore(id.ToString(), convertedValues);
        }

        /// <summary>
        /// 批量获取文档的所有字段，包括没有标记 FtFieldAttribute 的字段。
        /// </summary>
        /// <param name="ids">文档ID数组</param>
        /// <returns>文档对象数组</returns>
        public T[] Get(params object[] ids)
        {
            if (ids == null || ids.Length == 0) return new T[0];
            return ids.Select(id => Get(id)).Where(doc => doc != null).ToArray();
        }

        /// <summary>
        /// 统一的文档反序列化核心方法，与 Search 方法使用相同的逻辑
        /// </summary>
        /// <param name="id">文档ID</param>
        /// <param name="fieldValues">字段值字典</param>
        /// <returns>反序列化后的文档对象</returns>
        internal T DeserializeDocumentCore(string id, Dictionary<string, object> fieldValues, bool isFromReturnClause = false)
        {
            var ttype = typeof(T);
            var prefix = _schema.DocumentAttribute.Prefix;
            var keyProperty = _schema.KeyProperty;

            var item = (T)ttype.CreateInstanceGetDefaultValue();

            foreach (var kv in fieldValues)
            {
                var name = kv.Key.Replace("-", "_");
                DocumentSchemaFieldInfo field = null;

                if (isFromReturnClause)
                {
                    var prop = ttype.GetPropertyOrFieldIgnoreCase(name);
                    if (prop == null || !_schema.FieldsMap.TryGetValue(prop.Name, out field))
                    {
                        // 如果没从FieldsMap中找到，但是找到了prop，也尝试赋值，可能是Redis中Index与字段的FtFieldAttribute不一致
                        if (prop != null && kv.Value != null)
                            SetPropertyOrFieldValue(item, prop, kv.Value);
                        continue;
                    }
                }
                else
                {
                    if (!_schema.FieldsMapRead.TryGetValue(name, out field))
                    {
                        // 对于没有 FtFieldAttribute 的字段，直接通过属性名设置
                        var prop = ttype.GetPropertyOrFieldIgnoreCase(name);
                        if (prop != null && kv.Value != null) 
                            SetPropertyOrFieldValue(item, prop, kv.Value);
                        continue;
                    }
                }

                if (field == null || kv.Value == null) continue;

                // 处理 Tag 类型的特殊逻辑
                if (kv.Value is string valstr && field.FieldType == FieldType.Tag)
                {
                    var convertedValue = field.Property.PropertyType.IsArrayOrList() ?
                        field.Property.PropertyType.FromObject(valstr.Split(new[] { (field.FieldAttribute as FtTagFieldAttribute).Separator ?? "," }, StringSplitOptions.None)) :
                        valstr;
                    ttype.SetPropertyOrFieldValue(item, field.Property.Name, convertedValue);
                }
                else
                {
                    ttype.SetPropertyOrFieldValue(item, field.Property.Name, field.Property.PropertyType.FromObject(kv.Value));
                }
            }

            // 设置key属性
            if (keyProperty != null)
            {
                var itemId = id;
                if (!string.IsNullOrEmpty(prefix) && itemId.StartsWith(prefix))
                    itemId = itemId.Substring(prefix.Length);
                ttype.SetPropertyOrFieldValue(item, keyProperty.Name, keyProperty.PropertyType.FromObject(itemId));
            }

            return item;

            void SetPropertyOrFieldValue(object target, MemberInfo member, object value)
            {
                bool canWrite = false;
                Type memberType = null;

                if (member is PropertyInfo propInfo)
                {
                    canWrite = propInfo.CanWrite;
                    memberType = propInfo.PropertyType;
                }
                else if (member is FieldInfo fieldInfo)
                {
                    canWrite = !fieldInfo.IsInitOnly && !fieldInfo.IsLiteral;
                    memberType = fieldInfo.FieldType;
                }

                if (canWrite && memberType != null)
                {
                    var convertedValue = ConvertValue(value.ToString(), memberType);
                    ttype.SetPropertyOrFieldValue(target, member.Name, convertedValue);
                }
            }
        }

        private object ConvertValue(string stringValue, Type targetType)
        {
            if (string.IsNullOrEmpty(stringValue)) return null;

            // 处理可空类型
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                targetType = underlyingType;
            }

            // 基本类型转换
            if (targetType == typeof(string)) return stringValue;
            if (targetType == typeof(int)) return int.Parse(stringValue);
            if (targetType == typeof(long)) return long.Parse(stringValue);
            if (targetType == typeof(decimal)) return decimal.Parse(stringValue);
            if (targetType == typeof(double)) return double.Parse(stringValue);
            if (targetType == typeof(float)) return float.Parse(stringValue);
            if (targetType == typeof(bool)) return bool.Parse(stringValue);
            if (targetType == typeof(DateTime)) return DateTime.Parse(stringValue);
            if (targetType.IsEnum) return Enum.Parse(targetType, stringValue);

            // 尝试使用 Convert.ChangeType
            try
            {
                return Convert.ChangeType(stringValue, targetType);
            }
            catch
            {
                return null;
            }
        }



        public long Delete(params long[] id)
        {
            if (id == null || id.Length == 0) return 0;
            return _client.Del(id.Select(a => $"{_schema.DocumentAttribute.Prefix}{a}").ToArray());
        }
        public long Delete(params string[] id)
        {
            if (id == null || id.Length == 0) return 0;
            return _client.Del(id.Select(a => $"{_schema.DocumentAttribute.Prefix}{a}").ToArray());
        }

#if isasync
        public Task DropIndexAsync(bool dd = false) => _client.FtDropIndexAsync(_schema.DocumentAttribute.Name, dd);
        public Task CreateIndexAsync() => GetCreateBuilder().ExecuteAsync();

        async Task SaveAsync(T doc, RedisClient.PipelineHook pipe)
        {
            var key = $"{_schema.DocumentAttribute.Prefix}{_schema.KeyProperty.GetValue(doc, null)}";
            var opts = _schema.Fields.Where((a, b) => b > 0).Select((a, b) => new object[]
            {
                a.FieldAttribute.FieldName,
                toRedisValue(a)
            }).SelectMany(a => a).ToArray();
            var field = _schema.Fields[0].FieldAttribute.FieldName;
            var value = _schema.Fields[0].Property.GetValue(doc, null);
            if (pipe != null) pipe.HMSet(key, field, value, opts);
            else await _client.HMSetAsync(key, field, value, opts);

            object toRedisValue(DocumentSchemaFieldInfo dsf)
            {
                var val = dsf.Property.GetValue(doc, null);
                if (dsf.FieldType == FieldType.Tag)
                {
                    if (dsf.Property.PropertyType.IsArrayOrList())
                        val = string.Join((dsf.FieldAttribute as FtTagFieldAttribute).Separator ?? ",", typeof(string[]).FromObject(val) as string[]);
                }
                return val;
            }
        }
        public Task SaveAsync(T doc) => SaveAsync(doc, null);
        public Task SaveAsync(T[] docs) => SaveAsync(docs as IEnumerable<T>);
        async public Task SaveAsync(IEnumerable<T> docs)
        {
            if (docs == null) return;
            using (var pipe = _client.StartPipe())
            {
                foreach (var doc in docs)
                    await SaveAsync(doc, pipe);
                pipe.EndPipe();
            }
        }

        async Task SaveAllAsync(T doc, RedisClient.PipelineHook pipe)
        {
            var key = $"{_schema.DocumentAttribute.Prefix}{_schema.KeyProperty.GetValue(doc, null)}";

            // 获取所有可读写的属性（排除 Key 属性和索引器属性）
            var allProperties = typeof(T).GetProperties().Where(p =>
                p != _schema.KeyProperty &&
                p.CanRead && p.CanWrite &&
                !p.GetIndexParameters().Any()).ToList();

            if (allProperties.Count == 0) return;

            // 构建字段列表
            var fieldValues = new List<object>();

            foreach (var prop in allProperties)
            {
                var fieldName = GetFieldName(prop);
                var fieldValue = GetFieldValue(prop, doc);
                fieldValues.Add(fieldName);
                fieldValues.Add(fieldValue);
            }

            if (fieldValues.Count >= 2)
            {
                var firstFieldName = (string)fieldValues[0];
                var firstFieldValue = fieldValues[1];
                var opts = fieldValues.Skip(2).ToArray();

                if (pipe != null) pipe.HMSet(key, firstFieldName, firstFieldValue, opts);
                else await _client.HMSetAsync(key, firstFieldName, firstFieldValue, opts);
            }

            string GetFieldName(PropertyInfo prop)
            {
                // 如果有 FtFieldAttribute，使用其定义的字段名，否则使用属性名
                if (_schema.FieldsMap.ContainsKey(prop.Name))
                {
                    return _schema.FieldsMap[prop.Name].FieldAttribute.FieldName;
                }
                return prop.Name;
            }

            object GetFieldValue(PropertyInfo prop, T document)
            {
                var val = prop.GetValue(document, null);

                // 如果有对应的 FtFieldAttribute，按其规则处理
                if (_schema.FieldsMap.ContainsKey(prop.Name))
                {
                    var fieldInfo = _schema.FieldsMap[prop.Name];
                    if (fieldInfo.FieldType == FieldType.Tag)
                    {
                        if (prop.PropertyType.IsArrayOrList())
                            val = string.Join((fieldInfo.FieldAttribute as FtTagFieldAttribute).Separator ?? ",", typeof(string[]).FromObject(val) as string[]);
                    }
                }

                return val;
            }
        }

        /// <summary>
        /// 异步保存文档的所有字段，包括没有标记 FtFieldAttribute 的字段。
        /// 没有 FtFieldAttribute 的字段将直接使用属性名称作为 Redis 字段名称。
        /// </summary>
        /// <param name="doc">要保存的文档</param>
        public Task SaveAllAsync(T doc) => SaveAllAsync(doc, null);

        /// <summary>
        /// 异步保存多个文档的所有字段，包括没有标记 FtFieldAttribute 的字段。
        /// </summary>
        /// <param name="docs">要保存的文档数组</param>
        public Task SaveAllAsync(T[] docs) => SaveAllAsync(docs as IEnumerable<T>);

        /// <summary>
        /// 异步保存多个文档的所有字段，包括没有标记 FtFieldAttribute 的字段。
        /// </summary>
        /// <param name="docs">要保存的文档集合</param>
        async public Task SaveAllAsync(IEnumerable<T> docs)
        {
            if (docs == null) return;
            using (var pipe = _client.StartPipe())
            {
                foreach (var doc in docs)
                    await SaveAllAsync(doc, pipe);
                pipe.EndPipe();
            }
        }

        /// <summary>
        /// 异步获取文档的所有字段，包括没有标记 FtFieldAttribute 的字段。
        /// 使用与 Search 方法相同的反序列化逻辑。
        /// </summary>
        /// <param name="id">文档ID</param>
        /// <returns>文档对象，如果不存在则返回 null</returns>
        public async Task<T> GetAsync(object id)
        {
            if (id == null) return default(T);

            var key = $"{_schema.DocumentAttribute.Prefix}{id}";
            var fieldValues = await _client.HGetAllAsync(key);

            if (fieldValues == null || fieldValues.Count == 0) return default(T);

            // 转换为与 Search 方法兼容的格式
            var convertedValues = fieldValues.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
            return DeserializeDocumentCore(id.ToString(), convertedValues);
        }

        /// <summary>
        /// 异步批量获取文档的所有字段，包括没有标记 FtFieldAttribute 的字段。
        /// </summary>
        /// <param name="ids">文档ID数组</param>
        /// <returns>文档对象数组</returns>
        public async Task<T[]> GetAsync(params object[] ids)
        {
            if (ids == null || ids.Length == 0) return new T[0];

            var tasks = ids.Select(id => GetAsync(id));
            var results = await Task.WhenAll(tasks);
            return results.Where(doc => doc != null).ToArray();
        }



        async public Task<long> DeleteAsync(params long[] id)
        {
            if (id == null || id.Length == 0) return 0;
            return await _client.DelAsync(id.Select(a => $"{_schema.DocumentAttribute.Prefix}{a}").ToArray());
        }
        async public Task<long> DeleteAsync(params string[] id)
        {
            if (id == null || id.Length == 0) return 0;
            return await _client.DelAsync(id.Select(a => $"{_schema.DocumentAttribute.Prefix}{a}").ToArray());
        }
#endif

        public FtDocumentRepositorySearchBuilder<T> Search(Expression<Func<T, bool>> query) => Search(ParseQueryExpression(query.Body, null));
        public FtDocumentRepositorySearchBuilder<T> Search(string query = "*")
        {
            if (string.IsNullOrEmpty(query)) query = "*";
            var q = _client.FtSearch(_schema.DocumentAttribute.Name, query);
            if (!string.IsNullOrEmpty(_schema.DocumentAttribute.Language)) q.Language(_schema.DocumentAttribute.Language);
            return new FtDocumentRepositorySearchBuilder<T>(this, _schema.DocumentAttribute.Name, query);
        }

        internal protected int ToTimestamp(DateTime dt) => (int)dt.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        internal protected List<KeyValuePair<string, string>> ParseSelectorExpression(Expression selector, bool isQuoteFieldName = false)
        {
            var fieldValues = new List<KeyValuePair<string, string>>();
            if (selector is LambdaExpression lambdaExp) selector = lambdaExp.Body;
            if (selector is UnaryExpression unaryExp) selector = unaryExp.Operand;
            switch (selector.NodeType)
            {
                case ExpressionType.MemberAccess:
                    {
                        var memExp = selector as MemberExpression;
                        var left = memExp.Member.Name;
                        var right = ParseQueryExpression(memExp, new ParseQueryExpressionOptions { IsQuoteFieldName = isQuoteFieldName });
                        fieldValues.Add(new KeyValuePair<string, string>(left, right));
                    }
                    break;
                case ExpressionType.New:
                    {
                        var newExp = selector as NewExpression;
                        for (var a = 0; a < newExp?.Members?.Count; a++)
                        {
                            var left = newExp.Members[a].Name;
                            var right = ParseQueryExpression(newExp.Arguments[a], new ParseQueryExpressionOptions { IsQuoteFieldName = isQuoteFieldName });
                            fieldValues.Add(new KeyValuePair<string, string>(left, right));
                        }
                    }
                    break;
                case ExpressionType.MemberInit:
                    {
                        var initExp = selector as MemberInitExpression;
                        for (var a = 0; a < initExp?.Bindings.Count; a++)
                        {
                            var initAssignExp = (initExp.Bindings[a] as MemberAssignment);
                            if (initAssignExp == null) continue;
                            var left = initAssignExp.Member.Name;
                            var right = ParseQueryExpression(initAssignExp.Expression, new ParseQueryExpressionOptions { IsQuoteFieldName = isQuoteFieldName });
                            fieldValues.Add(new KeyValuePair<string, string>(left, right));
                        }
                    }
                    break;
            }
            return fieldValues;
        }
        internal protected class ParseQueryExpressionOptions
        {
            public bool IsQuoteFieldName { get; set; } = true;
            public Func<MemberExpression, string> DiyParse { get; set; }
        }
        internal protected string ParseQueryExpression(Expression exp, ParseQueryExpressionOptions options)
        {
            if (options == null) options = new ParseQueryExpressionOptions();
            string parseExp(Expression thenExp) => ParseQueryExpression(thenExp, options);
            string toFt(object obj) => string.Format(CultureInfo.InvariantCulture, "{0}", toFtObject(obj));
            object toFtObject(object param)
            {
                if (param == null) return "NULL";

                if (param is bool || param is bool?)
                    return (bool)param ? 1 : 0;
                else if (param is string str)
                    return string.Concat("'", str.Replace("\\", "\\\\").Replace("'", "\\'"), "'");
                else if (param is char chr)
                    return string.Concat("'", chr.ToString().Replace("\\", "\\\\").Replace("'", "\\'").Replace('\0', ' '), "'");
                else if (param is Enum enm)
                    return string.Concat("'", enm.ToString().Replace("\\", "\\\\").Replace("'", "\\'").Replace(", ", ","), "'");
                else if (decimal.TryParse(string.Concat(param), out var trydec))
                    return param;

                else if (param is DateTime || param is DateTime?)
                    return ToTimestamp((DateTime)param);

                return string.Concat("\"", param.ToString().Replace("\\", "\\\\").Replace("'", "\\'"), "'");
            }
            string toFtTagString(string expResultStr)
            {
                if (expResultStr == null) return "";
                if (expResultStr.StartsWith("'") && expResultStr.EndsWith("'"))
                    return expResultStr.Substring(1, expResultStr.Length - 2)
                        .Replace("\\'", "'").Replace("\\\\", "\\");
                return expResultStr;
            }
            if (exp == null) return "";

            switch (exp.NodeType)
            {
                case ExpressionType.Not:
                    var notExp = (exp as UnaryExpression)?.Operand;
                    return $"-({parseExp(notExp)})";
                case ExpressionType.Quote: return parseExp((exp as UnaryExpression)?.Operand);
                case ExpressionType.Lambda: return parseExp((exp as LambdaExpression)?.Body);
                case ExpressionType.Invoke:
                    var invokeExp = exp as InvocationExpression;
                    var invokeReplaceExp = invokeExp.Expression;
                    var invokeLambdaExp = invokeReplaceExp as LambdaExpression;
                    if (invokeLambdaExp == null) return toFt(Expression.Lambda(exp).Compile().DynamicInvoke());
                    var invokeReplaceVistor = new ReplaceVisitor();
                    var len = Math.Min(invokeExp.Arguments.Count, invokeLambdaExp.Parameters.Count);
                    for (var a = 0; a < len; a++)
                        invokeReplaceExp = invokeReplaceVistor.Modify(invokeReplaceExp, invokeLambdaExp.Parameters[a], invokeExp.Arguments[a]);
                    return parseExp(invokeReplaceExp);
                case ExpressionType.TypeAs:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    var expOperand = (exp as UnaryExpression)?.Operand;
                    if (expOperand.Type.NullableTypeOrThis().IsEnum && exp.IsParameter() == false)
                        return toFt(Expression.Lambda(exp).Compile().DynamicInvoke());
                    return parseExp(expOperand);
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked: return $"-({parseExp((exp as UnaryExpression)?.Operand)})";
                case ExpressionType.Constant: return toFt((exp as ConstantExpression)?.Value);
                case ExpressionType.Conditional:
                    var condExp = exp as ConditionalExpression;
                    if (condExp.Test.IsParameter())
                        return "";
                    if ((bool)Expression.Lambda(condExp.Test).Compile().DynamicInvoke())
                        return parseExp(condExp.IfTrue);
                    else
                        return parseExp(condExp.IfFalse);
                case ExpressionType.MemberAccess:
                    var memberExp = exp as MemberExpression;
                    var memberType = memberExp.Expression?.Type ?? memberExp.Type;
                    var memberParseResult = "";
                    switch (memberType.FullName)
                    {
                        case "System.String": memberParseResult = ParseMemberAccessString(); break;
                        case "System.DateTime": memberParseResult = ParseMemberAccessDateTime(); break;
                    }
                    if (string.IsNullOrEmpty(memberParseResult) == false) return memberParseResult;

                    if (memberExp.IsParameter() == false) return toFt(Expression.Lambda(exp).Compile().DynamicInvoke());
                    if (memberExp.Expression.NodeType == ExpressionType.Parameter)
                    {
                        if (_schema.KeyProperty.Name == memberExp.Member.Name)
                            return options.IsQuoteFieldName ? $"@__key" : "__key";
                        if (_schema.FieldsMap.TryGetValue(memberExp.Member.Name, out var field))
                            return options.IsQuoteFieldName ? $"@{field.FieldAttribute.FieldName}" : field.FieldAttribute.FieldName;
                    }
                    else
                    {
                        return options?.DiyParse(memberExp);
                    }
                    break;

                    string ParseMemberAccessString()
                    {
                        if (memberExp.Expression != null)
                        {
                            var left = parseExp(memberExp.Expression);
                            switch (memberExp.Member.Name)
                            {
                                case "Length": return $"strlen({left})";
                            }
                        }
                        return null;
                    }
                    string ParseMemberAccessDateTime()
                    {
                        if (memberExp.Expression == null)
                        {
                            switch (memberExp.Member.Name)
                            {
                                case "Now": return ToTimestamp(DateTime.Now).ToString();
                                case "UtcNow": return ToTimestamp(DateTime.UtcNow).ToString();
                                case "Today": return ToTimestamp(DateTime.Today).ToString();
                                case "MinValue": return "0";
                                case "MaxValue": return ToTimestamp(new DateTime(2050, 1, 1)).ToString();
                            }
                            return null;
                        }
                        var left = parseExp(memberExp.Expression);
                        switch (memberExp.Member.Name)
                        {
                            case "Date": return $"timefmt({left},'%Y-%m-%d')";
                            case "TimeOfDay": return $"timefmt({left},'%H:%M:%S')";
                            case "DayOfWeek": return $"dayofweek({left})";
                            case "Day": return $"dayofmonth({left})";
                            case "DayOfYear": return $"dayofyear({left})+1";
                            case "Month": return $"month({left})";
                            case "Year": return $"year({left})";
                            case "Hour": return $"hour({left})";
                            case "Minute": return $"minute({left})";
                            case "Second": return $"timefmt({left},'%S')";
                        }
                        return null;
                    }
                case ExpressionType.Call:
                    var callExp = exp as MethodCallExpression;
                    var callType = callExp.Object?.Type ?? callExp.Method.DeclaringType;
                    var callParseResult = "";
                    switch (callType.FullName)
                    {
                        case "System.String": callParseResult = ParseCallString(); break;
                        case "System.Math": callParseResult = ParseCallMath(); break;
                        case "System.DateTime": callParseResult = ParseCallDateTime(); break;
                        case "FreeRedis.RediSearch.SearchBuilderStringExtensions":
                            callParseResult = ParseCallStringExtension();
                            break;
                        default: callParseResult = ParseCallOther(); break;
                    }
                    if (!string.IsNullOrEmpty(callParseResult)) return callParseResult;
                    break;

                    string ParseCallString()
                    {
                        if (callExp.Object != null)
                        {
                            var left = parseExp(callExp.Object);
                            switch (callExp.Method.Name)
                            {
                                case "StartsWith":
                                case "EndsWith":
                                case "Contains":
                                    var right = parseExp(callExp.Arguments[0]);
                                    if (right.StartsWith("'"))
                                    {
                                        switch (callExp.Method.Name)
                                        {
                                            case "StartsWith":
                                            case "Contains":
                                                right = $"'*{right.Substring(1)}";
                                                break;
                                        }
                                    }
                                    if (right.EndsWith("'"))
                                    {
                                        switch (callExp.Method.Name)
                                        {
                                            case "EndsWith":
                                            case "Contains":
                                                right = $"{right.Substring(0, right.Length - 1)}*'";
                                                break;
                                        }
                                    }
                                    return $"{left}:{right}";
                                case "ToLower": return $"lower({left})";
                                case "ToUpper": return $"upper({left})";
                                case "Substring":
                                    var substrArgs1 = parseExp(callExp.Arguments[0]);
                                    if (callExp.Arguments.Count == 1) return $"substr({left},{substrArgs1},-1)";
                                    return $"substr({left},{substrArgs1},{parseExp(callExp.Arguments[1])})";
                                case "Equals":
                                    var equalRight = parseExp(callExp.Arguments[0]);
                                    return $"{left}:[{equalRight} {equalRight}]";
                            }
                        }
                        return null;
                    }
                    string ParseCallStringExtension()
                    {
                        var left = parseExp(callExp.Arguments[0]);
                        switch (callExp.Method.Name)
                        {
                            case "GeoRadius":
                                var lon = parseExp(callExp.Arguments[1]);
                                var lat = parseExp(callExp.Arguments[2]);
                                var radius = parseExp(callExp.Arguments[3]);
                                var unit = parseExp(callExp.Arguments[4]);
                                return $"{left}:[{lon} {lat} {radius} {unit.Replace("'", "")}]";
                            case "ShapeWithin":
                                {
                                    var parameterName = parseExp(callExp.Arguments[1]);
                                    return $"{left}:[WITHIN ${parameterName.Replace("'", "")}]";
                                }
                            case "ShapeContains":
                                {
                                    var parameterName = parseExp(callExp.Arguments[1]);
                                    return $"{left}:[CONTAINS ${parameterName.Replace("'", "")}]";
                                }
                            case "ShapeIntersects":
                                {
                                    var parameterName = parseExp(callExp.Arguments[1]);
                                    return $"{left}:[INTERSECTS ${parameterName.Replace("'", "")}]";
                                }
                            case "ShapeDisjoint":
                                {
                                    var parameterName = parseExp(callExp.Arguments[1]);
                                    return $"{left}:[DISJOINT ${parameterName.Replace("'", "")}]";
                                }
                        }
                        return null;
                    }
                    string ParseCallMath()
                    {
                        switch (callExp.Method.Name)
                        {
                            case "Abs": return $"abs({parseExp(callExp.Arguments[0])})";
                            case "Sign": return $"sign({parseExp(callExp.Arguments[0])})";
                            case "Floor": return $"floor({parseExp(callExp.Arguments[0])})";
                            case "Ceiling": return $"ceiling({parseExp(callExp.Arguments[0])})";
                            case "Round":
                                if (callExp.Arguments.Count > 1 && callExp.Arguments[1].Type.FullName == "System.Int32") return $"round({parseExp(callExp.Arguments[0])}, {parseExp(callExp.Arguments[1])})";
                                return $"round({parseExp(callExp.Arguments[0])})";
                            case "Exp": return $"exp({parseExp(callExp.Arguments[0])})";
                            case "Log": return $"log({parseExp(callExp.Arguments[0])})";
                            case "Log10": return $"log10({parseExp(callExp.Arguments[0])})";
                            case "Pow": return $"pow({parseExp(callExp.Arguments[0])}, {parseExp(callExp.Arguments[1])})";
                            case "Sqrt": return $"sqrt({parseExp(callExp.Arguments[0])})";
                            case "Cos": return $"cos({parseExp(callExp.Arguments[0])})";
                            case "Sin": return $"sin({parseExp(callExp.Arguments[0])})";
                            case "Tan": return $"tan({parseExp(callExp.Arguments[0])})";
                            case "Acos": return $"acos({parseExp(callExp.Arguments[0])})";
                            case "Asin": return $"asin({parseExp(callExp.Arguments[0])})";
                            case "Atan": return $"atan({parseExp(callExp.Arguments[0])})";
                            case "Atan2": return $"atan2({parseExp(callExp.Arguments[0])}, {parseExp(callExp.Arguments[1])})";
                            case "Truncate": return $"truncate({parseExp(callExp.Arguments[0])}, 0)";
                        }
                        return null;
                    }
                    string ParseCallDateTime()
                    {
                        if (callExp.Object != null)
                        {
                            var left = parseExp(callExp.Object);
                            var args1 = callExp.Arguments.Count == 0 ? null : parseExp(callExp.Arguments[0]);
                            switch (callExp.Method.Name)
                            {
                                case "Equals": return $"{left}:[{args1} {args1}]";
                                case "ToString":
                                    if (callExp.Arguments.Count == 0) return $"timefmt({left},'%Y-%m-%d %H:%M:%S')";
                                    switch (args1)
                                    {
                                        case "'yyyy-MM-dd HH:mm:ss'": return $"timefmt({left},'%Y-%m-%d %H:%M:%S')";
                                        case "'yyyy-MM-dd HH:mm'": return $"timefmt({left},'%Y-%m-%d %H:%M')";
                                        case "'yyyy-MM-dd HH'": return $"timefmt({left},'%Y-%m-%d %H')";
                                        case "'yyyy-MM-dd'": return $"timefmt({left},'%Y-%m-%d')";
                                        case "'yyyy-MM'": return $"timefmt({left},'%Y-%m')";
                                        case "'yyyyMMddHHmmss'": return $"timefmt({left},'%Y%m%d%H%M%S')";
                                        case "'yyyyMMddHHmm'": return $"timefmt({left},'%Y%m%d%H%M')";
                                        case "'yyyyMMddHH'": return $"timefmt({left},'%Y%m%d%H')";
                                        case "'yyyyMMdd'": return $"timefmt({left},'%Y%m%d')";
                                        case "'yyyyMM'": return $"timefmt({left},'%Y%m')";
                                        case "'yyyy'": return $"timefmt({left},'%Y')";
                                        case "'HH:mm:ss'": return $"timefmt({left},'%H:%M:%S')";
                                    }
                                    args1 = Regex.Replace(args1, "(yyyy|MM|dd|HH|mm|ss)", m =>
                                    {
                                        switch (m.Groups[1].Value)
                                        {
                                            case "yyyy": return $"%Y";
                                            case "MM": return $"%m";
                                            case "dd": return $"%d";
                                            case "HH": return $"%H";
                                            case "mm": return $"%M";
                                            case "ss": return $"%S";
                                        }
                                        return m.Groups[0].Value;
                                    });
                                    return args1;
                            }
                        }
                        return null;
                    }
                    string ParseCallOther()
                    {
                        var objExp = callExp.Object;
                        var objType = objExp?.Type;
                        if (objType?.FullName == "System.Byte[]") return null;

                        var argIndex = 0;
                        if (objType == null && callExp.Method.DeclaringType == typeof(Enumerable))
                        {
                            objExp = callExp.Arguments.FirstOrDefault();
                            objType = objExp?.Type;
                            argIndex++;

                            if (objType == typeof(string))
                            {
                                switch (callExp.Method.Name)
                                {
                                    case "First":
                                    case "FirstOrDefault":
                                        return $"substr({parseExp(callExp.Arguments[0])},0,1)";
                                }
                            }
                        }
                        if (objType == null) objType = callExp.Method.DeclaringType;
                        if (objType != null || objType.IsArrayOrList())
                        {
                            string left = null;
                            switch (callExp.Method.Name)
                            {
                                case "Contains":

                                    left = objExp == null ? null : parseExp(objExp);
                                    var args1 = parseExp(callExp.Arguments[argIndex]);
                                    return $"{left}:{{{toFtTagString(args1)}}}";
                            }
                        }
                        return null;
                    }
            }
            if (exp is BinaryExpression expBinary && expBinary != null)
            {
                switch (expBinary.NodeType)
                {
                    case ExpressionType.OrElse:
                    case ExpressionType.Or:
                        return $"({parseExp(expBinary.Left)}|{parseExp(expBinary.Right)})";
                    case ExpressionType.AndAlso:
                    case ExpressionType.And:
                        return $"{parseExp(expBinary.Left)} {parseExp(expBinary.Right)}";

                    case ExpressionType.GreaterThan:
                        return $"{parseExp(expBinary.Left)}:[({parseExp(expBinary.Right)} +inf]";
                    case ExpressionType.GreaterThanOrEqual:
                        return $"{parseExp(expBinary.Left)}:[{parseExp(expBinary.Right)} +inf]";
                    case ExpressionType.LessThan:
                        return $"{parseExp(expBinary.Left)}:[-inf ({parseExp(expBinary.Right)}]";
                    case ExpressionType.LessThanOrEqual:
                        return $"{parseExp(expBinary.Left)}:[-inf {parseExp(expBinary.Right)}]";
                    case ExpressionType.NotEqual:
                    case ExpressionType.Equal:
                        var equalRight = parseExp(expBinary.Right);
                        if (ParseTryGetField(expBinary.Left, out var field))
                        {
                            if (field.FieldType == FieldType.Text)
                                return $"{(expBinary.NodeType == ExpressionType.NotEqual ? "-" : "")}{parseExp(expBinary.Left)}:{equalRight}";
                            else if (field.FieldType == FieldType.Tag)
                                return $"{(expBinary.NodeType == ExpressionType.NotEqual ? "-" : "")}{parseExp(expBinary.Left)}:{{{toFtTagString(equalRight)}}}";
                        }
                        return $"{(expBinary.NodeType == ExpressionType.NotEqual ? "-" : "")}{parseExp(expBinary.Left)}:[{equalRight} {equalRight}]";
                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                        return $"({parseExp(expBinary.Left)}+{parseExp(expBinary.Right)})";
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                        return $"({parseExp(expBinary.Left)}-{parseExp(expBinary.Right)})";
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                        return $"{parseExp(expBinary.Left)}*{parseExp(expBinary.Right)}";
                    case ExpressionType.Divide:
                        return $"{parseExp(expBinary.Left)}/{parseExp(expBinary.Right)}";
                    case ExpressionType.Modulo:
                        return $"{parseExp(expBinary.Left)}%{parseExp(expBinary.Right)}";
                }
            }
            if (exp.IsParameter() == false) return toFt(Expression.Lambda(exp).Compile().DynamicInvoke());
            throw new Exception($"Unable to parse this expression: {exp}");
        }
        internal protected bool ParseTryGetField(Expression exp, out DocumentSchemaFieldInfo field)
        {
            field = null;
            if (exp == null) return false;
            if (exp.NodeType != ExpressionType.MemberAccess) return false;
            var memberExp = exp as MemberExpression;
            if (memberExp == null) return false;
            if (memberExp.Expression.IsParameter() == false) return false;
            return _schema.FieldsMap.TryGetValue(memberExp.Member.Name, out field);
        }
    }

    /// <summary>
    /// 标记类为 RediSearch 文档类型，定义索引的基本配置信息。
    /// 每个用于 FtDocumentRepository 的实体类都必须标记此特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class FtDocumentAttribute : Attribute
    {
        /// <summary>
        /// 获取或设置索引的名称。这是在 Redis 中创建的索引的唯一标识符。
        /// 索引名称必须在 Redis 实例中唯一。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置文档键的前缀。只有具有此前缀的 Redis 键才会被包含在索引中。
        /// 例如，如果设置为 "user:"，则只有 "user:123"、"user:456" 等键会被索引。
        /// 前缀有助于区分不同类型的文档和提高索引性能。
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// 获取或设置文档过滤器表达式。只有满足此过滤条件的文档才会被包含在索引中。
        /// 过滤器使用 RediSearch 查询语法，可以基于字段值进行过滤。
        /// 例如："@status:active" 只索引状态为 active 的文档。
        /// </summary>
        public string Filter { get; set; }

        /// <summary>
        /// 获取或设置文档的默认语言。影响文本分析、词干提取和停用词处理。
        /// 支持的语言包括：chinese、english、arabic、danish、dutch、finnish、french、german、hungarian、italian、norwegian、portuguese、romanian、russian、spanish、swedish、turkish。
        /// 默认值为 "chinese"。
        /// </summary>
        public string Language { get; set; } = "chinese";

        /// <summary>
        /// 使用指定的索引名称初始化 FtDocumentAttribute 类的新实例。
        /// </summary>
        /// <param name="name">索引的名称</param>
        public FtDocumentAttribute(string name)
        {
            Name = name;
        }
    }
    /// <summary>
    /// 标记属性作为文档的主键。每个文档类必须有且仅有一个属性标记此特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class FtKeyAttribute : Attribute { }

    /// <summary>
    /// RediSearch 字段特性的基类，定义了所有字段类型的通用属性。
    /// </summary>
    public abstract class FtFieldAttribute : Attribute
    {
        /// <summary>
        /// 获取或设置字段在 Redis 中的名称。如果未设置，将使用属性名的小写形式。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置字段的别名。如果设置了别名，搜索时可以使用别名代替字段名。
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// 获取实际使用的字段名。如果设置了别名则返回别名，否则返回字段名。
        /// </summary>
        public string FieldName => string.IsNullOrEmpty(Alias) ? Name : Alias;
    }
    /// <summary>
    /// 标记属性为文本字段，支持全文搜索、词干提取、语音匹配等高级文本搜索功能。
    /// 适用于需要进行全文搜索的字符串属性，如标题、描述、内容等。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class FtTextFieldAttribute : FtFieldAttribute
    {
        /// <summary>
        /// 获取或设置字段在搜索中的权重。权重越高，匹配该字段的文档在搜索结果中的排名越靠前。
        /// 默认值为 1.0，范围通常为 0.1 到 10.0。
        /// </summary>
        public double Weight { get; set; }
        
        /// <summary>
        /// 获取或设置是否禁用词干提取。如果设置为 true，将不会对该字段进行词干提取处理。
        /// 词干提取可以匹配单词的不同形式（如 "running" 和 "run"）。
        /// </summary>
        public bool NoStem { get; set; }

        /// <summary>
        /// 获取或设置语音匹配算法。支持 "dm:en"（英语双重音标）和 "dm:fr"（法语双重音标）等。
        /// 语音匹配可以找到发音相似的单词。
        /// </summary>
        public string Phonetic { get; set; }

        /// <summary>
        /// 获取或设置字段是否可排序。如果设置为 true，可以在搜索结果中按此字段排序。
        /// 注意：可排序字段会占用额外的内存空间。
        /// </summary>
        public bool Sortable { get; set; }

        /// <summary>
        /// 获取或设置是否使用 UNF（Unicode Normalization Form）。
        /// 如果设置为 true，将对 Unicode 文本进行标准化处理。
        /// </summary>
        public bool Unf { get; set; }

        /// <summary>
        /// 获取或设置是否不创建搜索索引。如果设置为 true，字段数据会被存储但不能被搜索。
        /// 适用于需要存储但不需要搜索的字段，可以节省索引空间和提高性能。
        /// </summary>
        public bool NoIndex { get; set; }

        /// <summary>
        /// 获取或设置是否创建后缀树索引。如果设置为 true，支持通配符搜索和前缀匹配。
        /// 注意：后缀树会显著增加索引大小和内存使用。
        /// </summary>
        public bool WithSuffixTrie { get; set; }

        /// <summary>
        /// 获取或设置是否索引缺失值。如果设置为 true，没有该字段的文档也会被索引。
        /// 这允许搜索不包含特定字段的文档。
        /// </summary>
        public bool MissingIndex { get; set; }

        /// <summary>
        /// 获取或设置是否索引空值。如果设置为 true，字段值为空字符串的文档也会被索引。
        /// 这允许搜索包含空字段的文档。
        /// </summary>
        public bool EmptyIndex { get; set; }

        /// <summary>
        /// 初始化 FtTextFieldAttribute 类的新实例。
        /// </summary>
        public FtTextFieldAttribute() { }

        /// <summary>
        /// 使用指定的字段名初始化 FtTextFieldAttribute 类的新实例。
        /// </summary>
        /// <param name="name">字段在 Redis 中的名称</param>
        public FtTextFieldAttribute(string name)
        {
            Name = name;
        }
    }
    /// <summary>
    /// 标记属性为标签字段，用于精确匹配和分类搜索。
    /// 适用于类别、标签、状态等需要精确匹配的字符串或字符串数组属性。
    /// 标签字段不进行分词处理，支持多值（数组）存储。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class FtTagFieldAttribute : FtFieldAttribute
    {
        /// <summary>
        /// 获取或设置字段是否可排序。如果设置为 true，可以在搜索结果中按此字段排序。
        /// 注意：可排序字段会占用额外的内存空间。
        /// </summary>
        public bool Sortable { get; set; }

        /// <summary>
        /// 获取或设置是否使用 UNF（Unicode Normalization Form）。
        /// 如果设置为 true，将对 Unicode 文本进行标准化处理。
        /// </summary>
        public bool Unf { get; set; }

        /// <summary>
        /// 获取或设置是否不创建搜索索引。如果设置为 true，字段数据会被存储但不能被搜索。
        /// 适用于需要存储但不需要搜索的标签字段。
        /// </summary>
        public bool NoIndex { get; set; }

        /// <summary>
        /// 获取或设置多个标签值之间的分隔符。默认为逗号（","）。
        /// 当属性为字符串数组时，保存到 Redis 时会使用此分隔符连接，加载时会按此分隔符分割。
        /// </summary>
        public string Separator { get; set; }

        /// <summary>
        /// 获取或设置标签匹配是否区分大小写。如果设置为 true，"Tag" 和 "tag" 将被视为不同的标签。
        /// 默认为 false，即不区分大小写。
        /// </summary>
        public bool CaseSensitive { get; set; }

        /// <summary>
        /// 获取或设置是否创建后缀树索引。如果设置为 true，支持标签的前缀匹配。
        /// 注意：后缀树会显著增加索引大小和内存使用。
        /// </summary>
        public bool WithSuffixTrie { get; set; }

        /// <summary>
        /// 获取或设置是否索引缺失值。如果设置为 true，没有该字段的文档也会被索引。
        /// 这允许搜索不包含特定标签字段的文档。
        /// </summary>
        public bool MissingIndex { get; set; }

        /// <summary>
        /// 获取或设置是否索引空值。如果设置为 true，字段值为空的文档也会被索引。
        /// 这允许搜索包含空标签字段的文档。
        /// </summary>
        public bool EmptyIndex { get; set; }

        /// <summary>
        /// 初始化 FtTagFieldAttribute 类的新实例。
        /// </summary>
        public FtTagFieldAttribute() { }

        /// <summary>
        /// 使用指定的字段名初始化 FtTagFieldAttribute 类的新实例。
        /// </summary>
        /// <param name="name">字段在 Redis 中的名称</param>
        public FtTagFieldAttribute(string name)
        {
            Name = name;
        }
    }
    /// <summary>
    /// 标记属性为数值字段，支持范围查询、数值比较和数学运算。
    /// 适用于整数、浮点数、decimal 等数值类型属性，如价格、年龄、分数、时间戳等。
    /// 支持范围搜索（如 @price:[100 500]）和排序操作。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class FtNumericFieldAttribute : FtFieldAttribute
    {
        /// <summary>
        /// 获取或设置字段是否可排序。如果设置为 true，可以在搜索结果中按此字段排序。
        /// 数值字段通常设置为可排序，以支持按价格、时间、评分等排序。
        /// 注意：可排序字段会占用额外的内存空间。
        /// </summary>
        public bool Sortable { get; set; }

        /// <summary>
        /// 获取或设置是否不创建搜索索引。如果设置为 true，字段数据会被存储但不能被搜索。
        /// 适用于需要存储但不需要搜索的数值字段，如内部计算字段、统计数据等。
        /// 即使设置了 NoIndex，仍然可以用于排序（如果同时设置了 Sortable）。
        /// </summary>
        public bool NoIndex { get; set; }

        /// <summary>
        /// 获取或设置是否索引缺失值。如果设置为 true，没有该字段的文档也会被索引。
        /// 这允许搜索不包含特定数值字段的文档，或者查找字段值为 null 的文档。
        /// </summary>
        public bool MissingIndex { get; set; }

        /// <summary>
        /// 初始化 FtNumericFieldAttribute 类的新实例。
        /// </summary>
        public FtNumericFieldAttribute() { }

        /// <summary>
        /// 使用指定的字段名初始化 FtNumericFieldAttribute 类的新实例。
        /// </summary>
        /// <param name="name">字段在 Redis 中的名称</param>
        public FtNumericFieldAttribute(string name)
        {
            Name = name;
        }
    }
    /// <summary>
    /// 标记属性为地理位置字段，支持基于地理位置的搜索和距离计算。
    /// 适用于存储经纬度坐标的属性，支持按距离搜索、范围查询等地理空间操作。
    /// 字段值应为 "经度,纬度" 格式的字符串，如 "116.397128,39.916527"。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class FtGeoFieldAttribute : FtFieldAttribute
    {
        /// <summary>
        /// 获取或设置字段是否可排序。如果设置为 true，可以在搜索结果中按此字段排序。
        /// 对于地理位置字段，排序通常按照距离某个参考点的远近进行。
        /// 注意：可排序字段会占用额外的内存空间。
        /// </summary>
        public bool Sortable { get; set; }

        /// <summary>
        /// 获取或设置是否不创建搜索索引。如果设置为 true，字段数据会被存储但不能进行地理搜索。
        /// 适用于需要存储位置信息但不需要进行地理搜索的场景。
        /// 即使设置了 NoIndex，仍然可以用于排序（如果同时设置了 Sortable）。
        /// </summary>
        public bool NoIndex { get; set; }

        /// <summary>
        /// 获取或设置是否索引缺失值。如果设置为 true，没有该字段的文档也会被索引。
        /// 这允许搜索不包含地理位置信息的文档。
        /// </summary>
        public bool MissingIndex { get; set; }

        /// <summary>
        /// 初始化 FtGeoFieldAttribute 类的新实例。
        /// </summary>
        public FtGeoFieldAttribute() { }

        /// <summary>
        /// 使用指定的字段名初始化 FtGeoFieldAttribute 类的新实例。
        /// </summary>
        /// <param name="name">字段在 Redis 中的名称</param>
        public FtGeoFieldAttribute(string name)
        {
            Name = name;
        }
    }
    /// <summary>
    /// 标记属性为地理形状字段，支持复杂的地理形状搜索，如多边形、线段等。
    /// 适用于存储复杂地理形状数据的属性，支持形状相交、包含等高级地理空间查询。
    /// 字段值应为 WKT（Well-Known Text）格式的字符串，如 "POLYGON((0 0, 0 1, 1 1, 1 0, 0 0))"。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class FtGeoShapeFieldAttribute : FtFieldAttribute
    {
        /// <summary>
        /// 获取或设置坐标系统。指定地理形状使用的坐标参考系统。
        /// 常用值包括 SPHERICAL（球面坐标系）和 FLAT（平面坐标系）。
        /// 默认为 SPHERICAL，适用于地球表面的地理坐标。
        /// </summary>
        public CoordinateSystem System { get; set; }

        /// <summary>
        /// 获取或设置是否索引缺失值。如果设置为 true，没有该字段的文档也会被索引。
        /// 这允许搜索不包含地理形状信息的文档。
        /// </summary>
        public bool MissingIndex { get; set; }

        /// <summary>
        /// 初始化 FtGeoShapeFieldAttribute 类的新实例。
        /// </summary>
        public FtGeoShapeFieldAttribute() { }

        /// <summary>
        /// 使用指定的字段名初始化 FtGeoShapeFieldAttribute 类的新实例。
        /// </summary>
        /// <param name="name">字段在 Redis 中的名称</param>
        public FtGeoShapeFieldAttribute(string name)
        {
            Name = name;
        }
    }

    public class FtDocumentRepositorySearchBuilder<T>
    {
        SearchBuilder _searchBuilder;
        FtDocumentRepository<T> _repository;
        internal FtDocumentRepositorySearchBuilder(FtDocumentRepository<T> repository, string index, string query)
        {
            _repository = repository;
            _searchBuilder = new SearchBuilder(_repository._client, index, query);
        }

        List<T> FetchResult(SearchResult result)
        {
            var isFromReturnClause = _searchBuilder._return.Any();
            return result.Documents.Select(doc =>
            {
                // 使用统一的反序列化核心方法，并传递上下文
                return _repository.DeserializeDocumentCore(doc.Id, doc.Body, isFromReturnClause);
            }).ToList();
        }
        public long Total { get; private set; }
        public List<T> ToList() => ToList(out var _);
        public List<T> ToList(out long total)
        {
            var result = _searchBuilder.Execute();
            total = Total = result.Total;
            return FetchResult(result);
        }
#if isasync
        async public Task<List<T>> ToListAsync()
        {
            var result = await _searchBuilder.ExecuteAsync();
            Total = result.Total;
            return FetchResult(result);
        }
#endif

        public FtDocumentRepositorySearchBuilder<T> NoContent(bool value = true)
        {
            _searchBuilder.NoContent(value);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> Verbatim(bool value = true)
        {
            _searchBuilder.Verbatim(value);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> Filter(Expression<Func<T, object>> selector, object min, object max)
        {
            var fields = _repository.ParseSelectorExpression(selector.Body);
            if (fields.Any()) _searchBuilder.Filter(fields.FirstOrDefault().Value, min, max);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> Filter(string field, object min, object max)
        {
            _searchBuilder.Filter(field, min, max);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> InKeys(params string[] keys)
        {
            _searchBuilder.InKeys(keys);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> InFields(Expression<Func<T, object>> selector)
        {
            var fields = _repository.ParseSelectorExpression(selector.Body).Select(a => a.Value).ToArray();
            if (fields.Any()) _searchBuilder.InFields(fields);
            return this;
        }

        public FtDocumentRepositorySearchBuilder<T> Return(Expression<Func<T, object>> selector)
        {
            var identifiers = _repository.ParseSelectorExpression(selector.Body)
                .Select(a => new KeyValuePair<string, string>(a.Value, a.Key)).ToArray();
            if (identifiers.Any()) _searchBuilder.Return(identifiers);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> Sumarize(Expression<Func<T, object>> selector, long frags = -1, long len = -1, string separator = null)
        {
            var fields = _repository.ParseSelectorExpression(selector.Body).Select(a => a.Value).ToArray();
            if (fields.Any()) _searchBuilder.Sumarize(fields, frags, len, separator);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> Sumarize(string[] fields, long frags = -1, long len = -1, string separator = null)
        {
            _searchBuilder.Sumarize(fields, frags, len, separator);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> HighLight(Expression<Func<T, object>> selector, string tagsOpen = null, string tagsClose = null)
        {
            var fields = _repository.ParseSelectorExpression(selector.Body).Select(a => a.Value).ToArray();
            if (fields.Any()) _searchBuilder.HighLight(fields, tagsOpen, tagsClose);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> HighLight(string[] fields, string tagsOpen = null, string tagsClose = null)
        {
            _searchBuilder.HighLight(fields, tagsOpen, tagsClose);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> Slop(decimal value)
        {
            _searchBuilder.Slop(value);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> Timeout(long milliseconds)
        {
            _searchBuilder.Timeout(milliseconds);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> InOrder(bool value = true)
        {
            _searchBuilder.InOrder(value);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> Language(string value)
        {
            _searchBuilder.Language(value);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> Scorer(string value)
        {
            _searchBuilder.Scorer(value);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> SortBy(Expression<Func<T, object>> selector)
        {
            _searchBuilder.SortBy(_repository.ParseQueryExpression(selector, new FtDocumentRepository<T>.ParseQueryExpressionOptions { IsQuoteFieldName = false }), false);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> SortByDesc(Expression<Func<T, object>> selector)
        {
            _searchBuilder.SortBy(_repository.ParseQueryExpression(selector, new FtDocumentRepository<T>.ParseQueryExpressionOptions { IsQuoteFieldName = false }), true);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> SortBy(string sortBy, bool desc = false)
        {
            _searchBuilder.SortBy(sortBy, desc);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> Limit(long offset, long num)
        {
            _searchBuilder.Limit(offset, num);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> Params(string name, string value)
        {
            _searchBuilder.Params(name, value);
            return this;
        }
        public FtDocumentRepositorySearchBuilder<T> Dialect(int value)
        {
            _searchBuilder.Dialect(value);
            return this;
        }
    }


    public class FtDocumentRepositoryAggregateTuple<TDocument, TExtra>
    {
        public TDocument Document { get; set; }
        public TExtra Extra { get; set; }
    }
    public class FtDocumentRepositoryAggregateBuilder<TDocument, TExtra>
    {
        AggregateBuilder _aggregateBuilder;
        FtDocumentRepository<TDocument> _repository;
        internal FtDocumentRepositoryAggregateBuilder(FtDocumentRepository<TDocument> repository, AggregateBuilder aggregateBuilder)
        {
            _repository = repository;
            _aggregateBuilder = aggregateBuilder;
        }
        internal FtDocumentRepositoryAggregateBuilder(FtDocumentRepository<TDocument> repository, string index, string query)
        {
            _repository = repository;
            _aggregateBuilder = new AggregateBuilder(_repository._client, index, query);
        }

        //public List<T> ToList() => ToList(out var _);
        //public List<T> ToList(out long total)
        //{
        //    var prefix = _repository._schema.DocumentAttribute.Prefix;
        //    var keyProperty = _repository._schema.KeyProperty;
        //    var result = _aggregateBuilder.Execute();
        //    total = result.Total;
        //    var ttype = typeof(T);
        //    return result.Documents.Select(doc =>
        //    {
        //        var item = (T)ttype.CreateInstanceGetDefaultValue();
        //        foreach (var kv in doc.Body)
        //        {
        //            var name = kv.Key.Replace("-", "_");
        //            var prop = ttype.GetPropertyOrFieldIgnoreCase(name);
        //            if (prop == null) continue;
        //            if (kv.Value == null) continue;
        //            if (kv.Value is string valstr && _repository._schema.FieldsMap.TryGetValue(prop.Name, out var field) && field.FieldType == FieldType.Tag)
        //                ttype.SetPropertyOrFieldValue(item, prop.Name, valstr.Split(new[] { (field.FieldAttribute as FtTagFieldAttribute).Separator ?? "," }, StringSplitOptions.None));
        //            else
        //                ttype.SetPropertyOrFieldValue(item, prop.Name, prop.GetPropertyOrFieldType().FromObject(kv.Value));
        //        }
        //        var itemId = doc.Id;
        //        if (!string.IsNullOrEmpty(prefix))
        //            if (itemId.StartsWith(prefix))
        //                itemId = itemId.Substring(prefix.Length);
        //        typeof(T).SetPropertyOrFieldValue(item, keyProperty.Name, keyProperty.PropertyType.FromObject(itemId));
        //        return item;
        //    }).ToList();
        //}

        public FtDocumentRepositoryAggregateBuilder<TDocument, TExtra> Verbatim(bool value = true)
        {
            _aggregateBuilder.Verbatim(value);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<TDocument, TExtra> Load(Expression<Func<TDocument, object>> selector)
        {
            var fields = _repository.ParseSelectorExpression(selector.Body).Select(a => a.Value).ToArray();
            if (fields.Any()) _aggregateBuilder.Load(fields);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<TDocument, TExtra> Timeout(long milliseconds)
        {
            _aggregateBuilder.Timeout(milliseconds);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<TDocument, TNewExtra> GroupBy<TNewExtra>(Expression<Func<FtDocumentRepositoryAggregateTuple<TDocument, TExtra>, TNewExtra>> selector)
        {
            var fieldValues = new List<KeyValuePair<string, string>>();
            var exp = selector.Body;

            if (exp.NodeType == ExpressionType.New)
            {
                var newExp = exp as NewExpression;
                for (var a = 0; a < newExp?.Members?.Count; a++)
                {
                    var left = newExp.Members[a].Name;
                    var right = _repository.ParseQueryExpression(newExp.Arguments[a], new FtDocumentRepository<TDocument>.ParseQueryExpressionOptions { IsQuoteFieldName = false });
                    fieldValues.Add(new KeyValuePair<string, string>(left, right));
                }
            }
            else if (exp.NodeType == ExpressionType.MemberInit)
            {
                var initExp = exp as MemberInitExpression;
                for (var a = 0; a < initExp?.Bindings.Count; a++)
                {
                    var initAssignExp = (initExp.Bindings[a] as MemberAssignment);
                    if (initAssignExp == null) continue;
                    var left = initAssignExp.Member.Name;
                    var right = _repository.ParseQueryExpression(initAssignExp.Expression, new FtDocumentRepository<TDocument>.ParseQueryExpressionOptions { IsQuoteFieldName = false });
                    fieldValues.Add(new KeyValuePair<string, string>(left, right));
                }
            }
            return new FtDocumentRepositoryAggregateBuilder<TDocument, TNewExtra>(_repository, _aggregateBuilder);
        }
        public FtDocumentRepositoryAggregateBuilder<TDocument, TExtra> GroupBy(string[] properties = null, params AggregateReduce[] reduces)
        {
            _aggregateBuilder.GroupBy(properties, reduces);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<TDocument, TExtra> SortBy(string property, bool desc = false)
        {
            _aggregateBuilder.SortBy(property, desc);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<TDocument, TExtra> SortBy(string[] properties, bool[] desc, int max = 0)
        {
            _aggregateBuilder.SortBy(properties, desc, max);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<TDocument, TNewExtra> Apply<TNewExtra>(Expression<Func<TDocument, TExtra, TNewExtra>> selector)
        {
            var applies = _repository.ParseSelectorExpression(selector.Body, true);
            _aggregateBuilder._applies.Clear();
            foreach (var apply in applies)
                _aggregateBuilder.Apply(apply.Value, apply.Key);
            return new FtDocumentRepositoryAggregateBuilder<TDocument, TNewExtra>(_repository, _aggregateBuilder);
        }
        public FtDocumentRepositoryAggregateBuilder<TDocument, TExtra> Limit(long offset, long num)
        {
            _aggregateBuilder.Limit(offset, num);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<TDocument, TExtra> Filter(string value)
        {
            _aggregateBuilder.Filter(value);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<TDocument, TExtra> WithCursor(int count = -1, long maxIdle = -1)
        {
            _aggregateBuilder.WithCursor(count, maxIdle);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<TDocument, TExtra> Params(string name, string value)
        {
            _aggregateBuilder.Params(name, value);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<TDocument, TExtra> Dialect(int value)
        {
            _aggregateBuilder.Dialect(value);
            return this;
        }
    }
}