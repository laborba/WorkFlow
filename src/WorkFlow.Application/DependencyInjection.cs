using Microsoft.Extensions.DependencyInjection;
using WorkFlow.Application.Tenants.CreateTenant;
using WorkFlow.Application.Tenants.GetTenantByPublicId;
using WorkFlow.Application.Tenants.ChangeTenantStatus;

namespace WorkFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<CreateTenantHandler>();
        services.AddScoped<GetTenantByPublicIdHandler>();
        services.AddScoped<ChangeTenantStatusHandler>();

        return services;
    }
}