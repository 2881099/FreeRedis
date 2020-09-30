using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace FreeRedis.Tests
{
    public class Class1
    {
        [Fact]
        public void Test01()
        {
            var methodsCount = typeof(RedisClient).GetMethods().Count();
        }
    }
}
