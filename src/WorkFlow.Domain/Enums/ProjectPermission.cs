namespace WorkFlow.Domain.Enums;

public enum ProjectPermission
{
    EditProject = 1,
    ManageProjectMembers = 2,
    ManageProjectPermissions = 3,
    CompleteProject = 4,
    ReopenProject = 5,
    ArchiveProject = 6,

    CreateTask = 7,
    EditTask = 8,
    ClaimTask = 9,
    AssignTask = 10,
    ManageTaskCollaborators = 11,
    CancelTask = 12,
    ReopenTask = 13,

    ValidateTask = 14,
    SelfValidateTask = 15
}