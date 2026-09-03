using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkFlow.API.Authentication;
using WorkFlow.API.Contracts.Authentication;
using WorkFlow.API.Contracts.Common;
using WorkFlow.Application.Authentication;
using WorkFlow.Application.Authentication.Login;
using WorkFlow.Application.Tenants;
using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Controllers;

[ApiController]
[Route("api/authentication")]
public sealed class AuthenticationController :
    ControllerBase
{
    private readonly LoginHandler _loginHandler;

    public AuthenticationController(
        LoginHandler loginHandler)
    {
        _loginHandler =
            loginHandler;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command =
            new LoginCommand(
                request.TenantPublicId,
                request.Email,
                request.Password);

        var result =
            await _loginHandler.HandleAsync(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            var error =
                result.Error!;

            var errorResponse =
                new ErrorResponse(
                    error.Code,
                    error.Message);

            if (error == TenantErrors.NotFound)
            {
                return NotFound(
                    errorResponse);
            }

            if (error == TenantErrors.Inactive)
            {
                return Conflict(
                    errorResponse);
            }

            if (error ==
                AuthenticationErrors.InvalidCredentials)
            {
                return Unauthorized(
                    errorResponse);
            }

            if (error ==
                AuthenticationErrors.UserInactive)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var login =
            result.Value!;

        var response =
            new LoginResponse(
                login.AccessToken,
                login.ExpiresAt,
                login.UserPublicId,
                login.TenantPublicId,
                login.Name,
                login.Email,
                login.Role);

        return Ok(
            response);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me()
    {
        var userPublicIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        var tenantPublicIdValue =
            User.FindFirst(
                JwtClaimNames.TenantPublicId)?.Value;

        var name =
            User.FindFirst(
                ClaimTypes.Name)?.Value;

        var email =
            User.FindFirst(
                ClaimTypes.Email)?.Value;

        var roleValue =
            User.FindFirst(
                ClaimTypes.Role)?.Value;

        if (!Guid.TryParse(
                userPublicIdValue,
                out var userPublicId) ||
            string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) ||
            !Enum.TryParse<UserRole>(
                roleValue,
                out var role))
        {
            return Unauthorized();
        }

        Guid? tenantPublicId = null;

        if (role == UserRole.SystemAdmin)
        {
            if (!string.IsNullOrWhiteSpace(
                    tenantPublicIdValue))
            {
                return Unauthorized();
            }
        }
        else
        {
            if (!Guid.TryParse(
                    tenantPublicIdValue,
                    out var parsedTenantPublicId))
            {
                return Unauthorized();
            }

            tenantPublicId =
                parsedTenantPublicId;
        }

        return Ok(
            new CurrentUserResponse(
                userPublicId,
                tenantPublicId,
                name,
                email,
                role));
    }
}