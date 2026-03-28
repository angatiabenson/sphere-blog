using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using SphereBlog.API.Middleware;
using SphereBlog.Application.Interfaces;
using SphereBlog.Application.Services;
using SphereBlog.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));

// EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// DI - Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ITagService, TagService>();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero,
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString() ?? "unknown";
                var json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = "error",
                    code = 401,
                    message = "Authentication is required to access this resource.",
                    reference = correlationId
                });
                await context.Response.WriteAsync(json);
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString() ?? "unknown";
                var json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = "error",
                    code = 403,
                    message = "You do not have permission to access this resource.",
                    reference = correlationId
                });
                await context.Response.WriteAsync(json);
            }
        };
    });

builder.Services.AddAuthorization();

// Rate Limiting
var permitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 100);
var windowSeconds = builder.Configuration.GetValue("RateLimiting:WindowInSeconds", 60);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.ContentType = "application/json";
        var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString() ?? "unknown";
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = "error",
            code = 429,
            message = "Too many requests. Please try again later.",
            reference = correlationId
        });
        await context.HttpContext.Response.WriteAsync(json, cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        var isAuthEndpoint = context.Request.Path.StartsWithSegments("/api/auth");

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: isAuthEndpoint ? $"auth_{ip}" : ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isAuthEndpoint ? 10 : permitLimit,
                Window = isAuthEndpoint
                    ? TimeSpan.FromMinutes(1)
                    : TimeSpan.FromSeconds(windowSeconds),
            }
        );
    });
});

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

// Controllers + JSON options
builder
    .Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Override default validation error response to use our envelope
        options.InvalidModelStateResponseFactory = context =>
        {
            var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString() ?? "unknown";
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key,
                    e => e.Value!.Errors.Select(err => err.ErrorMessage).ToArray()
                );

            var firstError = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "One or more validation errors occurred.";

            var response = new
            {
                status = "error",
                code = 400,
                message = firstError,
                errors,
                reference = correlationId
            };

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
        };
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System
            .Text
            .Json
            .JsonNamingPolicy
            .CamelCase;
    });

// OpenAPI + Scalar
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Sphere Blog API",
            Version = "v1",
            Description =
                "A simple blog engine REST API.",
            Contact = new OpenApiContact { Name = "Sphere Blog Support" },
        }
    );

    // JWT Bearer security scheme
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token. Obtain one via the Login endpoint.",
        }
    );

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});

var app = builder.Build();

// Apply pending migrations on startup (skip for InMemory provider used in tests)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
    {
        dbContext.Database.Migrate();
    }
    else
    {
        dbContext.Database.EnsureCreated();
    }
}

// Middleware pipeline (order matters)
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Security headers
app.Use(
    async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        await next();
    }
);

// Scalar API docs at /docs
app.UseSwagger(c =>
{
    c.RouteTemplate = "openapi/{documentName}.json";
});

app.MapScalarApiReference(
    "/docs",
    options =>
    {
        options
             .WithTitle("Sphere Blog API")
             .WithTheme(ScalarTheme.Purple)
             .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
             .WithOpenApiRoutePattern("/openapi/{documentName}.json");
    }
);

app.MapControllers();

app.Run();

public partial class Program;
