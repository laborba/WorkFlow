using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Abstractions.Persistence.Models;

public sealed record ProjectListItemData(
    Guid PublicId,
    bool HasResponsibleUser,
    Guid? ResponsibleUserPublicId,
    string? ResponsibleUserName,
    string Name,
    string? Description,
    ProjectStatus Status,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ArchivedAt);