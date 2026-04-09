using System.Reflection;
using System.Text.Json.Serialization;
using CreditsSim.Application;
using CreditsSim.Infrastructure;
using CreditsSim.Infrastructure.Persistence;
using CreditsSim.WebAPI.Middleware;
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
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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

app.MapControllers();
app.Run();
