using WorkFlow.Application.Common.Errors;

namespace WorkFlow.Application.ProjectTasks;

public static class ProjectTaskErrors
{
    public static Error NotFound { get; } =
        new(
            "ProjectTasks.NotFound",
            "A tarefa informada não foi encontrada.");

    public static Error CreationNotAllowed { get; } =
        new(
            "ProjectTasks.CreationNotAllowed",
            "O usuário informado não possui permissão para criar tarefas neste projeto.");

    public static Error CreationBlockedByProjectStatus { get; } =
        new(
            "ProjectTasks.CreationBlockedByProjectStatus",
            "Não é possível criar tarefas em um projeto concluído ou arquivado.");

    public static Error AssignmentNotAllowed { get; } =
        new(
            "ProjectTasks.AssignmentNotAllowed",
            "O usuário informado não possui permissão para atribuir responsáveis às tarefas deste projeto.");

    public static Error AssignmentBlockedByProjectStatus { get; } =
        new(
            "ProjectTasks.AssignmentBlockedByProjectStatus",
            "Não é possível atribuir responsável a tarefas de um projeto concluído ou arquivado.");

    public static Error ResponsibleUserNotFound { get; } =
        new(
            "ProjectTasks.ResponsibleUserNotFound",
            "O usuário informado como responsável não foi encontrado.");

    public static Error ResponsibleUserInactive { get; } =
        new(
            "ProjectTasks.ResponsibleUserInactive",
            "Não é possível atribuir a tarefa a um usuário inativo.");

    public static Error ResponsibleUserNotActiveMember { get; } =
        new(
            "ProjectTasks.ResponsibleUserNotActiveMember",
            "O responsável precisa possuir participação ativa no projeto.");

    public static Error Archived { get; } =
        new(
            "ProjectTasks.Archived",
            "Uma tarefa arquivada não pode ter seu responsável alterado.");
}