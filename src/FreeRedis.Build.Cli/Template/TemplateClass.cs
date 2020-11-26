using System;
using System.Collections.Generic;

namespace FreeRedis.Build.Cli.Template
{
    public class TemplateClass
    {
        public List<string> Methods;

        public TemplateClass(List<string> methods)
        {
            Methods = methods;
        }
    }
}
