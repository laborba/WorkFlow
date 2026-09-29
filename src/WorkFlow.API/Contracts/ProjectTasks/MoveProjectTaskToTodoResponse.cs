using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.ProjectTasks;

public sealed record MoveProjectTaskToTodoResponse(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    ProjectTaskStatus Status,
    DateTime? UpdatedAt);