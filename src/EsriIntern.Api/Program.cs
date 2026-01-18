using EsriIntern.Api.Background;
using EsriIntern.Api.Data;
using EsriIntern.Api.Health;
using EsriIntern.Api.Middleware;
using EsriIntern.Api.Services;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация
builder.Services.Configure<ArcGisOptions>(builder.Configuration.GetSection("ArcGis"));
builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("Worker"));

// Валидация на конфигурацията
builder.Services.AddOptions<ArcGisOptions>()
    .Bind(builder.Configuration.GetSection("ArcGis"))
    .ValidateDataAnnotations()
    .Validate(o => !string.IsNullOrWhiteSpace(o.LayerUrl), "ArcGis:LayerUrl is required")
    .ValidateOnStart();

builder.Services.AddOptions<WorkerOptions>()
    .Bind(builder.Configuration.GetSection("Worker"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Controllers и API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ESRI Demographics API",
        Version = "v1",
        Description = "API за демографски данни по щати (население)"
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Database (SQLite) - подсигуряваме папка data в ContentRoot
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDir);

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("Sqlite") ?? $"Data Source={Path.Combine(dataDir, "app.db")}";
    opt.UseSqlite(cs);
});

// Polly retry политика
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger<ArcGisCountiesClient>? logger = null)
{
    return Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .Or<TaskCanceledException>()
        .OrResult(msg =>
            msg.StatusCode == HttpStatusCode.InternalServerError ||
            msg.StatusCode == HttpStatusCode.BadGateway ||
            msg.StatusCode == HttpStatusCode.ServiceUnavailable ||
            msg.StatusCode == HttpStatusCode.GatewayTimeout ||
            msg.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                logger?.LogWarning("Retrying HTTP request after {Delay}ms (attempt {RetryCount})",
                    timespan.TotalMilliseconds, retryCount);
            });
}

builder.Services.AddHttpClient<ArcGisCountiesClient>((sp, client) =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.Add("User-Agent", "EsriIntern-Backend-Demo/1.0");
})
.AddPolicyHandler((sp, _) =>
{
    var logger = sp.GetRequiredService<ILogger<ArcGisCountiesClient>>();
    return GetRetryPolicy(logger);
})
.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(60));

// Services
builder.Services.AddScoped<DemographicsRefresher>();

// Background worker
builder.Services.AddHostedService<DemographicsWorker>();

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck<DataHealthCheck>("database", tags: new[] { "db", "sqlite" });

// Response caching
builder.Services.AddResponseCaching();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<RequestLoggingMiddleware>();
}

app.UseResponseCaching();
app.UseCors();

// Ensure DB exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Swagger (Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ESRI Demographics API v1");
        c.RoutePrefix = "swagger"; // <-- FIX: UI е на /swagger (а не на root)
    });
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
