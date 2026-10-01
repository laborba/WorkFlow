using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.SendProjectTaskToValidation;

public sealed record SendProjectTaskToValidationResult(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid ResponsibleUserPublicId,
    Guid? ValidatorUserPublicId,
    ProjectTaskStatus Status,
    DateTime? UpdatedAt);