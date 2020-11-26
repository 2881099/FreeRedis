using FreeRedis.Build.Cli.Template;
using FreeRedis;
using RazorLight;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeRedis.Build.Cli
{
    class RedisClientBuilder
    {
        public static RedisClientBuilder Builder()
        {
            var redisClientType = typeof(RedisClient);
            var medthods = redisClientType.GetApis();

            return new RedisClientBuilder(medthods);
        }

        private IEnumerable<MethodInfo> methods;

        public RedisClientBuilder(IEnumerable<MethodInfo> methods)
        {
            this.methods = methods;
        }

        public async Task OutputIntefaceAsync(string filename)
        {
            var engine = new RazorLightEngineBuilder()
                .DisableEncoding()
                .UseFileSystemProject(Directory.GetCurrentDirectory())
                .UseMemoryCachingProvider()
                .Build();

            var model = new TemplateClass(methods.Select(_method=> _method.ToInterface()).ToList());

            #region Build Interface
            var result = await engine.CompileRenderAsync("Template/Inteface.cshtml", model);

            using(var writeStream = File.OpenWrite(filename))
            {
                using (var streamWrite = new StreamWriter(writeStream, Encoding.UTF8))
                    await streamWrite.WriteLineAsync(result);
            }
            #endregion
        }

        public async Task OutputProxyAsync(string filename)
        {
            var engine = new RazorLightEngineBuilder()
                .DisableEncoding()
                .UseFileSystemProject(Directory.GetCurrentDirectory())
                .UseMemoryCachingProvider()
                .Build();

            var model = new TemplateClass(methods.Select(_method => _method.ToProxy()).ToList());

            #region Build Interface
            var result = await engine.CompileRenderAsync("Template/Proxy.cshtml", model);

            using (var writeStream = File.OpenWrite(filename))
            {
                using (var streamWrite = new StreamWriter(writeStream, Encoding.UTF8))
                    await streamWrite.WriteLineAsync(result);
            }
            #endregion
        }

        public async Task OutputRedisHelperAsync(string filename)
        {
            var engine = new RazorLightEngineBuilder()
                .DisableEncoding()
                .UseFileSystemProject(Directory.GetCurrentDirectory())
                .UseMemoryCachingProvider()
                .Build();

            var model = new TemplateClass(methods.Select(_method => _method.ToMethod()).ToList());

            #region Build Interface
            var result = await engine.CompileRenderAsync("Template/Helper.cshtml", model);

            using (var writeStream = File.OpenWrite(filename))
            {
                using (var streamWrite = new StreamWriter(writeStream, Encoding.UTF8))
                    await streamWrite.WriteLineAsync(result);
            }
            #endregion
        }
    }
}
