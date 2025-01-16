using APIHorario.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.InstanceName = "APIHorario-";
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(1),
        LocalCacheExpiration = TimeSpan.FromSeconds(10)
    };
});
#pragma warning restore EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "API de Contagem";
    options.Theme = ScalarTheme.BluePlanet;
    options.DarkMode = true;
});

app.Logger.LogInformation("Endpoint OpenAPI: /openapi/v1.json");
app.Logger.LogInformation("Endpoint Scalar: /scalar/v1");

app.UseHttpsRedirection();

app.MapGet("/nocache", () =>
{
    var result = new Resultado() { Mensagem = "Teste sem HybridCache" };
    app.Logger.LogInformation($"{result.HorarioAtual} {result.Mensagem}");
    return result;
}).WithDescription("Exemplo de uso sem HybridCache");

app.MapGet("/cache", async (HybridCache cache,
    CancellationToken ct = default) =>
{
    var result = await cache.GetOrCreateAsync<Resultado>(key: "entry-cache",
        factory: async _ =>
        {
            return await Task.FromResult(new Resultado() { Mensagem = "Teste com cache" });
        },
        cancellationToken: ct);
    app.Logger.LogInformation($"Em {DateTime.Now:HH:mm:ss}: ---> {result.HorarioAtual} {result.Mensagem}");
    return result;
}).WithDescription("Utilizando HybridCache");

app.MapGet("/cachequerystring", async (string valorTeste,
    HybridCache cache,
    CancellationToken ct = default) =>
{
    var result = await cache.GetOrCreateAsync<Resultado>(key: $"entry-cache-{valorTeste}",
        factory: async _ =>
        {
            return await Task.FromResult(
                new Resultado() { Mensagem = $"Teste com HybridCache | Query string: {nameof(valorTeste)} = {valorTeste}" });
        },
        options: new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromSeconds(90),
            LocalCacheExpiration = TimeSpan.FromSeconds(20)
        },
        cancellationToken: ct);
    app.Logger.LogInformation($"Em {DateTime.Now:HH:mm:ss}: ---> {result.HorarioAtual} {result.Mensagem}");
    return result;
})
.WithDescription("Utilizando HybridCache com Query String");

app.Run();