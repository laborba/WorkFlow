using Microsoft.AspNetCore.Mvc;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.Users;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Application.Users.CreateUser;
using WorkFlow.Application.Users.GetUserByPublicId;
using WorkFlow.Application.Users.ListUsers;
using WorkFlow.Application.Users.UpdateUser;
using WorkFlow.Domain.Enums;
using WorkFlow.Application.Users.ChangeUserStatus;

namespace WorkFlow.API.Controllers;

[ApiController]
[Route("api/tenants/{tenantPublicId:guid}/users")]
public sealed class UsersController : ControllerBase
{
    private readonly CreateUserHandler
        _createUserHandler;

    private readonly GetUserByPublicIdHandler
        _getUserByPublicIdHandler;

    private readonly ListUsersHandler
        _listUsersHandler;

    private readonly UpdateUserHandler
        _updateUserHandler;

    private readonly ChangeUserStatusHandler
        _changeUserStatusHandler;

    public UsersController(
        CreateUserHandler createUserHandler,
        GetUserByPublicIdHandler getUserByPublicIdHandler,
        ListUsersHandler listUsersHandler,
        UpdateUserHandler updateUserHandler,
        ChangeUserStatusHandler changeUserStatusHandler)
    {
        _createUserHandler =
            createUserHandler;

        _getUserByPublicIdHandler =
            getUserByPublicIdHandler;

        _listUsersHandler =
            listUsersHandler;

        _updateUserHandler =
            updateUserHandler;

        _changeUserStatusHandler =
            changeUserStatusHandler;
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

    [HttpGet]
    [ProducesResponseType<ListUsersResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListUsersResponse>> List(
        Guid tenantPublicId,
        [FromQuery] ListUsersRequest request,
        CancellationToken cancellationToken)
    {
        var query =
            new ListUsersQuery(
                tenantPublicId,
                request.PageNumber,
                request.PageSize,
                request.Role.HasValue
                    ? (UserRole?)request.Role.Value
                    : null,
                request.IsActive,
                request.Search);

        var result =
            await _listUsersHandler.HandleAsync(
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

            if (error == TenantErrors.NotFound)
            {
                return NotFound(
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var listedUsers =
            result.Value!;

        var items =
            listedUsers.Items
                .Select(user =>
                    new UserListItemResponse(
                        user.PublicId,
                        user.Name,
                        user.Email,
                        user.Role,
                        user.IsActive,
                        user.CreatedAt,
                        user.UpdatedAt))
                .ToArray();

        var response =
            new ListUsersResponse(
                items,
                listedUsers.PageNumber,
                listedUsers.PageSize,
                listedUsers.TotalCount,
                listedUsers.TotalPages);

        return Ok(
            response);
    }

    [HttpPut("{userPublicId:guid}")]
    [ProducesResponseType<UpdateUserResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateUserResponse>> Update(
        Guid tenantPublicId,
        Guid userPublicId,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var command =
            new UpdateUserCommand(
                tenantPublicId,
                userPublicId,
                request.Name,
                request.Email);

        var result =
            await _updateUserHandler.HandleAsync(
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

            if (error == TenantErrors.NotFound ||
                error == UserErrors.NotFound)
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

        var updatedUser =
            result.Value!;

        var response =
            new UpdateUserResponse(
                updatedUser.PublicId,
                updatedUser.TenantPublicId,
                updatedUser.Name,
                updatedUser.Email,
                updatedUser.Role,
                updatedUser.IsActive,
                updatedUser.CreatedAt,
                updatedUser.UpdatedAt);

        return Ok(
            response);
    }

    [HttpPatch("{userPublicId:guid}/status")]
    [ProducesResponseType<ChangeUserStatusResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ChangeUserStatusResponse>> ChangeStatus(
        Guid tenantPublicId,
        Guid userPublicId,
    [FromBody] ChangeUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command =
            new ChangeUserStatusCommand(
                tenantPublicId,
                userPublicId,
                request.IsActive);

        var result =
            await _changeUserStatusHandler.HandleAsync(
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

            if (error == TenantErrors.NotFound ||
                error == UserErrors.NotFound)
            {
                return NotFound(
                    errorResponse);
            }

            if (error == TenantErrors.Inactive)
            {
                return Conflict(
                    errorResponse);
            }

            return BadRequest(
                errorResponse);
        }

        var changedUser =
            result.Value!;

        var response =
            new ChangeUserStatusResponse(
                changedUser.PublicId,
                changedUser.TenantPublicId,
                changedUser.IsActive,
                changedUser.UpdatedAt);

        return Ok(
            response);
    }
}