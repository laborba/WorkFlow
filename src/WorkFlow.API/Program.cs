using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WorkFlow.API.Authentication;
using WorkFlow.API.Authorization;
using WorkFlow.API.Exceptions;
using WorkFlow.Application;
using WorkFlow.Application.Abstractions.Security;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure;
using WorkFlow.API.Bootstrap;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(
        JwtOptions.SectionName));

builder.Services.Configure<SystemAdminBootstrapOptions>(
    builder.Configuration.GetSection(
        SystemAdminBootstrapOptions.SectionName));

var jwtOptions =
    builder.Configuration
        .GetSection(JwtOptions.SectionName)
        .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "As configurações JWT não foram encontradas.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException(
        "A chave JWT não foi configurada.");
}

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtOptions.Key)),

                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicyNames.TenantAccess,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.AddRequirements(
                new TenantAccessRequirement());
        });

    options.AddPolicy(
        AuthorizationPolicyNames.TenantAdmin,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.RequireRole(
                UserRole.TenantAdmin.ToString());
        });

    options.AddPolicy(
        AuthorizationPolicyNames.SystemAdmin,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.RequireRole(
                UserRole.SystemAdmin.ToString());
        });

    options.AddPolicy(
        AuthorizationPolicyNames.ProjectCreation,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.RequireRole(
                UserRole.TenantAdmin.ToString(),
                UserRole.ProjectManager.ToString());
        });
});

builder.Services.AddScoped<
    IAuthorizationHandler,
    TenantAccessHandler>();

var connectionString =
    builder.Configuration.GetConnectionString("PostgreSQL")
    ?? throw new InvalidOperationException(
        "A string de conexão 'PostgreSQL' não foi configurada.");

builder.Services.AddScoped<
    SystemAdminBootstrapper>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddScoped<
    IAccessTokenGenerator,
    JwtAccessTokenGenerator>();

var app = builder.Build();

using (var scope =
    app.Services.CreateScope())
{
    var systemAdminBootstrapper =
        scope.ServiceProvider
            .GetRequiredService<
                SystemAdminBootstrapper>();

    await systemAdminBootstrapper.ExecuteAsync();
}

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
