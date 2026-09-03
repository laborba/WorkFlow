using WorkFlow.Application.Common.Errors;

namespace WorkFlow.Application.Projects;

public static class ProjectErrors
{
    public static Error CreationNotAllowed { get; } =
        new(
            "Projects.CreationNotAllowed",
            "O usuário informado não possui permissão para criar projetos.");
}