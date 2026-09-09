namespace WorkFlow.Application.Projects.RemoveProjectMember;

public sealed record RemoveProjectMemberResult(
    Guid ProjectPublicId,
    Guid UserPublicId,
    DateTime RemovedAt);