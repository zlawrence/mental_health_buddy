using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MentalHealthApp.Infrastructure.Repositories;
using MentalHealthApp.Infrastructure.Resilience;
using MentalHealthApp.Infrastructure.Services;
using MentalHealthApp.Application.Mappings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Text;
using DotNetEnv;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    Env.Load();
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MentalHealthApp.Application.Mappings.MappingProfile).Assembly);

// Enable CORS for local frontend during development
builder.Services.AddCors(options =>
{
    // The calls could be coming from any IP. 
    options.AddPolicy("AllowLocalhost53739", policy =>
    {
        policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
    });
});

// MongoDB setup
var mongoSettings = builder.Configuration.GetSection("MongoDb");
var connectionString = mongoSettings["ConnectionString"];
var databaseName = mongoSettings["DatabaseName"];

builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(connectionString));
builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(databaseName);
});

// Register MongoDB collections
builder.Services.AddScoped(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<User>("users");
});
builder.Services.AddScoped(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<GuardRail>("guardRails");
});
builder.Services.AddScoped(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<Conversation>("conversations");
});
builder.Services.AddScoped(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<ConversationMessage>("conversationMessages");
});
builder.Services.AddScoped(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<TherapistInvitation>("therapistInvitations");
});
builder.Services.AddScoped(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<PatientProfile>("patientProfiles");
});
builder.Services.AddScoped(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<TherapistAccess>("therapistAccess");
});
builder.Services.AddScoped(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<MessageCount>("messageCounts");
});
builder.Services.AddScoped(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<EmergencyContact>("emergencyContacts");
});
builder.Services.AddScoped(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<AuditLog>("auditLogs");
});
builder.Services.AddScoped(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<MentalHealthApp.Domain.Entities.Subscription>("subscriptions");
});
builder.Services.AddScoped(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<MentalHealthApp.Domain.Entities.MessageLog>("messageLogs");
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});

builder.Services.AddAuthorization();

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGuardRailRepository, GuardRailRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IConversationMessageRepository, ConversationMessageRepository>();
builder.Services.AddScoped<ITherapistInvitationRepository, TherapistInvitationRepository>();
builder.Services.AddScoped<IPatientProfileRepository, PatientProfileRepository>();
builder.Services.AddScoped<ITherapistAccessRepository, TherapistAccessRepository>();
builder.Services.AddScoped<IMessageCountRepository, MessageCountRepository>();
builder.Services.AddScoped<IEmergencyContactRepository, EmergencyContactRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IMessageLogRepository, MessageLogRepository>();

// Resilience: singleton audit logger + singleton pipeline provider (circuit breaker state must survive across requests)
builder.Services.AddSingleton<IResilienceAuditLogger, ResilienceAuditLogger>();
builder.Services.AddSingleton<ApiResiliencePipelineProvider>();

// Register infrastructure services
builder.Services.AddScoped<IPasswordHashingService, PasswordHashingService>();
builder.Services.AddScoped<IJwtTokenService>(sp =>
{
    var jwtSettings = sp.GetRequiredService<IConfiguration>().GetSection("Jwt");
    var key = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured");
    var expiryMinutes = int.Parse(jwtSettings["ExpiryInMinutes"] ?? "1440");
    var issuer = jwtSettings["Issuer"] ?? "MentalHealthApp";
    var audience = jwtSettings["Audience"] ?? "MentalHealthApp";
    return new JwtTokenService(key, expiryMinutes, issuer, audience);
});
builder.Services.AddScoped<IRiskClassifier, RiskClassifier>();
builder.Services.AddScoped<IGuardRailEnforcementService, GuardRailEnforcementService>();
builder.Services.AddScoped<IRateLimitingService, RateLimitingService>();
builder.Services.AddScoped<ILLMTherapyService>(sp =>
{
    var anthropicSettings = sp.GetRequiredService<IConfiguration>().GetSection("Anthropic");
    var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_DEV_KEY") ?? throw new InvalidOperationException("Anthropic API Key not configured");
    var modelId = anthropicSettings["ModelId"] ?? throw new InvalidOperationException("Anthropic ModelId not configured");
    return new ClaudeService(apiKey, modelId, sp.GetRequiredService<ApiResiliencePipelineProvider>());
});

// Register Stripe services
builder.Services.AddScoped<IPaymentGateway>(sp =>
{
    var secretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY") ?? throw new InvalidOperationException("Stripe secret key not configured");
    var environmentTag = Environment.GetEnvironmentVariable("PAYMENT_ENVIRONMENT") ?? "dev";

    return new StripeGateway(secretKey, environmentTag, sp.GetRequiredService<ApiResiliencePipelineProvider>());
});
builder.Services.AddScoped<IStripeWebhookParser, StripeWebhookParser>();
builder.Services.AddScoped<ISubscriptionService>(sp =>
{
    var stripeSettings = sp.GetRequiredService<IConfiguration>().GetSection("Stripe");
    var successUrl = stripeSettings["SuccessUrl"] ?? "https://anxietybuddy.app/payment-success";
    var cancelUrl = stripeSettings["CancelUrl"] ?? "https://anxietybuddy.app/payment-canceled";
    return new MentalHealthApp.Infrastructure.Services.SubscriptionService(
        sp.GetRequiredService<ISubscriptionRepository>(),
        sp.GetRequiredService<IUserRepository>(),
        sp.GetRequiredService<IPaymentGateway>(),
        successUrl,
        cancelUrl
    );
});

// Email queue: singleton channel for the background processor; scoped decorator persists to DB on enqueue
builder.Services.AddSingleton<EmailQueueChannel>();
builder.Services.AddScoped<IEmailQueue, PersistingEmailQueue>();

// Postmark email service
builder.Services.AddSingleton<IThirdPartyEmailService>(sp =>
{
    var apiKey = Environment.GetEnvironmentVariable("POSTMARK_API_KEY")
        ?? throw new InvalidOperationException("Postmark API key not configured");
    var postmarkSettings = sp.GetRequiredService<IConfiguration>().GetSection("Postmark");
    var fromEmail = postmarkSettings["FromEmail"] ?? "noreply@anxietybuddy.app";
    return new PostmarkEmailService(
        apiKey,
        fromEmail,
        sp.GetRequiredService<ILogger<PostmarkEmailService>>(),
        sp.GetRequiredService<ApiResiliencePipelineProvider>());
});
builder.Services.AddHostedService<EmailQueueProcessor>();

// Register application services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IGuardRailService, GuardRailService>();
builder.Services.AddScoped<IPatientService>(sp =>
{
    var appSettings = sp.GetRequiredService<IConfiguration>().GetSection("App");
    var appBaseUrl = appSettings["BaseUrl"] ?? "https://anxietybuddy.app";
    return new PatientService(
        sp.GetRequiredService<IUserRepository>(),
        sp.GetRequiredService<IPatientProfileRepository>(),
        sp.GetRequiredService<IGuardRailRepository>(),
        sp.GetRequiredService<ITherapistInvitationRepository>(),
        sp.GetRequiredService<IEmailQueue>(),
        appBaseUrl);
});
builder.Services.AddScoped<ITherapistService, TherapistService>();
builder.Services.AddScoped<IAdminService, AdminService>();

// IP-based rate limiting — limits apply per remote IP across all controllers
var rateLimitMaxRequests = int.TryParse(Environment.GetEnvironmentVariable("RATE_LIMIT_MAX_REQUESTS"), out var rl) && rl > 0 ? rl : 100;
var rateLimitWindowSeconds = int.TryParse(Environment.GetEnvironmentVariable("RATE_LIMIT_WINDOW_SECONDS"), out var rw) && rw > 0 ? rw : 60;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitMaxRequests,
                Window = TimeSpan.FromSeconds(rateLimitWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!builder.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}


app.UseRateLimiter();

// Enable CORS policy
app.UseCors("AllowLocalhost53739");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
