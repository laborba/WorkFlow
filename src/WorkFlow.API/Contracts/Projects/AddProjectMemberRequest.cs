namespace WorkFlow.API.Contracts.Projects;

public sealed record AddProjectMemberRequest(
    Guid UserPublicId);