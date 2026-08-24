namespace WorkFlow.Domain.Enums;

public enum TaskHistoryAction
{
    Created = 1,
    TitleChanged = 2,
    DescriptionChanged = 3,
    PriorityChanged = 4,
    DueDateChanged = 5,
    ResponsibleAssigned = 6,
    ResponsibleReassigned = 7,
    ResponsibleRemoved = 8,
    MovedToTodo = 9,
    Started = 10,
    Paused = 11,
    Resumed = 12,
    SentToValidation = 13,
    ValidationClaimed = 14,
    ValidationReleased = 15,
    ValidationReassigned = 16,
    ValidationWithdrawn = 17,
    ValidationApproved = 18,
    ValidationRejected = 19,
    Cancelled = 20,
    Reopened = 21,
    Archived = 22,
    Restored = 23,
    CollaboratorAdded = 24,
    CollaboratorRemoved = 25
}