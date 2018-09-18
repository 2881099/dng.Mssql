对 System.Data.SqlClient 进行的二次封装，包含连接池、缓存

也是dotnetgen_sqlserver生成器所需sqlserver数据库基础封装

# 安装

> Install-Package dng.Mssql

# 使用

```csharp
public static System.Data.SqlClient.Executer MssqlInstance = 
    new System.Data.SqlClient.Executer(IDistributedCache, masterConnectionString, slaveConnectionStrings, ILogger);

MssqlInstance.ExecuteReader
MssqlInstance.ExecuteReaderAsync

MssqlInstance.ExecuteArray
MssqlInstance.ExecuteArrayAsync

MssqlInstance.ExecuteNonQuery
MssqlInstance.ExecuteNonQueryAsync

MssqlInstance.ExecuteScalar
MssqlInstance.ExecuteScalarAsync
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

# 读写分离

若配置了从数据库连接串，从数据库可以设置多个，访问策略为随机。从库实现了故障切换，自动恢复机制。

以下方法执行 sql 语句，为 select 开头，则默认查从数据库，反之则查主数据库。

MssqlInstance.ExecuteReader
MssqlInstance.ExecuteReaderAsync

MssqlInstance.ExecuteArray
MssqlInstance.ExecuteArrayAsync

以下方法在主数据库执行：

```csharp
MssqlInstance.ExecuteNonQuery
MssqlInstance.ExecuteNonQueryAsync

MssqlInstance.ExecuteScalar
MssqlInstance.ExecuteScalarAsync
```
