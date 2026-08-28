using WorkFlow.Application.Common.Errors;

namespace WorkFlow.Application.Tenants;

public static class TenantErrors
{
    public static Error RegistrationNumberAlreadyExists { get; } =
        new(
            "Tenants.RegistrationNumberAlreadyExists",
            "Já existe uma empresa com esse número de registro.");

    public static Error NotFound { get; } =
        new(
            "Tenants.NotFound",
            "A empresa informada não foi encontrada.");

    public static Error Inactive { get; } =
        new(
            "Tenants.Inactive",
            "Não é possível realizar esta operação porque a empresa está desativada.");
}