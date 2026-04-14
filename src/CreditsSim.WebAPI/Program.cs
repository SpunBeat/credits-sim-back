using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using CreditsSim.Application;
using CreditsSim.Infrastructure;
using CreditsSim.Infrastructure.Persistence;
using CreditsSim.WebAPI.Middleware;
using CreditsSim.WebAPI.Swagger;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Layers ────────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── API + JSON serialization ──────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ── Rate Limiting ─────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    // Partitioned by client IP — FixedWindow: 10 req/min, queue 2
    options.AddPolicy("SimulationsPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }));

    // Custom 429 response
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        ctx.HttpContext.Response.ContentType = "application/problem+json";

        var retryAfter = ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
            ? retry
            : TimeSpan.FromMinutes(1);

        ctx.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc6585#section-4",
            title = "Too Many Requests",
            status = 429,
            detail = $"Se excedio el limite de peticiones. Intenta de nuevo en {(int)retryAfter.TotalSeconds} segundos.",
        };

        await ctx.HttpContext.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }), ct);
    };
});

// ── Swagger / OpenAPI ─────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Credits Simulator API",
        Version = "v1",
        Description = "API para simulación de créditos personales con sistema de amortización francés (cuotas constantes).",
        Contact = new OpenApiContact
        {
            Name = "Credits Simulator",
            Url = new Uri("https://github.com/credits-sim")
        }
    });

    // Include XML comments from WebAPI and Application assemblies
    var webApiXml = Path.Combine(AppContext.BaseDirectory, "CreditsSim.WebAPI.xml");
    if (File.Exists(webApiXml))
        options.IncludeXmlComments(webApiXml, includeControllerXmlComments: true);

    var appXml = Path.Combine(AppContext.BaseDirectory, "CreditsSim.Application.xml");
    if (File.Exists(appXml))
        options.IncludeXmlComments(appXml);

    // Enable [SwaggerOperation] annotations
    options.EnableAnnotations();

    // Non-nullable C# properties → required in OpenAPI schema
    options.SchemaFilter<RequireNonNullableSchemaFilter>();

    // Enums as strings in the schema
    options.UseInlineDefinitionsForEnums();

    // Use fully qualified names to avoid schema collisions with generics
    options.CustomSchemaIds(type =>
    {
        if (!type.IsGenericType) return type.Name;
        var genericArgs = string.Join("", type.GetGenericArguments().Select(a => a.Name));
        return $"{type.Name.Split('`')[0]}{genericArgs}";
    });
});

// ── CORS ──────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ── Auto-migrate ──────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ── Middleware pipeline ───────────────────────────────────────────
app.UseExceptionHandler();
app.UseCors();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "swagger/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Credits Simulator API v1");
        c.DocumentTitle = "Credits Simulator — API Docs";
        c.DefaultModelsExpandDepth(2);
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    });
}

// Health check — exempt from rate limiting
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .DisableRateLimiting();

app.MapControllers();
app.Run();
