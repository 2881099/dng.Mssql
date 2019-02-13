using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;

namespace System.Data.SqlClient {
	partial class Executer {

		public static string Addslashes(string filter, params object[] parms) {
			if (filter == null || parms == null) return string.Empty;
			if (parms.Length == 0) return filter;
			object[] nparms = new object[parms.Length];
			for (int a = 0; a < parms.Length; a++) {
				if (parms[a] == null) nparms[a] = "NULL";
				else {
					if (parms[a] is bool || parms[a] is bool?)
						nparms[a] = (bool) parms[a] ? 1 : 0;
					else if (parms[a] is string || parms[a] is Enum)
						nparms[a] = string.Concat("'", parms[a].ToString().Replace("'", "''"), "'");
					else if (decimal.TryParse(string.Concat(parms[a]), out decimal trydec))
						nparms[a] = parms[a];
					else if (parms[a] is DateTime) {
						DateTime dt = (DateTime) parms[a];
						nparms[a] = string.Concat("'", dt.ToString("yyyy-MM-dd HH:mm:ss.fff"), "'");
					} else if (parms[a] is DateTime?) {
						DateTime? dt = parms[a] as DateTime?;
						nparms[a] = string.Concat("'", dt.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"), "'");
					} else if (parms[a] is IEnumerable) {
						string sb = "";
						var ie = parms[a] as IEnumerable;
						foreach (var z in ie) sb += z == null ? string.Concat(",NULL") : string.Concat(",'", z.ToString().Replace("'", "''"), "'");
						nparms[a] = string.IsNullOrEmpty(sb) ? sb : sb.Substring(1);
					} else {
						nparms[a] = string.Concat("'", parms[a].ToString().Replace("'", "''"), "'");
						//if (parms[a] is string) nparms[a] = string.Concat('N', nparms[a]);
					}
				}
			}
			try { string ret = string.Format(filter, nparms); return ret; } catch { return filter; }
		}

		private static DateTime dt1970 = new DateTime(1970, 1, 1);
		private static ThreadLocal<Random> rnd = new ThreadLocal<Random>(() => new Random());
		private static readonly int __staticMachine = ((0x00ffffff & Environment.MachineName.GetHashCode()) +
#if NETSTANDARD1_5 || NETSTANDARD1_6
			1
#else
			AppDomain.CurrentDomain.Id
#endif
			) & 0x00ffffff;
		private static readonly int __staticPid = Process.GetCurrentProcess().Id;
		private static int __staticIncrement = rnd.Value.Next();
		/// <summary>
		/// 生成类似Mongodb的ObjectId有序、不重复Guid
		/// </summary>
		/// <returns></returns>
		public static Guid NewMongodbId() {
			var now = DateTime.Now;
			var uninxtime = (int) now.Subtract(dt1970).TotalSeconds;
			int increment = Interlocked.Increment(ref __staticIncrement) & 0x00ffffff;
			var rand = rnd.Value.Next(0, int.MaxValue);
			var guid = $"{uninxtime.ToString("x8").PadLeft(8, '0')}{__staticMachine.ToString("x8").PadLeft(8, '0').Substring(2, 6)}{__staticPid.ToString("x8").PadLeft(8, '0').Substring(6, 2)}{increment.ToString("x8").PadLeft(8, '0')}{rand.ToString("x8").PadLeft(8, '0')}";
			return Guid.Parse(guid);
		}
	}
}

public static partial class Npgsql_ExtensionMethods {
	public static string To1010(this BitArray ba) {
		char[] ret = new char[ba.Length];
		for (int a = 0; a < ba.Length; a++) ret[a] = ba[a] ? '1' : '0';
		return new string(ret);
	}
}