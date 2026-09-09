namespace WorkFlow.Application.Projects.AddProjectMember;

public sealed record AddProjectMemberCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid RequestedByUserPublicId,
    Guid UserPublicId);