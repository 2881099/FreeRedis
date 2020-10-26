using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace FreeRedis.Tests.RedisClientTests
{
    public class GeoTests : TestBase
    {
        [Fact]
        public void GetAdd()
        {
            cli.Del("TestGeoAdd");
            Assert.Equal(3, cli.GeoAdd("TestGeoAdd", 
                new GeoMember(10, 20, "m1"), 
                new GeoMember(11, 21, "m2"), 
                new GeoMember(12, 22, "m3")));
        }

        [Fact]
        public void GeoDist()
        {
            cli.Del("TestGeoDist");
            Assert.Equal(3, cli.GeoAdd("TestGeoDist",
                new GeoMember(10, 20, "m1"),
                new GeoMember(11, 21, "m2"),
                new GeoMember(12, 22, "m3")));

            Assert.NotNull(cli.GeoDist("TestGeoDist", "m1", "m2"));
            Assert.NotNull(cli.GeoDist("TestGeoDist", "m1", "m3"));
            Assert.NotNull(cli.GeoDist("TestGeoDist", "m2", "m3"));
            Assert.Null(cli.GeoDist("TestGeoDist", "m1", "m31"));
            Assert.Null(cli.GeoDist("TestGeoDist", "m11", "m31"));
        }

        [Fact]
        public void GeoHash()
        {
            cli.Del("TestGeoHash");
            Assert.Equal(3, cli.GeoAdd("TestGeoHash", 
                new GeoMember(10, 20, "m1"),
                new GeoMember(11, 21, "m2"),
                new GeoMember(12, 22, "m3")));

            Assert.False(string.IsNullOrEmpty(cli.GeoHash("TestGeoHash", "m1")));
            Assert.Equal(2, cli.GeoHash("TestGeoHash", new[] { "m1", "m2" }).Select(a => string.IsNullOrEmpty(a) == false).Count());
            Assert.Equal(2, cli.GeoHash("TestGeoHash", new[] { "m1", "m2", "m22" }).Where(a => string.IsNullOrEmpty(a) == false).Count());
        }

        [Fact]
        public void GeoPos()
        {
            cli.Del("TestGeoPos");
            Assert.Equal(3, cli.GeoAdd("TestGeoPos",
                new GeoMember(10, 20, "m1"),
                new GeoMember(11, 21, "m2"),
                new GeoMember(12, 22, "m3")));

            Assert.Equal(4, cli.GeoPos("TestGeoPos", new[] { "m1", "m2", "m22", "m3" }).Length);
            //Assert.Equal((10, 20), rds.GeoPos("TestGeoPos", new[] { "m1", "m2", "m22", "m3" })[0]);
            //Assert.Equal((11, 21), rds.GeoPos("TestGeoPos", new[] { "m1", "m2", "m22", "m3" })[1]);
            Assert.Null(cli.GeoPos("TestGeoPos", new[] { "m1", "m2", "m22", "m3" })[2]);
            //Assert.Equal((12, 22), rds.GeoPos("TestGeoPos", new[] { "m1", "m2", "m22", "m3" })[3]);
        }

        [Fact]
        public void GeoRadius()
        {
            cli.Del("TestGeoRadius");
            Assert.Equal(2, cli.GeoAdd("TestGeoRadius", 
                new GeoMember(13.361389m, 38.115556m, "Palermo"), 
                new GeoMember(15.087269m, 37.502669m, "Catania")));

            var geopos = cli.GeoPos("TestGeoRadius", new[] { "m1", "Catania", "m2", "Palermo", "Catania2" });

            var georadius1 = cli.GeoRadius("TestGeoRadius", 15, 37, 200, GeoUnit.km);
            var georadius2 = cli.GeoRadius("TestGeoRadius", 15, 37, 200, GeoUnit.km, true);
            var georadius3 = cli.GeoRadius("TestGeoRadius", 15, 37, 200, GeoUnit.km, true, true);
            var georadius4 = cli.GeoRadius("TestGeoRadius", 15, 37, 200, GeoUnit.km, true, true, true);
            var georadius5 = cli.GeoRadius("TestGeoRadius", 15, 37, 200, GeoUnit.km, true, true, false);
            var georadius6 = cli.GeoRadius("TestGeoRadius", 15, 37, 200, GeoUnit.km, true, false);
            var georadius7 = cli.GeoRadius("TestGeoRadius", 15, 37, 200, GeoUnit.km, true, false, true);
            var georadius8 = cli.GeoRadius("TestGeoRadius", 15, 37, 200, GeoUnit.km, true, false, false);
            var georadius9 = cli.GeoRadius("TestGeoRadius", 15, 37, 200, GeoUnit.km, false, true, false);
            var georadius10 = cli.GeoRadius("TestGeoRadius", 15, 37, 200, GeoUnit.km, false, true, true);
            var georadius11 = cli.GeoRadius("TestGeoRadius", 15, 37, 200, GeoUnit.km, false, false, true);
        }

        [Fact]
        public void GeoRadiusByMember()
        {
            cli.Del("GeoRadiusByMember");
            Assert.Equal(2, cli.GeoAdd("GeoRadiusByMember",
                new GeoMember(13.361389m, 38.115556m, "Palermo"),
                new GeoMember(15.087269m, 37.502669m, "Catania")));

            var geopos = cli.GeoPos("GeoRadiusByMember", new[] { "m1", "Catania", "m2", "Palermo", "Catania2" });

            var georadius1 = cli.GeoRadius("GeoRadiusByMember", 15, 37, 200, GeoUnit.km);
            var georadius2 = cli.GeoRadius("GeoRadiusByMember", 15, 37, 200, GeoUnit.km, true);
            var georadius3 = cli.GeoRadius("GeoRadiusByMember", 15, 37, 200, GeoUnit.km, true, true);
            var georadius4 = cli.GeoRadius("GeoRadiusByMember", 15, 37, 200, GeoUnit.km, true, true, true);
            var georadius5 = cli.GeoRadius("GeoRadiusByMember", 15, 37, 200, GeoUnit.km, true, true, false);
            var georadius6 = cli.GeoRadius("GeoRadiusByMember", 15, 37, 200, GeoUnit.km, true, false);
            var georadius7 = cli.GeoRadius("GeoRadiusByMember", 15, 37, 200, GeoUnit.km, true, false, true);
            var georadius8 = cli.GeoRadius("GeoRadiusByMember", 15, 37, 200, GeoUnit.km, true, false, false);
            var georadius9 = cli.GeoRadius("GeoRadiusByMember", 15, 37, 200, GeoUnit.km, false, true, false);
            var georadius10 = cli.GeoRadius("GeoRadiusByMember", 15, 37, 200, GeoUnit.km, false, true, true);
            var georadius11 = cli.GeoRadius("GeoRadiusByMember", 15, 37, 200, GeoUnit.km, false, false, true);
        }
    }
}
