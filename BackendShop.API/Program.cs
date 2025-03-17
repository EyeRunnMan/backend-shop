using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using BackendShop.API.Extensions;
using DotNetEnv;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

// Initialize Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
        .AddEnvironmentVariables()
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting BackendShop API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog to the application
    builder.Host.UseSerilog();

    // Load environment variables
    if (File.Exists(".env"))
    {
        Env.Load(".env");
    }

    // Initialize Firebase Admin SDK
    try
    {
        var firebaseCredential = builder.Environment.IsDevelopment() &&
                                !string.IsNullOrEmpty(builder.Configuration["Firebase:ServiceAccountKeyPath"])
            ? GoogleCredential.FromFile(builder.Configuration["Firebase:ServiceAccountKeyPath"])
            : GoogleCredential.GetApplicationDefault();

        FirebaseApp.Create(new AppOptions
        {
            Credential = firebaseCredential,
            ProjectId = builder.Configuration["Firebase:ProjectId"]
        });
    }
    catch (Exception ex)
    {
        // Log but don't crash - some functionalities will still work without Firebase Admin
        Log.Warning("Failed to initialize Firebase Admin SDK: {ErrorMessage}", ex.Message);
    }

    // Configure CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(
                    builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ??
                        (builder.Environment.IsDevelopment()
                            ? new[] { "http://localhost:3000", "https://localhost:3000" }
                            : Array.Empty<string>()))
                .WithHeaders("Authorization", "Content-Type")
                .WithMethods("GET", "POST", "PUT", "DELETE")
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        });
    });

    // Add rate limiting 
    builder.Services.AddRateLimiter(options =>
    {
        // Use this for .NET 8
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Request.Path.ToString(),
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 10,
                    QueueLimit = 0,
                    Window = TimeSpan.FromMinutes(1)
                }));

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
        };
    });

    // Add Firebase authentication services
    builder.Services.AddFirebaseAuthentication(builder.Configuration);

    // Configure controllers with Json options
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

    // Configure HTTP client factory with Polly for resilience
    builder.Services.AddHttpClient();

    // Add API documentation
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "BackendShop API",
            Version = "v1",
            Description = "API for BackendShop"
        });

        // Configure Swagger to use JWT Bearer authentication
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

        // Include XML comments
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "BackendShop.API.xml");
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // Add health checks
    builder.Services.AddHealthChecks();

    // Build the application
    var app = builder.Build();

    // Enable global error handling
    app.UseGlobalErrorHandler();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "BackendShop API v1");
        });
    }
    else
    {
        // Use HSTS in production
        app.UseHsts();
    }

    // Add security headers
    app.UseSecurityHeaders();

    // Enable CORS
    app.UseCors();

    // Enable HTTPS redirection
    app.UseHttpsRedirection();

    // Use rate limiting
    app.UseRateLimiter();

    // Add Firebase authentication middleware
    app.UseFirebaseAuth();

    // Use Serilog request logging
    app.UseSerilogRequestLogging();

    // Map controllers and health check
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health").AllowAnonymous();

    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    // Ensure to flush and close the logger
    Log.CloseAndFlush();
}