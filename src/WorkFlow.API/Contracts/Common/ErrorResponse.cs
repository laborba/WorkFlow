namespace WorkFlow.API.Contracts.Common;

public sealed record ErrorResponse(
    string Code,
    string Message);