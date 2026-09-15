namespace WorkFlow.API.Contracts.ProjectTasks;

public sealed record AssignProjectTaskResponsibleRequest(
    Guid ResponsibleUserPublicId);