using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
	{
		public RedisResult<long> GetAdd(string key, params GeoMember[] members) => Call<long>("GEOADD"
			.Input(key)
			.InputIf(members?.Any() == true, members.Select(a => new object[] { a.Longitude, a.Latitude, a.Member }).ToArray())
			.FlagKey(key));

		public RedisResult<decimal> GeoDist(string key, string member1, string member2, GeoUnit unit = GeoUnit.M) => Call<decimal>("GEOADD"
			.Input(key, member1, member2)
			.InputIf(unit != GeoUnit.M, unit)
			.FlagKey(key));
		public RedisResult<string[]> GeoHash(string key, string[] members) => Call<string[]>("GEOADD".Input(key).Input(members).FlagKey(key));
		public RedisResult<GeoMember[]> GeoPos(string key, string[] members) => Call<object>("GEOPOS".Input(key).Input(members).FlagKey(key))
			.NewValue(a => (a as object[]).Select((z, y) =>
				{
					var zarr = z as object[];
					return new GeoMember(zarr[0].ConvertTo<decimal>(), zarr[1].ConvertTo<decimal>(), members[y]);
				}).ToArray()
			);
		public RedisResult<object> GeoRadius(string key, decimal longitude, decimal latitude, decimal radius, GeoUnit unit, bool withdoord, bool withdist, bool withhash, long count, Collation? collation, string storekey, string storedistkey) => Call<object>("GEORADIUS"
			.Input(key, longitude, latitude)
			.Input(radius, unit)
			.InputIf(withdoord, "WITHCOORD")
			.InputIf(withdist, "WITHDIST")
			.InputIf(withhash, "WITHHASH")
			.InputIf(count != 0, "COUNT", count)
			.InputIf(collation != null, collation)
			.InputIf(!string.IsNullOrWhiteSpace(storekey), "STORE", storekey)
			.InputIf(!string.IsNullOrWhiteSpace(storedistkey), "STOREDIST", storedistkey)
			.FlagKey(key));
		public RedisResult<object> GeoRadiusByMember(string key, string member, decimal radius, GeoUnit unit, bool withdoord, bool withdist, bool withhash, long count, Collation? collation, string storekey, string storedistkey) => Call<object>("GEORADIUSBYMEMBER"
			.Input(key, member, radius)
			.InputRaw(unit)
			.InputIf(withdoord, "WITHCOORD")
			.InputIf(withdist, "WITHDIST")
			.InputIf(withhash, "WITHHASH")
			.InputIf(count != 0, "COUNT", count)
			.InputIf(collation != null, collation)
			.InputIf(!string.IsNullOrWhiteSpace(storekey), "STORE", storekey)
			.InputIf(!string.IsNullOrWhiteSpace(storedistkey), "STOREDIST", storedistkey)
			.FlagKey(key));
    }
}
