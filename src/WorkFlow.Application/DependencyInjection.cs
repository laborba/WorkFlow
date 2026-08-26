using Microsoft.Extensions.DependencyInjection;
using WorkFlow.Application.Tenants.CreateTenant;
using WorkFlow.Application.Tenants.GetTenantByPublicId;

namespace WorkFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<CreateTenantHandler>();

        services.AddScoped<GetTenantByPublicIdHandler>();

        return services;
    }
}