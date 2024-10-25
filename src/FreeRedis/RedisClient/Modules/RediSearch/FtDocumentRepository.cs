using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

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
            public List<DocumentSchemaFieldInfo> Fields { get; set; }
            public Dictionary<string, DocumentSchemaFieldInfo> FieldsMap { get; set; }
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
                }).Where(a => a.attribute != null).ToList();
                if (fieldProprties.Any() == false) throw new Exception($"Not found: [FtFieldAttribute]");
                var schema = new DocumentSchemaInfo
                {
                    DocumentType = type,
                    DocumentAttribute = type.GetCustomAttributes(false).FirstOrDefault(a => a is FtDocumentAttribute) as FtDocumentAttribute,
                    KeyProperty = type.GetProperties().FirstOrDefault(p => p.GetCustomAttributes(false).FirstOrDefault(a => a is FtKeyAttribute) != null),
                };
                var fields = fieldProprties.Select(a => new DocumentSchemaFieldInfo
                {
                    DocumentSchema = schema,
                    Property = a.property,
                    FieldAttribute = a.attribute,
                    FieldType = GetMapFieldType(a.property, a.attribute)
                }).ToList();
                schema.Fields = fields;
                schema.FieldsMap = fields.ToDictionary(a => a.Property.Name, a => a);
                return schema;
            });
        }
        protected FieldType GetMapFieldType(PropertyInfo property, FtFieldAttribute ftattr)
        {
            //Text, Tag, Numeric, Geo, Vector, GeoShape
            if (ftattr is FtTextFieldAttribute) return FieldType.Text;
            if (ftattr is FtTagFieldAttribute) return FieldType.Tag;
            if (ftattr is FtNumericFieldAttribute) return FieldType.Numeric;
            return FieldType.Text;
        }

        public void DropIndex(bool dd = false)
        {
            _client.FtDropIndex(_schema.DocumentAttribute.Name, dd);
        }
        public void CreateIndex()
        {
            var attr = _schema.DocumentAttribute;
            var createBuilder = _client.FtCreate(attr.Name);
            if (!string.IsNullOrWhiteSpace(attr.Prefix)) createBuilder.Prefix(attr.Prefix);
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
                }
            }
            createBuilder.Execute();
        }

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

            if (selector.NodeType == ExpressionType.New)
            {
                var newExp = selector as NewExpression;
                for (var a = 0; a < newExp?.Members?.Count; a++)
                {
                    var left = newExp.Members[a].Name;
                    var right = ParseQueryExpression(newExp.Arguments[a], new ParseQueryExpressionOptions { IsQuoteFieldName = isQuoteFieldName });
                    fieldValues.Add(new KeyValuePair<string, string>(left, right));
                }
            }
            else if (selector.NodeType == ExpressionType.MemberInit)
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
            return fieldValues;
        }
        internal protected class ParseQueryExpressionOptions
        {
            public bool IsQuoteFieldName { get; set; } = true;
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
                    return string.Concat("\"", str.Replace("\\", "\\\\").Replace("\"", "\\\""), "\"");
                else if (param is char chr)
                    return string.Concat("\"", chr.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace('\0', ' '), "\"");
                else if (param is Enum enm)
                    return string.Concat("\"", enm.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace(", ", ","), "\"");
                else if (decimal.TryParse(string.Concat(param), out var trydec))
                    return param;

                else if (param is DateTime || param is DateTime?)
                    return ToTimestamp((DateTime)param);

                return string.Concat("\"", param.ToString().Replace("\\", "\\\\").Replace("\"", "\\\""), "\"");
            }
            string toFtTagString(string expResultStr)
            {
                if (expResultStr == null) return "";
                if (expResultStr.StartsWith("\"") && expResultStr.EndsWith("\""))
                    return expResultStr.Substring(1, expResultStr.Length - 2)
                        .Replace("\\\"", "\"").Replace("\\\\", "\\");
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
                    if (_schema.KeyProperty.Name == memberExp.Member.Name)
                        return options.IsQuoteFieldName ? $"@__key" : "__key";
                    if (_schema.FieldsMap.TryGetValue(memberExp.Member.Name, out var field))
                        return options.IsQuoteFieldName ? $"@{field.FieldAttribute.FieldName}" : field.FieldAttribute.FieldName;
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
                                case "StartsWith": return $"startswith({left},{parseExp(callExp.Arguments[0])})";
                                case "EndsWith": return $"endswith({left},{parseExp(callExp.Arguments[0])})";
                                case "Contains": return $"contains({left},{parseExp(callExp.Arguments[0])})";
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

    [AttributeUsage(AttributeTargets.Class)]
    public class FtDocumentAttribute : Attribute
    {
        public string Name { get; set; }
        public string Prefix { get; set; }
        public string Filter { get; set; }
        public string Language { get; set; } = "chinese";
        public FtDocumentAttribute(string name)
        {
            Name = name;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class FtKeyAttribute : Attribute { }

    public abstract class FtFieldAttribute : Attribute
    {
        public string Name { get; set; }
        public string Alias { get; set; }
        public string FieldName => string.IsNullOrEmpty(Alias) ? Name : Alias;
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class FtTextFieldAttribute : FtFieldAttribute
    {
        public double Weight { get; set; }
        public bool NoStem { get; set; }
        public string Phonetic { get; set; }
        public bool Sortable { get; set; }
        public bool Unf { get; set; }
        public bool NoIndex { get; set; }
        public bool WithSuffixTrie { get; set; }
        public bool MissingIndex { get; set; }
        public bool EmptyIndex { get; set; }
        public FtTextFieldAttribute() { }
        public FtTextFieldAttribute(string name)
        {
            Name = name;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class FtTagFieldAttribute : FtFieldAttribute
    {
        public bool Sortable { get; set; }
        public bool Unf { get; set; }
        public bool NoIndex { get; set; }
        public string Separator { get; set; }
        public bool CaseSensitive { get; set; }
        public bool WithSuffixTrie { get; set; }
        public bool MissingIndex { get; set; }
        public bool EmptyIndex { get; set; }
        public FtTagFieldAttribute() { }
        public FtTagFieldAttribute(string name)
        {
            Name = name;
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class FtNumericFieldAttribute : FtFieldAttribute
    {
        public bool Sortable { get; set; }
        public bool NoIndex { get; set; }
        public bool MissingIndex { get; set; }
        public FtNumericFieldAttribute() { }
        public FtNumericFieldAttribute(string name)
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

        public List<T> ToList() => ToList(out var _);
        public List<T> ToList(out long total)
        {
            var prefix = _repository._schema.DocumentAttribute.Prefix;
            var keyProperty = _repository._schema.KeyProperty;
            var result = _searchBuilder.Execute();
            total = result.Total;
            var ttype = typeof(T);
            return result.Documents.Select(doc =>
            {
                var item = (T)ttype.CreateInstanceGetDefaultValue();
                foreach (var kv in doc.Body)
                {
                    var name = kv.Key.Replace("-", "_");
                    var prop = ttype.GetPropertyOrFieldIgnoreCase(name);
                    if (prop == null) continue;
                    if (kv.Value == null) continue;
                    if (kv.Value is string valstr && _repository._schema.FieldsMap.TryGetValue(prop.Name, out var field) && field.FieldType == FieldType.Tag)
                        ttype.SetPropertyOrFieldValue(item, prop.Name, 
                            field.Property.PropertyType.IsArrayOrList() ?
                            field.Property.PropertyType.FromObject(valstr.Split(new[] { (field.FieldAttribute as FtTagFieldAttribute).Separator ?? "," }, StringSplitOptions.None)) : valstr
                            );
                    else
                        ttype.SetPropertyOrFieldValue(item, prop.Name, prop.GetPropertyOrFieldType().FromObject(kv.Value));
                }
                var itemId = doc.Id;
                if (!string.IsNullOrEmpty(prefix))
                    if (itemId.StartsWith(prefix))
                        itemId = itemId.Substring(prefix.Length);
                typeof(T).SetPropertyOrFieldValue(item, keyProperty.Name, keyProperty.PropertyType.FromObject(itemId));
                return item;
            }).ToList();
        }

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


    public class FtDocumentRepositoryAggregateBuilder<T>
    {
        AggregateBuilder _aggregateBuilder;
        FtDocumentRepository<T> _repository;
        internal FtDocumentRepositoryAggregateBuilder(FtDocumentRepository<T> repository, string index, string query)
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

        public FtDocumentRepositoryAggregateBuilder<T> Verbatim(bool value = true)
        {
            _aggregateBuilder.Verbatim(value);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<T> Load(Expression<Func<T, object>> selector)
        {
            var fields = _repository.ParseSelectorExpression(selector.Body).Select(a => a.Value).ToArray();
            if (fields.Any()) _aggregateBuilder.Load(fields);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<T> Timeout(long milliseconds)
        {
            _aggregateBuilder.Timeout(milliseconds);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<T> GroupBy(params string[] properties)
        {
            _aggregateBuilder.GroupBy(properties);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<T> GroupBy(string[] properties = null, params AggregateReduce[] reduces)
        {
            _aggregateBuilder.GroupBy(properties, reduces);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<T> SortBy(string property, bool desc = false)
        {
            _aggregateBuilder.SortBy(property, desc);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<T> SortBy(string[] properties, bool[] desc, int max = 0)
        {
            _aggregateBuilder.SortBy(properties, desc, max);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<T> Apply(Expression<Func<T, object>> selector)
        {
            var applies = _repository.ParseSelectorExpression(selector.Body, true);
            foreach (var apply in applies)
                _aggregateBuilder.Apply(apply.Value, apply.Key);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<T> Limit(long offset, long num)
        {
            _aggregateBuilder.Limit(offset, num);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<T> Filter(string value)
        {
            _aggregateBuilder.Filter(value);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<T> WithCursor(int count = -1, long maxIdle = -1)
        {
            _aggregateBuilder.WithCursor(count, maxIdle);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<T> Params(string name, string value)
        {
            _aggregateBuilder.Params(name, value);
            return this;
        }
        public FtDocumentRepositoryAggregateBuilder<T> Dialect(int value)
        {
            _aggregateBuilder.Dialect(value);
            return this;
        }
    }
}
