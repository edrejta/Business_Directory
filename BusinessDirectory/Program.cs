using System.Text;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Application.Options;
using Infrastructure;
using Infrastructure.Services;
using Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using BusinessDirectory.Application.Dtos;
using BusinessDirectory.OpenApi;
using System.Data.Common;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
    options.IncludeScopes = false;
});

// Të gjitha endpoint-et kërkojnë JWT (401 pa token). Për public: [AllowAnonymous].
builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.Filters.Add(new AuthorizeFilter(policy));
})
.ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var firstError = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault();

        return new BadRequestObjectResult(new ErrorResponseDto
        {
            Message = string.IsNullOrWhiteSpace(firstError) ? "Input i pavlefshem." : firstError
        });
    };
});

// Policy "AdminOnly" – për endpoint-et që duhet vetëm Admin (403 për jo-admin).
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Business Directory Backend API",
        Version = "1.0.0",
        Description = "API contract consumed by current frontend (auth, homepage, admin dashboard)."
    });

    options.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.OperationFilter<BearerAuthOperationFilter>();
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ErrorResponseDto { Message = "Shume kerkesa. Provo perseri me vone." },
            cancellationToken: token);
    };

    options.AddPolicy("auth-login", httpContext =>
    {
        var emailKey = ExtractLoginEmailForRateLimit(httpContext);
        var key = $"{httpContext.Connection.RemoteIpAddress}:{emailKey}:login";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    options.AddPolicy("auth-register", httpContext =>
    {
        var key = $"{httpContext.Connection.RemoteIpAddress}:register";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(15),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    options.AddPolicy("subscribe", httpContext =>
    {
        var key = $"{httpContext.Connection.RemoteIpAddress}:subscribe";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

// CORS për frontend-in (React/Vite në localhost:3000)
builder.Services.AddCors(options =>
{
    var corsOriginsRaw = builder.Configuration["CORS_ORIGINS"];
    var corsOrigins = string.IsNullOrWhiteSpace(corsOriginsRaw)
        ? new[] { "http://localhost:3000", "http://localhost:3001", "http://localhost:3002" }
        : corsOriginsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Content-Disposition");
    });
});

var databaseUrl = builder.Configuration["DATABASE_URL"] ?? Environment.GetEnvironmentVariable("DATABASE_URL");
var postgresConnectionString = builder.Configuration.GetConnectionString("PostgresConnection");
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var configuredProvider = builder.Configuration["DatabaseProvider"];
var provider = ResolveDatabaseProvider(configuredProvider, databaseUrl, defaultConnectionString, builder.Configuration.GetValue<bool>("UseSqliteForDev"));
var connectionString = ResolveConnectionString(provider, databaseUrl, postgresConnectionString, defaultConnectionString);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (provider == DatabaseProvider.Sqlite)
        options.UseSqlite("Data Source=BusinessDirectory.db");
    else if (provider == DatabaseProvider.Postgres)
        options.UseNpgsql(connectionString);
    else
        options.UseSqlServer(connectionString);
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(SmtpSettings.SectionName));
builder.Services.Configure<EmailVerificationSettings>(builder.Configuration.GetSection(EmailVerificationSettings.SectionName));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
        if (jwtSettings is null)
            throw new InvalidOperationException("JWT settings nuk janë konfiguruar në appsettings.json");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new ErrorResponseDto
                    {
                        Message = "I paautorizuar."
                    });
                }
            },
            OnForbidden = async context =>
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new ErrorResponseDto
                    {
                        Message = "Nuk ke te drejte."
                    });
                }
            }
        };
    });

var app = builder.Build();

app.Use(async (context, next) =>
{
    var requestId = context.Request.Headers["x-request-id"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(requestId))
        requestId = Guid.NewGuid().ToString("N");

    context.Response.Headers["x-request-id"] = requestId;

    var start = Stopwatch.StartNew();
    await next();
    start.Stop();

    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RequestLog");
    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
    logger.LogInformation(
        "request_id={RequestId} method={Method} path={Path} status={Status} duration_ms={DurationMs} user_id={UserId}",
        requestId,
        context.Request.Method,
        context.Request.Path.Value,
        context.Response.StatusCode,
        start.ElapsedMilliseconds,
        userId);
});

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (provider == DatabaseProvider.Sqlite)
        {
            db.Database.EnsureCreated();
            EnsureSqliteCompatibilitySchema(db);
        }
        else if (provider == DatabaseProvider.Postgres)
        {
            db.Database.EnsureCreated();
        }
        else
        {
            db.Database.Migrate();
            EnsureSqlServerCompatibilitySchema(db);
        }

        await DevDataSeeder.SeedAsync(
            db,
            app.Environment.IsDevelopment(),
            builder.Configuration,
            app.Logger,
            CancellationToken.None);
    }
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS duhet para redirect-it, që preflight OPTIONS të marrë përgjigje 200 me header-at e duhur
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePages(async statusContext =>
{
    var response = statusContext.HttpContext.Response;
    if ((response.StatusCode == StatusCodes.Status401Unauthorized || response.StatusCode == StatusCodes.Status403Forbidden) &&
        !response.HasStarted &&
        (response.ContentLength is null || response.ContentLength == 0))
    {
        response.ContentType = "application/json";
        var payload = response.StatusCode == StatusCodes.Status401Unauthorized
            ? new ErrorResponseDto { Message = "I paautorizuar." }
            : new ErrorResponseDto { Message = "Nuk ke te drejte." };

        await response.WriteAsJsonAsync(payload);
    }
});
app.MapControllers();

app.Run();

static void EnsureSqliteCompatibilitySchema(ApplicationDbContext db)
{
    var connection = db.Database.GetDbConnection();
    var openedHere = connection.State != System.Data.ConnectionState.Open;
    if (openedHere)
        connection.Open();

    try
    {
        EnsureColumn(connection, "Businesses", "Featured", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Businesses", "Latitude", "REAL NULL");
        EnsureColumn(connection, "Businesses", "Longitude", "REAL NULL");
        EnsureColumn(connection, "Businesses", "OpenDaysMask", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Businesses", "UpdatedAt", "TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP)");

        ExecuteNonQuery(connection, """
        CREATE TABLE IF NOT EXISTS Promotions (
            Id TEXT NOT NULL PRIMARY KEY,
            BusinessId TEXT NULL,
            Title TEXT NOT NULL,
            Description TEXT NOT NULL,
            ExpiresAt TEXT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1,
            CreatedAt TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP),
            FOREIGN KEY (BusinessId) REFERENCES Businesses (Id) ON DELETE SET NULL
        );
        """);

        ExecuteNonQuery(connection, """
        CREATE TABLE IF NOT EXISTS Reports (
            Id TEXT NOT NULL PRIMARY KEY,
            BusinessId TEXT NOT NULL,
            ReporterUserId TEXT NULL,
            Reason TEXT NOT NULL,
            Details TEXT NOT NULL,
            Status TEXT NOT NULL DEFAULT 'Open',
            CreatedAt TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP),
            ResolvedAt TEXT NULL,
            FOREIGN KEY (BusinessId) REFERENCES Businesses (Id) ON DELETE CASCADE,
            FOREIGN KEY (ReporterUserId) REFERENCES Users (Id) ON DELETE SET NULL
        );
        """);

        ExecuteNonQuery(connection, """
        CREATE TABLE IF NOT EXISTS NewsletterSubscribers (
            Id TEXT NOT NULL PRIMARY KEY,
            Email TEXT NOT NULL,
            CreatedAt TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP)
        );
        """);

        ExecuteNonQuery(connection, """
        CREATE TABLE IF NOT EXISTS AuditLogs (
            Id TEXT NOT NULL PRIMARY KEY,
            ActorUserId TEXT NOT NULL,
            Action TEXT NOT NULL,
            EntityType TEXT NOT NULL,
            EntityId TEXT NOT NULL,
            Metadata TEXT NULL,
            CreatedAt TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP),
            FOREIGN KEY (ActorUserId) REFERENCES Users (Id) ON DELETE RESTRICT
        );
        """);

        ExecuteNonQuery(connection, "CREATE UNIQUE INDEX IF NOT EXISTS IX_NewsletterSubscribers_Email ON NewsletterSubscribers(Email);");
        ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Businesses_Latitude_Longitude ON Businesses(Latitude, Longitude);");
        ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Businesses_Status ON Businesses(Status);");
        ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Businesses_City ON Businesses(City);");
        ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Promotions_IsActive ON Promotions(IsActive);");
        ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Promotions_ExpiresAt ON Promotions(ExpiresAt);");
        ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_Promotions_BusinessId ON Promotions(BusinessId);");
        ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_AuditLogs_CreatedAt ON AuditLogs(CreatedAt);");
        ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS IX_AuditLogs_ActorUserId ON AuditLogs(ActorUserId);");
    }
    finally
    {
        if (openedHere && connection.State == System.Data.ConnectionState.Open)
            connection.Close();
    }
}

static void EnsureSqlServerCompatibilitySchema(ApplicationDbContext db)
{
    var connection = db.Database.GetDbConnection();
    var openedHere = connection.State != System.Data.ConnectionState.Open;
    if (openedHere)
        connection.Open();

    try
    {
        ExecuteNonQuery(connection, """
            IF COL_LENGTH('Businesses', 'OpenDaysMask') IS NULL
            BEGIN
                ALTER TABLE [Businesses]
                ADD [OpenDaysMask] INT NOT NULL
                CONSTRAINT [DF_Businesses_OpenDaysMask] DEFAULT(0);
            END
            """);
    }
    finally
    {
        if (openedHere && connection.State == System.Data.ConnectionState.Open)
            connection.Close();
    }
}

static void EnsureColumn(DbConnection connection, string tableName, string columnName, string columnSqlDefinition)
{
    if (ColumnExists(connection, tableName, columnName))
        return;

    ExecuteNonQuery(connection, $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnSqlDefinition};");
}

static bool ColumnExists(DbConnection connection, string tableName, string columnName)
{
    using var cmd = connection.CreateCommand();
    cmd.CommandText = $"PRAGMA table_info('{tableName}');";
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        var currentName = reader.GetString(1);
        if (string.Equals(currentName, columnName, StringComparison.OrdinalIgnoreCase))
            return true;
    }

    return false;
}

static void ExecuteNonQuery(DbConnection connection, string sql)
{
    using var cmd = connection.CreateCommand();
    cmd.CommandText = sql;
    cmd.ExecuteNonQuery();
}

static string ExtractLoginEmailForRateLimit(HttpContext httpContext)
{
    try
    {
        if (!HttpMethods.IsPost(httpContext.Request.Method))
            return "unknown";

        if (!httpContext.Request.Path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase))
            return "unknown";

        httpContext.Request.EnableBuffering();
        using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = reader.ReadToEnd();
        httpContext.Request.Body.Position = 0;
        if (string.IsNullOrWhiteSpace(body))
            return "unknown";

        using var json = JsonDocument.Parse(body);
        if (!json.RootElement.TryGetProperty("email", out var emailProp))
            return "unknown";

        var email = emailProp.GetString();
        return string.IsNullOrWhiteSpace(email)
            ? "unknown"
            : email.Trim().ToLowerInvariant();
    }
    catch
    {
        if (httpContext.Request.Body.CanSeek)
            httpContext.Request.Body.Position = 0;

        return "unknown";
    }
}

static string? ResolveConnectionString(DatabaseProvider provider, string? databaseUrl, string? postgresConnectionString, string? configuredConnectionString)
{
    if (provider == DatabaseProvider.Postgres)
    {
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            if (databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return BuildPostgresConnectionStringFromUrl(databaseUrl);
            }

            return databaseUrl;
        }

        if (LooksLikePostgresConnectionString(postgresConnectionString))
            return postgresConnectionString;

        if (LooksLikePostgresConnectionString(configuredConnectionString))
            return configuredConnectionString;

        throw new InvalidOperationException("DatabaseProvider=postgres por nuk u gjet connection string Postgres. Vendos DATABASE_URL ose ConnectionStrings:PostgresConnection.");
    }

    if (provider == DatabaseProvider.SqlServer)
    {
        var sqlServerConnection = !string.IsNullOrWhiteSpace(databaseUrl)
            ? databaseUrl
            : configuredConnectionString;

        if (string.IsNullOrWhiteSpace(sqlServerConnection))
            throw new InvalidOperationException("DatabaseProvider=sqlserver por mungon connection string.");

        return NormalizeSqlServerConnectionString(sqlServerConnection);
    }

    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        if (databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return BuildPostgresConnectionStringFromUrl(databaseUrl);
        }

        return databaseUrl;
    }

    return configuredConnectionString;
}

static string NormalizeSqlServerConnectionString(string connectionString)
{
    var normalized = connectionString.Trim().TrimEnd(';');
    normalized += ";TrustServerCertificate=True;Encrypt=False";
    return normalized;
}

static bool LooksLikePostgresConnectionString(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return false;

    return value.Contains("Host=", StringComparison.OrdinalIgnoreCase) &&
           value.Contains("Username=", StringComparison.OrdinalIgnoreCase);
}

static string BuildPostgresConnectionStringFromUrl(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
    var database = uri.AbsolutePath.Trim('/');
    var port = uri.IsDefaultPort ? 5432 : uri.Port;

    return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Prefer;Trust Server Certificate=true";
}

static DatabaseProvider ResolveDatabaseProvider(string? configuredProvider, string? databaseUrl, string? configuredConnectionString, bool useSqliteForDev)
{
    if (!string.IsNullOrWhiteSpace(configuredProvider))
    {
        if (configuredProvider.Equals("postgres", StringComparison.OrdinalIgnoreCase))
            return DatabaseProvider.Postgres;
        if (configuredProvider.Equals("sqlserver", StringComparison.OrdinalIgnoreCase))
            return DatabaseProvider.SqlServer;
        if (configuredProvider.Equals("sqlite", StringComparison.OrdinalIgnoreCase))
            return DatabaseProvider.Sqlite;
    }

    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        if (databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return DatabaseProvider.Postgres;

        if (databaseUrl.Contains("Host=", StringComparison.OrdinalIgnoreCase) &&
            databaseUrl.Contains("Username=", StringComparison.OrdinalIgnoreCase))
            return DatabaseProvider.Postgres;
    }

    if (!string.IsNullOrWhiteSpace(configuredConnectionString))
    {
        if (configuredConnectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) &&
            configuredConnectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase))
            return DatabaseProvider.Postgres;

        if (configuredConnectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
            return DatabaseProvider.SqlServer;
    }

    return useSqliteForDev ? DatabaseProvider.Sqlite : DatabaseProvider.SqlServer;
}

enum DatabaseProvider
{
    Sqlite,
    SqlServer,
    Postgres
}
