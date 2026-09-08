using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.ListProjects;

public sealed record ProjectListItemResult(
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