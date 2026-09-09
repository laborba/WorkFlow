namespace WorkFlow.API.Contracts.Projects;

public sealed record RemoveProjectMemberResponse(
    Guid ProjectPublicId,
    Guid UserPublicId,
    DateTime RemovedAt);