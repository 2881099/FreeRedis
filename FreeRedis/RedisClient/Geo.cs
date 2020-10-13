using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
	{
		public RedisResult<long> GetAdd(string key, params GeoMember[] members) => Call<long>("GEOADD", key, ""
			.AddIf(members?.Any() == true, members.Select(a => new object[] { a.Longitude, a.Latitude, a.Member }).ToArray())
			.ToArray());

		public RedisResult<decimal> GeoDist(string key, string member1, string member2, GeoUnit unit = GeoUnit.M) => Call<decimal>("GEOADD", key, ""
			.AddIf(true, member1, member2)
			.AddIf(unit != GeoUnit.M, unit)
			.ToArray());
		public RedisResult<string[]> GeoHash(string key, string[] members) => Call<string[]>("GEOADD", key, "".AddIf(members?.Any() == true, members).ToArray());
		public RedisResult<GeoMember[]> GeoPos(string key, string[] members) => Call<object>("GEOPOS", key, "".AddIf(members?.Any() == true, members).ToArray())
			.NewValue(a => (a as object[]).Select((z, y) =>
				{
					var zarr = z as object[];
					return new GeoMember(zarr[0].ConvertTo<decimal>(), zarr[1].ConvertTo<decimal>(), members[y]);
				}).ToArray()
			);
		public RedisResult<object> GeoRadius(string key, decimal longitude, decimal latitude, decimal radius, GeoUnit unit, bool withdoord, bool withdist, bool withhash, long count, Collation? collation, string storekey, string storedistkey) => Call<object>("GEORADIUS", key, ""
			.AddIf(true, longitude, latitude, radius, unit)
			.AddIf(withdoord, "WITHCOORD")
			.AddIf(withdist, "WITHDIST")
			.AddIf(withhash, "WITHHASH")
			.AddIf(count != 0, "COUNT", count)
			.AddIf(collation != null, collation)
			.AddIf(!string.IsNullOrWhiteSpace(storekey), "STORE", storekey)
			.AddIf(!string.IsNullOrWhiteSpace(storedistkey), "STOREDIST", storedistkey)
			.ToArray());
		public RedisResult<object> GeoRadiusByMember(string key, string member, decimal radius, GeoUnit unit, bool withdoord, bool withdist, bool withhash, long count, Collation? collation, string storekey, string storedistkey) => Call<object>("GEORADIUSBYMEMBER", key, ""
			.AddIf(true, member, radius, unit)
			.AddIf(withdoord, "WITHCOORD")
			.AddIf(withdist, "WITHDIST")
			.AddIf(withhash, "WITHHASH")
			.AddIf(count != 0, "COUNT", count)
			.AddIf(collation != null, collation)
			.AddIf(!string.IsNullOrWhiteSpace(storekey), "STORE", storekey)
			.AddIf(!string.IsNullOrWhiteSpace(storedistkey), "STOREDIST", storedistkey)
			.ToArray());
    }
}
