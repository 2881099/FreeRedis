using FreeRedis.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FreeRedis
{
    partial class RedisClient
	{
		public long GeoAdd(string key, params GeoMember[] members) => Call<long>("GEOADD"
			.Input(key)
			.InputIf(members?.Any() == true, members.Select(a => new object[] { a.longitude, a.latitude, a.member }).ToArray())
			.FlagKey(key), rt => rt.ThrowOrValue());

		public decimal? GeoDist(string key, string member1, string member2, GeoUnit unit = GeoUnit.m) => Call<decimal?>("GEODIST"
			.Input(key, member1, member2)
			.InputIf(unit != GeoUnit.m, unit)
			.FlagKey(key), rt => rt.ThrowOrValue());

		public string GeoHash(string key, string member) => GeoHash(key, new[] { member }).FirstOrDefault();
		public string[] GeoHash(string key, string[] members) => Call<string[]>("GEOHASH".Input(key).Input(members).FlagKey(key), rt => rt.ThrowOrValue());

		public GeoMember GeoPos(string key, string member) => GeoPos(key, new[] { member }).FirstOrDefault();
		public GeoMember[] GeoPos(string key, string[] members) => Call<object, GeoMember[]>("GEOPOS".Input(key).Input(members).FlagKey(key), rt => rt
			.NewValue(a => (a as List<object>).Select((z, y) =>
				{
					if (z == null) return null;
					var zarr = z as List<object>;
					return new GeoMember(zarr[0].ConvertTo<decimal>(), zarr[1].ConvertTo<decimal>(), members[y]);
				}).ToArray()
			).ThrowOrValue());

		public GeoRadiusResult[] GeoRadius(string key, decimal longitude, decimal latitude, decimal radius, GeoUnit unit = GeoUnit.m,
			bool withdoord = false, bool withdist = false, bool withhash = false,
			long? count = null, Collation? collation = null) => GeoRadius("GEORADIUS", key, null, longitude, latitude, radius, unit,
				withdoord, withdist, withhash, count, collation);
		public long GeoRadiusStore(string key, decimal longitude, decimal latitude, decimal radius, GeoUnit unit = GeoUnit.m, 
			long? count = null, Collation? collation = null, 
			string storekey = null, string storedistkey = null)
		{
			if (string.IsNullOrWhiteSpace(storekey) && string.IsNullOrWhiteSpace(storedistkey)) throw new ArgumentNullException(nameof(storekey));
			return Call<long>("GEORADIUS"
				.Input(key, longitude, latitude)
				.Input(radius, unit)
				.InputIf(count != null, "COUNT", count)
				.InputIf(collation != null, collation)
				.InputIf(!string.IsNullOrWhiteSpace(storekey), "STORE", storekey)
				.InputIf(!string.IsNullOrWhiteSpace(storedistkey), "STOREDIST", storedistkey)
				.FlagKey(key), rt => rt.ThrowOrValue());
		}
		
		GeoRadiusResult[] GeoRadius(string cmd, string key, string member, decimal? longitude, decimal? latitude, decimal radius, GeoUnit unit = GeoUnit.m,
			bool withdoord = false, bool withdist = false, bool withhash = false,
			long? count = null, Collation? collation = null) => Call<object, GeoRadiusResult[]>(cmd
			.Input(key)
			.InputIf(!string.IsNullOrEmpty(member), member)
			.InputIf(longitude != null && latitude != null, longitude, latitude)
			.InputRaw(radius)
			.InputRaw(unit)
			.InputIf(withdoord, "WITHCOORD")
			.InputIf(withdist, "WITHDIST")
			.InputIf(withhash, "WITHHASH")
			.InputIf(count != null, "COUNT", count)
			.InputIf(collation != null, collation)
			.FlagKey(key), rt => rt.NewValue(a =>
			{
				if (withdoord || withdist || withhash)
				{
					var objs = a as List<object>;
					return objs.Select(x =>
					{
						var objs2 = x as List<object>;
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
				}
				return (a as List<object>).Select(x => new GeoRadiusResult { member = x.ConvertTo<string>() }).ToArray();
			}).ThrowOrValue());

		public GeoRadiusResult[] GeoRadiusByMember(string key, string member, decimal radius, GeoUnit unit = GeoUnit.m,
			bool withdoord = false, bool withdist = false, bool withhash = false,
			long? count = null, Collation? collation = null) => GeoRadius("GEORADIUSBYMEMBER", key, member, null, null, radius, unit,
				withdoord, withdist, withhash, count, collation);
		public long GeoRadiusByMemberStore(string key, string member, decimal radius, GeoUnit unit = GeoUnit.m,
			long? count = null, Collation? collation = null,
			string storekey = null, string storedistkey = null)
		{
			if (string.IsNullOrWhiteSpace(storekey) && string.IsNullOrWhiteSpace(storedistkey)) throw new ArgumentNullException(nameof(storekey));
			return Call<long>("GEORADIUSBYMEMBER"
				.Input(key, member, radius)
				.InputRaw(unit)
				.InputIf(count != null, "COUNT", count)
				.InputIf(collation != null, collation)
				.InputIf(!string.IsNullOrWhiteSpace(storekey), "STORE", storekey)
				.InputIf(!string.IsNullOrWhiteSpace(storedistkey), "STOREDIST", storedistkey)
				.FlagKey(key), rt => rt.ThrowOrValue());
		}
	}
}
