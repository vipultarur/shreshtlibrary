using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using WebApplication1.Data;
using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;
using Microsoft.AspNetCore.HttpOverrides;

Env.Load(); // Load .env file for local development
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));
// Override config with env variables if they exist
builder.Configuration.AddEnvironmentVariables();
// Add services to the container.

builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
});

builder.Services.AddMemoryCache();

builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters()
                .AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = WebApplication1.Utils.DrfValidationResponseFactory.CreateResponse;
});

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    var connBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
    if (connBuilder.MaxPoolSize > 10) 
    {
        connBuilder.MaxPoolSize = 10;
    }
    connBuilder.Timeout = 30;
    connBuilder.CommandTimeout = 60;
    connectionString = connBuilder.ConnectionString;

    builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString, 
            npgsqlOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            })
        .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
}

// Add Notification Services
builder.Services.AddSingleton<WebApplication1.Services.INotificationService, WebApplication1.Services.FirebaseNotificationService>();
builder.Services.AddHostedService<WebApplication1.Services.NotificationBackgroundService>();
builder.Services.AddHostedService<WebApplication1.Services.AttendanceBackgroundService>();

// Add Email Service
builder.Services.AddScoped<WebApplication1.Services.IEmailService, WebApplication1.Services.EmailService>();

// Add WhatsApp Service
builder.Services.AddHttpClient<WebApplication1.Services.WhatsAppNotificationService>();

// Add Repositories
builder.Services.AddScoped(typeof(WebApplication1.Repositories.IRepository<>), typeof(WebApplication1.Repositories.Repository<>));
builder.Services.AddScoped<WebApplication1.Repositories.IStudentRepository, WebApplication1.Repositories.StudentRepository>();
builder.Services.AddScoped<WebApplication1.Repositories.IAttendanceRepository, WebApplication1.Repositories.AttendanceRepository>();

// Add Domain Services
builder.Services.AddScoped<WebApplication1.Services.IStudentProfileService, WebApplication1.Services.StudentProfileService>();
builder.Services.AddScoped<WebApplication1.Services.IStudentDashboardService, WebApplication1.Services.StudentDashboardService>();
builder.Services.AddScoped<WebApplication1.Services.IStudentReferralService, WebApplication1.Services.StudentReferralService>();
builder.Services.AddScoped<WebApplication1.Services.IPaymentService, WebApplication1.Services.PaymentService>();
builder.Services.AddScoped<WebApplication1.Services.IStudentAdminService, WebApplication1.Services.StudentAdminService>();
builder.Services.AddScoped<WebApplication1.Services.IAdminSeatService, WebApplication1.Services.AdminSeatService>();
builder.Services.AddScoped<WebApplication1.Services.IAdminAttendanceService, WebApplication1.Services.AdminAttendanceService>();
builder.Services.AddScoped<WebApplication1.Services.IAdminNotificationService, WebApplication1.Services.AdminNotificationService>();
builder.Services.AddScoped<WebApplication1.Services.INotificationDispatcher, WebApplication1.Services.NotificationDispatcher>();
builder.Services.AddScoped<WebApplication1.Services.IAuthService, WebApplication1.Services.AuthService>();
builder.Services.AddScoped<WebApplication1.Services.IAdminLibraryService, WebApplication1.Services.AdminLibraryService>();
builder.Services.AddScoped<WebApplication1.Services.IAdminBillingService, WebApplication1.Services.AdminBillingService>();
builder.Services.AddScoped<WebApplication1.Services.IAdminSettingsService, WebApplication1.Services.AdminSettingsService>();
builder.Services.AddScoped<WebApplication1.Services.IStudyService, WebApplication1.Services.StudyService>();
builder.Services.AddScoped<WebApplication1.Services.ILibraryService, WebApplication1.Services.LibraryService>();
builder.Services.AddScoped<WebApplication1.Services.IStudentSeatService, WebApplication1.Services.StudentSeatService>();
builder.Services.AddScoped<WebApplication1.Services.IStudentNotificationService, WebApplication1.Services.StudentNotificationService>();
builder.Services.AddScoped<WebApplication1.Services.IReportsService, WebApplication1.Services.ReportsService>();
builder.Services.AddScoped<WebApplication1.Services.ISuperAdminService, WebApplication1.Services.SuperAdminService>();
builder.Services.AddScoped<WebApplication1.Services.IAdminSlidersService, WebApplication1.Services.AdminSlidersService>();
builder.Services.AddScoped<WebApplication1.Services.ICloudinaryService, WebApplication1.Services.CloudinaryService>();

// Add Foundation Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<WebApplication1.Services.ICurrentUserService, WebApplication1.Services.CurrentUserService>();
builder.Services.AddSingleton<WebApplication1.Services.IDateTimeProvider, WebApplication1.Services.DateTimeProvider>();
builder.Services.AddHostedService<WebApplication1.Services.TokenCleanupService>();
builder.Services.AddHostedService<WebApplication1.Services.AutoBackupService>();
builder.Services.AddScoped<WebApplication1.Services.IAdminDashboardService, WebApplication1.Services.AdminDashboardService>();
builder.Services.AddScoped<WebApplication1.Services.IAttendanceService, WebApplication1.Services.AttendanceService>();

// Configure CORS securely
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
        if (allowedOrigins == null || allowedOrigins.Length == 0)
        {
            Log.Warning("CORS AllowedOrigins is empty. Using secure defaults.");
            allowedOrigins = new[] { "https://shreshtlibrary.com", "https://admin.shreshtlibrary.com", "https://shreshtlibrary.onrender.com" };
        }
        
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                  .AllowAnyHeader()
                  .DisallowCredentials();
        }
    });
});

// Configure Forwarded Headers for reverse proxies like Render
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, WebApplication1.Utils.PermissionPolicyProvider>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, WebApplication1.Utils.PermissionAuthorizationHandler>();
// §1.2: PermissionAuthorizationHandler is registered as Scoped so it can accept IServiceScopeFactory for DB re-fetch
// IServiceScopeFactory is a singleton by default, safe to inject into any lifetime

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = builder.Configuration["Jwt:Secret"]; // Load from securely configured providers
if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
{
    Log.Fatal("JWT Secret is not configured securely.");
    throw new InvalidOperationException("FATAL: JWT Secret is not configured correctly. Set Jwt:Secret securely with at least 32 characters.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        RoleClaimType = "role",
        NameClaimType = System.Security.Claims.ClaimTypes.Name,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
        ClockSkew = TimeSpan.Zero // Force exact expiration
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && 
                (path.Value!.EndsWith("/receipt")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Configure Rate Limiting matching DRF setup
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("AnonRateThrottle", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ip, _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1)
        });
    });
    options.AddPolicy("UserRateThrottle", context =>
    {
        var userId = context.User?.FindFirst("user_id")?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(userId, _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1)
        });
    });
    options.AddPolicy("OtpRateThrottle", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ip, _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromMinutes(5)
        });
    });
    options.RejectionStatusCode = 429;
});

// Configure OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Shresht API", Version = "v1" });
    
    // JWT Authentication setup for Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\""
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddProblemDetails();

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        Log.Information("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating the database.");
    }
}

app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
app.UseMiddleware<WebApplication1.Middleware.CorrelationIdMiddleware>();
app.UseMiddleware<WebApplication1.Middleware.GlobalExceptionHandlerMiddleware>();
// §1.9 — Audit all 401/403 responses (log before response finalises)
app.UseMiddleware<WebApplication1.Middleware.SecurityAuditMiddleware>();
app.UseResponseCompression();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shresht API v1");
        c.RoutePrefix = "api-docs";
    });
}

var mediaPath = app.Environment.IsDevelopment() 
    ? Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "shreshtlibrary", "media"))
    : Path.Combine(builder.Environment.ContentRootPath, "media");

if (!Directory.Exists(mediaPath))
{
    Directory.CreateDirectory(mediaPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(mediaPath),
    RequestPath = "/media"
});
app.MapGet("/media/{*path}", async (string path, WebApplication1.Data.ApplicationDbContext context, ILogger<Program> logger, HttpResponse response) =>
{
    try
    {
        var file = await context.LibraryDatabasefiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Name == path);
        if (file != null)
        {
            return Results.File(file.Data, file.ContentType);
        }
    }
    catch (Exception ex)
    {
        // Ignore DB error, maybe table doesn't exist yet, fallback to disk
        logger.LogWarning(ex, "DB Error for media: {Message}", ex.Message);
    }
    
    var safePath = path.TrimStart('/');
    if (safePath.Contains("..") || safePath.Contains(':'))
        return Results.Forbid();

    var physicalPath = Path.Combine(mediaPath, safePath.Replace('/', Path.DirectorySeparatorChar));
    var fullPath = Path.GetFullPath(physicalPath);
    if (!fullPath.StartsWith(Path.GetFullPath(mediaPath), StringComparison.OrdinalIgnoreCase))
        return Results.Forbid();
    
    if (System.IO.File.Exists(fullPath))
    {
        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var mime = ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            _ => null
        };
        
        if (mime == null) 
        {
            logger.LogWarning("Blocked attempt to access non-image file: {Path}", fullPath);
            return Results.Forbid();
        }
        
        return Results.File(fullPath, mime, enableRangeProcessing: true);
    }

    logger.LogInformation("File not found. Path requested: {Path}, Physical path checked: {FullPath}", path, fullPath);
    return Results.NotFound(new { message = "File not found in DB or Disk", requestedPath = path, physicalPath = fullPath });
});

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("AllowAll");

app.UseRateLimiter();

app.UseAuthentication();
// §1.4 — Reject tokens that were revoked server-side (logout, password change)
app.UseMiddleware<WebApplication1.Middleware.TokenRevocationMiddleware>();
app.UseAuthorization();

app.MapMethods("/", new[] { "HEAD" }, () => Results.Ok());
app.MapGet("/", (IWebHostEnvironment env) =>
{
    var path = Path.Combine(env.ContentRootPath, "landing.html");
    return Results.File(path, "text/html");
});

app.MapControllers();



app.Run();

public partial class Program { }
