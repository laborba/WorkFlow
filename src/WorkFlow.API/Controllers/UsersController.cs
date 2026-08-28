using Microsoft.AspNetCore.Mvc;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.Users;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Application.Users.CreateUser;
using WorkFlow.Application.Users.GetUserByPublicId;

namespace WorkFlow.API.Controllers;

[ApiController]
[Route("api/tenants/{tenantPublicId:guid}/users")]
public sealed class UsersController : ControllerBase
{
    private readonly CreateUserHandler
        _createUserHandler;

    private readonly GetUserByPublicIdHandler
        _getUserByPublicIdHandler;

    public UsersController(
        CreateUserHandler createUserHandler,
        GetUserByPublicIdHandler getUserByPublicIdHandler)
    {
        _createUserHandler =
            createUserHandler;

        _getUserByPublicIdHandler =
            getUserByPublicIdHandler;
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

    [HttpGet("{userPublicId:guid}")]
    [ProducesResponseType<GetUserByPublicIdResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetUserByPublicIdResponse>> GetByPublicId(
        Guid tenantPublicId,
        Guid userPublicId,
        CancellationToken cancellationToken)
    {
        var query =
            new GetUserByPublicIdQuery(
                tenantPublicId,
                userPublicId);

        var result =
            await _getUserByPublicIdHandler.HandleAsync(
                query,
                cancellationToken);

        if (result.IsFailure)
        {
            var error =
                result.Error!;

            var errorResponse =
                new ErrorResponse(
                    error.Code,
                    error.Message);

            if (error == TenantErrors.NotFound ||
                error == UserErrors.NotFound)
            {
                return NotFound(
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var user =
            result.Value!;

        var response =
            new GetUserByPublicIdResponse(
                user.PublicId,
                user.TenantPublicId,
                user.Name,
                user.Email,
                user.Role,
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt);

        return Ok(
            response);
    }
}