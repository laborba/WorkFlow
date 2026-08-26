namespace WorkFlow.Application.Common.Errors;

public sealed record Error(
    string Code,
    string Message);