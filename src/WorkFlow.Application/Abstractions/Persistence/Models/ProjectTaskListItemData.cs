using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Abstractions.Persistence.Models;

public sealed record ProjectTaskListItemData(
    Guid PublicId,
    Guid? CreatedByUserPublicId,
    bool HasResponsibleUser,
    Guid? ResponsibleUserPublicId,
    string? ResponsibleUserName,
    bool HasValidatorUser,
    Guid? ValidatorUserPublicId,
    string Title,
    string? Description,
    ProjectTaskStatus Status,
    ProjectTaskPriority Priority,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ArchivedAt);