using Microsoft.Extensions.DependencyInjection;
using WorkFlow.Application.Authentication.Login;
using WorkFlow.Application.Projects.AddProjectMember;
using WorkFlow.Application.Projects.CreateProject;
using WorkFlow.Application.Projects.GetProjectByPublicId;
using WorkFlow.Application.Projects.GrantProjectMemberPermission;
using WorkFlow.Application.Projects.ListProjectMemberPermissions;
using WorkFlow.Application.Projects.ListProjectMembers;
using WorkFlow.Application.Projects.ListProjects;
using WorkFlow.Application.Projects.PauseProject;
using WorkFlow.Application.Projects.RemoveProjectMember;
using WorkFlow.Application.Projects.ResumeProject;
using WorkFlow.Application.Projects.RevokeProjectMemberPermission;
using WorkFlow.Application.Projects.StartProject;
using WorkFlow.Application.Projects.UpdateProject;
using WorkFlow.Application.Tenants.ChangeTenantStatus;
using WorkFlow.Application.Tenants.CreateTenant;
using WorkFlow.Application.Tenants.GetTenantByPublicId;
using WorkFlow.Application.Tenants.ListTenants;
using WorkFlow.Application.Tenants.UpdateTenant;
using WorkFlow.Application.Users.ChangeUserRole;
using WorkFlow.Application.Users.ChangeUserStatus;
using WorkFlow.Application.Users.CreateUser;
using WorkFlow.Application.Users.GetUserByPublicId;
using WorkFlow.Application.Users.ListUsers;
using WorkFlow.Application.Users.UpdateUser;

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
        services.AddScoped<ListUsersHandler>();
        services.AddScoped<UpdateUserHandler>();
        services.AddScoped<ChangeUserStatusHandler>();
        services.AddScoped<ChangeUserRoleHandler>();

        services.AddScoped<LoginHandler>();

        services.AddScoped<CreateProjectHandler>();
        services.AddScoped<GetProjectByPublicIdHandler>();
        services.AddScoped<ListProjectsHandler>();
        services.AddScoped<UpdateProjectHandler>();
        services.AddScoped<StartProjectHandler>();
        services.AddScoped<PauseProjectHandler>();
        services.AddScoped<ResumeProjectHandler>();

        services.AddScoped<AddProjectMemberHandler>();
        services.AddScoped<ListProjectMembersHandler>();
        services.AddScoped<RemoveProjectMemberHandler>();

        services.AddScoped<GrantProjectMemberPermissionHandler>();
        services.AddScoped<ListProjectMemberPermissionsHandler>();
        services.AddScoped<RevokeProjectMemberPermissionHandler>();

        return services;
    }
}