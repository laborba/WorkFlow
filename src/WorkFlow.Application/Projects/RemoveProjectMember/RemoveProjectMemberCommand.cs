namespace WorkFlow.Application.Projects.RemoveProjectMember;

public sealed record RemoveProjectMemberCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid RequestedByUserPublicId,
    Guid UserPublicId);