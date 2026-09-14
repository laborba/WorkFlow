using WorkFlow.Application.Common.Errors;

namespace WorkFlow.Application.ProjectTasks;

public static class ProjectTaskErrors
{
    public static Error CreationNotAllowed { get; } =
        new(
            "ProjectTasks.CreationNotAllowed",
            "O usuário informado não possui permissão para criar tarefas neste projeto.");

    public static Error CreationBlockedByProjectStatus { get; } =
        new(
            "ProjectTasks.CreationBlockedByProjectStatus",
            "Não é possível criar tarefas em um projeto concluído ou arquivado.");
}