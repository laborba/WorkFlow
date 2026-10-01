namespace WorkFlow.Application.ProjectTasks.SendProjectTaskToValidation;

public sealed record SendProjectTaskToValidationCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid TaskPublicId,
    Guid SentByUserPublicId);