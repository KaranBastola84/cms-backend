using Microsoft.EntityFrameworkCore;
using JWTAuthAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;


var builder = WebApplication.CreateBuilder(args); // Create a builder for the web application

// Configure CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers(); // Add support for controllers
builder.Services.AddEndpointsApiExplorer(); // Add support for API endpoint exploration

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

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minute clock skew
        };
    });

// Configure Stripe Settings
builder.Services.Configure<JWTAuthAPI.Models.StripeSettings>(builder.Configuration.GetSection("StripeSettings"));
Stripe.StripeConfiguration.ApiKey = builder.Configuration["StripeSettings:SecretKey"];

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
// Dashboard Services
builder.Services.AddScoped<JWTAuthAPI.Services.IDashboardService, JWTAuthAPI.Services.DashboardService>(); // Register DashboardService for admin analytics
builder.Services.AddScoped(typeof(Microsoft.AspNetCore.Identity.IPasswordHasher<>), typeof(Microsoft.AspNetCore.Identity.PasswordHasher<>)); // Register password hasher for students
builder.Services.AddHttpContextAccessor(); // Required for AuditService to access HTTP context

var app = builder.Build(); // Build the web application

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

app.UseAuthentication(); // add this
app.UseAuthorization();

app.MapControllers(); // Map controller routes


app.Run();