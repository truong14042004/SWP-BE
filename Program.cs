using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Google.Cloud.Storage.V1;
using PayOS;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Options;
using SWP_BE.Services;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.Configure<GoogleAuthOptions>(
    builder.Configuration.GetSection(GoogleAuthOptions.SectionName));
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<AiOptions>(
    builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.Configure<GithubOptions>(
    builder.Configuration.GetSection(GithubOptions.SectionName));
builder.Services.Configure<GithubOAuthOptions>(
    builder.Configuration.GetSection(GithubOAuthOptions.SectionName));
builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<PayOsOptions>(
    builder.Configuration.GetSection(PayOsOptions.SectionName));
builder.Services.Configure<MarketPulseOptions>(
    builder.Configuration.GetSection(MarketPulseOptions.SectionName));
builder.Services.Configure<InternalAuthOptions>(
    builder.Configuration.GetSection(InternalAuthOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IPasswordAuthService, PasswordAuthService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton(_ => StorageClient.Create());
builder.Services.AddScoped<IFileStorageService, GoogleCloudStorageService>();
builder.Services.AddScoped<IPaymentProcessingService, PaymentProcessingService>();
builder.Services.AddScoped<IStudentReviewQuotaService, StudentReviewQuotaService>();
builder.Services.AddScoped<IUserSkillSyncService, UserSkillSyncService>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IAiTextGenerationService, GeminiTextGenerationService>();
builder.Services.AddScoped<IAiReviewSummaryService, AiReviewSummaryService>();
builder.Services.AddScoped<IAutoEvolveAiService, AutoEvolveAiService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHttpClient<IGitHubAnalysisService, GitHubAnalysisService>();
builder.Services.AddScoped<ILatentTalentAiService, LatentTalentAiService>();
builder.Services.AddScoped<IRoadmapMaterializer, RoadmapMaterializer>();
builder.Services.AddSingleton<ISkillExtractor, SkillExtractor>();
// TopCV được cào bằng script Python (Scrapling) chạy như tiến trình con ngay
// trong cùng container — không cần service riêng, không HTTP, không token.
builder.Services.AddScoped<IJobScraper, ScraplingProcessScraper>();
builder.Services.AddScoped<IJobScraper, ScraplingLinkedinScraper>();
builder.Services.AddScoped<IJobScraper, TopDevScraper>();
builder.Services.AddScoped<IJobScraper, ITNaviScraper>();
builder.Services.AddScoped<IJobScraper, VietnamWorksScraper>();
builder.Services.AddScoped<IMarketPulseRunner, MarketPulseRunner>();
builder.Services.AddHostedService<MarketPulseScheduler>();
builder.Services.AddSingleton(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IConfiguration>()
        .GetSection(PayOsOptions.SectionName)
        .Get<PayOsOptions>() ?? new PayOsOptions();

    return new PayOSClient(options.ClientId, options.ApiKey, options.ChecksumKey);
});

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SWP-BE API",
        Version = "v1",
        Description = "API documentation for the SWP-BE service."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT access token."
    });

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
            []
        }
    });
});
var defaultAllowedOrigins = new[]
{
    "https://swp-fe-careermap-2026-47ca0.web.app",
    "http://localhost:5173",
    "http://127.0.0.1:5173",
    "http://localhost:5174",
    "http://127.0.0.1:5174"
};
var configuredAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? [];
var allowedOrigins = defaultAllowedOrigins
    .Concat(configuredAllowedOrigins)
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
    
    // Clean up incorrect user FullNames in test database seed
    dbContext.Database.ExecuteSqlRaw(@"
        UPDATE users SET ""FullName"" = 'Industry Mentor 01' WHERE ""Username"" = 'mentor1';
        UPDATE users SET ""FullName"" = 'Academic Counselor 01' WHERE ""Username"" = 'counselor1';
    ");
}

app.Use(async (context, next) =>
{
    var origin = context.Request.Headers.Origin.ToString().Trim().TrimEnd('/');
    if (!string.IsNullOrWhiteSpace(origin)
        && allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
    {
        context.Response.Headers.AccessControlAllowOrigin = origin;
        context.Response.Headers.Vary = "Origin";
        context.Response.Headers.AccessControlAllowHeaders = "content-type, authorization";
        context.Response.Headers.AccessControlAllowMethods = "GET, POST, PUT, PATCH, DELETE, OPTIONS";
        context.Response.Headers.AccessControlAllowCredentials = "true";
    }

    if (HttpMethods.IsOptions(context.Request.Method))
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }

    try
    {
        await next();
    }
    catch (Exception exception)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(exception, "Unhandled request failure.");

        if (!context.Response.HasStarted)
        {
            context.Response.Clear();
            if (!string.IsNullOrWhiteSpace(origin)
                && allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            {
                context.Response.Headers.AccessControlAllowOrigin = origin;
                context.Response.Headers.Vary = "Origin";
                context.Response.Headers.AccessControlAllowHeaders = "content-type, authorization";
                context.Response.Headers.AccessControlAllowMethods = "GET, POST, PUT, PATCH, DELETE, OPTIONS";
            }

            context.Response.StatusCode = exception switch
            {
                InvalidOperationException => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            };
            await context.Response.WriteAsJsonAsync(new
            {
                message = context.Response.StatusCode == StatusCodes.Status500InternalServerError
                    ? "Server error."
                    : exception.Message,
                detail = exception.Message,
                type = exception.GetType().Name
            });
        }
    }
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<SWP_BE.Hubs.NotificationHub>("/hubs/notification");

app.MapGet("/", () => Results.Ok(new { status = "ok", service = "SWP-BE" }));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/health/db", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
    return canConnect
        ? Results.Ok(new { status = "healthy", database = "connected" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var urls = app.Urls
            .Where(url => url.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                || url.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var url = urls.FirstOrDefault() ?? "http://localhost:5019";
        TryOpenBrowser($"{url.TrimEnd('/')}/swagger");
    });
}

app.Run();

static void TryOpenBrowser(string url)
{
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch
    {
        // Opening a browser is a local-development convenience only.
    }
}
