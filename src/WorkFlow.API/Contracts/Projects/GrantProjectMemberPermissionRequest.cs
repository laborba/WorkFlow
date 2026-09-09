using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.Projects;

public sealed record GrantProjectMemberPermissionRequest(
    ProjectPermission Permission);