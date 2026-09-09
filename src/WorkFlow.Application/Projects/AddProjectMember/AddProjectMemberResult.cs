namespace WorkFlow.Application.Projects.AddProjectMember;

public sealed record AddProjectMemberResult(
    Guid ProjectPublicId,
    Guid UserPublicId,
    Guid AddedByUserPublicId,
    DateTime AddedAt);