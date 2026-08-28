using Microsoft.AspNetCore.Mvc;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.Users;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Application.Users.CreateUser;

namespace WorkFlow.API.Controllers;

[ApiController]
[Route("api/tenants/{tenantPublicId:guid}/users")]
public sealed class UsersController : ControllerBase
{
    private readonly CreateUserHandler
        _createUserHandler;

    public UsersController(
        CreateUserHandler createUserHandler)
    {
        _createUserHandler =
            createUserHandler;
    }

    [HttpPost]
    [ProducesResponseType<CreateUserResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateUserResponse>> Create(
        Guid tenantPublicId,
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var command =
            new CreateUserCommand(
                tenantPublicId,
                request.Name,
                request.Email,
                request.Password,
                request.Role);

        var result =
            await _createUserHandler.HandleAsync(
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

            if (error == TenantErrors.Inactive ||
                error == UserErrors.EmailAlreadyExists)
            {
                return Conflict(
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var createdUser =
            result.Value!;

        var response =
            new CreateUserResponse(
                createdUser.PublicId,
                createdUser.TenantPublicId,
                createdUser.Name,
                createdUser.Email,
                createdUser.Role,
                createdUser.IsActive,
                createdUser.CreatedAt);

        return Created(
            $"/api/tenants/" +
            $"{response.TenantPublicId}/users/" +
            $"{response.PublicId}",
            response);
    }
}