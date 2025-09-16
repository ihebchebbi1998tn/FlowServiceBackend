using MyApi.Data;
using MyApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Read DATABASE_URL from environment or fallback
var rawConnection = Environment.GetEnvironmentVariable("DATABASE_URL") ??
    builder.Configuration.GetConnectionString("DefaultConnection");

string connectionString;

// Logging setup
var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("Startup");
logger.LogInformation($"Raw connection: {rawConnection?.Substring(0, Math.Min(80, rawConnection?.Length ?? 0))}...");

if (!string.IsNullOrEmpty(rawConnection))
{
    if (rawConnection.StartsWith("postgres://") || rawConnection.StartsWith("postgresql://"))
    {
        try
        {
            var uri = new Uri(rawConnection);

            var userInfo = uri.UserInfo?.Split(':', 2) ?? new string[0];
            var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var database = uri.AbsolutePath?.TrimStart('/') ?? "";

            var npgBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Username = username,
                Password = password,
                Database = database,
                // Neon requires SSL
                SslMode = SslMode.Require,
            };

            // Append query params if they exist (?sslmode=...&...)
            var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
            foreach (var kv in queryParams)
            {
                try { npgBuilder[kv.Key] = kv.Value.ToString(); } catch { }
            }

            connectionString = npgBuilder.ToString();
            logger.LogInformation("✅ Successfully built connection string from DATABASE_URL");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to parse DATABASE_URL, falling back to raw connection");
            connectionString = rawConnection;
        }
    }
    else
    {
        // Already in key=value form
        connectionString = rawConnection;
    }
}
else
{
    // Fallback for local dev only
    connectionString = "Host=localhost;Port=5432;Database=myapi_dev;Username=postgres;Password=dev_password;SSL Mode=Disable";
    logger.LogWarning("⚠️ Using fallback development connection string");
}

// Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyHere12345";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "FlowServiceBackend",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "FlowServiceFrontend",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();


// Register custom services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPreferencesService, PreferencesService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ISkillService, SkillService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Render port
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Urls.Add($"http://0.0.0.0:{port}");

// Auto-migrate DB
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        migrationLogger.LogInformation("📦 Applying database migrations...");
        context.Database.Migrate();
        migrationLogger.LogInformation("✅ Database migrations completed successfully.");
    }
    catch (Exception ex)
    {
        migrationLogger.LogError(ex, "❌ Error while migrating the database.");
    }
}

// Middleware pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");

// Only use HTTPS redirection in development or when HTTPS port is properly configured
if (builder.Environment.IsDevelopment() || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORT")))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Root redirect → Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

// Health endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

app.Run();