using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.ProjectTasks;

public sealed record ClaimProjectTaskValidationResponse(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid ValidatorUserPublicId,
    ProjectTaskStatus Status,
    DateTime? UpdatedAt);