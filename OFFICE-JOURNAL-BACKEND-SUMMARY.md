# OFFICE JOURNAL BACKEND SUMMARY

## 1. Project Overview

### Purpose
This repository contains the backend API for the Developer Journal / Office Journal application. Its job is to let authenticated users register, log in, verify their current identity, create and manage daily development logs, and keep one personal note. The backend is implemented as a .NET ASP.NET Core API using a Clean Architecture layout and a CQRS-style application layer.

### Actual Technology Snapshot
- All project files currently target `net10.0`.
- The API uses ASP.NET Core controllers, JWT bearer authentication, Serilog, Swagger/OpenAPI, and CORS.
- The application layer uses MediatR and FluentValidation.
- Persistence is built with Entity Framework Core and the PostgreSQL provider from Npgsql.
- Password hashing uses BCrypt.Net-Next.
- The test project uses xUnit, Moq, FluentAssertions, and Coverlet.

### What The System Stores
The backend persists three business entities:
- users
- daily logs
- notes

The daily log model captures day-by-day work summaries, blockers, solutions, learnings, tips, and an optional Git link. The note model stores one free-form note per user. The auth flow stores user credentials and a role.

### Architecture
The solution is split into four major projects:
- `src/DevLoggerBackend.Api` for HTTP entry points, middleware, configuration, startup, and controllers
- `src/DevLoggerBackend.Application` for use cases, command/query handlers, DTOs, validation, abstractions, and shared application behavior
- `src/DevLoggerBackend.Domain` for entities, enums, and shared base types
- `src/DevLoggerBackend.Infrastructure` for EF Core, repositories, service implementations, and dependency wiring

The main runtime flow is:
1. An HTTP request reaches the API.
2. Middleware handles cross-cutting concerns and error formatting.
3. Controllers delegate to MediatR.
4. Validators run through the MediatR pipeline.
5. Handlers execute business rules.
6. Handlers call repository and service abstractions.
7. Infrastructure implements those abstractions against EF Core and PostgreSQL.
8. DTOs are returned to the client as JSON.

### Important Behavior Worth Knowing
- The auth `verify` endpoint is implemented directly in `AuthController`; the `VerifyAuthQuery` type exists but is currently a placeholder and is not used by the controller.
- Daily log handlers are scoped to the authenticated user through `ICurrentUserService`.
- The note feature is a one-note-per-user upsert flow.
- Startup can apply database migrations automatically, and it contains special handling for PostgreSQL authentication failures during development startup.

---

## 2. Complete Application Execution Flow

### Startup Sequence
When the app starts in `src/DevLoggerBackend.Api/Program.cs`:
1. The web host is created.
2. If the `PORT` environment variable exists, Kestrel binds to `http://0.0.0.0:{PORT}`.
3. Otherwise the API binds to `http://localhost:5000`.
4. Serilog is configured from configuration plus explicit console and rolling file sinks.
5. Application services are registered.
6. Infrastructure services are registered.
7. Controllers are added.
8. JWT bearer authentication is configured from the `Jwt` section in configuration.
9. Swagger is configured with a bearer security definition and security requirement.
10. Health checks are registered in DI.
11. CORS is configured using the allowed frontend origins.
12. The app is built.
13. Swagger UI is enabled in development or when `EnableSwaggerInProduction` is true.
14. In development, the app attempts to open the Swagger URL automatically in the browser after startup.
15. If `ApplyMigrationsOnStartup` is enabled, migrations are applied before the pipeline runs.
16. The API pipeline is wired up and the app starts listening.

### Migration Startup Behavior
Database migration startup logic is in `WebApplicationExtensions.ApplyDatabaseMigrationsAsync`.
- The method creates a scope, resolves `AppDbContext`, and calls `Database.MigrateAsync()`.
- If PostgreSQL returns authentication error `28P01`, the app logs the failure.
- In development, that authentication failure is swallowed after logging so the app can keep starting.
- In non-development environments, the same failure is rethrown.

### Request Lifecycle
For a typical request:
1. The global exception middleware runs first.
2. CORS is applied.
3. Authentication runs.
4. Authorization runs.
5. The controller action is selected.
6. The controller sends a command or query to MediatR.
7. ValidationBehavior runs all registered validators for that request type.
8. The handler executes the use case.
9. Repositories perform EF Core reads or writes.
10. `AppDbContext.SaveChangesAsync` stamps timestamps on all tracked `BaseEntity` entries.
11. JSON is returned to the client.

### Error Handling Flow
Unhandled exceptions are converted into a consistent JSON response by `GlobalExceptionHandlingMiddleware`.
- `ValidationException` becomes `400 Bad Request` with a grouped error dictionary.
- `NotFoundException` becomes `404 Not Found`.
- `UnauthorizedException` becomes `401 Unauthorized`.
- `ConflictException` becomes `409 Conflict`.
- Any other exception becomes `500 Internal Server Error`.

The middleware also logs the exception through `ILogger` and writes a full diagnostic block to the console.

---

## 3. Complete Folder and File Documentation

### Root Level Files

#### `README.md`
- Project overview and quick-start instructions.
- Local run commands for restore, build, test, and run.
- Docker Compose instructions.
- EF Core migration commands.
- Next.js frontend environment variable setup.
- Render deployment notes.

#### `DevLoggerBackend.sln`
- Solution file that groups the API, Application, Domain, Infrastructure, and test projects.
- Used by the .NET SDK and Visual Studio for build and test orchestration.

#### `docker-compose.yml`
- Defines a local PostgreSQL 16 container and the API container.
- Uses a persistent volume for Postgres data.
- Exposes API port `5000` and database port `5432`.
- Sets `ASPNETCORE_ENVIRONMENT=Development` for the API container.
- Overrides the API connection string for container networking.
- Sets the frontend origin entry for local browser use.

---

### API Project

#### `src/DevLoggerBackend.Api/Program.cs`
- Entry point for the API.
- Selects the listening URL from `PORT` or falls back to `http://localhost:5000`.
- Configures Serilog console and rolling file sinks at `logs/devlogger-.log`.
- Registers application and infrastructure services.
- Configures JWT bearer auth and Swagger security definitions.
- Registers health checks and CORS.
- Optionally opens Swagger in the browser during development.
- Optionally applies migrations before the request pipeline starts.

#### `src/DevLoggerBackend.Api/Extensions/WebApplicationExtensions.cs`
- Provides `UseApiPipeline()` and `ApplyDatabaseMigrationsAsync()`.
- `UseApiPipeline()` wires the global exception middleware, CORS, authentication, authorization, and controller mapping.
- `ApplyDatabaseMigrationsAsync()` handles startup migration execution and the special PostgreSQL auth failure path.

#### `src/DevLoggerBackend.Api/Middleware/GlobalExceptionHandlingMiddleware.cs`
- Wraps the request pipeline in a try/catch.
- Logs unhandled exceptions.
- Emits a console debug block with the exception text and inner exception text.
- Converts known application exceptions into structured JSON error responses.

#### `src/DevLoggerBackend.Api/Models/ErrorResponse.cs`
- Shared error payload returned by the middleware.
- Contains `StatusCode`, `Message`, `TraceId`, and optional `Errors`.
- `Errors` is a dictionary keyed by field name with an array of messages.

#### `src/DevLoggerBackend.Api/Controllers/AuthController.cs`
- Routes under `api/auth`.
- `POST /api/auth/register` creates a new account.
- `POST /api/auth/login` authenticates a user and returns a token plus user profile data.
- `GET /api/auth/verify` is protected by `[Authorize]` and returns the current authenticated user.
- The verify action resolves `IUserRepository` and `ICurrentUserService` directly from DI instead of using `VerifyAuthQuery`.
- Registration returns `201 Created` with a simple success message.

#### `src/DevLoggerBackend.Api/Controllers/DailyLogsController.cs`
- Routes under `api/dailylogs`.
- Protected by `[Authorize]` at the controller level.
- `GET /api/dailylogs` returns the current user's logs ordered by log date descending.
- `GET /api/dailylogs/{id}` returns one log for the current user.
- `POST /api/dailylogs` creates a new log and returns `201 Created` with `CreatedAtAction`.
- `PUT /api/dailylogs/{id}` updates an existing log owned by the current user.
- `DELETE /api/dailylogs/{id}` deletes a log owned by the current user.
- `POST /api/dailylogs/search` performs keyword and optional date-range search.

#### `src/DevLoggerBackend.Api/Controllers/NotesController.cs`
- Routes under `api/notes`.
- Protected by `[Authorize]`.
- `GET /api/notes` returns the current user's note or `204 No Content` when no note exists.
- `PUT /api/notes` creates the note when missing or updates it when present.

#### `src/DevLoggerBackend.Api/appsettings.json`
- Main runtime configuration.
- Contains the default PostgreSQL connection string.
- Contains JWT issuer, audience, key, and expiry minutes.
- Contains CORS allowed origins.
- Enables Swagger in production.
- Enables automatic migrations on startup.
- Contains Serilog logging rules.
- The checked-in values include local development secrets, so this file is source-sensitive.

#### `src/DevLoggerBackend.Api/appsettings.Development.json`
- Development-only Serilog override.
- Lowers the default log level to `Debug`.

#### `src/DevLoggerBackend.Api/Properties/launchSettings.json`
- Launch profile for local development.
- Uses `http://localhost:5000`.
- Opens the browser to `/swagger`.
- Sets `ASPNETCORE_ENVIRONMENT=Development`.

#### `src/DevLoggerBackend.Api/Dockerfile`
- Multi-stage Docker build for the API.
- Restores, publishes, and runs the app in a .NET container.
- Designed to work with Docker Compose and deployment platforms.

#### `src/DevLoggerBackend.Api/DevLoggerBackend.Api.csproj`
- Targets `net10.0`.
- Enables nullable reference types and implicit usings.
- Generates XML documentation files.
- References JWT bearer auth, EF Core design, Serilog sinks, Swagger, and JWT token packages.
- References the Application and Infrastructure projects.

---

### Application Project

#### `src/DevLoggerBackend.Application/DependencyInjection.cs`
- Registers MediatR handlers from the application assembly.
- Registers all FluentValidation validators from the same assembly.
- Adds `ValidationBehavior<,>` as the MediatR pipeline behavior.

#### `src/DevLoggerBackend.Application/Abstractions/Persistence/IUnitOfWork.cs`
- Defines `SaveChangesAsync`.
- Implemented by `AppDbContext`.

#### `src/DevLoggerBackend.Application/Abstractions/Repositories/IDailyLogRepository.cs`
- Declares read, search, add, update, and remove operations for daily logs.
- Includes `GetAllAsync`, `GetByUserIdAsync`, `GetByIdAsync`, and `SearchByUserIdAsync`.

#### `src/DevLoggerBackend.Application/Abstractions/Repositories/INoteRepository.cs`
- Declares note lookup by user and insert.
- The note feature currently relies on a one-note-per-user model.

#### `src/DevLoggerBackend.Application/Abstractions/Repositories/IUserRepository.cs`
- Declares add, lookup by email, lookup by id, and `GetDefaultUserAsync`.
- `GetDefaultUserAsync` exists but is not currently used by the request handlers.

#### `src/DevLoggerBackend.Application/Abstractions/Services/ICurrentUserService.cs`
- Exposes the current authenticated user id as `Guid?`.

#### `src/DevLoggerBackend.Application/Abstractions/Services/IPasswordHasher.cs`
- Abstracts hashing and verification.

#### `src/DevLoggerBackend.Application/Abstractions/Services/ITokenService.cs`
- Abstracts token generation for auth.

#### `src/DevLoggerBackend.Application/Common/Behaviors/ValidationBehavior.cs`
- Runs every validator registered for the incoming request type.
- Uses a shared validation context for all validators of the same request.
- Aggregates all failures and throws `ValidationException` when any exist.

#### `src/DevLoggerBackend.Application/Common/Exceptions/ConflictException.cs`
- Used when a request conflicts with current state, such as duplicate registration.

#### `src/DevLoggerBackend.Application/Common/Exceptions/NotFoundException.cs`
- Used when a requested resource is missing or inaccessible.

#### `src/DevLoggerBackend.Application/Common/Exceptions/UnauthorizedException.cs`
- Used when authentication is missing, invalid, or no longer resolvable.

#### `src/DevLoggerBackend.Application/Common/Models/PagedResult.cs`
- Generic paging container with `Items`, `TotalCount`, `PageNumber`, and `PageSize`.
- Present in the codebase even though current handlers do not yet return paged results.

#### `src/DevLoggerBackend.Application/Features/Auth/Commands/RegisterCommand.cs`
- `RegisterCommand` carries name, email, password, and confirm password.
- Handler checks for an existing email and throws `ConflictException` on duplicates.
- The new user is trimmed, email-normalized to lowercase, assigned the `Developer` role, and persisted.
- Password hashing is delegated to `IPasswordHasher`.
- The handler saves through `IUnitOfWork`.

#### `src/DevLoggerBackend.Application/Features/Auth/Commands/LoginCommand.cs`
- `LoginCommand` carries email and password.
- Handler loads the user by email and verifies the password.
- Invalid credentials throw `UnauthorizedException`.
- The response contains a `UserDto` plus a JWT token.
- Role display formatting is special-cased so `SeniorDeveloper` becomes `Senior Developer` and `TeamLead` becomes `Team Lead`.

#### `src/DevLoggerBackend.Application/Features/Auth/Queries/VerifyAuthQuery.cs`
- Placeholder query returning `UserDto?`.
- Current handler always returns `null`.
- The code comment explicitly says JWT claims should eventually be used to load the authenticated profile.
- This query is not used by `AuthController.Verify` at the moment.

#### `src/DevLoggerBackend.Application/Features/Auth/Dtos/LoginRequestDto.cs`
- Login payload with `Email` and `Password`.

#### `src/DevLoggerBackend.Application/Features/Auth/Dtos/LoginResponseDto.cs`
- Login response with `User` and `Token`.

#### `src/DevLoggerBackend.Application/Features/Auth/Dtos/RegisterRequestDto.cs`
- Registration payload with `Name`, `Email`, `Password`, and `ConfirmPassword`.

#### `src/DevLoggerBackend.Application/Features/Auth/Dtos/UserDto.cs`
- Lightweight profile DTO containing `Id`, `Name`, `Email`, and `Role`.

#### `src/DevLoggerBackend.Application/Features/Auth/Validators/RegisterCommandValidator.cs`
- Requires name to be present and at least 2 characters long.
- Requires a valid email address.
- Requires password to be present and at least 8 characters long.
- Requires confirm password to be present and to match the password.

#### `src/DevLoggerBackend.Application/Features/Auth/Validators/LoginCommandValidator.cs`
- Requires a non-empty email.
- Requires a valid email format.
- Requires a non-empty password.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Commands/CreateDailyLogCommand.cs`
- `CreateDailyLogCommand` wraps `CreateDailyLogDto`.
- Handler parses `LogDate` with `DateOnly.TryParse` and throws a validation exception when parsing fails.
- The log is owned by the current authenticated user.
- `ProblemsFaced`, `Solutions`, and `Learnings` are trimmed before storage.
- `GitLink` is set to `null` when blank.
- `TasksWorked` and `Tips` are stored as sent.
- The handler currently injects `IUserRepository`, but it does not use it.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Commands/UpdateDailyLogCommand.cs`
- Updates an existing log by id using the same DTO shape as create.
- Verifies the current user owns the record before updating it.
- Throws `NotFoundException` when the log is missing or belongs to another user.
- Uses the same date parsing and field normalization rules as create.
- Updates `UpdatedAtUtc` and persists through `IUnitOfWork`.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Commands/DeleteDailyLogCommand.cs`
- Deletes a daily log by id.
- Enforces ownership by comparing the record's `UserId` against the current user.
- Throws `NotFoundException` if the record is absent or belongs to someone else.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Queries/GetAllDailyLogsQuery.cs`
- Returns all daily logs for the authenticated user only.
- Despite the name, it does not return logs for every user in the system.
- Maps entities to `DailyLogDto`.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Queries/GetDailyLogByIdQuery.cs`
- Returns one log for the authenticated user by id.
- Enforces ownership and throws `NotFoundException` for missing or foreign records.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Queries/SearchDailyLogsQuery.cs`
- Accepts `Keyword`, `DateFrom`, and `DateTo` as strings.
- Converts date strings to `DateOnly?`.
- Invalid or blank search date values are treated as `null` rather than causing an exception.
- Search is always scoped to the current user.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Dtos/CreateDailyLogDto.cs`
- Input DTO with `LogDate`, `TasksWorked`, `ProblemsFaced`, `Solutions`, `Learnings`, `Tips`, and optional `GitLink`.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Dtos/DailyLogDto.cs`
- Output DTO with id, log date, text fields, optional Git link, created time, and updated time.
- Dates are formatted as `yyyy-MM-dd`.
- Timestamps are serialized with the round-trip `O` format.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Dtos/SearchDailyLogsRequestDto.cs`
- Search payload with optional keyword and date bounds.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Validators/CreateDailyLogCommandValidator.cs`
- Requires a non-empty log date.
- Requires the log date to parse as `DateOnly`.
- Requires the log date not to be in the future.
- Requires `TasksWorked` to be non-empty.
- Validates `GitLink` as an absolute HTTP or HTTPS URL when present.

#### `src/DevLoggerBackend.Application/Features/DailyLogs/Validators/UpdateDailyLogCommandValidator.cs`
- Mirrors the create validator.
- Also requires the command id to be non-empty.

#### `src/DevLoggerBackend.Application/Features/Notes/Commands/SaveNoteCommand.cs`
- Upsert command for the current user note.
- If no note exists, the handler creates one.
- If a note already exists, the handler updates it in place.
- Ownership comes from `ICurrentUserService`.

#### `src/DevLoggerBackend.Application/Features/Notes/Queries/GetNoteQuery.cs`
- Retrieves the current user's note.
- Returns `null` when no note exists.

#### `src/DevLoggerBackend.Application/Features/Notes/Dtos/NoteDto.cs`
- Immutable note response containing `Id`, `Content`, and `UpdatedAtUtc`.
- Also defines `SaveNoteDto` as the input record with just `Content`.

#### `src/DevLoggerBackend.Application/Features/Notes/Validators/SaveNoteCommandValidator.cs`
- Requires note content to be non-null.
- Caps content at 100000 characters.
- Blank strings are permitted because the rule checks nullability, not emptiness.

#### `src/DevLoggerBackend.Application/DevLoggerBackend.Application.csproj`
- Targets `net10.0`.
- References MediatR, FluentValidation, and the dependency injection abstractions package.
- References the Domain project.

---

### Domain Project

#### `src/DevLoggerBackend.Domain/Common/BaseEntity.cs`
- Shared base type for persisted entities.
- Contains `Id`, `CreatedAtUtc`, and `UpdatedAtUtc`.

#### `src/DevLoggerBackend.Domain/Entities/User.cs`
- User entity with name, email, password hash, role, daily log collection, and optional note.

#### `src/DevLoggerBackend.Domain/Entities/DailyLog.cs`
- Daily work log entity with `DateOnly LogDate`.
- Stores tasks worked, problems faced, solutions, learnings, tips, optional Git link, and user relationship fields.

#### `src/DevLoggerBackend.Domain/Entities/Note.cs`
- One note per user, with content and user relationship fields.

#### `src/DevLoggerBackend.Domain/Enums/UserRole.cs`
- Role enum values are `Developer`, `SeniorDeveloper`, `TeamLead`, and `Manager`.
- New registrations default to `Developer`.

#### `src/DevLoggerBackend.Domain/DevLoggerBackend.Domain.csproj`
- Targets `net10.0`.
- Uses nullable reference types and implicit usings.

---

### Infrastructure Project

#### `src/DevLoggerBackend.Infrastructure/DependencyInjection.cs`
- Resolves the PostgreSQL connection string from `DATABASE_URL` first and then from `ConnectionStrings:DefaultConnection`.
- Throws an exception if neither is configured.
- Registers `AppDbContext` with retry-on-failure enabled for Npgsql.
- Registers the repository implementations.
- Registers BCrypt password hashing.
- Registers the token service.
- Registers `IHttpContextAccessor` and current user resolution.

#### `src/DevLoggerBackend.Infrastructure/Persistence/AppDbContext.cs`
- Implements both `DbContext` and `IUnitOfWork`.
- Exposes `DbSet<User>`, `DbSet<DailyLog>`, and `DbSet<Note>`.
- Overrides `SaveChangesAsync` to stamp created and updated timestamps on every tracked `BaseEntity`.
- Applies all entity configurations from the infrastructure assembly.
- Seeds three demo users during model creation.

#### `src/DevLoggerBackend.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- Maps `User` to the `Users` table.
- Requires name, email, and password hash.
- Limits name to 200 characters and email to 320 characters.
- Enforces a unique index on email.

#### `src/DevLoggerBackend.Infrastructure/Persistence/Configurations/DailyLogConfiguration.cs`
- Maps `DailyLog` to `DailyLogs`.
- Requires the text fields.
- Gives `Tips` a default empty string.
- Limits `GitLink` to 2048 characters.
- Configures a cascade relationship to `User`.
- Adds an index on `LogDate`.

#### `src/DevLoggerBackend.Infrastructure/Persistence/Configurations/NoteConfiguration.cs`
- Maps `Note` to `Notes`.
- Requires content and caps it at 100000 characters.
- Enforces a unique `UserId` index.
- Configures a one-to-one cascade relationship to `User`.

#### `src/DevLoggerBackend.Infrastructure/Repositories/DailyLogRepository.cs`
- EF Core implementation of daily log storage.
- `GetAllAsync` returns every log ordered by log date descending, but current handlers do not use this method.
- `GetByUserIdAsync` returns the current user's logs ordered descending.
- `GetByIdAsync` is used for ownership checks and edit/delete flows.
- `SearchByUserIdAsync` filters by user, keyword, and optional date range, then orders descending.
- The keyword search matches across every text field, the Git link, and the date string.
- Read operations use `AsNoTracking()` where appropriate.

#### `src/DevLoggerBackend.Infrastructure/Repositories/NoteRepository.cs`
- Looks up a note by user id with `SingleOrDefaultAsync`.
- Adds new notes.
- The one-note-per-user rule is reinforced by the repository and by the database unique index.

#### `src/DevLoggerBackend.Infrastructure/Repositories/UserRepository.cs`
- Adds users.
- Looks up users by email with normalized lowercase comparison.
- Looks up users by id.
- `GetDefaultUserAsync` returns the first user ordered by email.

#### `src/DevLoggerBackend.Infrastructure/Services/BcryptPasswordHasher.cs`
- Wraps BCrypt hashing and verification.

#### `src/DevLoggerBackend.Infrastructure/Services/CurrentUserService.cs`
- Reads the authenticated user's id from `ClaimTypes.NameIdentifier`.
- Returns `null` when there is no HTTP context or no matching claim.
- Assumes the claim value is a valid GUID.

#### `src/DevLoggerBackend.Infrastructure/Services/PlaceholderTokenService.cs`
- Generates JWTs for authenticated users.
- Uses `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, and `Jwt:ExpiryMinutes`.
- Adds `NameIdentifier`, `Email`, and `Role` claims.
- Signs tokens with HMAC SHA256.
- The class name says placeholder, but the implementation is the actual token issuer used by the app.

#### `src/DevLoggerBackend.Infrastructure/Persistence/Migrations/20260227170537_InitialCreate.cs`
- Creates the `Users` and `DailyLogs` tables.
- Adds indexes on `Users.Email`, `DailyLogs.UserId`, and `DailyLogs.LogDate`.
- Seeds the three initial users.

#### `src/DevLoggerBackend.Infrastructure/Persistence/Migrations/20260719130000_AddNotes.cs`
- Creates the `Notes` table.
- Adds the one-to-one foreign key to `Users`.
- Adds a unique index on `UserId`.

#### `src/DevLoggerBackend.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- Generated snapshot of the current EF Core model.
- Used by future migrations.

#### `src/DevLoggerBackend.Infrastructure/DevLoggerBackend.Infrastructure.csproj`
- Targets `net10.0`.
- References EF Core, Npgsql, BCrypt.Net-Next, configuration abstractions, dependency injection abstractions, and JWT packages.
- Includes a framework reference to `Microsoft.AspNetCore.App`.

---

### Test Project

#### `tests/DevLoggerBackend.Application.Tests/DevLoggerBackend.Application.Tests.csproj`
- Targets `net10.0`.
- Uses xUnit, Moq, FluentAssertions, Microsoft.NET.Test.Sdk, and Coverlet.
- References the Application and Domain projects.

#### `tests/DevLoggerBackend.Application.Tests/Features/DailyLogs/Commands/CreateDailyLogCommandHandlerTests.cs`
- Verifies the create handler adds a new daily log and saves changes.
- Uses mocked repository and unit of work dependencies.
- Uses a mocked current user id so the handler can build an owned record.

#### `tests/DevLoggerBackend.Application.Tests/Features/DailyLogs/Queries/GetAllDailyLogsQueryHandlerTests.cs`
- Verifies the query handler returns the repository result mapped to DTOs.
- Confirms the handler reads logs from the current user's repository path.

---

## 4. Code Execution Flow

### Authentication and Authorization
- Registration creates a new developer user after duplicate-email checking.
- Login verifies the password and issues a JWT.
- JWT validation checks issuer, audience, lifetime, and signing key.
- `NameIdentifier` is the claim used for the current user id.
- Authenticated endpoints are protected with `[Authorize]`.

### Daily Log Flow
- Controllers accept `CreateDailyLogDto` or `SearchDailyLogsRequestDto`.
- Create and update validators enforce date, task text, and Git link rules.
- Handlers scope all data to the authenticated user.
- Search accepts a keyword and optional date boundaries.
- The repository applies the final filtering and descending sort order.

### Notes Flow
- `GET /api/notes` returns `204 No Content` when the note does not exist.
- `PUT /api/notes` upserts the note for the current user.
- Content length is capped at 100000 characters.

### Validation Flow
- Validators are registered by assembly scanning.
- MediatR runs `ValidationBehavior` before the actual handler.
- Validation failures are grouped by property name and returned through the middleware as a structured 400 response.

### Timestamp Flow
- Handlers often set `CreatedAtUtc` and `UpdatedAtUtc` when creating records.
- `AppDbContext.SaveChangesAsync` also stamps timestamps on every `BaseEntity`.
- Updates set `UpdatedAtUtc` so the entity reflects the latest write time.

---

## 5. Database Documentation

### Tables
- `Users` stores user identity, password hash, and role.
- `DailyLogs` stores one user's daily journal entries.
- `Notes` stores one note per user.

### Relationships
- One user has many daily logs.
- One user has at most one note.
- Every daily log belongs to exactly one user.
- Every note belongs to exactly one user.

### Constraints and Indexes
- `Users.Email` is unique.
- `DailyLogs.UserId` is indexed.
- `DailyLogs.LogDate` is indexed.
- `Notes.UserId` is unique.
- `DailyLogs.GitLink` has a max length of 2048.
- `Notes.Content` has a max length of 100000.

### Seed Data
- The model seeds three sample users.
- The seed data is intended for local development and manual login testing.
- Roles seeded in the database include developer, senior developer, and team lead.

### Schema Management
- The current schema is represented by EF Core migrations.
- The startup path can apply migrations automatically.
- The migration snapshot tracks the current model state for future schema changes.

---

## 6. Dependencies

### Internal Dependencies
- API depends on Application and Infrastructure.
- Application depends on Domain.
- Infrastructure depends on Application and Domain.
- Tests depend on Application and Domain.

### External Libraries
- ASP.NET Core
- Microsoft.AspNetCore.Authentication.JwtBearer
- Entity Framework Core
- Npgsql.EntityFrameworkCore.PostgreSQL
- MediatR
- FluentValidation
- Serilog
- BCrypt.Net-Next
- Swashbuckle.AspNetCore
- xUnit
- Moq
- FluentAssertions

### Why They Are Used
- MediatR keeps controllers thin and use cases isolated.
- FluentValidation centralizes request validation.
- EF Core and Npgsql provide PostgreSQL persistence.
- Serilog provides structured logging to console and file.
- BCrypt protects passwords.
- Swashbuckle documents the API and supports bearer-token testing.

---

## 7. Configuration

### Sources
- `src/DevLoggerBackend.Api/appsettings.json`
- `src/DevLoggerBackend.Api/appsettings.Development.json`
- environment variables such as `PORT`, `DATABASE_URL`, and `ASPNETCORE_ENVIRONMENT`

### Key Settings
- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:ExpiryMinutes`
- `AllowedOrigins`
- `EnableSwaggerInProduction`
- `ApplyMigrationsOnStartup`
- `Serilog` minimum levels and overrides

### Runtime Notes
- The API defaults to port 5000 unless `PORT` is present.
- CORS uses the configured allowed origins list.
- Docker Compose overrides the connection string for container-to-container networking.
- Swagger can be exposed in production when the flag is enabled.

### Secrets Note
- The checked-in configuration includes real development values, so the repo should be treated as sensitive from a secrets-management standpoint.
- Production should move these values to environment-specific secret storage.

---

## 8. Feature Documentation

### Authentication
- Endpoints: `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/verify`
- Registration checks for duplicate email, hashes the password, stores the user as a developer, and returns a 201 message response.
- Login returns a token plus a simplified user DTO.
- Verify reads the current user id from the token claim and returns the loaded user profile.
- The placeholder query file exists but is not part of the actual verify endpoint flow.

### Daily Logs
- Endpoints: `GET /api/dailylogs`, `GET /api/dailylogs/{id}`, `POST /api/dailylogs`, `PUT /api/dailylogs/{id}`, `DELETE /api/dailylogs/{id}`, `POST /api/dailylogs/search`
- Every operation is restricted to the authenticated user.
- Create and update share the same DTO shape.
- Search is POST-based and accepts optional date filters plus a keyword.
- Output DTOs use string-formatted dates and timestamps.

### Notes
- Endpoints: `GET /api/notes`, `PUT /api/notes`
- GET returns 204 when there is no note.
- PUT creates or updates a single per-user note.
- The note model is intentionally simple and one-record-per-user.

---

## 9. Architecture and Design Patterns

### Architecture Style
- Clean Architecture with explicit boundaries between transport, application, domain, and infrastructure.
- CQRS-style request handling through MediatR.

### Patterns in Use
- Repository pattern for persistence abstraction.
- Unit of Work through `IUnitOfWork` and `AppDbContext`.
- Pipeline behavior for validation.
- Middleware for exception normalization.
- DTOs for API contracts.
- Dependency injection for wiring services together.

### Design Notes
- The code favors async I/O throughout.
- Read operations frequently use `AsNoTracking()`.
- Ownership is enforced in handlers instead of controller code.
- The note feature is designed as an upsert rather than a create/delete lifecycle.

---

## 10. Developer Notes

### Local Run
- Restore, build, and run from the repository root.
- Ensure PostgreSQL is available locally or via Docker Compose.
- `dotnet run --project src/DevLoggerBackend.Api` starts the API.

### Migrations
- EF Core migration commands use the API project as the startup project and the Infrastructure project as the migrations project.
- Startup migration execution is enabled by configuration in the checked-in settings.

### Extending The Codebase
- New use cases should be added in the Application project.
- New persistence behavior should live in Infrastructure.
- New HTTP endpoints belong in the API project.
- New request rules should be enforced with validators.

### Important Caveats
- `VerifyAuthQuery` is a stub.
- `GetDefaultUserAsync` and `GetAllAsync` exist but are not used by current request handlers.
- `CreateDailyLogCommandHandler` injects `IUserRepository` without using it.
- Health checks are registered but there is no mapped health endpoint in the current pipeline.
- The README still says .NET 8, while the project files themselves target `net10.0`.

---

## 11. Additional Observations

### Logging
- The app logs through Serilog and the exception middleware.
- Startup logs are written to a rolling file under `logs/`.

### Security
- Passwords are hashed with BCrypt.
- JWT bearer auth is used for protected routes.
- Validation runs before handlers.
- The code still relies on source-controlled development config values, so production hardening is still needed.

### Practical Implementation Details
- `AuthController.Verify` uses `[FromServices]` parameters instead of constructor-injected repositories.
- `DailyLogsController.Create` returns a `CreatedAtAction` response pointing at `GetById`.
- Search dates are parsed leniently and invalid values are ignored rather than rejected.
- Note GET intentionally returns 204 instead of a null payload.
- `PlaceholderTokenService` is the real token issuer despite its name.

### Potential Future Improvements
- Replace the placeholder auth verification query with a real profile lookup flow.
- Remove unused injections and repository helpers if they remain unnecessary.
- Add paging to daily log retrieval and search.
- Add integration tests around API endpoints and EF Core behavior.
- Add health check routing if operational monitoring is needed.

---

## Test Coverage Summary

The current test project covers two application handlers:
- `tests/DevLoggerBackend.Application.Tests/Features/DailyLogs/Commands/CreateDailyLogCommandHandlerTests.cs`
- `tests/DevLoggerBackend.Application.Tests/Features/DailyLogs/Queries/GetAllDailyLogsQueryHandlerTests.cs`

Coverage today is focused on the daily log use case:
- create daily log adds and saves a new entity
- get all daily logs returns repository results as DTOs

There are currently no tests for auth, notes, controllers, middleware, or infrastructure repositories.
