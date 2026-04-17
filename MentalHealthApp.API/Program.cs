using System.Runtime.CompilerServices;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MentalHealthApp.Infrastructure.Repositories;
using MentalHealthApp.Infrastructure.Services;
using MentalHealthApp.Application.Mappings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Text;
using DotNetEnv;

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
    return new ClaudeService(apiKey, modelId);
});

// Register application services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IGuardRailService, GuardRailService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<ITherapistService, TherapistService>();
builder.Services.AddScoped<IAdminService, AdminService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();

// Enable CORS policy
app.UseCors("AllowLocalhost53739");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
