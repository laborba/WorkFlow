using Microsoft.AspNetCore.Diagnostics;
using WorkFlow.API.Contracts.Common;

namespace WorkFlow.API.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var errorResponse = exception switch
        {
            ArgumentException => new ErrorResponse(
                "Validation.InvalidArgument",
                exception.Message),

            InvalidOperationException => new ErrorResponse(
                "BusinessRule.InvalidOperation",
                exception.Message),

            _ => new ErrorResponse(
                "Server.UnexpectedError",
                "Ocorreu um erro inesperado no servidor.")
        };

        var statusCode = exception switch
        {
            ArgumentException =>
                StatusCodes.Status400BadRequest,

            InvalidOperationException =>
                StatusCodes.Status409Conflict,

            _ =>
                StatusCodes.Status500InternalServerError
        };

        if (statusCode ==
            StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Ocorreu um erro inesperado durante a requisição.");
        }
        else
        {
            _logger.LogWarning(
                exception,
                "A requisição foi rejeitada por uma regra da aplicação.");
        }

        httpContext.Response.StatusCode =
            statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            errorResponse,
            cancellationToken);

        return true;
    }
}