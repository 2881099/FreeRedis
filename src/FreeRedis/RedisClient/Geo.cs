using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FreeRedis
{
    partial class RedisClient
    {
#if isasync
        #region async (copy from sync)
        public Task<long> GeoAddAsync(string key, params GeoMember[] members)
        {
            var cmd = "GEOADD".InputKey(key);
            foreach (var mem in members) cmd.InputRaw(mem.longitude).InputRaw(mem.latitude).InputRaw(mem.member);
            return CallAsync(cmd, rt => rt.ThrowOrValue<long>());
        }
        public Task<decimal?> GeoDistAsync(string key, string member1, string member2, GeoUnit unit = GeoUnit.m) => CallAsync("GEODIST"
            .InputKey(key)
            .Input(member1, member2)
            .InputIf(unit != GeoUnit.m, unit), rt => rt.ThrowOrValue<decimal?>());

        public Task<string> GeoHashAsync(string key, string member) => CallAsync("GEOHASH".InputKey(key, member), rt => rt.ThrowOrValue((a, _) => a.FirstOrDefault().ConvertTo<string>()));
        public Task<string[]> GeoHashAsync(string key, string[] members) => CallAsync("GEOHASH".InputKey(key, members), rt => rt.ThrowOrValue<string[]>());

        public Task<GeoMember> GeoPosAsync(string key, string member) => GeoPosAsync(key, new[] { member }, rt => rt.FirstOrDefault());
        public Task<GeoMember[]> GeoPosAsync(string key, string[] members) => GeoPosAsync(key, members, rt => rt);
        Task<T> GeoPosAsync<T>(string key, string[] members, Func<GeoMember[], T> parse) => CallAsync("GEOPOS".InputKey(key, members), rt => rt
            .ThrowOrValue((a, _) => parse(a.Select((z, y) =>
            {
                if (z == null) return null;
                var zarr = z as object[];
                return new GeoMember(zarr[0].ConvertTo<decimal>(), zarr[1].ConvertTo<decimal>(), members[y]);
            }).ToArray())
            ));

        public Task<GeoRadiusResult[]> GeoRadiusAsync(string key, decimal longitude, decimal latitude, decimal radius, GeoUnit unit = GeoUnit.m,
            bool withdoord = false, bool withdist = false, bool withhash = false,
            long? count = null, Collation? collation = null) => GeoRadiusAsync("GEORADIUS", key, null, longitude, latitude, radius, unit,
                withdoord, withdist, withhash, count, collation);

        public Task<long> GeoRadiusStoreAsync(string key, decimal longitude, decimal latitude, decimal radius, GeoUnit unit = GeoUnit.m,
            long? count = null, Collation? collation = null,
            string storekey = null, string storedistkey = null)
        {
            if (string.IsNullOrWhiteSpace(storekey) && string.IsNullOrWhiteSpace(storedistkey)) throw new ArgumentNullException(nameof(storekey));
            return CallAsync("GEORADIUS"
                .InputKey(key)
                .Input(longitude, latitude, radius, unit)
                .InputIf(count != null, "COUNT", count)
                .InputIf(collation != null, collation)
                .InputIf(!string.IsNullOrWhiteSpace(storekey), "STORE", storekey)
                .InputIf(!string.IsNullOrWhiteSpace(storedistkey), "STOREDIST", storedistkey), rt => rt.ThrowOrValue<long>());
        }

        Task<GeoRadiusResult[]> GeoRadiusAsync(string cmd, string key, string member, decimal? longitude, decimal? latitude, decimal radius, GeoUnit unit = GeoUnit.m,
            bool withdoord = false, bool withdist = false, bool withhash = false,
            long? count = null, Collation? collation = null) => CallAsync(cmd
            .InputKey(key)
            .InputIf(!string.IsNullOrEmpty(member), member)
            .InputIf(longitude != null && latitude != null, longitude, latitude)
            .InputRaw(radius)
            .InputRaw(unit)
            .InputIf(withdoord, "WITHCOORD")
            .InputIf(withdist, "WITHDIST")
            .InputIf(withhash, "WITHHASH")
            .InputIf(count != null, "COUNT", count)
            .InputIf(collation != null, collation), rt => rt.ThrowOrValue((a, _) =>
            {
                if (withdoord || withdist || withhash)
                    return a.Select(x =>
                    {
                        var objs2 = x as object[];
                        var grr = new GeoRadiusResult { member = objs2[0].ConvertTo<string>() };
                        var objs2idx = 0;
                        if (withdist) grr.dist = objs2[++objs2idx].ConvertTo<decimal>();
                        if (withhash) grr.hash = objs2[++objs2idx].ConvertTo<long>();
                        if (withdoord)
                        {
                            var objs3 = objs2[++objs2idx].ConvertTo<decimal[]>();
                            grr.longitude = objs3[0];
                            grr.latitude = objs3[1];
                        }
                        return grr;
                    }).ToArray();
                return a.Select(x => new GeoRadiusResult { member = x.ConvertTo<string>() }).ToArray();
            }));

        public Task<GeoRadiusResult[]> GeoRadiusByMemberAsync(string key, string member, decimal radius, GeoUnit unit = GeoUnit.m,
            bool withdoord = false, bool withdist = false, bool withhash = false,
            long? count = null, Collation? collation = null) => GeoRadiusAsync("GEORADIUSBYMEMBER", key, member, null, null, radius, unit,
                withdoord, withdist, withhash, count, collation);

        public Task<long> GeoRadiusByMemberStoreAsync(string key, string member, decimal radius, GeoUnit unit = GeoUnit.m,
            long? count = null, Collation? collation = null,
            string storekey = null, string storedistkey = null)
        {
            if (string.IsNullOrWhiteSpace(storekey) && string.IsNullOrWhiteSpace(storedistkey)) throw new ArgumentNullException(nameof(storekey));
            return CallAsync("GEORADIUSBYMEMBER"
                .InputKey(key)
                .Input(member, radius, unit)
                .InputIf(count != null, "COUNT", count)
                .InputIf(collation != null, collation)
                .InputIf(!string.IsNullOrWhiteSpace(storekey), "STORE", storekey)
                .InputIf(!string.IsNullOrWhiteSpace(storedistkey), "STOREDIST", storedistkey), rt => rt.ThrowOrValue<long>());
        }
        #endregion
#endif

        public long GeoAdd(string key, params GeoMember[] members)
        {
            var cmd = "GEOADD".InputKey(key);
            foreach (var mem in members) cmd.InputRaw(mem.longitude).InputRaw(mem.latitude).InputRaw(mem.member);
            return Call(cmd, rt => rt.ThrowOrValue<long>());
        }
        public decimal? GeoDist(string key, string member1, string member2, GeoUnit unit = GeoUnit.m) => Call("GEODIST"
            .InputKey(key)
            .Input(member1, member2)
            .InputIf(unit != GeoUnit.m, unit), rt => rt.ThrowOrValue<decimal?>());

        public string GeoHash(string key, string member) => Call("GEOHASH".InputKey(key, member), rt => rt.ThrowOrValue((a, _) => a.FirstOrDefault().ConvertTo<string>()));
        public string[] GeoHash(string key, string[] members) => Call("GEOHASH".InputKey(key, members), rt => rt.ThrowOrValue<string[]>());

        public GeoMember GeoPos(string key, string member) => GeoPos(key, new[] { member }, rt => rt.FirstOrDefault());
        public GeoMember[] GeoPos(string key, string[] members) => GeoPos(key, members, rt => rt);
        T GeoPos<T>(string key, string[] members, Func<GeoMember[], T> parse) => Call("GEOPOS".InputKey(key, members), rt => rt
            .ThrowOrValue((a, _) => parse(a.Select((z, y) =>
                {
                    if (z == null) return null;
                    var zarr = z as object[];
                    return new GeoMember(zarr[0].ConvertTo<decimal>(), zarr[1].ConvertTo<decimal>(), members[y]);
                }).ToArray())
            ));

        public GeoRadiusResult[] GeoRadius(string key, decimal longitude, decimal latitude, decimal radius, GeoUnit unit = GeoUnit.m,
            bool withdoord = false, bool withdist = false, bool withhash = false,
            long? count = null, Collation? collation = null) => GeoRadius("GEORADIUS", key, null, longitude, latitude, radius, unit,
                withdoord, withdist, withhash, count, collation);

        public long GeoRadiusStore(string key, decimal longitude, decimal latitude, decimal radius, GeoUnit unit = GeoUnit.m, 
            long? count = null, Collation? collation = null, 
            string storekey = null, string storedistkey = null)
        {
            if (string.IsNullOrWhiteSpace(storekey) && string.IsNullOrWhiteSpace(storedistkey)) throw new ArgumentNullException(nameof(storekey));
            return Call("GEORADIUS"
                .InputKey(key)
                .Input(longitude, latitude, radius, unit)
                .InputIf(count != null, "COUNT", count)
                .InputIf(collation != null, collation)
                .InputIf(!string.IsNullOrWhiteSpace(storekey), "STORE", storekey)
                .InputIf(!string.IsNullOrWhiteSpace(storedistkey), "STOREDIST", storedistkey), rt => rt.ThrowOrValue<long>());
        }
        
        GeoRadiusResult[] GeoRadius(string cmd, string key, string member, decimal? longitude, decimal? latitude, decimal radius, GeoUnit unit = GeoUnit.m,
            bool withdoord = false, bool withdist = false, bool withhash = false,
            long? count = null, Collation? collation = null) => Call(cmd
            .InputKey(key)
            .InputIf(!string.IsNullOrEmpty(member), member)
            .InputIf(longitude != null && latitude != null, longitude, latitude)
            .InputRaw(radius)
            .InputRaw(unit)
            .InputIf(withdoord, "WITHCOORD")
            .InputIf(withdist, "WITHDIST")
            .InputIf(withhash, "WITHHASH")
            .InputIf(count != null, "COUNT", count)
            .InputIf(collation != null, collation), rt => rt.ThrowOrValue((a, _) =>
            {
                if (withdoord || withdist || withhash)
                    return a.Select(x =>
                    {
                        var objs2 = x as object[];
                        var grr = new GeoRadiusResult { member = objs2[0].ConvertTo<string>() };
                        var objs2idx = 0;
                        if (withdist) grr.dist = objs2[++objs2idx].ConvertTo<decimal>();
                        if (withhash) grr.hash = objs2[++objs2idx].ConvertTo<long>();
                        if (withdoord)
                        {
                            var objs3 = objs2[++objs2idx].ConvertTo<decimal[]>();
                            grr.longitude = objs3[0];
                            grr.latitude = objs3[1];
                        }
                        return grr;
                    }).ToArray();
                return a.Select(x => new GeoRadiusResult { member = x.ConvertTo<string>() }).ToArray();
            }));

        public GeoRadiusResult[] GeoRadiusByMember(string key, string member, decimal radius, GeoUnit unit = GeoUnit.m,
            bool withdoord = false, bool withdist = false, bool withhash = false,
            long? count = null, Collation? collation = null) => GeoRadius("GEORADIUSBYMEMBER", key, member, null, null, radius, unit,
                withdoord, withdist, withhash, count, collation);

        public long GeoRadiusByMemberStore(string key, string member, decimal radius, GeoUnit unit = GeoUnit.m,
            long? count = null, Collation? collation = null,
            string storekey = null, string storedistkey = null)
        {
            if (string.IsNullOrWhiteSpace(storekey) && string.IsNullOrWhiteSpace(storedistkey)) throw new ArgumentNullException(nameof(storekey));
            return Call("GEORADIUSBYMEMBER"
                .InputKey(key)
                .Input(member, radius, unit)
                .InputIf(count != null, "COUNT", count)
                .InputIf(collation != null, collation)
                .InputIf(!string.IsNullOrWhiteSpace(storekey), "STORE", storekey)
                .InputIf(!string.IsNullOrWhiteSpace(storedistkey), "STOREDIST", storedistkey), rt => rt.ThrowOrValue<long>());
        }
    }

    public class GeoMember
    {
        public readonly decimal longitude;
        public readonly decimal latitude;
        public readonly string member;
        public GeoMember(decimal longitude, decimal latitude, string member) { this.longitude = longitude; this.latitude = latitude; this.member = member; }
    }

    //1) 1) "Palermo"
    //   2) "190.4424"
    //   3) (integer) 3479099956230698
    //   4) 1) "13.361389338970184"
    //      2) "38.115556395496299"
    //2) 1) "Catania"
    //   2) "56.4413"
    //   3) (integer) 3479447370796909
    //   4) 1) "15.087267458438873"
    //      2) "37.50266842333162"
    public class GeoRadiusResult
    {
        public string member;
        public decimal? dist;
        public decimal? hash;
        public decimal? longitude;
        public decimal? latitude;
    }
}
