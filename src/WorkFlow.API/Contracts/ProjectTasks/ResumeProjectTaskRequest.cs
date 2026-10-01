namespace WorkFlow.API.Contracts.ProjectTasks;

public sealed record ResumeProjectTaskRequest(
    DateTime? NewDueDate);