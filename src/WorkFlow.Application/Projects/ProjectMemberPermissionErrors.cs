using WorkFlow.Application.Common.Errors;

namespace WorkFlow.Application.Projects;

public static class ProjectMemberPermissionErrors
{
    public static Error ManageNotAllowed { get; } =
        new(
            "ProjectMemberPermissions.ManageNotAllowed",
            "O usuário informado não possui permissão para gerenciar as permissões deste projeto.");

    public static Error AlreadyActive { get; } =
        new(
            "ProjectMemberPermissions.AlreadyActive",
            "O membro informado já possui esta permissão ativa.");

    public static readonly Error NotActive =
        new(
            "ProjectMemberPermissions.NotActive",
            "O membro informado não possui esta permissão ativa.");
}