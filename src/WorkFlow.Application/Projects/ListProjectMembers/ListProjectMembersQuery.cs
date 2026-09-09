namespace WorkFlow.Application.Projects.ListProjectMembers;

public sealed record ListProjectMembersQuery(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid RequestedByUserPublicId,
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null);