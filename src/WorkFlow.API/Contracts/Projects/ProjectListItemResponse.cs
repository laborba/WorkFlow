using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.Projects;

public sealed record ProjectListItemResponse(
    Guid PublicId,
    string Name,
    string? Description,
    ProjectStatus Status,
    Guid? ResponsibleUserPublicId,
    string? ResponsibleUserName,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ArchivedAt);