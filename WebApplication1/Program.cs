using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.SqlServer;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFusionCache("FusionCache")
    .WithDefaultEntryOptions(options =>
    {
        options.SetDuration(TimeSpan.FromSeconds(1));
        options.SetDistributedCacheDuration(TimeSpan.FromHours(2));
        options.SetFactoryTimeouts(
            softTimeout: TimeSpan.FromMilliseconds(50),
            hardTimeout: TimeSpan.FromSeconds(45),
            keepTimedOutFactoryResult: true
        );
        options.SetFailSafe(
            isEnabled: true,
            maxDuration: TimeSpan.FromMinutes(5),
            throttleDuration: TimeSpan.FromSeconds(5)
        );
        options.SetDistributedCacheFailSafeOptions(TimeSpan.FromDays(60));
    })
    .WithCacheKeyPrefixByCacheName()
    .WithSerializer(new FusionCacheSystemTextJsonSerializer())
    .WithDistributedCache(new SqlServerCache(Options.Create(new SqlServerCacheOptions
    {
        ConnectionString = "Server=127.0.0.1,1433;Database=Fusion;User Id=sa;Password=Local123;TrustServerCertificate=true;",
        SchemaName = "dbo",
        TableName = "FusionCache"
    })));


var app = builder.Build();

app.MapGet("/value", async ([FromQuery(Name = "tryGet")] bool? tryGet) =>
{
    var cache = app.Services.GetRequiredService<IFusionCacheProvider>().GetCache("FusionCache");

    if (tryGet == true)
    {
        _ = await cache.TryGetAsync<Result?>("my-key");
    }

    Func<FusionCacheFactoryExecutionContext<Result?>, CancellationToken, Task<Result?>> factory = async (fusionContext, ct) =>
    {
        fusionContext.Tags = ["my-tag"];

        await Task.Delay(100, ct);

        return new Result();
    };

    MaybeValue<Result?> result = await cache.GetOrSetAsync("my-key", factory, failSafeDefaultValue: null);

    return Results.Ok(result);
});

app.MapGet("/value-slow", async ([FromQuery(Name = "tryGet")] bool? tryGet) =>
{
    var cache = app.Services.GetRequiredService<IFusionCacheProvider>().GetCache("FusionCache");

    if (tryGet == true)
    {
        _ = await cache.TryGetAsync<Result?>("my-key");
    }

    Func<FusionCacheFactoryExecutionContext<Result?>, CancellationToken, Task<Result?>> factory = async (fusionContext, ct) =>
    {
        fusionContext.Tags = ["my-tag"];

        await Task.Delay(10000, ct);
        await Task.Delay(10000, ct);
        await Task.Delay(10000, ct);
        await Task.Delay(10000, ct);
        await Task.Delay(10000, ct);

        return new Result();
    };

    MaybeValue<Result?> result = await cache.GetOrSetAsync("my-key", factory, failSafeDefaultValue: null);

    return Results.Ok(result);
});

app.MapGet("/expire", async () =>
{
    var cache = app.Services.GetRequiredService<IFusionCacheProvider>().GetCache("FusionCache");
    await cache.RemoveByTagAsync("my-tag");
    return Results.Ok();
});

app.Run();

internal record Result
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}