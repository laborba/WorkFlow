using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.ProjectTasks;

public sealed record SendProjectTaskToValidationResponse(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid ResponsibleUserPublicId,
    Guid? ValidatorUserPublicId,
    ProjectTaskStatus Status,
    DateTime? UpdatedAt);