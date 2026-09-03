using WorkFlow.Application.Common.Errors;

namespace WorkFlow.Application.Users;

public static class UserErrors
{
    public static Error NotFound { get; } =
        new(
            "Users.NotFound",
            "O usuário informado não foi encontrado.");

    public static Error Inactive { get; } =
        new(
            "Users.Inactive",
            "Não é possível realizar esta operação porque o usuário está desativado.");

    public static Error EmailAlreadyExists { get; } =
        new(
            "Users.EmailAlreadyExists",
            "Já existe um usuário com esse e-mail nesta empresa.");

    public static Error SystemAdminCannotBelongToTenant { get; } =
        new(
            "Users.SystemAdminCannotBelongToTenant",
            "O perfil de administrador do sistema não pode ser associado a uma empresa.");
}