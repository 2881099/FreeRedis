using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeRedis.Build.Cli
{
    internal static class TypeExtension
    {

        public static IEnumerable<MethodInfo> GetApis(this Type type)
        {
            var addMethods = type.GetEvents().Select(_event => _event.GetAddMethod());
            var removeMethods = type.GetEvents().Select(_event => _event.GetRemoveMethod());
            var getMethods = type.GetProperties().Select(_propety => _propety.GetGetMethod());
            var setMethods = type.GetProperties().Select(_propety => _propety.GetSetMethod());

            var enumerable = addMethods
                .Concat(removeMethods)
                .Concat(getMethods)
                .Concat(setMethods)
                .Where(_method=>_method != null)
                .Select(_method => _method.Name);

            var methods = enumerable.ToList();
            methods.Add("Dispose");

            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(_method=> !methods.Contains(_method.Name));
        }

        public static string DisplayCsharp(this Type type, bool isNameSpace = true)
        {
            if (type == null) return null;
            if (type == typeof(void)) return "void";
            if (type.IsGenericParameter) return type.Name;
            if (type.IsArray) return $"{DisplayCsharp(type.GetElementType())}[]";
            var sb = new StringBuilder();
            var nestedType = type;
            while (nestedType.IsNested)
            {
                sb.Insert(0, ".").Insert(0, DisplayCsharp(nestedType.DeclaringType, false));
                nestedType = nestedType.DeclaringType;
            }
            if (isNameSpace && string.IsNullOrWhiteSpace(nestedType.Namespace) == false)
                sb.Insert(0, ".").Insert(0, nestedType.Namespace);

            if (type.IsGenericType == false)
                return sb.Append(type.Name).ToString();

            var genericParameters = type.GetGenericArguments();
            if (type.IsNested && type.DeclaringType.IsGenericType)
            {
                var dic = genericParameters.ToDictionary(a => a.Name);
                foreach (var nestedGenericParameter in type.DeclaringType.GetGenericArguments())
                    if (dic.ContainsKey(nestedGenericParameter.Name))
                        dic.Remove(nestedGenericParameter.Name);
                genericParameters = dic.Values.ToArray();
            }
            if (genericParameters.Any() == false)
                return sb.Append(type.Name).ToString();

            sb.Append(type.Name.Remove(type.Name.IndexOf('`'))).Append("<");
            var genericTypeIndex = 0;
            foreach (var genericType in genericParameters)
            {
                if (genericTypeIndex++ > 0) sb.Append(", ");
                sb.Append(DisplayCsharp(genericType, true));
            }
            return sb.Append(">").ToString();
        }

        public static string ToInterface(this MethodInfo method, bool isAlign = true)
        {
            if (method == null) return null;
            var sb = new StringBuilder();
            //if (method.IsPublic) sb.Append("public ");
            //if (method.IsAssembly) sb.Append("internal ");
            //if (method.IsFamily) sb.Append("protected ");
            //if (method.IsPrivate) sb.Append("private ");
            //if (method.IsPrivate) sb.Append("private ");
            //if (method.IsStatic) sb.Append("static ");
            //if (method.IsAbstract && method.DeclaringType.IsInterface == false) sb.Append("abstract ");
            //if (method.IsVirtual && method.DeclaringType.IsInterface == false) sb.Append(isOverride ? "override " : "virtual ");
            if (isAlign)
                sb.Append("        ");
            sb.Append(method.ReturnType.DisplayCsharp()).Append(" ").Append(method.Name);

            var genericParameters = method.GetGenericArguments();
            if (method.DeclaringType.IsNested && method.DeclaringType.DeclaringType.IsGenericType)
            {
                var dic = genericParameters.ToDictionary(a => a.Name);
                foreach (var nestedGenericParameter in method.DeclaringType.DeclaringType.GetGenericArguments())
                    if (dic.ContainsKey(nestedGenericParameter.Name))
                        dic.Remove(nestedGenericParameter.Name);
                genericParameters = dic.Values.ToArray();
            }
            if (genericParameters.Any())
                sb.Append("<")
                    .Append(string.Join(", ", genericParameters.Select(a => a.DisplayCsharp())))
                    .Append(">");

            sb.Append("(").Append(string.Join(", ", method.GetParameters().Select(a => $"{a.ParameterType.DisplayCsharp()} {a.Name}"))).Append(")");

            if (isAlign)
                sb.Append(";\r\n\r\n");

            return sb.ToString();
        }

        public static string ToProxy(this MethodInfo method)
        {
            if (method == null) return null;
            var sb = new StringBuilder();
            sb.Append("        public ");

            var returnType = method.ReturnType;
            var isNoResultAsync = returnType == typeof(Task) || returnType == typeof(ValueTask);
            var isGenericAsync = returnType.IsGenericType && (returnType.GetGenericTypeDefinition() == typeof(ValueTask<>) || returnType.GetGenericTypeDefinition() == typeof(Task<>));
            var isAsync = isGenericAsync || isNoResultAsync;

            if (isAsync)
                sb.Append("async ");

            var funcInfo = method.ToInterface(false);
            sb.AppendLine(funcInfo);
            sb.AppendLine("        {");

            var isResult = false;

            if (isAsync)
            {
                sb.AppendLine($"            await _context.BeforeAsync(typeof(RedisClient).GetMethod({method.Name}))");
            }
            else
            {
                sb.AppendLine($"            _context.Before(typeof(RedisClient).GetMethod({method.Name}))");
            }

            if (isAsync)
            {
                if (isNoResultAsync)
                    sb.Append("            await _redisClient.").Append(method.Name);
                else
                {
                    isResult = true;
                    sb.Append("            var result = await _redisClient.").Append(method.Name);
                }
            }
            else
            {
                if (returnType == typeof(void))
                {
                    sb.Append("            _redisClient.").Append(method.Name);
                }
                else
                {
                    isResult = true;
                    sb.Append("            var result = _redisClient.").Append(method.Name);
                }
            }

            var genericParameters = method.GetGenericArguments();
            if (method.DeclaringType.IsNested && method.DeclaringType.DeclaringType.IsGenericType)
            {
                var dic = genericParameters.ToDictionary(a => a.Name);
                foreach (var nestedGenericParameter in method.DeclaringType.DeclaringType.GetGenericArguments())
                    if (dic.ContainsKey(nestedGenericParameter.Name))
                        dic.Remove(nestedGenericParameter.Name);
                genericParameters = dic.Values.ToArray();
            }
            if (genericParameters.Any())
                sb.Append("<")
                    .Append(string.Join(", ", genericParameters.Select(a => a.DisplayCsharp())))
                    .Append(">");

            sb.Append("(").Append(string.Join(", ", method.GetParameters().Select(a => $" {a.Name}"))).AppendLine(");");

            if (isAsync)
            {
                sb.AppendLine($"            await _context.AfterAsync(typeof(RedisClient).GetMethod({method.Name}))");
            }
            else
            {
                sb.AppendLine($"            _context.After(typeof(RedisClient).GetMethod({method.Name}))");
            }

            if (isResult)
                sb.AppendLine("            return result;");

            sb.Append("        }\r\n\r\n");
            return sb.ToString();
        }

        public static string ToMethod(this MethodInfo method)
        {
            if (method == null) return null;
            var sb = new StringBuilder();
            sb.Append("        public static ");

            var returnType = method.ReturnType;
            var isNoResultAsync = returnType == typeof(Task) || returnType == typeof(ValueTask);
            var isGenericAsync = returnType.IsGenericType && (returnType.GetGenericTypeDefinition() == typeof(ValueTask<>) || returnType.GetGenericTypeDefinition() == typeof(Task<>));
            var isAsync = isGenericAsync || isNoResultAsync;

            var isResult = false;

            if (isAsync)
                sb.Append("async ");

            var funcInfo = method.ToInterface(false);
            sb.AppendLine(funcInfo);
            sb.AppendLine("        {");

            if (isAsync)
            {
                if (isNoResultAsync)
                    sb.Append("            await _redisClient.").Append(method.Name);
                else
                {
                    isResult = true;
                    sb.Append("            var result = await _redisClient.").Append(method.Name);
                }
            }
            else
            {
                if (returnType == typeof(void))
                {
                    sb.Append("            _redisClient.").Append(method.Name);
                }
                else
                {
                    isResult = true;
                    sb.Append("            var result = _redisClient.").Append(method.Name);
                }
            }

            var genericParameters = method.GetGenericArguments();
            if (method.DeclaringType.IsNested && method.DeclaringType.DeclaringType.IsGenericType)
            {
                var dic = genericParameters.ToDictionary(a => a.Name);
                foreach (var nestedGenericParameter in method.DeclaringType.DeclaringType.GetGenericArguments())
                    if (dic.ContainsKey(nestedGenericParameter.Name))
                        dic.Remove(nestedGenericParameter.Name);
                genericParameters = dic.Values.ToArray();
            }
            if (genericParameters.Any())
                sb.Append("<")
                    .Append(string.Join(", ", genericParameters.Select(a => a.DisplayCsharp())))
                    .Append(">");

            sb.Append("(").Append(string.Join(", ", method.GetParameters().Select(a => $" {a.Name}"))).AppendLine(");");

            if (isResult)
                sb.AppendLine("            return result;");

            sb.Append("        }\r\n\r\n");
            return sb.ToString();
        }
    }
}
