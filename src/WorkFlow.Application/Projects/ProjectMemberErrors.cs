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

    public static Error RemoveNotAllowed { get; } =
        new(
            "ProjectMembers.RemoveNotAllowed",
            "O usuário informado não possui permissão para remover membros deste projeto.");

    public static Error NotActive { get; } =
        new(
            "ProjectMembers.NotActive",
            "O usuário informado não possui participação ativa neste projeto.");
}