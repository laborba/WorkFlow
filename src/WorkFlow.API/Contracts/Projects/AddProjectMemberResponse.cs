namespace WorkFlow.API.Contracts.Projects;

public sealed record AddProjectMemberResponse(
    Guid ProjectPublicId,
    Guid UserPublicId,
    Guid AddedByUserPublicId,
    DateTime AddedAt);