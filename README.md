This repo demonstrates how the L2 (DB) cache is removed even when fail safe is activated with Fusion cache.

# Setup

## Start the local database with Docker
```bash
docker compose up -d
```

## Init the DB
1. Connect to the DB using an SQL manager for example [Azure Data studio](https://learn.microsoft.com/en-us/azure-data-studio/download-azure-data-studio?tabs=win-install%2Cwin-user-install%2Credhat-install%2Cwindows-uninstall%2Credhat-uninstall)
  1.1. Connection string: `Server=127.0.0.1,1433;Database=master;User Id=sa;Password=Local123;TrustServerCertificate=true;`
  1.2. Specific credentials: Server IP: 127.0.0.1,1433 | User Id: sa | Password: Local123
2. Run the [Sql create DB script](./sql-create-db.sql) to create the database
3. Run the [Sql init DB script](./sql-init.sql) to create the table to fusion cache

## Run the Dotnet webapplication
Run the application via CLI or IDE

# Test
There is a Http file with the endpoints in the app. [Http file](./WebApplication1/WebApplication1.http)

# Reproduction

## Create initial value
GET http://localhost:5190/value

## Check value in DB
```sql
SELECT TOP (1000) [Id]
    ,[Value]
    ,[ExpiresAtTime]
    ,[SlidingExpirationInSeconds]
    ,[AbsoluteExpiration]
FROM [dbo].[FusionCache]
```

Example:
|Id|Value|ExpiresAtTime|SlidingExpirationInSeconds|AbsoluteExpiration|
|---|---|---|---|---|
|v2:FusionCache:__fc:t:my-tag|0x7B2256616C7565223A3633383938313132313532383739353034302C2254696D657374616D70223A3633383938313132313532383830373237372C224C6F676963616C45787069726174696F6E54696D657374616D70223A3633383938393736313532383830373237372C2254616773223A6E756C6C2C224D65746164617461223A7B2249735374616C65223A66616C73652C22456167657245787069726174696F6E54696D657374616D70223A6E756C6C2C2245546167223A6E756C6C2C224C6173744D6F64696669656454696D657374616D70223A6E756C6C2C2253697A65223A302C225072696F72697479223A337D7D|2025-11-17 11.35.52.8808298 +00:00|NULL|2025-11-17 11.35.52.8808298 +00:00|
|v2:FusionCache:my-key|0x7B2256616C7565223A7B2254696D657374616D70223A22323032352D31312D30375431313A33363A31322E353139353531345A227D2C2254696D657374616D70223A3633383938313132313732353139383033392C224C6F676963616C45787069726174696F6E54696D657374616D70223A3633383938313139333732353139383033392C2254616773223A5B226D792D746167225D2C224D65746164617461223A7B2249735374616C65223A66616C73652C22456167657245787069726174696F6E54696D657374616D70223A6E756C6C2C2245546167223A6E756C6C2C224C6173744D6F64696669656454696D657374616D70223A6E756C6C2C2253697A65223A6E756C6C2C225072696F72697479223A6E756C6C7D7D|2026-01-06 11.36.12.5201021 +00:00|NULL|2026-01-06 11.36.12.5201021 +00:00|

## Expire the tag
GET http://localhost:5190/expire

## Get value with try get first
GET http://localhost:5190/value-slow?tryGet=true

## Check value in DB
```sql
SELECT TOP (1000) [Id]
    ,[Value]
    ,[ExpiresAtTime]
    ,[SlidingExpirationInSeconds]
    ,[AbsoluteExpiration]
FROM [dbo].[FusionCache]
```

The value is now gone from L2 cache until the factory finish.