namespace WorkFlow.Domain.Enums;

public enum ProjectHistoryAction
{
    Created = 1,
    Started = 2,
    Paused = 3,
    Resumed = 4,
    Completed = 5,
    Reopened = 6,
    Archived = 7,
    Restored = 8,
    DueDateChanged = 9,
    ResponsibleChanged = 10,
    MemberAdded = 11,
    MemberRemoved = 12,
    PermissionGranted = 13,
    PermissionRevoked = 14
}