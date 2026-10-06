namespace WorkFlow.Application.ProjectTasks.ClaimProjectTaskValidation;

public sealed record ClaimProjectTaskValidationCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid TaskPublicId,
    Guid ClaimedByUserPublicId);