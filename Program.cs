using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuickParkAPI.Data;
using QuickParkAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ────────────────────────────────────────────────────────────
string jwtSecret = (builder.Configuration["Jwt:Secret"] ?? "").PadRight(32, '_'); //The API is using HS256 (SecurityAlgorithms.HmacSha256) for its JWT signing algorithm. This requires a key that is at least 256 bits (32 bytes) long, which is why the previous short key (YOUR_JWT_SECRET_KEY_HERE which is 24 bytes) needed padding to prevent runtime exceptions.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Fail fast with a helpful message if the connection string is missing or still a placeholder
if (string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("PLACEHOLDER"))
{
    throw new InvalidOperationException(
        "Database connection string is not configured. " +
        "Set the 'ConnectionStrings__DefaultConnection' environment variable on Render " +
        "with a valid PostgreSQL connection string. " +
        "Example: Host=<host>;Port=5432;Database=QuickPark;Username=<user>;Password=<pass>;");
}

// ── EF Core + PostgreSQL ─────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ── JWT Auth ──────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
        // Return JSON errors instead of default challenge redirects
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"error\":\"Access denied. No token provided.\"}");
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"error\":\"Forbidden.\"}");
            }
        };
    });

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("QuickParkCors", policy =>
    {
        var frontendUrl = builder.Configuration["FrontendUrl"];
        var allowedOrigins = new List<string> { "http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173" };
        
        if (!string.IsNullOrEmpty(frontendUrl))
        {
            allowedOrigins.Add(frontendUrl);
        }

        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // camelCase responses to match what frontend expects
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// ── Listen on port 5000 (same as old Node backend) ────────────────────────────
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// ── Migrate DB + Seed Admin on startup ───────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    db.Database.Migrate();
    await AdminSeeder.SeedAsync(db, config);
}

// ── Middleware Pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseCors("QuickParkCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Health Check ──────────────────────────────────────────────────────────────
app.MapGet("/api/health", (AppDbContext db) =>
{
    var canConnect = db.Database.CanConnect();
    return Results.Ok(new
    {
        status = "OK",
        timestamp = DateTime.UtcNow.ToString("o"),
        db = canConnect ? "connected" : "disconnected"
    });
});

Console.WriteLine($"QuickPark .NET API starting on http://0.0.0.0:{port}");
app.Run();
