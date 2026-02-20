using System.Text;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Application.Options;
<<<<<<< HEAD
using BusinessDirectory.Json;
=======
using BusinessDirectory.Infrastructure.Services;
>>>>>>> af9bb66db4a48f411ede95e57b30cb6e690570b1
using Infrastructure;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// All endpoints require JWT by default. For public endpoints use [AllowAnonymous].
builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.Filters.Add(new AuthorizeFilter(policy));
})
.AddJsonOptions(options =>
{
    // Pranon edhe numra për fusha string (p.sh. kur frontendi dërgon username si number) – zvogëlon 400 validation.
    options.JsonSerializerOptions.Converters.Add(new StringFromNumberOrStringConverter());
});

// Admin policy (optional if you use [Authorize(Roles="Admin")], but it’s fine to keep)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

<<<<<<< HEAD
// CORS për frontend-in (Business_Directory_Front në http://localhost:3000).
// Pa këtë, request-et cross-origin bllokohen dhe frontendi tregon "Failed to fetch".
// Preflight OPTIONS përgjigjet me Access-Control-Allow-Origin, -Methods, -Headers.
=======
builder.Services.AddEndpointsApiExplorer();

// Swagger + JWT support (Authorize button)
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Business Directory API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // Keep this only if the class exists in your project. If it errors, remove this line.
    options.OperationFilter<BusinessDirectory.Swagger.ExamplesOperationFilter>();
});

// CORS for frontend (React/Vite)
>>>>>>> af9bb66db4a48f411ede95e57b30cb6e690570b1
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Database: SQLite for dev/testing fallback, SQL Server if connection string is configured
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useSqlite = builder.Configuration.GetValue<bool>("UseSqliteForDev")
    || string.IsNullOrWhiteSpace(connectionString)
    || !connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useSqlite)
        options.UseSqlite("Data Source=BusinessDirectory.db");
    else
        options.UseSqlServer(connectionString);
});

// JWT settings + services
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBusinessService, BusinessService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>();

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Apply DB setup/migrations in dev
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (useSqlite)
        db.Database.EnsureCreated();
    else
        db.Database.Migrate();

    app.UseSwagger();
    app.UseSwaggerUI();
}

<<<<<<< HEAD
// CORS duhet para Authentication/Authorization, që preflight OPTIONS të marrë 200 me header-at e duhur
app.UseCors();
=======
// CORS should be before HTTPS redirection (so preflight works)
app.UseCors("AllowFrontend");

>>>>>>> af9bb66db4a48f411ede95e57b30cb6e690570b1
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

