using System.Reflection;
using System.Text.Json.Serialization;
using InventoryPro.Api.Common;
using InventoryPro.Api.Configuration;
using InventoryPro.Api.Extensions;
using InventoryPro.Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// =====================================================================
// 1. SERVICE REGISTRATION
// In Spring Boot this is automatic component scanning (@Service, @Repository).
// In .NET we register every dependency explicitly in the DI container here.
// =====================================================================

// Strongly-typed configuration binding (same idea as @ConfigurationProperties).
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize/deserialize enums as strings instead of numbers,
        // so the JavaScript frontend receives "Admin" instead of 0.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Route model-validation failures through our ApiResponse envelope.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = ApiValidationResponse.Build;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "InventoryPro API",
        Version = "v1",
        Description = "REST API for the InventoryPro stock management system.",
        Contact = new OpenApiContact
        {
            Name = "InventoryPro",
            Email = "admin@inventorypro.local"
        }
    });

    // Pull the /// XML comments into Swagger so every endpoint is described.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Adds the padlock button in Swagger UI so you can paste a JWT and
    // call protected endpoints. Same idea as configuring bearer auth in springdoc.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT obtained from /api/auth/login (no 'Bearer ' prefix needed)."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS: allow the plain HTML/JS frontend (served from a different origin)
// to call this API from the browser.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// Built-in health checks. Useful for load balancers (AWS ALB) and Docker.
builder.Services.AddHealthChecks();

// Application services registered by our own extension method (Phase 2+).
builder.Services.AddApplicationServices();

// EF Core DbContext: SQL Server by default, SQLite fallback via Database__Provider.
builder.Services.AddDatabase(builder.Configuration);

// JWT bearer authentication + authorization (role checks come from [Authorize(Roles = ...)]).
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

var app = builder.Build();

// =====================================================================
// 2. HTTP REQUEST PIPELINE (middleware)
// In Spring this is the Filter chain. Order matters: first registered runs first.
// =====================================================================

// Catches every exception and converts it to the standard ApiResponse envelope.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Serve the plain HTML/CSS/JavaScript frontend from wwwroot on the same origin
// as the API. Serving both from one port avoids CORS in the browser.
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "InventoryPro API v1");
        options.RoutePrefix = "swagger";
    });
}

// Only force HTTPS outside development, so local http:// preview keeps working.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("DefaultCorsPolicy");

// Authentication must run before Authorization: it reads and validates the JWT,
// then Authorization checks the [Authorize] attributes.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Create/update the schema and seed baseline data.
// SQL Server uses EF Core migrations; the SQLite dev fallback uses EnsureCreated.
if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
{
    await app.Services.InitializeDatabaseAsync();
}

app.Run();
