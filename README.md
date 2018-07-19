对 System.Data.SqlClient 进行的二次封装，包含连接池、缓存

也是dotnetgen_sqlserver生成器所需sqlserver数据库基础封装

# 安装

> Install-Package dng.Mssql

# 使用

```csharp
public static System.Data.SqlClient.Executer MssqlInstance = 
    new System.Data.SqlClient.Executer(IDistributedCache, connectionString, ILogger);

//MssqlInstance.ExecuteReader
//MssqlInstance.ExecuteReaderAsync

//ExecuteArray
//ExecuteArrayAsync

//ExecuteNonQuery
//ExecuteNonQueryAsync

//ExecuteScalar
//ExecuteScalarAsync
```

# 事务

```csharp
MssqlInstance.Transaction(() => {

});
```

# 缓存壳

```csharp
MssqlInstance.CacheShell(key, timeoutSeconds, () => {
    return dataSource;
});

MssqlInstance.RemoveCache(key);
```