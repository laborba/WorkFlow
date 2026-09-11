namespace WorkFlow.API.Contracts.Projects;

public sealed record ResumeProjectRequest(
    DateTime? NewDueDate);