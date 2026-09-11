using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.Projects;

public sealed record ProjectStatusResponse(
    Guid PublicId,
    Guid TenantPublicId,
    ProjectStatus Status,
    DateTime? DueDate,
    DateTime? UpdatedAt,
    DateTime? ArchivedAt);