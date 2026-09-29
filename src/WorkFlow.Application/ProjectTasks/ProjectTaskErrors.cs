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

    public static Error ResponsibleRemovalNotAllowed { get; } =
        new(
            "ProjectTasks.ResponsibleRemovalNotAllowed",
            "O usuário não possui permissão para remover o responsável desta tarefa.");

    public static Error ResponsibleRemovalBlockedByProjectStatus { get; } =
        new(
            "ProjectTasks.ResponsibleRemovalBlockedByProjectStatus",
            "Não é possível remover o responsável de tarefas de um projeto concluído ou arquivado.");

    public static Error ResponsibleRemovalBlockedByTaskStatus { get; } =
        new(
            "ProjectTasks.ResponsibleRemovalBlockedByTaskStatus",
            "O estado atual da tarefa não permite remover o responsável.");

    public static Error ClaimNotAllowed { get; } =
        new(
            "ProjectTasks.ClaimNotAllowed",
            "O usuário informado não possui permissão para assumir esta tarefa.");

    public static Error ClaimBlockedByProjectStatus { get; } =
        new(
            "ProjectTasks.ClaimBlockedByProjectStatus",
            "Não é possível assumir tarefas de um projeto concluído ou arquivado.");

    public static Error ClaimBlockedByTaskStatus { get; } =
        new(
            "ProjectTasks.ClaimBlockedByTaskStatus",
            "O estado atual da tarefa não permite que ela seja assumida.");

    public static Error AlreadyAssigned { get; } =
        new(
            "ProjectTasks.AlreadyAssigned",
            "A tarefa já possui um responsável.");

    public static Error UpdateNotAllowed { get; } =
        new(
            "ProjectTasks.UpdateNotAllowed",
            "O usuário informado não possui permissão para atualizar esta tarefa.");

    public static Error UpdateBlockedByProjectStatus { get; } =
        new(
            "ProjectTasks.UpdateBlockedByProjectStatus",
            "Não é possível atualizar tarefas de um projeto arquivado.");

    public static Error StartNotAllowed { get; } =
        new(
            "ProjectTasks.StartNotAllowed",
            "Somente o responsável atual pode iniciar esta tarefa.");

    public static Error StartBlockedByProjectStatus { get; } =
        new(
            "ProjectTasks.StartBlockedByProjectStatus",
            "A tarefa só pode ser iniciada quando o projeto estiver em andamento.");

    public static Error StartBlockedByTaskStatus { get; } =
        new(
            "ProjectTasks.StartBlockedByTaskStatus",
            "Somente tarefas pendentes podem ser iniciadas.");

    public static Error StartRequiresResponsible { get; } =
        new(
            "ProjectTasks.StartRequiresResponsible",
            "A tarefa precisa possuir um responsável para ser iniciada.");

    public static Error MoveToTodoNotAllowed { get; } =
        new(
            "ProjectTasks.MoveToTodoNotAllowed",
            "O usuário não possui permissão para mover esta tarefa para pendente.");

    public static Error MoveToTodoBlockedByProjectStatus { get; } =
        new(
            "ProjectTasks.MoveToTodoBlockedByProjectStatus",
            "Não é possível mover tarefas para pendente quando o projeto está concluído ou arquivado.");

    public static Error MoveToTodoBlockedByTaskStatus { get; } =
        new(
            "ProjectTasks.MoveToTodoBlockedByTaskStatus",
            "Somente tarefas no backlog podem ser movidas para pendente.");

    public static Error Archived { get; } =
        new(
            "ProjectTasks.Archived",
            "Uma tarefa arquivada não pode ter seu responsável alterado.");

}