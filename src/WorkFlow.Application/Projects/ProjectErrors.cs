using WorkFlow.Application.Common.Errors;

namespace WorkFlow.Application.Projects;

public static class ProjectErrors
{
    public static Error NotFound { get; } =
        new(
            "Projects.NotFound",
            "O projeto informado não foi encontrado.");

    public static Error CreationNotAllowed { get; } =
        new(
            "Projects.CreationNotAllowed",
            "O usuário informado não possui permissão para criar projetos.");

    public static Error ViewNotAllowed { get; } =
        new(
            "Projects.ViewNotAllowed",
            "O usuário informado não possui permissão para visualizar este projeto.");

    public static Error UpdateNotAllowed { get; } =
        new(
            "Projects.UpdateNotAllowed",
            "O usuário informado não possui permissão para atualizar este projeto.");

    public static Error Archived { get; } =
        new(
            "Projects.Archived",
            "Não é possível alterar um projeto arquivado.");
}