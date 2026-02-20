using System.Text;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Application.Options;
using BusinessDirectory.Json;
using Infrastructure;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Të gjitha endpoint-et kërkojnë JWT (401 pa token). Për public: [AllowAnonymous].
builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.Filters.Add(new AuthorizeFilter(policy));
})
.AddJsonOptions(options =>
{
    // Pranon edhe numra për fusha string (p.sh. kur frontendi dërgon username si number) – zvogëlon 400 validation.
    options.JsonSerializerOptions.Converters.Add(new StringFromNumberOrStringConverter());
});

// Policy "AdminOnly" – për endpoint-et që duhet vetëm Admin (403 për jo-admin).
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS për frontend-in (Business_Directory_Front në http://localhost:3000).
// Pa këtë, request-et cross-origin bllokohen dhe frontendi tregon "Failed to fetch".
// Preflight OPTIONS përgjigjet me Access-Control-Allow-Origin, -Methods, -Headers.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useSqlite = builder.Configuration.GetValue<bool>("UseSqliteForDev") 
    || string.IsNullOrEmpty(connectionString) 
    || !connectionString.Contains("Server=");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useSqlite)
        options.UseSqlite("Data Source=BusinessDirectory.db");
    else
        options.UseSqlServer(connectionString);
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddScoped<IAuthService, AuthService>();

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (useSqlite)
            db.Database.EnsureCreated();
        else
            db.Database.Migrate();
    }
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS duhet para Authentication/Authorization, që preflight OPTIONS të marrë 200 me header-at e duhur
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
