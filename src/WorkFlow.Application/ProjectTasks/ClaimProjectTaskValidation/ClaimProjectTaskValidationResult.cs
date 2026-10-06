using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.ClaimProjectTaskValidation;

public sealed record ClaimProjectTaskValidationResult(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid ValidatorUserPublicId,
    ProjectTaskStatus Status,
    DateTime? UpdatedAt);