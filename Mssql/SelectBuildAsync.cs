﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;

namespace System.Data.SqlClient {
	partial class SelectBuild<TReturnInfo> {
		async public Task<List<TReturnInfo>> ToListAsync(int expireSeconds, string cacheKey = null) {
			string sql = null;
			string[] objNames = new string[_dals.Count - 1];
			for (int b = 1; b < _dals.Count; b++) {
				string name = _dals[b].GetType().Name;
				objNames[b - 1] = string.Concat("Obj_", name);
			}
			if (expireSeconds > 0 && string.IsNullOrEmpty(cacheKey)) {
				sql = this.ToString();
				cacheKey = sql.Substring(sql.IndexOf(" \r\nFROM ") + 8);
			}
			List<object> cacheList = expireSeconds > 0 ? new List<object>() : null;
			return await _exec.CacheShellAsync(cacheKey, expireSeconds, async () => {
				List<TReturnInfo> ret = new List<TReturnInfo>();
				if (string.IsNullOrEmpty(sql)) sql = this.ToString();
				await _exec.ExecuteReaderAsync(async dr => {
					int dataIndex = -1;
					if (_skip > 0) dataIndex++;
					var read = await _dals[0].GetItemAsync(dr, dataIndex);
					TReturnInfo info = (TReturnInfo) read.result;
					dataIndex = read.dataIndex;
					Type type = info.GetType();
					ret.Add(info);
					if (cacheList != null) cacheList.Add(type.GetMethod("Stringify").Invoke(info, null));
					var fillps = new List<(string memberAccessPath, object value)>();
					for (int b = 0; b < objNames.Length; b++) {
						var read2 = await _dals[b + 1].GetItemAsync(dr, dataIndex);
						object obj = read2.result;
						dataIndex = read2.dataIndex;
						var alias = _dalsAlias[b + 1];
						fillps.Add((alias.StartsWith("[Obj_") && alias.EndsWith("]") ? alias.Trim('[', ']') : objNames[b], obj));
						if (cacheList != null) cacheList.Add(obj?.GetType().GetMethod("Stringify").Invoke(obj, null));
					}
					fillps.Sort((x, y) => x.memberAccessPath.Length.CompareTo(y.memberAccessPath.Length));
					foreach (var fillp in fillps)
						FillPropertyValue(info, fillp.memberAccessPath, fillp.value);
					int dataCount = dr.FieldCount;
					object[] overValue = new object[dataCount - dataIndex - 1];
					for (var a = 0; a < overValue.Length; a++) overValue[a] = await dr.IsDBNullAsync(++dataIndex) ? null : await dr.GetFieldValueAsync<object>(dataIndex);
					if (overValue.Length > 0) type.GetProperty("OverValue")?.SetValue(info, overValue, null);
				}, CommandType.Text, sql, _params.ToArray());
				return ret;
			}, list => JsonConvert.SerializeObject(cacheList), cacheValue => ToListDeserialize(cacheValue, objNames));
		}
		async public Task<List<TReturnInfo>> ToListAsync() {
			return await this.ToListAsync(0);
		}
		async public Task<TReturnInfo> ToOneAsync() {
			List<TReturnInfo> ret = await this.Limit(1).ToListAsync();
			return ret.Count > 0 ? ret[0] : default(TReturnInfo);
		}
		/// <summary>
		/// 查询指定字段，返回元组或单值
		/// </summary>
		/// <typeparam name="T">元组或单值，如：.Aggregate&lt;(int id, string name)&gt;("id,title")，或 .Aggregate&lt;int&gt;("id")</typeparam>
		/// <param name="fields">返回的字段，用逗号分隔，如：id,name</param>
		/// <param name="parms">输入参数可以是任意对象，或者SqlParameter</param>
		/// <returns></returns>
		async public Task<List<T>> AggregateAsync<T>(string fields, params object[] parms) {
			string where = string.IsNullOrEmpty(_where) ? string.Empty : string.Concat(" \r\nWHERE ", _where.Substring(5));
			string having = string.IsNullOrEmpty(_groupby) ||
							string.IsNullOrEmpty(_having) ? string.Empty : string.Concat(" \r\nHAVING ", _having.Substring(5));
			string top = _limit > 0 ? $"TOP {_skip + _limit} " : string.Empty;
			string rownum = _skip > 0 ? $"ROW_NUMBER() OVER({_orderby}) AS rownum, " : string.Empty;
			string sql = string.Concat(_select, top, rownum, this.ParseCondi(fields, parms), _overField, _table, _join, where, _groupby, having, _skip > 0 ? string.Empty : _orderby);
			if (_skip > 0) sql = $"WITH t AS ( {sql} ) SELECT t.* FROM t WHERE rownum > {_skip}";

			List<T> ret = new List<T>();
			Type type = typeof(T);

			await _exec.ExecuteReaderAsync(async dr => {
				int dataIndex = -1;
				if (_skip > 0) dataIndex++;
				var read = await this.AggregateReadTupleAsync(type, dr, dataIndex);
				ret.Add(read.result == null ? default(T) : (T)read.result);
				dataIndex = read.dataIndex;
			}, CommandType.Text, sql, _params.ToArray());
			return ret;
		}
		async public Task<T> AggregateScalarAsync<T>(string field, params object[] parms) {
			var items = await this.AggregateAsync<T>(field, parms);
			return items.Count > 0 ? items[0] : default(T);
		}
		async private Task<(object result, int dataIndex)> AggregateReadTupleAsync(Type type, SqlDataReader dr, int dataIndex) {
			bool isTuple = type.Namespace == "System" && type.Name.StartsWith("ValueTuple`");
			if (isTuple) {
				FieldInfo[] fs = type.GetFields();
				Type[] types = new Type[fs.Length];
				object[] parms = new object[fs.Length];
				for (int a = 0; a < fs.Length; a++) {
					types[a] = fs[a].FieldType;
					var read = await this.AggregateReadTupleAsync(types[a], dr, dataIndex);
					parms[a] = read.result;
					dataIndex = read.dataIndex;
				}
				ConstructorInfo constructor = type.GetConstructor(types);
				return (constructor.Invoke(parms), dataIndex);
			}
			return (await dr.IsDBNullAsync(++dataIndex) ? null : await dr.GetFieldValueAsync<object>(dataIndex), dataIndex);
		}
		/// <summary>
		/// 执行SQL，若查询语句存在记录则返回 true，否则返回 false
		/// </summary>
		/// <returns></returns>
		async public Task<bool> AnyAsync() => await this.AggregateScalarAsync<int>("1") == 1;
		async public Task<int> CountAsync() {
			return await this.AggregateScalarAsync<int>("count(1)");
		}
	}
}