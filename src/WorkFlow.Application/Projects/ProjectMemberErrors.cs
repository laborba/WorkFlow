using WorkFlow.Application.Common.Errors;

namespace WorkFlow.Application.Projects;

public static class ProjectMemberErrors
{
    public static Error AddNotAllowed { get; } =
        new(
            "ProjectMembers.AddNotAllowed",
            "O usuário informado não possui permissão para adicionar membros a este projeto.");

    public static Error AlreadyActive { get; } =
        new(
            "ProjectMembers.AlreadyActive",
            "O usuário informado já possui participação ativa neste projeto.");
}