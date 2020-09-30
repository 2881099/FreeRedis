using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeRedis
{
	public partial class RedisClient : RedisClientBase, IDisposable
	{
		public RedisClient(string host, bool ssl) : base(host, ssl) { }

		public void Dispose()
        {
			base.SafeReleaseSocket();
        }
    }

	public enum ClusterSetSlotType { Importing, Migrating, Stable, Node }
	public enum ClusterResetType { Hard, Soft }
	public enum ClusterFailOverType { Force, TakeOver }
	public enum ClientUnBlockType { Timeout, Error }
	public enum ClientReplyType { On, Off, Skip }
	public enum ClientType { Normal, Master, Slave, PubSub }
	public enum Confirm { Yes, No }
	public enum GeoUnit { M, KM, MI, FT }
	public enum Collation { Asc, Desc }
	public enum InsertDirection { Before, After }
	public enum ScriptDebugOption { Yes, Sync, No }
	public enum BitOpOperation { And, Or, Xor, Not }
	public class GeoMember
	{
		public decimal Longitude { get; set; }
		public decimal Latitude { get; set; }
		public string Member { get; set; }

		public GeoMember(decimal longitude, decimal latitude, string member) { Longitude = longitude; Latitude = latitude; Member = member; }
	}
	public class ScanValue<T>
	{
		public long Cursor { get; set; }
		public T[] Items { get; set; }
		public ScanValue(long cursor, T[] items) { Cursor = cursor; Items = items; }
	}
	public class SortedSetMember<T>
	{
		public T Member { get; set; }
		public decimal Score { get; set; }
		public SortedSetMember(T member, decimal score) { this.Member = member; this.Score = score; }
	}
}
