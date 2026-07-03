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
    .WriteTo.Console());
// Override config with env variables if they exist
builder.Configuration.AddEnvironmentVariables();
// Add services to the container.

builder.Services.AddControllers(options => 
{
    options.Filters.Add<WebApplication1.Utils.ApiExceptionFilter>();
})
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
builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), 
        npgsqlOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        }));

// Add Notification Services
builder.Services.AddSingleton<WebApplication1.Services.INotificationService, WebApplication1.Services.FirebaseNotificationService>();
builder.Services.AddHostedService<WebApplication1.Services.NotificationBackgroundService>();
builder.Services.AddHostedService<WebApplication1.Services.AttendanceBackgroundService>();

// Add Email Service
builder.Services.AddScoped<WebApplication1.Services.IEmailService, WebApplication1.Services.EmailService>();

// Add Repositories
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
                  .AllowAnyMethod()
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
        ClockSkew = TimeSpan.Zero // Force exact expiration
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
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ip, _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1)
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
        // First ensure EF migration history table exists
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                ""MigrationId"" character varying(150) NOT NULL,
                ""ProductVersion"" character varying(32) NOT NULL,
                CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
            );
        ");

        // Insert the initial migration so it doesn't fail trying to recreate Django tables
        db.Database.ExecuteSqlRaw(@"
            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
            SELECT '20260702161914_UpdateLibraryInfoSchema', '8.0.0'
            WHERE NOT EXISTS (
                SELECT 1 FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20260702161914_UpdateLibraryInfoSchema'
            );
        ");

        db.Database.Migrate();
        Log.Information("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating the database.");
    }
}

app.UseForwardedHeaders();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(exceptionHandlerApp =>
    {
        exceptionHandlerApp.Run(async context =>
        {
            var exceptionHandlerFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            if (exceptionHandlerFeature?.Error != null)
            {
                Log.Error(exceptionHandlerFeature.Error, "Unhandled exception caught by global handler");
            }
            var problemDetailsService = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Http.IProblemDetailsService>();
            await problemDetailsService.TryWriteAsync(new Microsoft.AspNetCore.Http.ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = 500,
                    Title = "An internal server error occurred.",
                    Detail = "Please contact support."
                }
            });
        });
    });
    app.UseStatusCodePages();
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
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shresht API v1");
    c.RoutePrefix = "api-docs";
});

if (app.Environment.IsDevelopment())
{
    // Minimal setup for local dev
}

var mediaPath = app.Environment.IsDevelopment() 
    ? Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "shreshtlibrary", "media"))
    : Path.Combine(builder.Environment.ContentRootPath, "media");

if (!Directory.Exists(mediaPath))
{
    Directory.CreateDirectory(mediaPath);
}
// Static files removed to prevent serving dummy files on disk

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
            _ => "application/octet-stream"
        };
        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        return Results.File(bytes, mime);
    }

    logger.LogInformation("File not found. Path requested: {Path}, Physical path checked: {FullPath}", path, fullPath);
    return Results.NotFound(new { message = "File not found in DB or Disk", requestedPath = path, physicalPath = fullPath });
});

app.UseResponseCompression();
app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", async (WebApplication1.Data.ApplicationDbContext db, IWebHostEnvironment env) =>
{
    var path = System.IO.Path.Combine(env.ContentRootPath, "landing.html");
    if (!System.IO.File.Exists(path))
    {
        return Results.Ok(new { status = "online", message = "Shresht Library API is running successfully.", version = "1.0" });
    }

    var html = await System.IO.File.ReadAllTextAsync(path);
    
    return Results.Content(html, "text/html");
});
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WebApplication1.Data.ApplicationDbContext>();
    try
    {
        var alterSql = @"
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS library_name character varying(255) DEFAULT 'Shresht Library';
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS logo character varying(255) DEFAULT '';
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS banner_image character varying(255);
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS established_year integer;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS owner_name character varying(255) DEFAULT 'Admin';
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS contact_number character varying(50) DEFAULT '0000000000';
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS email character varying(255) DEFAULT 'admin@shreshtlibrary.com';
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS website character varying(255);
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS opening_time time without time zone DEFAULT '08:00:00';
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS closing_time time without time zone DEFAULT '22:00:00';
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS weekly_off character varying(100);
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS total_capacity integer DEFAULT 100;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS available_seats integer DEFAULT 100;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS address_line1 character varying(255) DEFAULT 'Library Address';
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS address_line2 character varying(255);
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS area character varying(100) DEFAULT 'Area';
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS city character varying(100) DEFAULT 'City';
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS state character varying(100) DEFAULT 'State';
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS country character varying(100) DEFAULT 'India';
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS pin_code character varying(20) DEFAULT '000000';
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS latitude numeric(10,6) DEFAULT 0.0;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS longitude numeric(10,6) DEFAULT 0.0;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS google_map_url character varying(500);

ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS wifi boolean;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS ac boolean;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS cctv boolean;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS drinking_water boolean;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS lockers boolean;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS charging_points boolean;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS parking boolean;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS reading_area boolean;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS computer_access boolean;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS printing boolean;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS facebook_url character varying(255);
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS instagram_url character varying(255);
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS whatsapp_number character varying(50);
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS telegram_url character varying(255);
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS youtube_url character varying(255);
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS twitter_url character varying(255);
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS linkedin_url character varying(255);
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS tagline character varying(255);
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS mission text;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS vision text;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS history text;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS welcome_message text;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS services text;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS courses_supported text;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS statistics_description text;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS faq jsonb;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS testimonials jsonb;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS emergency_contact character varying(100);
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS footer_text character varying(255);
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS membership_details jsonb;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS registration_process text;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS required_documents text;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS membership_benefits text;
ALTER TABLE IF EXISTS library_libraryinfo ADD COLUMN IF NOT EXISTS library_rules text;
";
        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(dbContext.Database, alterSql);
        Console.WriteLine("Successfully executed direct ALTER TABLE statements for library_libraryinfo on startup.");

    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to execute script_safe.sql: {ex.Message}");
    }
}

app.Run();

public partial class Program { }
