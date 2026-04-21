using Microsoft.EntityFrameworkCore;
using JWTAuthAPI.Data;
using JWTAuthAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;


var builder = WebApplication.CreateBuilder(args); // Create a builder for the web application

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Configure global request size limits (30MB max)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 31457280; // 30 MB
    options.ValueLengthLimit = 31457280;
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 31457280; // 30 MB
});

// Configure CORS policy - SECURITY: Restrict to specific origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                             ?? new[] { "http://localhost:5174", "http://localhost:5173" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    }); // Add support for controllers with string enum serialization
builder.Services.AddEndpointsApiExplorer(); // Add support for API endpoint exploration
builder.Services.AddHealthChecks();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 2;
});

// Configure rate limiting to prevent abuse
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<JWTAuthAPI.Middleware.RateLimitingMiddleware>();

// Configure Swagger with JWT Bearer Authentication
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "JWT Auth API",
        Version = "v1",
        Description = "JWT Authentication API with Role-Based Access Control",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Email = "your.email@example.com"
        }
    });

    // Define the Bearer authentication scheme
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
    });

    // Make sure Swagger UI requires a Bearer token to be specified
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))); // Configure the database context with PostgreSQL

string? GetJwtSetting(string setting)
{
    return builder.Configuration[$"Jwt:{setting}"] ?? builder.Configuration[$"JWT:{setting}"];
}

var jwtKey = GetJwtSetting("Key") ?? throw new InvalidOperationException("JWT Key is not configured");
var jwtIssuer = GetJwtSetting("Issuer");
var jwtAudience = GetJwtSetting("Audience");

if (builder.Environment.IsProduction() && (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience)))
{
    throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured in production.");
}

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var validateIssuer = !string.IsNullOrWhiteSpace(jwtIssuer);
        var validateAudience = !string.IsNullOrWhiteSpace(jwtAudience);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = validateIssuer,
            ValidateAudience = validateAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minute clock skew
        };
    });

// Configure Stripe Settings
builder.Services.Configure<JWTAuthAPI.Models.StripeSettings>(builder.Configuration.GetSection("Stripe"));
Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

builder.Services.AddScoped<JWTAuthAPI.Services.JwtService>(); // Register JwtService for dependency injection
builder.Services.AddScoped<JWTAuthAPI.Services.IEmailService, JWTAuthAPI.Services.EmailService>(); // Register EmailService for dependency injection
builder.Services.AddScoped<JWTAuthAPI.Services.IAuditService, JWTAuthAPI.Services.AuditService>(); // Register AuditService for dependency injection
builder.Services.AddScoped<JWTAuthAPI.Services.ICourseService, JWTAuthAPI.Services.CourseService>(); // Register CourseService for dependency injection
builder.Services.AddScoped<JWTAuthAPI.Services.IBatchService, JWTAuthAPI.Services.BatchService>(); // Register BatchService for dependency injection
builder.Services.AddScoped<JWTAuthAPI.Services.IStudentService, JWTAuthAPI.Services.StudentService>(); // Register StudentService for dependency injection
builder.Services.AddScoped<JWTAuthAPI.Services.IFileService, JWTAuthAPI.Services.FileService>(); // Register FileService for dependency injection
builder.Services.AddScoped<JWTAuthAPI.Services.IReceiptService, JWTAuthAPI.Services.ReceiptService>(); // Register ReceiptService for dependency injection
builder.Services.AddScoped<JWTAuthAPI.Services.IAttendanceService, JWTAuthAPI.Services.AttendanceService>(); // Register AttendanceService for dependency injection
// Payment & Finance Services
builder.Services.AddScoped<JWTAuthAPI.Services.IPaymentPlanService, JWTAuthAPI.Services.PaymentPlanService>(); // Register PaymentPlanService
builder.Services.AddScoped<JWTAuthAPI.Services.IStripePaymentService, JWTAuthAPI.Services.StripePaymentService>(); // Register StripePaymentService
builder.Services.AddScoped<JWTAuthAPI.Services.IFeeStructureService, JWTAuthAPI.Services.FeeStructureService>(); // Register FeeStructureService
builder.Services.AddScoped<JWTAuthAPI.Services.IFinancialReportService, JWTAuthAPI.Services.FinancialReportService>(); // Register FinancialReportService
builder.Services.AddScoped<JWTAuthAPI.Services.IPermissionService, JWTAuthAPI.Services.PermissionService>(); // Register PermissionService
builder.Services.AddScoped<JWTAuthAPI.Services.ICertificateService, JWTAuthAPI.Services.CertificateService>(); // Register CertificateService
// Dashboard Services
builder.Services.AddScoped<JWTAuthAPI.Services.IDashboardService, JWTAuthAPI.Services.DashboardService>(); // Register DashboardService for admin analytics
builder.Services.AddScoped(typeof(Microsoft.AspNetCore.Identity.IPasswordHasher<>), typeof(Microsoft.AspNetCore.Identity.PasswordHasher<>)); // Register password hasher for students
builder.Services.AddHttpContextAccessor(); // Required for AuditService to access HTTP context

var app = builder.Build(); // Build the web application

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                statusCode = 500,
                isSuccess = false,
                errorMessage = new[] { "An unexpected error occurred." }
            });
        });
    });

    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

// Add Security Headers
app.Use(async (context, next) =>
{
    // Prevent clickjacking
    context.Response.Headers["X-Frame-Options"] = "DENY";
    // Prevent MIME type sniffing
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    // Enable XSS protection
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    // Referrer policy
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    // Content Security Policy
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; img-src 'self' data: https:; style-src 'self' 'unsafe-inline'; script-src 'self'";
    await next();
});

// Serve only product images publicly. Student documents are intentionally excluded.
var publicProductUploadsPath = Path.Combine(app.Environment.ContentRootPath, "Uploads", "Products");
if (!Directory.Exists(publicProductUploadsPath))
{
    Directory.CreateDirectory(publicProductUploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        publicProductUploadsPath),
    RequestPath = "/Uploads/Products"
});

// Enable CORS
app.UseCors("AllowAll");

// Add rate limiting middleware
app.UseMiddleware<JWTAuthAPI.Middleware.RateLimitingMiddleware>();

app.UseAuthentication(); // add this
app.UseAuthorization();

app.MapControllers(); // Map controller routes
app.MapHealthChecks("/health");

// Seed default role permissions on startup
using (var scope = app.Services.CreateScope())
{
    var permissionService = scope.ServiceProvider.GetRequiredService<JWTAuthAPI.Services.IPermissionService>();
    await permissionService.SeedDefaultPermissionsAsync();

    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    var adminExists = await dbContext.ApplicationUsers.AnyAsync(u => u.Role == Roles.Admin);
    if (!adminExists)
    {
        var adminEmail = app.Configuration["BootstrapAdmin:Email"];
        var adminUsername = app.Configuration["BootstrapAdmin:Username"];
        var adminPassword = app.Configuration["BootstrapAdmin:Password"];

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var finalUsername = string.IsNullOrWhiteSpace(adminUsername) ? adminEmail : adminUsername;
            var usernameExists = await dbContext.ApplicationUsers.AnyAsync(u => u.Username == finalUsername);
            var emailExists = await dbContext.ApplicationUsers.AnyAsync(u => u.Email == adminEmail);

            if (!usernameExists && !emailExists)
            {
                dbContext.ApplicationUsers.Add(new ApplicationUser
                {
                    Username = finalUsername,
                    Email = adminEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Role = Roles.Admin,
                    IsActive = true,
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                await dbContext.SaveChangesAsync();
                startupLogger.LogWarning("Bootstrap admin account created for {Email}. Change this password immediately after first login.", adminEmail);
            }
            else
            {
                startupLogger.LogWarning("Bootstrap admin creation skipped because username or email already exists.");
            }
        }
        else
        {
            startupLogger.LogWarning("No admin account exists. Set BootstrapAdmin:Email and BootstrapAdmin:Password for initial provisioning.");
        }
    }
}

app.Run();