using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects;

public sealed record ProjectStatusResult(
    Guid PublicId,
    Guid TenantPublicId,
    ProjectStatus Status,
    DateTime? DueDate,
    DateTime? UpdatedAt,
    DateTime? ArchivedAt);