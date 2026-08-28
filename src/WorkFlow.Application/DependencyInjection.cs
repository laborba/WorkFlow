using Microsoft.Extensions.DependencyInjection;
using WorkFlow.Application.Tenants.CreateTenant;
using WorkFlow.Application.Tenants.GetTenantByPublicId;
using WorkFlow.Application.Tenants.ChangeTenantStatus;
using WorkFlow.Application.Tenants.UpdateTenant;
using WorkFlow.Application.Tenants.ListTenants;
using WorkFlow.Application.Users.CreateUser;
using WorkFlow.Application.Users.GetUserByPublicId;

namespace WorkFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<CreateTenantHandler>();
        services.AddScoped<GetTenantByPublicIdHandler>();
        services.AddScoped<ChangeTenantStatusHandler>();
        services.AddScoped<UpdateTenantHandler>();
        services.AddScoped<ListTenantsHandler>();
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<GetUserByPublicIdHandler>();

        return services;
    }
}