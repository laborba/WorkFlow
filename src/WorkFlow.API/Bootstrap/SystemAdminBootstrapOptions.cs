namespace WorkFlow.API.Bootstrap;

public sealed class SystemAdminBootstrapOptions
{
    public const string SectionName =
        "SystemAdminBootstrap";

    public bool Enabled { get; init; }

    public string Name { get; init; } =
        string.Empty;

    public string Email { get; init; } =
        string.Empty;

    public string Password { get; init; } =
        string.Empty;
}