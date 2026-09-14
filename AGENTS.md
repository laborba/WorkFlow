# WorkFlow — Agent Instructions

## Working mode

Act as the developer responsible for implementing the technical solution.

The user acts as the implementer/reviewer.

Before changing existing code:
- inspect the current implementation;
- inspect related tests;
- preserve established architecture and patterns;
- do not assume the contents of files you have not inspected.

Do not redesign established architecture unless there is a concrete technical reason.

Prefer completing coherent vertical slices rather than making scattered unrelated changes.

## Technology

- Windows 11
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- PostgreSQL with Npgsql
- xUnit

## Solution structure

Projects include:

- WorkFlow.API
- WorkFlow.Application
- WorkFlow.Domain
- WorkFlow.Infrastructure
- WorkFlow.UnitTests
- WorkFlow.IntegrationTests

Preserve the existing Domain / Application / Infrastructure / API separation.

Business rules belong in Domain when appropriate.

Application handlers coordinate:
- repositories;
- authorization;
- domain behavior;
- Unit of Work.

Infrastructure implements persistence.

Controllers must remain thin.

## Multi-tenancy

Multi-tenant isolation is a mandatory security rule.

Operational routes follow patterns such as:

`/api/tenants/{tenantPublicId}/...`

Never allow data belonging to one Tenant to be accessed through another Tenant.

Tenant identity must be validated in the backend.

SystemAdmin is global and does not receive ordinary operational access to Tenant resources.

## Roles

Existing roles:

- SystemAdmin
- TenantAdmin
- ProjectManager
- Member

TenantAdmin has administrative bypass for appropriate project operations inside its own Tenant.

ProjectManager and Member normally require:
- active ProjectMember membership;
- the specific ProjectPermission required by the operation.

## Project permissions

Existing permissions:

- EditProject
- ManageProjectMembers
- ManageProjectPermissions
- CompleteProject
- ReopenProject
- ArchiveProject
- CreateTask
- EditTask
- ClaimTask
- AssignTask
- ManageTaskCollaborators
- CancelTask
- ReopenTask
- ValidateTask
- SelfValidateTask

Do not substitute a broader permission when a specific permission exists.

## Project lifecycle

Implemented lifecycle:

- Planning -> InProgress
- InProgress -> Paused
- Paused -> InProgress
- InProgress -> Completed
- Completed -> InProgress
- Planning -> Archived
- Completed -> Archived
- Archived -> previous status

Completion requires:
- at least one task;
- every task must be Done or Cancelled.

Archived projects must be restored before operational modification.

Restore and Reopen are different operations.

## Security

Never place real credentials in source control.

Never commit:
- JWTs;
- passwords;
- secrets;
- API keys;
- private connection credentials;
- user-specific HTTP environment files.

Do not modify ignore rules to include secret files.

Do not print real secrets in summaries or commit messages.

## HTTP files

Manual HTTP requests are under:

`src/WorkFlow.API/Http`

Every request added to a `.http` file must have a title immediately above it beginning with:

`###`

Do not store real JWT tokens in versioned `.http` or environment files.

Manual authenticated API validation remains a user-reviewed step unless explicitly requested otherwise.

## Automated tests

Maintain unit and integration tests for relevant behavior.

Never remove, skip, weaken or rewrite an existing test merely to make a change pass.

When behavior changes intentionally, update tests to represent the intended rule.

Before presenting a vertical as ready for review, run:

`dotnet build`

and:

`dotnet test`

All automated tests must pass.

Integration tests use PostgreSQL.

Expected test database:

`workflow_test`

The environment variable currently used by integration tests is:

`WORKFLOW_TEST_CONNECTION_STRING`

Never replace the test database with a production or development database merely to make tests execute.

## Current validated baseline

At the end of the complete Project lifecycle vertical:

- 1033 automated tests pass;
- 0 failures.

This number is a baseline, not a target to preserve artificially.
New tests should increase it.

## Documentation

README.md describes the current public state of the project.

Keep documentation synchronized with completed verticals.

If older documentation conflicts with current code and README, inspect the implementation and ask for clarification rather than silently reverting newer behavior.

## Git safety

Never perform any of the following without explicit user approval:

- git commit
- git push
- git merge
- git rebase
- git reset
- branch deletion
- force operations

You may run read-only Git commands such as:

- git status
- git diff
- git diff --check
- git log
- git branch

Before asking for permission to commit:

1. run `dotnet build`;
2. run `dotnet test`;
3. ensure all required tests pass;
4. run `git diff --check`;
5. run `git status`;
6. summarize the files changed and the important behavioral changes.

Commit messages must be written in Portuguese.

Do not amend existing commits unless explicitly requested.

## Definition of done for a vertical

A vertical is not complete until, when applicable:

1. rules are defined;
2. Application is implemented;
3. Infrastructure/persistence is implemented;
4. relevant automated tests exist and pass;
5. API is implemented;
6. manual API validation is completed;
7. README/documentation is updated;
8. Git status is reviewed;
9. commit is explicitly approved and created;
10. push to origin is explicitly approved and completed;
11. working tree is clean.

Do not skip automated testing.

Do not claim that manual API testing was performed unless it actually was.

## Implementation style

Prefer concrete, complete implementation over tutorial-style explanations.

Inspect existing neighboring classes and follow their conventions.

Do not introduce new abstractions when an established project abstraction already solves the problem.

Do not make unrelated refactors while implementing a vertical.

If a broader refactor would materially improve the codebase, mention it separately instead of silently including it.

## Current development direction

The Project lifecycle is complete.

Development is moving into ProjectTask operations.

Task assignment is intentionally separate from task creation because `AssignTask` is a distinct permission.

Preserve this separation.