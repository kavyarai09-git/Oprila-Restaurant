// ================================================
// FileName:        Program.cs
// Project:         Restaurant Booking System
// Description:     Main entry point for the Restaurant API application.
// Created:         23/04/2026
// Modified:        23/04/2026
// API Version:     v1.0.0
// Author:          Deviprasad Rai P (deviprasadr@ccsym.com)
// Last Modified:   Deviprasad Rai P (deviprasadr@ccsym.com)
// ================================================

using System.Text;
using AspNetCoreRateLimit;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RestaurantApi.AppointmentBooking.Services;
using RestaurantApi.Auth.Services;
using RestaurantApi.Menu.Services;
using RestaurantApi.Shared.Data;
using RestaurantApi.Shared.Middleware;
using Serilog;
using Serilog.Events;



// ═══════════════════════════════════════════════════════════════════
//  SERILOG – configure before anything else
// ═══════════════════════════════════════════════════════════════════
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/restaurant-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting RestaurantAPI");
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ═══════════════════════════════════════════════════════════════
    //  CONFIGURATION VALIDATION
    // ═══════════════════════════════════════════════════════════════
    var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

    if (jwtKey.Length < 32)
        throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");

    // ═══════════════════════════════════════════════════════════════
    //  DATABASE
    // ═══════════════════════════════════════════════════════════════
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlite(builder.Configuration.GetConnectionString("Default")
               ?? "Data Source=restaurant.db",
            b => b.MigrationsAssembly("RestaurantApi"))
           .EnableDetailedErrors(builder.Environment.IsDevelopment())
           .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));

    // ═══════════════════════════════════════════════════════════════
    //  APPLICATION SERVICES
    // ═══════════════════════════════════════════════════════════════
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IMenuService, MenuService>();
    builder.Services.AddScoped<IAppointmentService, AppointmentService>();
    builder.Services.AddScoped<IVoiceBookingService, VoiceBookingService>();
    builder.Services.AddScoped<ISpeechService, SpeechService>();
    builder.Services.AddHttpClient("OpenAI");

    // ═══════════════════════════════════════════════════════════════
    //  JWT AUTHENTICATION
    // ═══════════════════════════════════════════════════════════════
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

    builder.Services.AddAuthorization();

    // ═══════════════════════════════════════════════════════════════
    //  RATE LIMITING (per IP)
    // ═══════════════════════════════════════════════════════════════
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(opt =>
    {
        opt.EnableEndpointRateLimiting = true;
        opt.StackBlockedRequests = false;
        opt.HttpStatusCode = 429;
        opt.RealIpHeader = "X-Real-IP";
        opt.GeneralRules = new List<RateLimitRule>
        {
            new() { Endpoint = "*",                      Period = "1m",  Limit = 120 },
            new() { Endpoint = "POST:/api/auth/login",   Period = "5m",  Limit = 10  },
            new() { Endpoint = "POST:/api/appointments", Period = "1m",  Limit = 20  },
            new() { Endpoint = "POST:/api/voice/*",      Period = "1m",  Limit = 30  }
        };
    });
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    // ═══════════════════════════════════════════════════════════════
    //  CORS
    // ═══════════════════════════════════════════════════════════════
    builder.Services.AddCors(opt =>
    {
        opt.AddPolicy("WebApp", p =>
            p.WithOrigins(
                    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:3000", "http://localhost:5173" })
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials());

        opt.AddPolicy("TwilioWebhook", p =>
            p.AllowAnyOrigin().AllowAnyHeader().WithMethods("POST"));
    });

    // ═══════════════════════════════════════════════════════════════
    //  CONTROLLERS + VALIDATION
    // ═══════════════════════════════════════════════════════════════
    builder.Services
        .AddControllers()
        .ConfigureApiBehaviorOptions(opt =>
        {
            // Return our custom envelope instead of the default validation ProblemDetails
            opt.InvalidModelStateResponseFactory = ctx =>
            {
                var errors = ctx.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        k => k.Key,
                        v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                var result = new Microsoft.AspNetCore.Mvc.ObjectResult(new
                {
                    success = false,
                    message = "Validation failed.",
                    errors,
                    timestamp = DateTime.UtcNow.ToString("o")
                })
                { StatusCode = 422 };
                return result;
            };
        });

    builder.Services.AddEndpointsApiExplorer();

    // ═══════════════════════════════════════════════════════════════
    //  SWAGGER / OPENAPI
    // ═══════════════════════════════════════════════════════════════
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "🍽 Restaurant Booking API",
            Version = "v1",
            Description = """
                ## Production-ready REST API for restaurant appointment management

                ### Features
                | Feature | Details |
                |---------|---------|
                | **Menu Management** | Categories, items, dietary flags, availability toggle |
                | **Table Booking** | Availability engine, auto-table assignment, multi-filter |
                | **AI Phone Booking** | OpenAI conversational flow via direct API or Twilio |
                | **Twilio Integration** | Real phone-call TwiML webhooks (speech-to-text) |
                | **Admin Dashboard** | Stats, source breakdown, today's schedule |
                | **JWT Auth** | Admin / Staff role separation |
                | **Rate Limiting** | Per-IP, per-endpoint rules |
                | **Structured Logging** | Serilog → Console + rolling file |
                | **Health Checks** | /health endpoint for load-balancer probes |

                ### Default Credentials
                | Username | Password | Role |
                |----------|----------|------|
                | admin | Admin@1234 | Admin |
                | staff | Staff@1234 | Staff |
                """
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization – Enter: **Bearer {token}**",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer"), new List<string>()
            }
        });

        var xmlPath = Path.Combine(AppContext.BaseDirectory, "RestaurantAPI.xml");
        if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
    });

    // ═══════════════════════════════════════════════════════════════
    //  HEALTH CHECKS
    // ═══════════════════════════════════════════════════════════════
    builder.Services.AddHealthChecks();

    // ═══════════════════════════════════════════════════════════════
    //  BUILD
    // ═══════════════════════════════════════════════════════════════

    var app = builder.Build();

    // ── Auto-migrate on startup ───────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Database migration applied successfully.");
    }

    // ═══════════════════════════════════════════════════════════════
    //  MIDDLEWARE PIPELINE  (order matters!)
    // ═══════════════════════════════════════════════════════════════

    // 1. Global exception handler (outermost)
    app.UseMiddleware<ExceptionMiddleware>();

    // 2. Request logging
    app.UseMiddleware<RequestLoggingMiddleware>();

    // 3. Rate limiting
    app.UseIpRateLimiting();

    // 4. HTTPS redirect (when behind TLS terminator, disable this and handle at proxy)
    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    // 5. CORS
    app.UseCors("WebApp");

    // 6. Swagger (always on; restrict to internal in prod via reverse proxy)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurant API v1");
        c.RoutePrefix = string.Empty;   // Swagger at root
        c.DocumentTitle = "Restaurant API";
        c.DisplayRequestDuration();
        c.EnableFilter();
        c.EnableDeepLinking();
        c.DefaultModelsExpandDepth(-1);        // Collapse schemas by default
    });

    // 7. Auth
    app.UseAuthentication();
    app.UseAuthorization();

    // 8. Health check endpoint
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // 9. Controllers
    app.MapControllers();

    Log.Information("RestaurantAPI is ready on {Urls}", string.Join(", ", app.Urls));
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "RestaurantAPI terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync();
}