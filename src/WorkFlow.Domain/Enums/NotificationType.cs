namespace WorkFlow.Domain.Enums;

public enum NotificationType
{
    TaskAssigned = 1,
    TaskWaitingValidation = 2,
    TaskValidationRejected = 3,
    TaskCommented = 4,
    UserMentioned = 5,
    TaskDueSoon = 6,
    TaskOverdue = 7
}