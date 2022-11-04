using Microsoft.Extensions.Caching.Memory;
using SamlMetdataProxy;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IMetadataCache>((sp) =>
{
    var memoryCache = sp.GetService<IMemoryCache>() ??
        throw new InvalidOperationException("DI did not create a memory cache.");
    var cache = new MetadataCache(memoryCache);
    cache.AddFederationsFromConfig(builder.Configuration);
    return cache;
});

var app = builder.Build();
app.UseHttpsRedirection();

app.MapGet("/{federation}", async (IMetadataCache cache, string federation, string entityId) =>
{
    return await AppHelpers.GetStreamResultAsync(cache, federation, entityId);
});

app.Run();
