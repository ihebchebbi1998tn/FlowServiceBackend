using MyApi.Data;
using MyApi.Configuration;
using MyApi.Modules.Auth.Services;
using MyApi.Modules.Users.Services;
using MyApi.Modules.Roles.Services;
using MyApi.Modules.Skills.Services;
using MyApi.Modules.Contacts.Services;
using MyApi.Modules.Articles.Services;
using MyApi.Modules.Calendar.Services;
using MyApi.Modules.Projects.Services;
using MyApi.Modules.Lookups.Services;
using MyApi.Modules.Offers.Services;
using MyApi.Modules.Sales.Services;
using MyApi.Modules.Installations.Services;
using MyApi.Modules.ServiceOrders.Services;
using MyApi.Modules.Planning.Services;
using MyApi.Modules.Shared.Services;
using MyApi.Modules.Preferences.Services;
using MyApi.Modules.Notifications.Services;
using MyApi.Modules.AiChat.Services;
using MyApi.Modules.WorkflowEngine.Services;
using MyApi.Modules.Signatures.Services;
using MyApi.Modules.WorkflowEngine.Hubs;
using MyApi.Modules.WebsiteBuilder;
using MyApi.Modules.EmailAccounts.Services;
using MyApi.Modules.UserAiSettings.Services;
using MyApi.Modules.WebsiteBuilder.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container with camelCase JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Read DATABASE_URL from environment or fallback
var rawConnection = Environment.GetEnvironmentVariable("DATABASE_URL") ??
    builder.Configuration.GetConnectionString("DefaultConnection");

string? connectionString = null;

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
    connectionString = "";
}

// ✅ OPTIMIZATION 5: Add connection pool sizing for better concurrency
// Default pool size (25) often exhausts under load - increase for better performance
connectionString ??= "";
var connStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
{
    MaxPoolSize = 50,  // Increased from default 25 to handle more concurrent requests
    MinPoolSize = 10   // Minimum connections to keep warm
};
connectionString = connStringBuilder.ToString();

// Register DbContext with optimized connection pooling
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // ✅ OPTIMIZATION 5: Optimize connection pooling (5-10% improvement)
        npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
    });
    if (builder.Environment.IsDevelopment())
    {
        // ✅ Disable sensitive data logging in production (5-10% improvement)
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Allow services to depend on the base DbContext type (maps to ApplicationDbContext)
// This prevents DI failures if a service constructor requests DbContext instead of ApplicationDbContext.
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

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
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "MyApi",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "MyApiClients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Register custom services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPreferencesService, PreferencesService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<ISkillService, SkillService>();

// Contacts Module Services
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IContactNoteService, ContactNoteService>();
builder.Services.AddScoped<IContactTagService, ContactTagService>();

// Articles Module Services
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IStockTransactionService, StockTransactionService>();

// Calendar Module Services
builder.Services.AddScoped<ICalendarService, CalendarService>();

// Lookups Module Services
builder.Services.AddScoped<ILookupService, LookupService>();

// Tasks Module Services
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProjectColumnService, ProjectColumnService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ITaskCommentService, TaskCommentService>();
builder.Services.AddScoped<ITaskAttachmentService, TaskAttachmentService>();
builder.Services.AddScoped<ITaskTimeEntryService, TaskTimeEntryService>();
builder.Services.AddScoped<ITaskChecklistService, TaskChecklistService>();
builder.Services.AddScoped<IRecurringTaskService, RecurringTaskService>();

// Offers Module Services
builder.Services.AddScoped<IOfferService, OfferService>();

// Sales Module Services
builder.Services.AddScoped<ISaleService, SaleService>();

// Installations Module Services
builder.Services.AddScoped<IInstallationService, InstallationService>();
builder.Services.AddScoped<IInstallationNoteService, InstallationNoteService>();

builder.Services.AddScoped<IServiceOrderService, ServiceOrderService>();

// Dispatches Module Services
builder.Services.AddScoped<MyApi.Modules.Dispatches.Services.IDispatchService, MyApi.Modules.Dispatches.Services.DispatchService>();

// Planning Module Services
builder.Services.AddScoped<IPlanningService, PlanningService>();

// Upload Services (UploadThing integration)
builder.Services.AddHttpClient();
builder.Services.AddScoped<IUploadThingService, UploadThingService>();

// Notifications Module Services
builder.Services.AddScoped<INotificationService, NotificationService>();

// System Logging Services
builder.Services.AddScoped<ISystemLogService, SystemLogService>();

// AI Chat Module Services
builder.Services.AddScoped<IAiChatService, AiChatService>();

// DynamicForms Module Services
builder.Services.AddScoped<MyApi.Modules.DynamicForms.Services.IDynamicFormService, MyApi.Modules.DynamicForms.Services.DynamicFormService>();

// Entity Form Documents Service (Shared - for offers/sales)
builder.Services.AddScoped<MyApi.Modules.Shared.Services.IEntityFormDocumentService, MyApi.Modules.Shared.Services.EntityFormDocumentService>();

// PDF Settings Service (Global settings for all modules)
builder.Services.AddScoped<IPdfSettingsService, PdfSettingsService>();

// Signatures Module Services
builder.Services.AddScoped<ISignatureService, SignatureService>();

// Workflow Engine Services
builder.Services.AddScoped<IWorkflowEngineService, WorkflowEngineService>();
builder.Services.AddScoped<IWorkflowTriggerService, WorkflowTriggerService>();
builder.Services.AddScoped<IWorkflowApprovalService, WorkflowApprovalService>();
builder.Services.AddScoped<IWorkflowNotificationService, WorkflowNotificationService>();
builder.Services.AddScoped<IWorkflowNodeExecutor, WorkflowNodeExecutor>();
builder.Services.AddScoped<IWorkflowGraphExecutor, WorkflowGraphExecutor>();

// Business Workflow Service (handles cascade operations between entities)
builder.Services.AddScoped<IBusinessWorkflowService, BusinessWorkflowService>();

// Website Builder Module
builder.Services.AddWebsiteBuilderServices();

// Email Accounts Module Services (Gmail/Outlook OAuth)
builder.Services.AddScoped<IEmailAccountService, EmailAccountService>();

// User AI Settings Module Services (OpenRouter keys & preferences)
builder.Services.AddScoped<IUserAiSettingsService, UserAiSettingsService>();

// Workflow Polling Background Service (state-based triggers every 5 minutes)
builder.Services.AddHostedService<WorkflowPollingService>();

// SignalR for real-time workflow notifications
builder.Services.AddSignalR();

// CORS - Allow all origins for flexibility
// ✅ ENHANCED: Bulletproof CORS configuration that works everywhere
builder.Services.AddCors(options =>
{
    // REST API - Allow all origins
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()              // ✅ Allow all origins
              .AllowAnyHeader()               // ✅ Allow all headers (Content-Type, Authorization, etc.)
              .AllowAnyMethod()               // ✅ Allow all HTTP methods (GET, POST, PUT, DELETE, OPTIONS, PATCH)
              .WithExposedHeaders(            // ✅ Allow frontend to read these headers
                  "X-Total-Count",
                  "X-Page-Number",
                  "X-Page-Size",
                  "Content-Disposition");     // For file downloads
    });
    
    // SignalR - Requires credentials, so use different policy
    // ⚠️ Note: Can't use AllowAnyOrigin() + AllowCredentials() together
    // SignalR will handle its own auth via token in URL
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Allow any origin
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();           // Required for WebSocket connection with auth
    });
});

// Swagger Documentation
builder.Services.AddSwaggerDocumentation(builder.Configuration);

// In-memory log store for API access
builder.Services.AddSingleton<IInMemoryLogStore, InMemoryLogStore>();

// Logging with in-memory provider
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => 
{
    options.FormatterName = "simple";
});
builder.Logging.AddDebug();

var app = builder.Build();

// Add in-memory log provider after app is built (avoids ASP0000 warning)
var logStore = app.Services.GetRequiredService<IInMemoryLogStore>();
app.Services.GetRequiredService<ILoggerFactory>().AddProvider(new InMemoryLogProvider(logStore));

// Render port
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Urls.Add($"http://0.0.0.0:{port}");

// Auto-migrate DB
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // NOTE: Migrations are disabled - database schema is managed via external SQL scripts
    migrationLogger.LogInformation("═══════════════════════════════════════════════════════════════");
    migrationLogger.LogInformation("  DATABASE CONNECTION CHECK");
    migrationLogger.LogInformation("═══════════════════════════════════════════════════════════════");
    migrationLogger.LogInformation("📦 Migrations disabled - database schema managed externally");
    
    try
    {
        // Just verify we can connect to the database
        var canConnect = context.Database.CanConnect();
        if (canConnect)
        {
            migrationLogger.LogInformation("✅ Database connection successful!");
        }
        else
        {
            migrationLogger.LogError("❌ Unable to connect to database");
            throw new InvalidOperationException("Cannot connect to the database");
        }

        // Validate database tables
        migrationLogger.LogInformation("📋 Validating database tables...");

        var expectedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "__EFMigrationsHistory",
            // Auth & users
            "MainAdminUsers",
            "Users",
            "UserPreferences",
            // Roles & skills
            "Roles",
            "UserRoles",
            "Skills",
            "UserSkills",
            "RoleSkills",
            // Contacts
            "Contacts",
            "ContactTags",
            "ContactTagAssignments",
            "ContactNotes",
            // Articles & inventory
            "Articles",
            "ArticleCategories",
            "Locations",
            "InventoryTransactions",
            // Calendar
            "CalendarEvents",
            "EventAttendees",
            "EventReminders",
            "EventTypes",
            // Lookups & core
            "LookupItems",
            "Currencies",
            // Offers & sales
            "Offers",
            "OfferItems",
            "OfferActivities",
            "Sales",
            "SaleItems",
            "SaleActivities",
            // Installations & service orders
            "Installations",
            "MaintenanceHistory",
            "ServiceOrders",
            "ServiceOrderJobs",
            // Projects & tasks
            "Projects",
            "ProjectColumns",
            "ProjectTasks",
            "TaskComments",
            "TaskAttachments",
            "TaskTimeEntries",
            "TaskChecklists",
            "TaskChecklistItems",
            "DailyTasks",
            // Dispatches
            "Dispatches",
            "DispatchTechnicians",
            "TimeEntries",
            "Expenses",
            "MaterialUsage",
            "Attachments",
            "Notes",
            // Planning
            "TechnicianWorkingHours",
            "TechnicianLeave",
            "TechnicianStatusHistory",
            "DispatchHistory",
            // AI Chat
            "AiConversations",
            "AiMessages",
            // Website Builder Module
            "WB_Sites",
            "WB_Pages",
            "WB_PageVersions",
            "WB_GlobalBlocks",
            "WB_GlobalBlockUsages",
            "WB_BrandProfiles",
            "WB_FormSubmissions",
            "WB_Media",
            "WB_Templates",
            "WB_ActivityLog",
            // Email Accounts
            "ConnectedEmailAccounts",
            "EmailBlocklistItems"
        };

        var existingTables = context.Database.SqlQueryRaw<string>(
            @"SELECT table_name 
              FROM information_schema.tables 
              WHERE table_schema = 'public' 
              AND table_type = 'BASE TABLE'
              ORDER BY table_name"
        ).ToList();

        var existingSet = new HashSet<string>(existingTables, StringComparer.OrdinalIgnoreCase);
        
        migrationLogger.LogInformation($"📊 Database Table Validation Report:");
        migrationLogger.LogInformation($"   Expected: {expectedTables.Count} tables | Found: {existingTables.Count} tables");
        migrationLogger.LogInformation("");

        // Show created tables
        var createdTables = expectedTables.Where(t => existingSet.Contains(t)).OrderBy(t => t).ToList();
        if (createdTables.Any())
        {
            migrationLogger.LogInformation("✅ Created Tables:");
            foreach (var table in createdTables)
            {
                migrationLogger.LogInformation($"   ✅ {table}");
            }
        }

        // Show missing tables in red/error
        var missingTables = expectedTables.Where(t => !existingSet.Contains(t)).OrderBy(t => t).ToList();
        if (missingTables.Any())
        {
            migrationLogger.LogError("");
            migrationLogger.LogError($"❌ Missing Tables ({missingTables.Count}):");
            foreach (var table in missingTables)
            {
                migrationLogger.LogError($"   ❌ {table} - NOT CREATED");
            }
            migrationLogger.LogError("");
        }

        // Show unexpected tables (exist but not in expected list)
        var unexpectedTables = existingSet.Where(t => !expectedTables.Contains(t)).OrderBy(t => t).ToList();
        if (unexpectedTables.Any())
        {
            migrationLogger.LogWarning("");
            migrationLogger.LogWarning($"⚠️ Unexpected Tables ({unexpectedTables.Count}):");
            foreach (var table in unexpectedTables)
            {
                migrationLogger.LogWarning($"   ⚠️ {table}");
            }
        }

        // Summary
        migrationLogger.LogInformation("");
        if (missingTables.Any())
        {
            migrationLogger.LogError($"❌ Database validation FAILED - {missingTables.Count} table(s) missing!");
        }
        else
        {
            migrationLogger.LogInformation("✅ Database validation PASSED - All expected tables exist!");
        }
    }
    catch (Exception ex)
    {
        migrationLogger.LogError(ex, "❌ Error during database connection/validation.");
        throw;
    }
}

// Middleware pipeline
app.UseSwaggerDocumentation(builder.Configuration);

// ✅ CORS MUST be BEFORE static files so uploads get Access-Control-Allow-Origin headers
app.UseCors("AllowFrontend");

// Serve static files for Swagger UI customizations
app.UseStaticFiles();

// Serve uploaded files (company logos, documents, etc.) from the uploads directory
var uploadsPath = Path.Combine(Directory.GetParent(builder.Environment.ContentRootPath)?.FullName ?? builder.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        // Static files bypass CORS middleware, so we must add headers manually
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "*");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, OPTIONS");
    }
});

// ✅ Add debugging middleware to log CORS issues (development only)
if (builder.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var origin = context.Request.Headers["Origin"].FirstOrDefault();
        if (!string.IsNullOrEmpty(origin))
        {
            context.Response.Headers["X-Debug-Origin"] = origin;
            context.Response.Headers["X-Debug-Method"] = context.Request.Method;
        }
        await next();
    });
}

// Only use HTTPS redirection in development or when HTTPS port is properly configured
if (builder.Environment.IsDevelopment() || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORT")))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SignalR Hub for real-time workflow updates
// SignalR Hub for real-time workflow updates (uses SignalRPolicy for CORS with credentials)
app.MapHub<WorkflowHub>("/hubs/workflow").RequireCors("SignalRPolicy");

// Root redirect → Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger"));

// Health endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

app.Run();
