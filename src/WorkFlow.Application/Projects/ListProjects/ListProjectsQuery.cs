using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.ListProjects;

public sealed record ListProjectsQuery(
    Guid TenantPublicId,
    Guid RequestedByUserPublicId,
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    ProjectStatus? Status = null,
    Guid? ResponsibleUserPublicId = null);