# OFFICE-JOURNAL-BACKEND-SUMMARY

## 1. Project Overview

### Purpose
DevLoggerBackend is the backend API for the office/developer journal application. It supports user registration and login, JWT-based authenticated access, daily development log management, a one-note-per-user feature, and operational observability through Prometheus metrics and structured logging.

### Business Problem It Solves
The service gives developers a central place to record daily work, blockers, solutions, learnings, tips, and a personal note. It also provides account management so each person's journal data stays isolated and secure.

### Overall Architecture
The solution follows Clean Architecture with four main runtime layers:
- `src/DevLoggerBackend.Api` handles HTTP, middleware, startup, configuration, and controllers.
- `src/DevLoggerBackend.Application` contains CQRS commands, queries, validators, abstractions, DTOs, and cross-cutting behaviors.
- `src/DevLoggerBackend.Domain` contains entities, enums, and shared base models.
- `src/DevLoggerBackend.Infrastructure` provides EF Core persistence, repository implementations, and concrete service implementations.

### How The Major Components Work Together
Requests enter the API project, pass through middleware, and reach controllers. Controllers send commands or queries through MediatR. MediatR executes FluentValidation pipeline behavior first, then the relevant handler. Handlers use repository and service abstractions from the Application layer. Infrastructure implements those abstractions using EF Core and PostgreSQL. DTOs are returned to the controller, serialized to JSON, and sent back to the client.

## 2. Complete Application Execution Flow

### Startup Sequence
1. `src/DevLoggerBackend.Api/Program.cs` creates the web host.
2. If the `PORT` environment variable is present, the app binds to `http://0.0.0.0:{PORT}`. Otherwise it binds to `http://localhost:5000`.
3. Serilog is configured from configuration plus console and rolling file sinks.
4. `AddApplication()` registers MediatR and validators.
5. `AddInfrastructure()` registers EF Core, repositories, password hashing, token generation, and current-user resolution.
6. Controllers, authentication, Swagger, health checks, and CORS are added.
7. The app is built.
8. Swagger UI is enabled in development or when `EnableSwaggerInProduction` is true.
9. Database migrations can run at startup when `ApplyMigrationsOnStartup` is enabled.
10. `UseHttpMetrics()`, `UseApiPipeline()`, `MapMetrics()`, and `Run()` start request processing.

### Initialization Process
`src/DevLoggerBackend.Api/Extensions/WebApplicationExtensions.cs` contains startup helpers. `ApplyDatabaseMigrationsAsync()` creates a DI scope, resolves `AppDbContext`, and applies EF migrations. It specifically catches PostgreSQL authentication error `28P01` and only allows startup to continue in development.

### Request/Response Lifecycle
1. A request enters `GlobalExceptionHandlingMiddleware`.
2. CORS runs.
3. Authentication reads the JWT bearer token.
4. Authorization checks `[Authorize]` on the controller or action.
5. ASP.NET Core model binding maps JSON into request DTOs.
6. The controller creates a MediatR command or query.
7. `ValidationBehavior<,>` runs all validators registered for that request type.
8. The handler executes business logic.
9. Repositories read or write through `AppDbContext`.
10. `AppDbContext.SaveChangesAsync()` updates timestamps on tracked `BaseEntity` instances.
11. The controller returns an HTTP result.
12. The exception middleware converts unexpected failures into structured JSON if needed.

### Data Flow
Configuration and environment variables feed startup. Startup registers DI services. Controllers receive HTTP input and forward it into the application layer. The application layer enforces validation and business rules. Infrastructure translates those rules into database operations. The domain layer defines the object model shared by all layers.

### Module Communication
- API -> Application via MediatR commands and queries.
- Application -> Infrastructure via repository, unit-of-work, and service interfaces.
- Infrastructure -> Domain via EF Core entity mappings.
- Authentication -> Current user service via JWT claims.

## 3. Complete Folder and File Documentation

### Root and Support Files

#### `DevLoggerBackend.sln`
- Purpose and code present: Visual Studio solution that groups the API, Application, Domain, Infrastructure, and test projects.
- How it works: build and test tooling uses the solution to discover all projects and their relationships.
- Executed when: opening the workspace in Visual Studio or running solution-level build/test commands.
- Depends on: all `.csproj` files in `src/` and `tests/`.
- Fits into the flow: defines the top-level workspace structure, not runtime behavior.

#### `README.md`
- Purpose and code present: human-facing project guide with local run commands, Docker usage, migration commands, frontend integration setup, and Render deployment notes.
- How it works: documentation only, no runtime code.
- Executed when: read by developers.
- Depends on: the current project layout, ports, connection strings, and config keys.
- Fits into the flow: provides the operational instructions needed to start and integrate the backend.

#### `docker-compose.yml`
- Purpose and code present: local container orchestration for PostgreSQL and the API.
- How it works: starts `postgres:16` with a persisted volume and a separate API container built from `src/DevLoggerBackend.Api/Dockerfile`.
- Executed when: `docker compose up --build` is run.
- Depends on: Docker, the API Dockerfile, the app settings used by the container, and the host port mapping.
- Fits into the flow: provides a repeatable local environment for the backend and database.

#### `monitoring/prometheus.yml`
- Purpose and code present: Prometheus scrape configuration.
- How it works: scrapes Prometheus itself and the backend API on `host.docker.internal:5000` every 15 seconds.
- Executed when: Prometheus starts with this configuration file.
- Depends on: the backend exposing `/metrics` through `prometheus-net.AspNetCore`.
- Fits into the flow: supports observability and dashboarding, not business logic.

#### `DOCKER-GRAFANA-SUMMARY.md`
- Purpose and code present: operational notes describing Prometheus, Grafana, API metrics, and dashboard queries.
- How it works: documentation only.
- Executed when: read by developers or operators.
- Depends on: the metrics endpoint, Prometheus config, and Grafana datasource settings.
- Fits into the flow: captures the monitoring setup that surrounds the application runtime.

#### `OFFICE-JOURNAL-BACKEND-SUMMARY.md`
- Purpose and code present: the generated technical reference for this project.
- How it works: documents the architecture, execution flow, file map, database model, dependencies, configuration, and key observations.
- Executed when: read by developers who need a full project reference.
- Depends on: every inspected source, config, test, and support file in the repository.
- Fits into the flow: serves as the long-form onboarding and maintenance guide for the codebase.

### API Project

#### `src/DevLoggerBackend.Api/Program.cs`
- Purpose and code present: top-level application entry point and host bootstrapper.
- Code present: host setup, Serilog configuration, service registration, JWT bearer auth, Swagger setup, health checks, CORS policy, migration startup, Prometheus metrics, pipeline wiring, and `Run()`.
- How it works: reads config, configures listeners, and builds the full HTTP pipeline. It also opens Swagger automatically in development.
- Executed when: the API process starts via `dotnet run`, Docker, or a hosting platform.
- Depends on: `DevLoggerBackend.Application`, `DevLoggerBackend.Infrastructure`, Serilog, JWT bearer auth, Swagger, Prometheus, and environment/config values such as `PORT`, `Jwt:*`, `AllowedOrigins`, `ApplyMigrationsOnStartup`, and `EnableSwaggerInProduction`.
- Fits into the flow: this is the root of the runtime path for every request.

#### `src/DevLoggerBackend.Api/Extensions/WebApplicationExtensions.cs`
- Purpose and code present: extension methods `UseApiPipeline()` and `ApplyDatabaseMigrationsAsync()`.
- How it works: `UseApiPipeline()` wires the global exception middleware, CORS, authentication, authorization, and controller routing. `ApplyDatabaseMigrationsAsync()` applies EF migrations and handles PostgreSQL auth failures during startup.
- Executed when: called from `Program.cs` during app startup.
- Depends on: `GlobalExceptionHandlingMiddleware`, `AppDbContext`, EF Core, and Npgsql.
- Fits into the flow: centralizes the reusable startup and pipeline wiring.

#### `src/DevLoggerBackend.Api/Middleware/GlobalExceptionHandlingMiddleware.cs`
- Purpose and code present: global try/catch wrapper for request processing.
- Code present: `GlobalExceptionHandlingMiddleware`, `Invoke()`, and `HandleExceptionAsync()`.
- How it works: catches unhandled exceptions, logs them, prints a diagnostic block to the console, and converts known exceptions into a structured `ErrorResponse` JSON body.
- Executed when: every HTTP request passes through the middleware pipeline.
- Depends on: `ErrorResponse`, FluentValidation `ValidationException`, and application exceptions `NotFoundException`, `UnauthorizedException`, and `ConflictException`.
- Fits into the flow: normalizes error responses and prevents raw exceptions from escaping to clients.

#### `src/DevLoggerBackend.Api/Models/ErrorResponse.cs`
- Purpose and code present: response DTO for errors.
- Code present: `ErrorResponse` with `StatusCode`, `Message`, `TraceId`, and optional `Errors`.
- How it works: serialized by the exception middleware.
- Executed when: errors are returned from the middleware.
- Depends on: nothing else.
- Fits into the flow: provides a consistent client-facing error format.

#### `src/DevLoggerBackend.Api/Controllers/AuthController.cs`
- Purpose and code present: HTTP endpoints for registration, login, and identity verification.
- Code present: `Register()`, `Login()`, and `Verify()`.
- How it works: `Register()` sends `RegisterCommand`; `Login()` sends `LoginCommand`; `Verify()` checks the current user using `ICurrentUserService` and `IUserRepository`.
- Executed when: auth endpoints are called.
- Depends on: MediatR, auth command/query DTOs, `IUserRepository`, `ICurrentUserService`, and `UnauthorizedException`.
- Fits into the flow: provides the public auth surface for client apps.

#### `src/DevLoggerBackend.Api/Controllers/DailyLogsController.cs`
- Purpose and code present: authenticated CRUD and search endpoints for daily logs.
- Code present: `GetAll()`, `GetById()`, `Create()`, `Update()`, `Delete()`, and `Search()`.
- How it works: converts request DTOs into MediatR requests and returns DTOs or HTTP status codes.
- Executed when: logged-in users access `/api/dailylogs`.
- Depends on: MediatR, daily log commands/queries and DTOs, and `[Authorize]`.
- Fits into the flow: exposes daily log business capabilities over HTTP.

#### `src/DevLoggerBackend.Api/Controllers/NotesController.cs`
- Purpose and code present: authenticated note retrieval and save endpoints.
- Code present: `Get()` and `Save()`.
- How it works: `Get()` returns `204 No Content` when the note does not exist; `Save()` upserts the note for the current user.
- Executed when: logged-in users access `/api/notes`.
- Depends on: MediatR, note commands/queries and DTOs, and `[Authorize]`.
- Fits into the flow: exposes the one-note-per-user feature over HTTP.

#### `src/DevLoggerBackend.Api/appsettings.json`
- Purpose and code present: main runtime configuration.
- Code present: default connection string, JWT values, allowed origins, swagger toggle, migration toggle, and Serilog rules.
- How it works: loaded by the host at startup and read by `Program.cs`, infrastructure DI, and the token service.
- Executed when: configuration is built during app startup.
- Depends on: the deployment environment and any overriding environment variables.
- Fits into the flow: supplies the operational settings used by the runtime.

#### `src/DevLoggerBackend.Api/appsettings.Development.json`
- Purpose and code present: development-only Serilog override.
- Code present: lower default log level set to `Debug`.
- How it works: merged over `appsettings.json` when `ASPNETCORE_ENVIRONMENT=Development`.
- Executed when: local development or any environment marked Development.
- Depends on: the base app settings.
- Fits into the flow: makes local debugging more verbose.

#### `src/DevLoggerBackend.Api/Properties/launchSettings.json`
- Purpose and code present: local launch profile for Visual Studio and `dotnet run`.
- Code present: project launch profile, browser auto-open, URL, and `ASPNETCORE_ENVIRONMENT=Development`.
- How it works: helps local dev start the app on `http://localhost:5000/swagger`.
- Executed when: the project is launched from tooling that honors launch settings.
- Depends on: `Program.cs` startup behavior.
- Fits into the flow: streamlines local development.

#### `src/DevLoggerBackend.Api/Dockerfile`
- Purpose and code present: multi-stage container build for the API.
- Code present: SDK build stage, publish stage, runtime stage, port exposure, and `ENTRYPOINT`.
- How it works: restores and publishes the project, then runs the compiled DLL in the ASP.NET runtime image.
- Executed when: Docker builds the API image.
- Depends on: the solution, the API project, and the Docker build context root.
- Fits into the flow: enables containerized deployment.

#### `src/DevLoggerBackend.Api/DevLoggerBackend.Api.csproj`
- Purpose and code present: API project file and package reference manifest.
- Code present: `net10.0` target, nullable and implicit usings, XML docs, and package/project references.
- How it works: defines the API's build-time dependencies.
- Executed when: the SDK restores, builds, and publishes the project.
- Depends on: Application, Infrastructure, JWT bearer auth, EF Core design-time tooling, Prometheus, Serilog, Swagger, and JWT token packages.
- Fits into the flow: declares the runtime and tooling surface for the API layer.

### Application Project

#### `src/DevLoggerBackend.Application/DependencyInjection.cs`
- Purpose and code present: application-layer service registration.
- Code present: `AddApplication()`.
- How it works: registers MediatR handlers from the application assembly, all FluentValidation validators, and the validation pipeline behavior.
- Executed when: called from `Program.cs`.
- Depends on: MediatR, FluentValidation, and `ValidationBehavior<,>`.
- Fits into the flow: wires the use-case layer into DI.

#### `src/DevLoggerBackend.Application/Common/Behaviors/ValidationBehavior.cs`
- Purpose and code present: MediatR pipeline behavior for request validation.
- Code present: `ValidationBehavior<TRequest, TResponse>` and `Handle()`.
- How it works: runs every validator for the current request, aggregates errors, and throws `ValidationException` if any failures exist.
- Executed when: any MediatR request passes through the pipeline.
- Depends on: FluentValidation and MediatR.
- Fits into the flow: enforces request rules before business logic runs.

#### `src/DevLoggerBackend.Application/Common/Models/PagedResult.cs`
- Purpose and code present: generic paging container.
- Code present: `PagedResult<T>` with `Items`, `TotalCount`, `PageNumber`, and `PageSize`.
- How it works: value object for future paged queries.
- Executed when: only if future code uses it.
- Depends on: nothing else.
- Fits into the flow: currently unused, but ready for list endpoints that need pagination.

#### `src/DevLoggerBackend.Application/Common/Exceptions/ConflictException.cs`
- Purpose and code present: custom exception for conflicting state.
- Code present: `ConflictException`.
- How it works: thrown by handlers such as duplicate registration.
- Executed when: a business rule conflict occurs.
- Depends on: nothing else.
- Fits into the flow: mapped to HTTP 409 by the exception middleware.

#### `src/DevLoggerBackend.Application/Common/Exceptions/NotFoundException.cs`
- Purpose and code present: custom exception for missing resources.
- Code present: `NotFoundException`.
- How it works: thrown when a record is missing or the current user does not own it.
- Executed when: access checks fail or records do not exist.
- Depends on: nothing else.
- Fits into the flow: mapped to HTTP 404 by the exception middleware.

#### `src/DevLoggerBackend.Application/Common/Exceptions/UnauthorizedException.cs`
- Purpose and code present: custom exception for authentication failures.
- Code present: `UnauthorizedException`.
- How it works: thrown when a current user cannot be resolved or login fails.
- Executed when: a request requires authentication but the user is not valid.
- Depends on: nothing else.
- Fits into the flow: mapped to HTTP 401 by the exception middleware.

#### `src/DevLoggerBackend.Application/Abstractions/Persistence/IUnitOfWork.cs`
- Purpose and code present: unit-of-work abstraction.
- Code present: `IUnitOfWork` with `SaveChangesAsync()`.
- How it works: allows handlers to commit changes without knowing the concrete DbContext.
- Executed when: handlers persist data.
- Depends on: nothing else.
- Fits into the flow: decouples business logic from EF Core.

#### `src/DevLoggerBackend.Application/Abstractions/Repositories/IUserRepository.cs`
- Purpose and code present: user data access contract.
- Code present: `AddAsync()`, `GetByEmailAsync()`, `GetByIdAsync()`, and `GetDefaultUserAsync()`.
- How it works: defines the user lookup and insert operations used by auth flows.
- Executed when: implemented by Infrastructure and called from handlers/controllers.
- Depends on: `DevLoggerBackend.Domain.Entities.User`.
- Fits into the flow: supports authentication and identity lookup.

#### `src/DevLoggerBackend.Application/Abstractions/Repositories/INoteRepository.cs`
- Purpose and code present: note data access contract.
- Code present: `GetByUserIdAsync()` and `AddAsync()`.
- How it works: supports note retrieval and creation/update flows.
- Executed when: implemented by Infrastructure and called from note handlers.
- Depends on: `DevLoggerBackend.Domain.Entities.Note`.
- Fits into the flow: supports the per-user note feature.

#### `src/DevLoggerBackend.Application/Abstractions/Repositories/IDailyLogRepository.cs`
- Purpose and code present: daily log data access contract.
- Code present: `GetAllAsync()`, `GetByUserIdAsync()`, `GetByIdAsync()`, `SearchByUserIdAsync()`, `AddAsync()`, `Update()`, and `Remove()`.
- How it works: defines the repository surface used by daily log handlers.
- Executed when: implemented by Infrastructure and called from daily log use cases.
- Depends on: `DevLoggerBackend.Domain.Entities.DailyLog`.
- Fits into the flow: supports all daily log persistence and search operations.

#### `src/DevLoggerBackend.Application/Abstractions/Services/ICurrentUserService.cs`
- Purpose and code present: abstraction for the authenticated user's identity.
- Code present: `UserId` getter.
- How it works: exposes the current user id to handlers.
- Executed when: infrastructure resolves it from the HTTP context.
- Depends on: nothing else.
- Fits into the flow: enables per-user authorization checks inside use cases.

#### `src/DevLoggerBackend.Application/Abstractions/Services/IPasswordHasher.cs`
- Purpose and code present: password hashing contract.
- Code present: `Hash()` and `Verify()`.
- How it works: lets auth handlers remain independent of BCrypt.
- Executed when: implemented by Infrastructure and used in auth flows.
- Depends on: nothing else.
- Fits into the flow: supports secure credential storage and login verification.

#### `src/DevLoggerBackend.Application/Abstractions/Services/ITokenService.cs`
- Purpose and code present: JWT generation contract.
- Code present: `GenerateToken(User user)`.
- How it works: lets login handlers produce tokens without owning JWT implementation details.
- Executed when: implemented by Infrastructure and called during login.
- Depends on: `DevLoggerBackend.Domain.Entities.User`.
- Fits into the flow: produces the bearer token used by authenticated requests.

#### `src/DevLoggerBackend.Application/Features/Auth/Commands/RegisterCommand.cs`
- Purpose and code present: registration request and handler.
- Code present: `RegisterCommand` and `RegisterCommandHandler`.
- How it works: checks for an existing email, hashes the password, creates a new user with the `Developer` role, persists it, and commits through the unit of work.
- Executed when: `POST /api/auth/register` sends the command.
- Depends on: user repository, password hasher, unit of work, `User`, `UserRole`, and `ConflictException`.
- Fits into the flow: creates new accounts.

#### `src/DevLoggerBackend.Application/Features/Auth/Commands/LoginCommand.cs`
- Purpose and code present: login request and handler.
- Code present: `LoginCommand` and `LoginCommandHandler`.
- How it works: loads the user by email, verifies the password, throws `UnauthorizedException` on failure, and returns `LoginResponseDto` with a JWT and user profile.
- Executed when: `POST /api/auth/login` sends the command.
- Depends on: user repository, password hasher, token service, auth DTOs, and `UserRole`.
- Fits into the flow: authenticates users and issues access tokens.

#### `src/DevLoggerBackend.Application/Features/Auth/Queries/VerifyAuthQuery.cs`
- Purpose and code present: placeholder identity verification query.
- Code present: `VerifyAuthQuery` and `VerifyAuthQueryHandler`.
- How it works: currently returns `null` and contains a TODO to use JWT claims for loading the authenticated profile.
- Executed when: only if future code sends this query.
- Depends on: `UserDto`.
- Fits into the flow: a stub for a future auth profile lookup path, not used by the current controller.

#### `src/DevLoggerBackend.Application/Features/Auth/Validators/RegisterCommandValidator.cs`
- Purpose and code present: validation rules for registration.
- Code present: `RegisterCommandValidator`.
- How it works: enforces name, email, password, and confirm-password rules, including password match.
- Executed when: `RegisterCommand` passes through MediatR validation.
- Depends on: FluentValidation.
- Fits into the flow: blocks malformed registrations before business logic runs.

#### `src/DevLoggerBackend.Application/Features/Auth/Validators/LoginCommandValidator.cs`
- Purpose and code present: validation rules for login.
- Code present: `LoginCommandValidator`.
- How it works: requires a valid email and a non-empty password.
- Executed when: `LoginCommand` passes through MediatR validation.
- Depends on: FluentValidation.
- Fits into the flow: blocks malformed login requests early.

#### `src/DevLoggerBackend.Application/Features/Auth/Dtos/RegisterRequestDto.cs`
- Purpose and code present: registration input DTO.
- Code present: `RegisterRequestDto` with `Name`, `Email`, `Password`, and `ConfirmPassword`.
- How it works: model-bound from the register request body and then mapped into `RegisterCommand`.
- Executed when: the register endpoint receives JSON.
- Depends on: nothing else.
- Fits into the flow: carries client registration data into the controller.

#### `src/DevLoggerBackend.Application/Features/Auth/Dtos/LoginRequestDto.cs`
- Purpose and code present: login input DTO.
- Code present: `LoginRequestDto` with `Email` and `Password`.
- How it works: model-bound from the login request body and mapped into `LoginCommand`.
- Executed when: the login endpoint receives JSON.
- Depends on: nothing else.
- Fits into the flow: carries login credentials into the controller.

#### `src/DevLoggerBackend.Application/Features/Auth/Dtos/LoginResponseDto.cs`
- Purpose and code present: login output DTO.
- Code present: `LoginResponseDto` with `User` and `Token`.
- How it works: serialized back to the client after successful login.
- Executed when: the login handler succeeds.
- Depends on: `UserDto`.
- Fits into the flow: returns the authenticated session payload to the frontend.

#### `src/DevLoggerBackend.Application/Features/Auth/Dtos/UserDto.cs`
- Purpose and code present: lightweight user profile DTO.
- Code present: `UserDto` with `Id`, `Name`, `Email`, and `Role`.
- How it works: used in login and verify responses.
- Executed when: controllers serialize auth responses.
- Depends on: nothing else.
- Fits into the flow: exposes user identity in a client-friendly shape.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Commands/CreateDailyLogCommand.cs`
- Purpose and code present: create-daily-log request and handler.
- Code present: `CreateDailyLogCommand` and `CreateDailyLogCommandHandler`.
- How it works: parses `LogDate`, validates user identity, normalizes text fields, creates a `DailyLog`, saves it, and maps it to `DailyLogDto`.
- Executed when: `POST /api/dailylogs` sends the command.
- Depends on: daily log repository, user repository, unit of work, current user service, `DailyLog`, `CreateDailyLogDto`, `DailyLogDto`, and `UnauthorizedException`.
- Fits into the flow: creates a new owned log for the signed-in user.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Commands/UpdateDailyLogCommand.cs`
- Purpose and code present: update-daily-log request and handler.
- Code present: `UpdateDailyLogCommand` and `UpdateDailyLogCommandHandler`.
- How it works: resolves the current user, loads the record, checks ownership, parses the date, updates fields, saves changes, and returns a DTO.
- Executed when: `PUT /api/dailylogs/{id}` sends the command.
- Depends on: daily log repository, unit of work, current user service, `CreateDailyLogDto`, `DailyLogDto`, `NotFoundException`, and `UnauthorizedException`.
- Fits into the flow: replaces an owned log with updated content.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Commands/DeleteDailyLogCommand.cs`
- Purpose and code present: delete-daily-log request and handler.
- Code present: `DeleteDailyLogCommand` and `DeleteDailyLogCommandHandler`.
- How it works: validates current-user ownership, removes the record, and commits changes.
- Executed when: `DELETE /api/dailylogs/{id}` sends the command.
- Depends on: daily log repository, unit of work, current user service, `NotFoundException`, and `UnauthorizedException`.
- Fits into the flow: deletes an owned log.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Queries/GetAllDailyLogsQuery.cs`
- Purpose and code present: query for the current user's daily logs.
- Code present: `GetAllDailyLogsQuery` and `GetAllDailyLogsQueryHandler`.
- How it works: resolves the current user id, loads logs scoped to that user, and maps them to DTOs.
- Executed when: `GET /api/dailylogs` is called.
- Depends on: daily log repository, current user service, `DailyLogDto`, and `UnauthorizedException`.
- Fits into the flow: fetches the owned daily log feed.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Queries/GetDailyLogByIdQuery.cs`
- Purpose and code present: query for one daily log.
- Code present: `GetDailyLogByIdQuery` and `GetDailyLogByIdQueryHandler`.
- How it works: resolves the current user id, loads the log, verifies ownership, and returns a DTO.
- Executed when: `GET /api/dailylogs/{id}` is called.
- Depends on: daily log repository, current user service, `DailyLogDto`, `NotFoundException`, and `UnauthorizedException`.
- Fits into the flow: returns one owned log by id.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Queries/SearchDailyLogsQuery.cs`
- Purpose and code present: search query for daily logs.
- Code present: `SearchDailyLogsQuery`, `SearchDailyLogsQueryHandler`, and private `ParseDate()`.
- How it works: converts optional string dates into `DateOnly?`, scopes to the current user, searches repository text/date filters, and returns DTOs.
- Executed when: `POST /api/dailylogs/search` is called.
- Depends on: daily log repository, current user service, `DailyLogDto`, `NotFoundException`, and `UnauthorizedException`.
- Fits into the flow: supports keyword/date filtering over a user's logs.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Dtos/CreateDailyLogDto.cs`
- Purpose and code present: create/update input DTO for daily logs.
- Code present: `CreateDailyLogDto` with `LogDate`, `TasksWorked`, `ProblemsFaced`, `Solutions`, `Learnings`, `Tips`, and `GitLink`.
- How it works: receives the raw client payload before validation and mapping.
- Executed when: daily log create or update requests are model-bound.
- Depends on: nothing else.
- Fits into the flow: transports user input into the application layer.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Dtos/DailyLogDto.cs`
- Purpose and code present: daily log output DTO plus entity mapper.
- Code present: `DailyLogDto` and `FromEntity()`.
- How it works: converts the domain entity into API-friendly string-formatted dates and timestamps.
- Executed when: daily log handlers return results.
- Depends on: `DevLoggerBackend.Domain.Entities.DailyLog`.
- Fits into the flow: serializes daily logs to clients.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Dtos/SearchDailyLogsRequestDto.cs`
- Purpose and code present: search input DTO.
- Code present: `SearchDailyLogsRequestDto` with `Keyword`, `DateFrom`, and `DateTo`.
- How it works: model-bound in the controller and passed into the search query.
- Executed when: the search endpoint receives JSON.
- Depends on: nothing else.
- Fits into the flow: carries search parameters into the API.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Validators/CreateDailyLogCommandValidator.cs`
- Purpose and code present: validation rules for creating logs.
- Code present: `CreateDailyLogCommandValidator`.
- How it works: requires a valid non-future log date, non-empty tasks worked text, and a valid absolute HTTP/HTTPS Git link when present.
- Executed when: `CreateDailyLogCommand` is validated by the MediatR pipeline.
- Depends on: FluentValidation.
- Fits into the flow: blocks malformed daily log creation requests.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Validators/UpdateDailyLogCommandValidator.cs`
- Purpose and code present: validation rules for updating logs.
- Code present: `UpdateDailyLogCommandValidator`.
- How it works: mirrors the create rules and also requires a non-empty id.
- Executed when: `UpdateDailyLogCommand` is validated by the MediatR pipeline.
- Depends on: FluentValidation.
- Fits into the flow: blocks malformed daily log update requests.

#### `src/DevLoggerBackend.Application/Features/Notes/Commands/SaveNoteCommand.cs`
- Purpose and code present: note upsert command and handler.
- Code present: `SaveNoteCommand` and `SaveNoteCommandHandler`.
- How it works: loads the current user's note, creates one if missing, updates it if present, saves changes, and returns a DTO.
- Executed when: `PUT /api/notes` sends the command.
- Depends on: note repository, unit of work, current user service, `Note`, `NoteDto`, and `UnauthorizedException`.
- Fits into the flow: implements the per-user note feature as an upsert.

#### `src/DevLoggerBackend.Application/Features/Notes/Queries/GetNoteQuery.cs`
- Purpose and code present: note retrieval query and handler.
- Code present: `GetNoteQuery` and `GetNoteQueryHandler`.
- How it works: resolves the current user id, loads that user's note, and returns null or a DTO.
- Executed when: `GET /api/notes` sends the query.
- Depends on: note repository, current user service, `NoteDto`, and `UnauthorizedException`.
- Fits into the flow: reads the current user's note without creating one.

#### `src/DevLoggerBackend.Application/Features/Notes/Dtos/NoteDto.cs`
- Purpose and code present: note response DTO and save input record.
- Code present: `NoteDto`, `NoteDto.FromEntity()`, and `SaveNoteDto`.
- How it works: the immutable record returns the note id, content, and update timestamp; `SaveNoteDto` carries the write payload.
- Executed when: note handlers read or write notes.
- Depends on: `DevLoggerBackend.Domain.Entities.Note`.
- Fits into the flow: serializes note data to and from the API.

#### `src/DevLoggerBackend.Application/Features/Notes/Validators/SaveNoteCommandValidator.cs`
- Purpose and code present: validation rules for note saving.
- Code present: `SaveNoteCommandValidator`.
- How it works: requires non-null content and limits length to 100000 characters.
- Executed when: `SaveNoteCommand` is validated by the MediatR pipeline.
- Depends on: FluentValidation.
- Fits into the flow: guards the note write operation.

#### `src/DevLoggerBackend.Application/DevLoggerBackend.Application.csproj`
- Purpose and code present: application project manifest.
- Code present: `net10.0` target, nullable and implicit usings, and package/project references.
- How it works: declares the application layer's compile-time dependencies.
- Executed when: the SDK restores and builds the project.
- Depends on: Domain, MediatR, FluentValidation, and dependency injection abstractions.
- Fits into the flow: defines the use-case layer boundary.

### Domain Project

#### `src/DevLoggerBackend.Domain/Common/BaseEntity.cs`
- Purpose and code present: shared base entity for audited models.
- Code present: `BaseEntity` with `Id`, `CreatedAtUtc`, and `UpdatedAtUtc`.
- How it works: inherited by all persisted domain entities.
- Executed when: entities are created, tracked, and saved by EF Core.
- Depends on: nothing else.
- Fits into the flow: provides shared identity and timestamp fields.

#### `src/DevLoggerBackend.Domain/Entities/User.cs`
- Purpose and code present: user aggregate root.
- Code present: `User` with `Name`, `Email`, `PasswordHash`, `Role`, `DailyLogs`, and `Note`.
- How it works: represents an authenticated account and its owned content.
- Executed when: EF Core materializes users or handlers create new ones.
- Depends on: `BaseEntity`, `UserRole`, `DailyLog`, and `Note`.
- Fits into the flow: anchors ownership for logs and notes.

#### `src/DevLoggerBackend.Domain/Entities/DailyLog.cs`
- Purpose and code present: daily journal entry entity.
- Code present: `DailyLog` with `LogDate`, work text fields, optional `GitLink`, `UserId`, and `User`.
- How it works: stores a single day's work summary and ownership link.
- Executed when: EF Core materializes logs or handlers create/update them.
- Depends on: `BaseEntity` and `User`.
- Fits into the flow: stores the main journaling content.

#### `src/DevLoggerBackend.Domain/Entities/Note.cs`
- Purpose and code present: user note entity.
- Code present: `Note` with `Content`, `UserId`, and `User`.
- How it works: stores one note per user.
- Executed when: EF Core materializes notes or handlers create/update them.
- Depends on: `BaseEntity` and `User`.
- Fits into the flow: stores the personal note content.

#### `src/DevLoggerBackend.Domain/Enums/UserRole.cs`
- Purpose and code present: user role enumeration.
- Code present: `Developer`, `SeniorDeveloper`, `TeamLead`, and `Manager`.
- How it works: stored as an integer in the database and mapped to role strings in auth responses.
- Executed when: users are created, authenticated, or seeded.
- Depends on: nothing else.
- Fits into the flow: supports basic role labeling and future authorization expansion.

#### `src/DevLoggerBackend.Domain/DevLoggerBackend.Domain.csproj`
- Purpose and code present: domain project manifest.
- Code present: `net10.0` target with nullable and implicit usings.
- How it works: keeps the domain layer free of infrastructure dependencies.
- Executed when: the SDK restores and builds the project.
- Depends on: nothing else.
- Fits into the flow: preserves the pure model layer boundary.

### Infrastructure Project

#### `src/DevLoggerBackend.Infrastructure/DependencyInjection.cs`
- Purpose and code present: infrastructure service registration and connection-string resolution.
- Code present: `AddInfrastructure()`, `ResolveConnectionString()`, and `BuildConnectionStringFromDatabaseUrl()`.
- How it works: reads `DATABASE_URL` first, then `ConnectionStrings:DefaultConnection`, configures PostgreSQL EF Core, registers repositories and services, and installs `HttpContextAccessor`.
- Executed when: `Program.cs` calls `AddInfrastructure()`.
- Depends on: Application abstractions, `AppDbContext`, repository implementations, service implementations, EF Core, Npgsql, configuration, and HTTP context access.
- Fits into the flow: binds the abstract application contracts to concrete implementations.

#### `src/DevLoggerBackend.Infrastructure/Persistence/AppDbContext.cs`
- Purpose and code present: EF Core DbContext and unit-of-work implementation.
- Code present: `DbSet<User>`, `DbSet<DailyLog>`, `DbSet<Note>`, `SaveChangesAsync()`, `OnModelCreating()`, and `SeedUsers()`.
- How it works: stamps timestamps on `BaseEntity` entries, applies entity configurations, and seeds three fixed users for local testing.
- Executed when: repositories query or save data and when migrations build the model.
- Depends on: `IUnitOfWork`, domain entities, EF Core, and the configuration classes in the same assembly.
- Fits into the flow: is the bridge from application persistence abstractions to PostgreSQL.

#### `src/DevLoggerBackend.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- Purpose and code present: EF Core configuration for `User`.
- Code present: `UserConfiguration`.
- How it works: maps to the `Users` table, sets key and field lengths, requires data, and creates a unique email index.
- Executed when: EF Core builds the model.
- Depends on: `User`.
- Fits into the flow: shapes the user table schema.

#### `src/DevLoggerBackend.Infrastructure/Persistence/Configurations/DailyLogConfiguration.cs`
- Purpose and code present: EF Core configuration for `DailyLog`.
- Code present: `DailyLogConfiguration`.
- How it works: maps to `DailyLogs`, sets required fields and Git link length, configures the cascade foreign key to `User`, and indexes `LogDate`.
- Executed when: EF Core builds the model.
- Depends on: `DailyLog`.
- Fits into the flow: shapes the daily log table and relationship.

#### `src/DevLoggerBackend.Infrastructure/Persistence/Configurations/NoteConfiguration.cs`
- Purpose and code present: EF Core configuration for `Note`.
- Code present: `NoteConfiguration`.
- How it works: maps to `Notes`, constrains content length, enforces one note per user with a unique index, and configures the one-to-one relationship with `User`.
- Executed when: EF Core builds the model.
- Depends on: `Note`.
- Fits into the flow: shapes the per-user note table and constraint.

#### `src/DevLoggerBackend.Infrastructure/Repositories/UserRepository.cs`
- Purpose and code present: EF Core implementation of the user repository.
- Code present: `UserRepository` with `AddAsync()`, `GetByEmailAsync()`, `GetByIdAsync()`, and `GetDefaultUserAsync()`.
- How it works: adds users, performs normalized email lookup, and loads by id or default order.
- Executed when: auth handlers or future code call user repository methods.
- Depends on: `AppDbContext`, `User`, and EF Core query APIs.
- Fits into the flow: persists and retrieves account data.

#### `src/DevLoggerBackend.Infrastructure/Repositories/DailyLogRepository.cs`
- Purpose and code present: EF Core implementation of the daily log repository.
- Code present: `DailyLogRepository` with `GetAllAsync()`, `GetByUserIdAsync()`, `GetByIdAsync()`, `SearchByUserIdAsync()`, `AddAsync()`, `Update()`, and `Remove()`.
- How it works: returns logs ordered by date descending, filters by user and search terms, and performs add/update/delete operations.
- Executed when: daily log handlers call repository methods.
- Depends on: `AppDbContext`, `DailyLog`, and EF Core query APIs.
- Fits into the flow: implements all daily log persistence behavior.

#### `src/DevLoggerBackend.Infrastructure/Repositories/NoteRepository.cs`
- Purpose and code present: EF Core implementation of the note repository.
- Code present: `NoteRepository` with `GetByUserIdAsync()` and `AddAsync()`.
- How it works: loads the single note per user and inserts new notes.
- Executed when: note handlers call repository methods.
- Depends on: `AppDbContext`, `Note`, and EF Core query APIs.
- Fits into the flow: implements note persistence.

#### `src/DevLoggerBackend.Infrastructure/Services/BcryptPasswordHasher.cs`
- Purpose and code present: BCrypt password hashing adapter.
- Code present: `BcryptPasswordHasher`.
- How it works: delegates hash and verify operations to `BCrypt.Net.BCrypt`.
- Executed when: registration hashes passwords and login verifies them.
- Depends on: `IPasswordHasher` and BCrypt.Net-Next.
- Fits into the flow: protects user credentials.

#### `src/DevLoggerBackend.Infrastructure/Services/PlaceholderTokenService.cs`
- Purpose and code present: JWT token generator.
- Code present: `PlaceholderTokenService`.
- How it works: reads JWT settings, creates `NameIdentifier`, `Email`, and `Role` claims, signs with HMAC SHA256, and writes a token string.
- Executed when: login succeeds and a token is needed.
- Depends on: `ITokenService`, `User`, configuration, `System.IdentityModel.Tokens.Jwt`, claims APIs, and symmetric signing keys.
- Fits into the flow: creates the bearer token consumed by authenticated API requests.

#### `src/DevLoggerBackend.Infrastructure/Services/CurrentUserService.cs`
- Purpose and code present: current-user resolver for request handlers.
- Code present: `CurrentUserService`.
- How it works: reads `ClaimTypes.NameIdentifier` from the current HTTP context and parses it as a GUID.
- Executed when: handlers need the authenticated user id.
- Depends on: `ICurrentUserService`, `IHttpContextAccessor`, and claims APIs.
- Fits into the flow: lets application handlers scope work to the signed-in user.

#### `src/DevLoggerBackend.Infrastructure/Migrations/20260725125919_InitialCreate.cs`
- Purpose and code present: EF Core migration that creates the initial schema.
- Code present: `InitialCreate.Up()` and `InitialCreate.Down()`.
- How it works: creates `Users`, `DailyLogs`, and `Notes`, builds indexes, seeds three users, and reverses those changes in `Down()`.
- Executed when: EF Core applies migrations at startup or via `dotnet ef database update`.
- Depends on: the model built from `AppDbContext`.
- Fits into the flow: defines the database schema and seed baseline.

#### `src/DevLoggerBackend.Infrastructure/Migrations/20260725125919_InitialCreate.Designer.cs`
- Purpose and code present: auto-generated migration designer.
- Code present: EF Core model-building metadata for `InitialCreate`.
- How it works: records the snapshot of the model as EF saw it during migration generation.
- Executed when: EF tooling loads migration metadata.
- Depends on: `AppDbContext` and EF Core design-time services.
- Fits into the flow: supports migration execution and tooling.

#### `src/DevLoggerBackend.Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
- Purpose and code present: current EF Core model snapshot.
- Code present: snapshot metadata for all entities, keys, relationships, indexes, and seed data.
- How it works: lets EF Core detect schema drift and generate future migrations correctly.
- Executed when: EF tooling inspects the current model.
- Depends on: `AppDbContext` and EF Core design-time infrastructure.
- Fits into the flow: anchors future database evolution.

#### `src/DevLoggerBackend.Infrastructure/DevLoggerBackend.Infrastructure.csproj`
- Purpose and code present: infrastructure project manifest.
- Code present: `net10.0` target, framework reference to ASP.NET Core, and package/project references.
- How it works: defines the infrastructure layer's compile-time dependencies.
- Executed when: the SDK restores and builds the project.
- Depends on: Application, Domain, EF Core, Npgsql, BCrypt.Net-Next, configuration abstractions, dependency injection abstractions, and JWT packages.
- Fits into the flow: declares the concrete infrastructure boundary.

### Test Project

#### `tests/DevLoggerBackend.Application.Tests/DevLoggerBackend.Application.Tests.csproj`
- Purpose and code present: application-layer test project manifest.
- Code present: `net10.0` target, test package references, and project references to Application and Domain.
- How it works: configures the xUnit test project and coverage collection.
- Executed when: `dotnet test` runs.
- Depends on: xUnit, Moq, FluentAssertions, Microsoft.NET.Test.Sdk, and Coverlet.
- Fits into the flow: verifies application behavior in isolation.

#### `tests/DevLoggerBackend.Application.Tests/Features/DailyLogs/Commands/CreateDailyLogCommandHandlerTests.cs`
- Purpose and code present: unit test for daily log creation.
- Code present: `CreateDailyLogCommandHandlerTests.Handle_ShouldCreateDailyLog_WhenPayloadIsValid()`.
- How it works: uses mocks for repository, unit of work, and current-user service, then verifies the handler adds a log and saves changes.
- Executed when: xUnit runs the test suite.
- Depends on: application command/DTO types, domain entities, Moq, and FluentAssertions.
- Fits into the flow: proves the create handler's core behavior.

#### `tests/DevLoggerBackend.Application.Tests/Features/DailyLogs/Queries/GetAllDailyLogsQueryHandlerTests.cs`
- Purpose and code present: unit test for daily log retrieval.
- Code present: `GetAllDailyLogsQueryHandlerTests.Handle_ShouldReturnLogsSortedFromRepositoryResult()`.
- How it works: mocks the repository and current-user service, returns a sample log, and verifies the handler maps it into the response.
- Executed when: xUnit runs the test suite.
- Depends on: application query/DTO types, domain entities, Moq, and FluentAssertions.
- Fits into the flow: proves the query handler's mapping and user-scoped retrieval.

## 4. Code Execution Flow

### Application Startup
`Program.cs` is the entry point. It configures host URLs, Serilog, DI, authentication, Swagger, CORS, and metrics. `UseApiPipeline()` then wires middleware and MVC routing. If enabled, `ApplyDatabaseMigrationsAsync()` runs before the app begins listening.

### Configuration Loading
Configuration comes from `appsettings.json`, environment-specific appsettings, launch settings during local development, and environment variables such as `PORT`, `DATABASE_URL`, `ConnectionStrings__DefaultConnection`, and `AllowedOrigins__0`.

### Dependency Injection
`AddApplication()` registers MediatR and validators. `AddInfrastructure()` wires EF Core, repositories, the password hasher, token service, and current user service. Controllers resolve `IMediator`, and some actions resolve repositories or services directly from DI.

### Routing
Controllers are attribute-routed under `api/[controller]`. `AuthController`, `DailyLogsController`, and `NotesController` expose the current feature set. `MapControllers()` is called inside `UseApiPipeline()`.

### Middleware Execution
The request pipeline begins with `GlobalExceptionHandlingMiddleware`, then CORS, authentication, authorization, and controller execution. Prometheus middleware records HTTP metrics.

### Authentication And Authorization
Registration stores a hashed password and default role. Login verifies the hash and issues a JWT. JWT validation checks issuer, audience, lifetime, and signature. The authenticated user id comes from `ClaimTypes.NameIdentifier` through `CurrentUserService`.

### Validation
FluentValidation validators are attached to commands through MediatR pipeline behavior. Validation failures are transformed into a grouped field-error payload by the exception middleware.

### Business Logic
Handlers implement use cases:
- register and login for auth
- create, update, delete, list, and search for daily logs
- get and upsert for notes

### Service Layer
The service layer consists of `IPasswordHasher`, `ITokenService`, and `ICurrentUserService`. Infrastructure provides the actual BCrypt, JWT, and HTTP-context-backed implementations.

### Repository Layer
Repositories encapsulate EF Core queries and mutations. They are called by handlers and never by controllers directly, except for the custom auth verify action.

### Database Interactions
`AppDbContext` manages entity sets and timestamp stamping. EF configurations define table names, indexes, and relationships. Migrations create the PostgreSQL schema and seed users.

### Logging
Serilog writes to console and `logs/devlogger-.log`. The exception middleware logs unhandled exceptions, and startup migration failures are logged with a dedicated logger.

### Exception Handling
Known application exceptions are normalized to 400, 401, 404, or 409 responses. Unknown exceptions become 500 responses. Validation errors are grouped by field.

### Background Jobs, Schedulers, And Event Processing
There are no background jobs or schedulers in the codebase. The operational side is limited to metrics exposure and optional automatic migrations.

## 5. Database Documentation

### Database Models
- `Users` stores identity, password hash, role, and audit timestamps.
- `DailyLogs` stores the user's day-by-day journal entries and an optional Git link.
- `Notes` stores one personal note per user.

### Relationships
- One `User` has many `DailyLog` rows.
- One `User` has at most one `Note`.
- `DailyLog.UserId` and `Note.UserId` are foreign keys to `Users.Id`.

### Constraints And Indexes
- `Users.Email` is unique.
- `DailyLogs.LogDate` is indexed.
- `DailyLogs.UserId` is indexed.
- `Notes.UserId` is unique.
- `DailyLogs.GitLink` has a max length of 2048.
- `Notes.Content` has a max length of 100000.

### Migration And Initialization
The current schema comes from `src/DevLoggerBackend.Infrastructure/Migrations/20260725125919_InitialCreate.cs` and its designer and snapshot files. The migration creates all tables in one step and seeds three users with deterministic timestamps and BCrypt hashes.

### CRUD Operations
- Users are created by registration and read during login and verification.
- Daily logs are created, listed, updated, searched, and deleted by the authenticated user.
- Notes are read and upserted per user.

### Query Execution Flow
Handlers call repository methods. Repositories translate those calls into EF Core queries. EF Core generates SQL against PostgreSQL. Read queries often use `AsNoTracking()` for efficiency.

## 6. Dependencies

### Internal Project Dependencies
- `DevLoggerBackend.Api` depends on Application and Infrastructure.
- `DevLoggerBackend.Application` depends on Domain.
- `DevLoggerBackend.Infrastructure` depends on Application and Domain.
- `tests/DevLoggerBackend.Application.Tests` depends on Application and Domain.

### External Libraries And Packages
- `Microsoft.AspNetCore.Authentication.JwtBearer` for bearer-token auth.
- `MediatR` for CQRS dispatching.
- `FluentValidation` and `FluentValidation.DependencyInjectionExtensions` for request validation.
- `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, and `Microsoft.EntityFrameworkCore.Design` for database access and migrations.
- `BCrypt.Net-Next` for password hashing.
- `System.IdentityModel.Tokens.Jwt` for JWT creation and validation.
- `Serilog.AspNetCore`, `Serilog.Sinks.Console`, and `Serilog.Sinks.File` for logging.
- `prometheus-net.AspNetCore` for metrics.
- `Swashbuckle.AspNetCore` for OpenAPI and Swagger UI.
- `xunit`, `Moq`, `FluentAssertions`, `coverlet.collector`, and `Microsoft.NET.Test.Sdk` for testing.

### Purpose Of Each Important Dependency
- MediatR keeps controllers thin and centralizes use cases.
- FluentValidation keeps input rules separate from controllers and handlers.
- EF Core and Npgsql provide PostgreSQL persistence.
- BCrypt protects passwords.
- JWT packages provide authenticated session tokens.
- Serilog improves observability.
- Swagger documents the API and supports manual testing.
- Prometheus exposes runtime metrics.

## 7. Configuration

### Configuration Files
- `src/DevLoggerBackend.Api/appsettings.json`
- `src/DevLoggerBackend.Api/appsettings.Development.json`
- `src/DevLoggerBackend.Api/Properties/launchSettings.json`
- `docker-compose.yml`
- `monitoring/prometheus.yml`

### Environment Variables
- `PORT` controls the external bind port, especially on Render.
- `DATABASE_URL` overrides the database connection string if present.
- `ConnectionStrings__DefaultConnection` can also define the PostgreSQL connection.
- `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, and `Jwt__ExpiryMinutes` configure token generation and validation.
- `AllowedOrigins__0` and related array keys configure CORS origins.
- `ASPNETCORE_ENVIRONMENT` selects Development versus Production behavior.

### Runtime Configuration
`Program.cs` reads the JWT, CORS, swagger, and migration settings. Infrastructure reads the database connection string or `DATABASE_URL`. `PlaceholderTokenService` reads the JWT settings when issuing tokens.

### Build Configuration
The projects target `net10.0`. The API project enables XML documentation generation. The test project is not packable and includes coverage tooling.

### Deployment Configuration
The Dockerfile publishes the API into a runtime image. Docker Compose starts the API and PostgreSQL locally. The README also describes Render deployment with environment variables and a startup command.

### Secrets Management
The checked-in `appsettings.json` contains a JWT key and database credentials, so the repository should be treated as sensitive. Production should override those values with environment variables or secret storage.

## 8. Feature Documentation

### Authentication
- Files responsible: `AuthController.cs`, `RegisterCommand.cs`, `LoginCommand.cs`, `VerifyAuthQuery.cs`, auth DTOs, auth validators, `UserRepository.cs`, `BcryptPasswordHasher.cs`, `PlaceholderTokenService.cs`, `CurrentUserService.cs`, and JWT config in `Program.cs`.
- Execution flow: register -> validate -> check duplicate -> hash password -> persist user; login -> validate -> lookup user -> verify password -> issue JWT -> return user and token; verify -> read current user from token -> load user -> return profile.
- APIs involved: `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/verify`.
- Database interactions: users are inserted and read from the `Users` table.

### Daily Logs
- Files responsible: daily log controllers, commands, queries, validators, DTOs, repository, entity, configurations, migration, and current-user service.
- Execution flow: requests are validated, ownership is enforced through the current user id, repository methods query or mutate `DailyLogs`, and DTOs are returned to the client.
- APIs involved: `GET /api/dailylogs`, `GET /api/dailylogs/{id}`, `POST /api/dailylogs`, `PUT /api/dailylogs/{id}`, `DELETE /api/dailylogs/{id}`, and `POST /api/dailylogs/search`.
- Database interactions: reads and writes to `DailyLogs`, plus foreign key linkage to `Users`.

### Notes
- Files responsible: notes controller, save/get handlers, note DTOs, note validator, repository, entity, configuration, and migration.
- Execution flow: GET returns the current user's note or 204; PUT upserts the note and saves it.
- APIs involved: `GET /api/notes` and `PUT /api/notes`.
- Database interactions: reads and writes the `Notes` table, enforcing one note per user.

### Observability And Ops
- Files responsible: `Program.cs`, `monitoring/prometheus.yml`, `DOCKER-GRAFANA-SUMMARY.md`, `Dockerfile`, and `docker-compose.yml`.
- Execution flow: metrics middleware exposes HTTP metrics, Prometheus scrapes them, and Grafana dashboards can visualize them.
- APIs involved: `/metrics` and `/swagger` for diagnostics.
- Database interactions: none directly, though DB connection health is visible through the app's operational behavior.

## 9. Architecture And Design Patterns

### Overall Architecture
The codebase uses Clean Architecture with a CQRS-style application layer. The API layer is thin, the application layer contains business use cases, the domain layer holds the model, and the infrastructure layer owns all external integrations.

### Folder Organization
- `Api` for transport, startup, and middleware.
- `Application` for use cases, abstractions, DTOs, and validation.
- `Domain` for entities and enums.
- `Infrastructure` for EF Core, repositories, service adapters, and migrations.
- `tests` for application-layer unit tests.

### Design Patterns Used
- Repository pattern for persistence abstraction.
- Unit of Work through `IUnitOfWork` and `AppDbContext`.
- CQRS through separate commands and queries.
- MediatR pipeline behavior for validation.
- Middleware for global exception handling.
- DTO mapping for API contracts.
- Dependency injection throughout.

### Coding Standards And Practices
- Async EF Core calls are used throughout.
- Read queries often use `AsNoTracking()`.
- Ownership checks are done in handlers, not controllers.
- Validation is centralized.
- Entities inherit shared audit fields from `BaseEntity`.

## 10. Developer Notes

- Seed users are available for local login testing.
- `VerifyAuthQuery` is currently a stub and not used by the controller.
- `GetDefaultUserAsync()` and `IDailyLogRepository.GetAllAsync()` exist but are currently unused by request handlers.
- `CreateDailyLogCommandHandler` injects `IUserRepository` but does not use it.
- Health checks are registered but no `/health` endpoint is mapped.
- `SaveNoteCommandValidator` allows empty note content because it only enforces non-null and max length.
- Search dates are leniently parsed; invalid date strings are treated as absent filters rather than errors.
- `CurrentUserService` assumes the JWT claim value is a valid GUID.
- `AppDbContext.SaveChangesAsync()` stamps timestamps even when handlers already set them, which is redundant but harmless.

## 11. Additional Observations

### Performance Considerations
- `ToLower().Contains()` search logic can be expensive on large tables and may limit index usage.
- `UserRepository.GetByEmailAsync()` also uses lowercasing in the query, which is simple but not necessarily optimal for large datasets.
- `GetAllDailyLogsQuery` loads the full result set for the current user without paging.

### Security Considerations
- The source-controlled JWT key and database credentials should not ship to production as-is.
- Swagger is enabled in production by default when the flag is true, which may or may not be desired in hardened deployments.
- JWT verification depends on a symmetric key and properly configured issuer/audience values.
- Auth verification currently does not use the placeholder query, so there is one less abstraction in the path than the codebase suggests.

### Potential Improvements
- Replace `VerifyAuthQuery` with a real profile lookup or remove it if not needed.
- Add paging to daily log list and search operations.
- Add integration tests for controllers, middleware, repository behavior, and migrations.
- Add a health endpoint now that health checks are registered.
- Consider removing unused dependencies and unused repository methods if they remain unnecessary.
- Consider case-insensitive search and email handling that preserve index friendliness.

### Hidden Implementation Details
- The seeded BCrypt hashes correspond to known demo passwords and are deterministic to keep EF Core model snapshots stable.
- `CurrentUserService` is claim-based, so any custom token changes must still include `ClaimTypes.NameIdentifier`.
- The note feature is intentionally an upsert, not a multi-note collection.
- `Program.cs` will try to open the Swagger UI automatically in development on supported platforms.
