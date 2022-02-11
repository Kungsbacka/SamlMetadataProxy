using Microsoft.Extensions.Caching.Memory;
using SamlMetdataProxy;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IMetadataCache>((sp) =>
{
    var memoryCache = sp.GetService<IMemoryCache>() ??
        throw new InvalidOperationException("DI did not create a memory cache.");
    var cache = new MetadataCache(memoryCache);
    cache.AddFederationFromConfig(builder.Configuration, "SwedenConnect");
    cache.AddFederationFromConfig(builder.Configuration, "Skolfederation");
    return cache;
});

var app = builder.Build();
app.UseHttpsRedirection();

app.MapGet("/{federation}", async (IMetadataCache cache, string federation, string entityId) =>
{
    return await AppHelpers.GetStreamResult(cache, federation, entityId);
});

app.Run();
