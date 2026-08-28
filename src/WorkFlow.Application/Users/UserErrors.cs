using WorkFlow.Application.Common.Errors;

namespace WorkFlow.Application.Users;

public static class UserErrors
{
    public static Error EmailAlreadyExists { get; } =
        new(
            "Users.EmailAlreadyExists",
            "Já existe um usuário com esse e-mail nesta empresa.");

    public static Error SystemAdminCannotBelongToTenant { get; } =
        new(
            "Users.SystemAdminCannotBelongToTenant",
            "O perfil de administrador do sistema não pode ser associado a uma empresa.");
}