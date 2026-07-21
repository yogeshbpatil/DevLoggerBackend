# OFFICE JOURNAL BACKEND SUMMARY

## 1. Project Overview

### Purpose
The project is a backend API for a developer journal / office journal application. Its main responsibility is to let authenticated users record daily development activity, search historical entries, and maintain a personal note. The backend is implemented as a .NET 8 ASP.NET Core API using Clean Architecture principles.

### Business Problem It Solves
The application helps developers and team members capture:
- daily work summaries
- problems encountered
- solutions applied
- learnings and tips
- optional GitHub links
- a personal note for quick thoughts or reminders

Instead of relying on scattered notes or manual logs, the system provides a structured API for storing and retrieving this information securely.

### Overall Architecture
The solution follows a layered Clean Architecture style with four main projects:
- [src/DevLoggerBackend.Api](src/DevLoggerBackend.Api) – presentation layer, controllers, middleware, configuration, startup
- [src/DevLoggerBackend.Application](src/DevLoggerBackend.Application) – application logic, CQRS handlers, DTOs, validators, abstractions
- [src/DevLoggerBackend.Domain](src/DevLoggerBackend.Domain) – domain entities, enums, shared base types
- [src/DevLoggerBackend.Infrastructure](src/DevLoggerBackend.Infrastructure) – EF Core persistence, repositories, service implementations, dependency registration

The application uses:
- ASP.NET Core controllers for HTTP endpoints
- MediatR for CQRS command/query handling
- FluentValidation for request validation
- Entity Framework Core with PostgreSQL for persistence
- JWT-based authentication
- Serilog for structured logging

### How the Major Components Work Together
1. A client sends an HTTP request to the API.
2. The request passes through middleware and authentication.
3. Controllers forward the request to MediatR.
4. A validator may reject invalid input before the handler runs.
5. A handler executes the relevant business rule.
6. The handler uses repository abstractions and service abstractions.
7. The infrastructure layer implements those abstractions against EF Core and PostgreSQL.
8. Results are mapped to DTOs and returned as JSON responses.

---

## 2. Complete Application Execution Flow

### Startup Sequence
When the application starts from [src/DevLoggerBackend.Api/Program.cs](src/DevLoggerBackend.Api/Program.cs):
1. The web host is created with configuration from environment variables and configuration files.
2. Serilog is configured for console and file logging.
3. Application services are registered using [src/DevLoggerBackend.Application/DependencyInjection.cs](src/DevLoggerBackend.Application/DependencyInjection.cs).
4. Infrastructure services are registered using [src/DevLoggerBackend.Infrastructure/DependencyInjection.cs](src/DevLoggerBackend.Infrastructure/DependencyInjection.cs).
5. Controllers are enabled.
6. JWT authentication is configured.
7. Swagger is enabled for development or when explicitly configured.
8. CORS is configured for the frontend origins.
9. The app builds and runs the API pipeline.
10. Optional database migrations are applied at startup.

### Initialization Process
The startup flow includes:
- reading configuration from [src/DevLoggerBackend.Api/appsettings.json](src/DevLoggerBackend.Api/appsettings.json) and [src/DevLoggerBackend.Api/appsettings.Development.json](src/DevLoggerBackend.Api/appsettings.Development.json)
- resolving the PostgreSQL connection string from either `ConnectionStrings:DefaultConnection` or `DATABASE_URL`
- registering EF Core with Npgsql
- applying migrations automatically when configured

### Request/Response Lifecycle
A typical request flows like this:
1. An HTTP request reaches the ASP.NET Core pipeline.
2. The global exception middleware intercepts errors and formats consistent JSON error responses.
3. CORS policy is applied.
4. Authentication and authorization middleware examine the token.
5. The request is routed to the appropriate controller.
6. The controller calls MediatR with a command or query.
7. Validation executes through the MediatR pipeline behavior.
8. The matching handler performs the business logic.
9. The handler interacts with repositories and persistence.
10. The result is mapped to DTOs and returned as an HTTP response.

### Data Flow Through the System
- Controllers accept DTO input.
- Commands/queries carry the intent of the operation.
- Handlers use repositories.
- Repositories issue EF Core queries and changes against [src/DevLoggerBackend.Infrastructure/Persistence/AppDbContext.cs](src/DevLoggerBackend.Infrastructure/Persistence/AppDbContext.cs).
- The context writes data to PostgreSQL.
- The response is converted to DTOs for the client.

### Cross-Module Communication
- API layer depends on application layer abstractions and MediatR.
- Application layer depends on domain entities and abstractions defined in the application project.
- Infrastructure layer implements those abstractions and is wired in by dependency injection.

---

## 3. Complete Folder and File Documentation

### Root Level Files

#### [README.md](README.md)
- Purpose: Documentation for setup, running, Docker, migration, and deployment.
- Code present: Markdown instructions and shell commands.
- Main contents: local run instructions, Docker instructions, EF Core migration commands, frontend integration notes, Render deployment guidance.
- Execution: Read by developers; not executed as code.
- Dependencies: None.

#### [DevLoggerBackend.sln](DevLoggerBackend.sln)
- Purpose: Visual Studio solution file that groups all application projects and tests.
- Code present: Project entries and build configurations for API, Application, Domain, Infrastructure, and test projects.
- Execution: Used by the .NET SDK and Visual Studio to build/test the solution.

#### [docker-compose.yml](docker-compose.yml)
- Purpose: Defines a local Docker environment with PostgreSQL and the backend API.
- Code present: Two services: `postgres` and `api`.
- Main contents: PostgreSQL container configuration, API container build, environment variables, port mapping, and persisted database volume.
- Execution: Used by Docker Compose to start the local stack.

---

### API Project Documentation

#### [src/DevLoggerBackend.Api/Program.cs](src/DevLoggerBackend.Api/Program.cs)
- Purpose: Entry point for the web application.
- Code present: ASP.NET Core host setup, Serilog configuration, dependency registration, JWT auth, Swagger, CORS, database migration application, and the app pipeline.
- Key classes/functions: none; it contains top-level statements that configure the host.
- Execution: Runs automatically when the app is launched with `dotnet run` or by Docker.
- Depends on: [src/DevLoggerBackend.Api/Extensions/WebApplicationExtensions.cs](src/DevLoggerBackend.Api/Extensions/WebApplicationExtensions.cs), [src/DevLoggerBackend.Application/DependencyInjection.cs](src/DevLoggerBackend.Application/DependencyInjection.cs), [src/DevLoggerBackend.Infrastructure/DependencyInjection.cs](src/DevLoggerBackend.Infrastructure/DependencyInjection.cs).
- Called by: The .NET runtime when the API starts.

#### [src/DevLoggerBackend.Api/Extensions/WebApplicationExtensions.cs](src/DevLoggerBackend.Api/Extensions/WebApplicationExtensions.cs)
- Purpose: Centralizes API pipeline configuration and startup migration logic.
- Code present: `UseApiPipeline` and `ApplyDatabaseMigrationsAsync` extension methods.
- Key classes/functions:
  - `UseApiPipeline(WebApplication app)` – wires middleware, CORS, auth, authorization, and controllers
  - `ApplyDatabaseMigrationsAsync(WebApplication app)` – applies EF Core migrations at startup
- Execution: Called from [src/DevLoggerBackend.Api/Program.cs](src/DevLoggerBackend.Api/Program.cs) during app startup.
- Depends on: [src/DevLoggerBackend.Api/Middleware/GlobalExceptionHandlingMiddleware.cs](src/DevLoggerBackend.Api/Middleware/GlobalExceptionHandlingMiddleware.cs), [src/DevLoggerBackend.Infrastructure/Persistence/AppDbContext.cs](src/DevLoggerBackend.Infrastructure/Persistence/AppDbContext.cs).

#### [src/DevLoggerBackend.Api/Middleware/GlobalExceptionHandlingMiddleware.cs](src/DevLoggerBackend.Api/Middleware/GlobalExceptionHandlingMiddleware.cs)
- Purpose: Catches unhandled exceptions and converts them into consistent JSON error responses.
- Code present: Middleware class with `Invoke` and a private exception handling method.
- Key classes/functions:
  - `Invoke(HttpContext context)` – wraps the next middleware and catches exceptions
  - `HandleExceptionAsync(HttpContext context, Exception exception)` – maps exceptions to HTTP statuses and payloads
- Execution: Runs for every request before the controller layer.
- Depends on: [src/DevLoggerBackend.Api/Models/ErrorResponse.cs](src/DevLoggerBackend.Api/Models/ErrorResponse.cs), application exceptions in [src/DevLoggerBackend.Application/Common/Exceptions](src/DevLoggerBackend.Application/Common/Exceptions).

#### [src/DevLoggerBackend.Api/Controllers/AuthController.cs](src/DevLoggerBackend.Api/Controllers/AuthController.cs)
- Purpose: Exposes authentication endpoints.
- Code present: endpoints for registration, login, and JWT verification.
- Key classes/functions:
  - `Register` – handles account creation
  - `Login` – authenticates a user and returns a token
  - `Verify` – validates the current authenticated identity and returns user metadata
- Execution: Invoked by clients calling `/api/auth/*`.
- Depends on: application auth commands and DTOs, repositories and current user service abstractions.

#### [src/DevLoggerBackend.Api/Controllers/DailyLogsController.cs](src/DevLoggerBackend.Api/Controllers/DailyLogsController.cs)
- Purpose: Exposes CRUD and search endpoints for daily logs.
- Code present: methods for listing, retrieving by id, creating, updating, deleting, and searching logs.
- Key classes/functions:
  - `GetAll` – retrieves all logs for the current user
  - `GetById` – gets one specific log
  - `Create` – creates a log
  - `Update` – updates a log
  - `Delete` – deletes a log
  - `Search` – filters by keyword and date range
- Execution: Called via `/api/dailylogs` endpoints.
- Depends on: command/query types in [src/DevLoggerBackend.Application/Features/DailyLogs](src/DevLoggerBackend.Application/Features/DailyLogs).

#### [src/DevLoggerBackend.Api/Controllers/NotesController.cs](src/DevLoggerBackend.Api/Controllers/NotesController.cs)
- Purpose: Exposes endpoints for reading and saving the current user’s note.
- Code present: GET and PUT endpoints.
- Key classes/functions:
  - `Get` – retrieves the current user’s note or returns no content
  - `Save` – creates or updates a note
- Execution: Called via `/api/notes`.
- Depends on: notes commands/queries in [src/DevLoggerBackend.Application/Features/Notes](src/DevLoggerBackend.Application/Features/Notes).

#### [src/DevLoggerBackend.Api/Models/ErrorResponse.cs](src/DevLoggerBackend.Api/Models/ErrorResponse.cs)
- Purpose: Standard JSON error shape used by the global exception middleware.
- Code present: response properties for status code, message, trace id, and validation errors.
- Key classes/functions: `ErrorResponse`.

#### [src/DevLoggerBackend.Api/Properties/launchSettings.json](src/DevLoggerBackend.Api/Properties/launchSettings.json)
- Purpose: Local launch profile for development.
- Code present: launch settings for the project and Swagger URL.
- Execution: Used by the .NET CLI when launching in development.

#### [src/DevLoggerBackend.Api/appsettings.json](src/DevLoggerBackend.Api/appsettings.json)
- Purpose: Main runtime configuration file.
- Code present: PostgreSQL connection string, JWT settings, allowed CORS origins, Swagger toggle, migration toggle, and Serilog settings.
- Execution: Loaded by ASP.NET Core configuration at startup.

#### [src/DevLoggerBackend.Api/appsettings.Development.json](src/DevLoggerBackend.Api/appsettings.Development.json)
- Purpose: Development-specific overrides.
- Code present: Serilog minimum level override.
- Execution: Applied in development environments.

#### [src/DevLoggerBackend.Api/Dockerfile](src/DevLoggerBackend.Api/Dockerfile)
- Purpose: Container build recipe for the API.
- Code present: multi-stage Docker build for .NET 8, restore and publish steps, container startup command.
- Execution: Used by Docker Compose and deployment pipelines.

---

### Application Project Documentation

#### [src/DevLoggerBackend.Application/DependencyInjection.cs](src/DevLoggerBackend.Application/DependencyInjection.cs)
- Purpose: Registers MediatR, validators, and pipeline behavior into the DI container.
- Code present: `AddApplication(IServiceCollection services)`.
- Key classes/functions: `AddApplication`.
- Execution: Called from [src/DevLoggerBackend.Api/Program.cs](src/DevLoggerBackend.Api/Program.cs).
- Depends on: [src/DevLoggerBackend.Application/Common/Behaviors/ValidationBehavior.cs](src/DevLoggerBackend.Application/Common/Behaviors/ValidationBehavior.cs).

#### Application Abstractions

##### [src/DevLoggerBackend.Application/Abstractions/Persistence/IUnitOfWork.cs](src/DevLoggerBackend.Application/Abstractions/Persistence/IUnitOfWork.cs)
- Purpose: Defines a unit-of-work contract for saving changes.
- Code present: `SaveChangesAsync` method.

##### [src/DevLoggerBackend.Application/Abstractions/Repositories/IDailyLogRepository.cs](src/DevLoggerBackend.Application/Abstractions/Repositories/IDailyLogRepository.cs)
- Purpose: Repository abstraction for daily logs.
- Code present: methods to read, search, add, update, and remove daily logs.

##### [src/DevLoggerBackend.Application/Abstractions/Repositories/INoteRepository.cs](src/DevLoggerBackend.Application/Abstractions/Repositories/INoteRepository.cs)
- Purpose: Repository abstraction for notes.
- Code present: methods to get a note by user and add a new note.

##### [src/DevLoggerBackend.Application/Abstractions/Repositories/IUserRepository.cs](src/DevLoggerBackend.Application/Abstractions/Repositories/IUserRepository.cs)
- Purpose: Repository abstraction for users.
- Code present: methods for adding users, finding by email/id, and getting a default user.

##### [src/DevLoggerBackend.Application/Abstractions/Services/ICurrentUserService.cs](src/DevLoggerBackend.Application/Abstractions/Services/ICurrentUserService.cs)
- Purpose: Exposes the currently authenticated user id.
- Code present: `UserId` property.

##### [src/DevLoggerBackend.Application/Abstractions/Services/IPasswordHasher.cs](src/DevLoggerBackend.Application/Abstractions/Services/IPasswordHasher.cs)
- Purpose: Smooths over password hashing implementation details.
- Code present: `Hash` and `Verify` methods.

##### [src/DevLoggerBackend.Application/Abstractions/Services/ITokenService.cs](src/DevLoggerBackend.Application/Abstractions/Services/ITokenService.cs)
- Purpose: Defines token generation for authentication.
- Code present: `GenerateToken(User user)`.

#### Common Application Infrastructure

##### [src/DevLoggerBackend.Application/Common/Behaviors/ValidationBehavior.cs](src/DevLoggerBackend.Application/Common/Behaviors/ValidationBehavior.cs)
- Purpose: A MediatR pipeline behavior that validates requests before handlers run.
- Code present: `ValidationBehavior<TRequest, TResponse>` that runs all validators and throws `ValidationException` on failures.
- Execution: Invoked automatically for commands/queries because it is registered as a pipeline behavior.

##### [src/DevLoggerBackend.Application/Common/Exceptions/ConflictException.cs](src/DevLoggerBackend.Application/Common/Exceptions/ConflictException.cs)
- Purpose: Signals a conflict such as duplicate registration data.

##### [src/DevLoggerBackend.Application/Common/Exceptions/NotFoundException.cs](src/DevLoggerBackend.Application/Common/Exceptions/NotFoundException.cs)
- Purpose: Signals that a resource was not found.

##### [src/DevLoggerBackend.Application/Common/Exceptions/UnauthorizedException.cs](src/DevLoggerBackend.Application/Common/Exceptions/UnauthorizedException.cs)
- Purpose: Signals invalid or missing authentication.

##### [src/DevLoggerBackend.Application/Common/Models/PagedResult.cs](src/DevLoggerBackend.Application/Common/Models/PagedResult.cs)
- Purpose: Generic pagination model placeholder for future paging support.
- Code present: items, total count, page number, and page size.

#### Authentication Feature

##### [src/DevLoggerBackend.Application/Features/Auth/Commands/RegisterCommand.cs](src/DevLoggerBackend.Application/Features/Auth/Commands/RegisterCommand.cs)
- Purpose: Implements user registration.
- Code present: `RegisterCommand` record and `RegisterCommandHandler`.
- Key behavior: checks for duplicate email, hashes the password, creates a new `User`, saves it via the unit of work, and returns `Unit.Value`.
- Depends on: [src/DevLoggerBackend.Application/Abstractions/Repositories/IUserRepository.cs](src/DevLoggerBackend.Application/Abstractions/Repositories/IUserRepository.cs), [src/DevLoggerBackend.Application/Abstractions/Services/IPasswordHasher.cs](src/DevLoggerBackend.Application/Abstractions/Services/IPasswordHasher.cs), [src/DevLoggerBackend.Application/Abstractions/Persistence/IUnitOfWork.cs](src/DevLoggerBackend.Application/Abstractions/Persistence/IUnitOfWork.cs).

##### [src/DevLoggerBackend.Application/Features/Auth/Commands/LoginCommand.cs](src/DevLoggerBackend.Application/Features/Auth/Commands/LoginCommand.cs)
- Purpose: Implements login logic.
- Code present: `LoginCommand` record and `LoginCommandHandler`.
- Key behavior: finds the user by email, verifies the password, and returns a token plus simplified user info.
- Depends on: repository, password hasher, token service.

##### [src/DevLoggerBackend.Application/Features/Auth/Dtos/LoginRequestDto.cs](src/DevLoggerBackend.Application/Features/Auth/Dtos/LoginRequestDto.cs)
- Purpose: DTO for incoming login payload.

##### [src/DevLoggerBackend.Application/Features/Auth/Dtos/LoginResponseDto.cs](src/DevLoggerBackend.Application/Features/Auth/Dtos/LoginResponseDto.cs)
- Purpose: DTO for login response containing the user object and token.

##### [src/DevLoggerBackend.Application/Features/Auth/Dtos/RegisterRequestDto.cs](src/DevLoggerBackend.Application/Features/Auth/Dtos/RegisterRequestDto.cs)
- Purpose: DTO for the registration request.

##### [src/DevLoggerBackend.Application/Features/Auth/Dtos/UserDto.cs](src/DevLoggerBackend.Application/Features/Auth/Dtos/UserDto.cs)
- Purpose: DTO for returning user profile information.

##### [src/DevLoggerBackend.Application/Features/Auth/Validators/RegisterCommandValidator.cs](src/DevLoggerBackend.Application/Features/Auth/Validators/RegisterCommandValidator.cs)
- Purpose: Validates registration input.
- Code present: rules for name length, email format, password length, and password confirmation matching.

##### [src/DevLoggerBackend.Application/Features/Auth/Validators/LoginCommandValidator.cs](src/DevLoggerBackend.Application/Features/Auth/Validators/LoginCommandValidator.cs)
- Purpose: Validates login requests.

##### [src/DevLoggerBackend.Application/Features/Auth/Queries/VerifyAuthQuery.cs](src/DevLoggerBackend.Application/Features/Auth/Queries/VerifyAuthQuery.cs)
- Purpose: Placeholder query for auth verification.
- Code present: a query and handler that currently returns `null` with a TODO comment.
- Note: It is not currently wired to use JWT claims or repository-based user loading.

#### Daily Logs Feature

##### [src/DevLoggerBackend.Application/Features/DailyLogs/Commands/CreateDailyLogCommand.cs](src/DevLoggerBackend.Application/Features/DailyLogs/Commands/CreateDailyLogCommand.cs)
- Purpose: Creates a new daily log entry.
- Code present: `CreateDailyLogCommand` record and handler.
- Key behavior: parses the `logDate`, creates a `DailyLog` entity, assigns the current user id, saves it, and returns a DTO.

##### [src/DevLoggerBackend.Application/Features/DailyLogs/Commands/UpdateDailyLogCommand.cs](src/DevLoggerBackend.Application/Features/DailyLogs/Commands/UpdateDailyLogCommand.cs)
- Purpose: Updates an existing daily log entry.
- Key behavior: loads the existing entity, checks ownership, parses the date, updates fields, and saves changes.

##### [src/DevLoggerBackend.Application/Features/DailyLogs/Commands/DeleteDailyLogCommand.cs](src/DevLoggerBackend.Application/Features/DailyLogs/Commands/DeleteDailyLogCommand.cs)
- Purpose: Deletes a daily log entry.
- Key behavior: verifies that the current user owns the log, removes it, and saves changes.

##### [src/DevLoggerBackend.Application/Features/DailyLogs/Queries/GetAllDailyLogsQuery.cs](src/DevLoggerBackend.Application/Features/DailyLogs/Queries/GetAllDailyLogsQuery.cs)
- Purpose: Retrieves all logs for the current user.
- Key behavior: loads logs from the repository and maps them to DTOs.

##### [src/DevLoggerBackend.Application/Features/DailyLogs/Queries/GetDailyLogByIdQuery.cs](src/DevLoggerBackend.Application/Features/DailyLogs/Queries/GetDailyLogByIdQuery.cs)
- Purpose: Retrieves a single log by id.
- Key behavior: validates ownership before returning the entity DTO.

##### [src/DevLoggerBackend.Application/Features/DailyLogs/Queries/SearchDailyLogsQuery.cs](src/DevLoggerBackend.Application/Features/DailyLogs/Queries/SearchDailyLogsQuery.cs)
- Purpose: Searches logs by keyword and date range.
- Key behavior: parses the dates and calls the repository’s search method.

##### [src/DevLoggerBackend.Application/Features/DailyLogs/Dtos/CreateDailyLogDto.cs](src/DevLoggerBackend.Application/Features/DailyLogs/Dtos/CreateDailyLogDto.cs)
- Purpose: Input DTO for creating or updating a daily log.
- Code present: date, task summary, problems, solutions, learnings, tips, and optional Git link fields.

##### [src/DevLoggerBackend.Application/Features/DailyLogs/Dtos/DailyLogDto.cs](src/DevLoggerBackend.Application/Features/DailyLogs/Dtos/DailyLogDto.cs)
- Purpose: Output DTO for daily log responses.
- Code present: a `FromEntity` factory that maps the domain entity to DTO fields.

##### [src/DevLoggerBackend.Application/Features/DailyLogs/Dtos/SearchDailyLogsRequestDto.cs](src/DevLoggerBackend.Application/Features/DailyLogs/Dtos/SearchDailyLogsRequestDto.cs)
- Purpose: DTO for search input with optional keyword and date fields.

##### [src/DevLoggerBackend.Application/Features/DailyLogs/Validators/CreateDailyLogCommandValidator.cs](src/DevLoggerBackend.Application/Features/DailyLogs/Validators/CreateDailyLogCommandValidator.cs)
- Purpose: Validates daily log creation.
- Key rules: valid date, non-future date, non-empty tasks worked, optional valid URL for GitLink.

##### [src/DevLoggerBackend.Application/Features/DailyLogs/Validators/UpdateDailyLogCommandValidator.cs](src/DevLoggerBackend.Application/Features/DailyLogs/Validators/UpdateDailyLogCommandValidator.cs)
- Purpose: Validates the update request.

#### Notes Feature

##### [src/DevLoggerBackend.Application/Features/Notes/Commands/SaveNoteCommand.cs](src/DevLoggerBackend.Application/Features/Notes/Commands/SaveNoteCommand.cs)
- Purpose: Saves a user note.
- Key behavior: retrieves the user’s note; creates a new one if absent; otherwise updates it; persists the change.

##### [src/DevLoggerBackend.Application/Features/Notes/Queries/GetNoteQuery.cs](src/DevLoggerBackend.Application/Features/Notes/Queries/GetNoteQuery.cs)
- Purpose: Retrieves the current user’s note.

##### [src/DevLoggerBackend.Application/Features/Notes/Dtos/NoteDto.cs](src/DevLoggerBackend.Application/Features/Notes/Dtos/NoteDto.cs)
- Purpose: DTO for notes with `Id`, `Content`, and timestamp.

##### [src/DevLoggerBackend.Application/Features/Notes/Validators/SaveNoteCommandValidator.cs](src/DevLoggerBackend.Application/Features/Notes/Validators/SaveNoteCommandValidator.cs)
- Purpose: Validates note content length.

---

### Domain Project Documentation

#### [src/DevLoggerBackend.Domain/Common/BaseEntity.cs](src/DevLoggerBackend.Domain/Common/BaseEntity.cs)
- Purpose: Shared base class for all persisted domain entities.
- Code present: `Id`, `CreatedAtUtc`, `UpdatedAtUtc` properties.

#### [src/DevLoggerBackend.Domain/Entities/User.cs](src/DevLoggerBackend.Domain/Entities/User.cs)
- Purpose: Represents an authenticated user.
- Code present: `Name`, `Email`, `PasswordHash`, `Role`, `DailyLogs`, and `Note` navigation properties.

#### [src/DevLoggerBackend.Domain/Entities/DailyLog.cs](src/DevLoggerBackend.Domain/Entities/DailyLog.cs)
- Purpose: Represents a daily log entry.
- Code present: date, text fields, optional Git link, and `UserId`/`User` relationships.

#### [src/DevLoggerBackend.Domain/Entities/Note.cs](src/DevLoggerBackend.Domain/Entities/Note.cs)
- Purpose: Represents a single note belonging to a user.
- Code present: `Content`, `UserId`, and navigation property.

#### [src/DevLoggerBackend.Domain/Enums/UserRole.cs](src/DevLoggerBackend.Domain/Enums/UserRole.cs)
- Purpose: Defines user roles used in the system.
- Code present: `Developer`, `SeniorDeveloper`, `TeamLead`, and `Manager`.

---

### Infrastructure Project Documentation

#### [src/DevLoggerBackend.Infrastructure/DependencyInjection.cs](src/DevLoggerBackend.Infrastructure/DependencyInjection.cs)
- Purpose: Registers infrastructure services into dependency injection.
- Code present: adds `AppDbContext`, repository implementations, password hasher, token service, `IHttpContextAccessor`, and current user service.
- Key behavior: resolves the PostgreSQL connection string from configuration and builds it from a `DATABASE_URL` when present.
- Depends on: [src/DevLoggerBackend.Infrastructure/Persistence/AppDbContext.cs](src/DevLoggerBackend.Infrastructure/Persistence/AppDbContext.cs).

#### [src/DevLoggerBackend.Infrastructure/Persistence/AppDbContext.cs](src/DevLoggerBackend.Infrastructure/Persistence/AppDbContext.cs)
- Purpose: EF Core database context for the entire application.
- Code present: `DbSet` properties for `Users`, `DailyLogs`, and `Notes`; timestamp management on save; model configuration application; seed data for demo users.
- Key classes/functions:
  - `SaveChangesAsync` – automatically sets created/updated timestamps
  - `OnModelCreating` – applies entity configurations and seeds initial users
- Execution: Used by repositories and the startup migration process.
- Depends on: entity configurations in [src/DevLoggerBackend.Infrastructure/Persistence/Configurations](src/DevLoggerBackend.Infrastructure/Persistence/Configurations).

#### Configuration Files

##### [src/DevLoggerBackend.Infrastructure/Persistence/Configurations/UserConfiguration.cs](src/DevLoggerBackend.Infrastructure/Persistence/Configurations/UserConfiguration.cs)
- Purpose: Maps the `User` entity to the `Users` table.
- Code present: table name, key, required fields, and unique email index.

##### [src/DevLoggerBackend.Infrastructure/Persistence/Configurations/DailyLogConfiguration.cs](src/DevLoggerBackend.Infrastructure/Persistence/Configurations/DailyLogConfiguration.cs)
- Purpose: Maps the `DailyLog` entity to the `DailyLogs` table.
- Code present: required text fields, default tips value, Git link length, foreign key to `Users`, and an index on `LogDate`.

##### [src/DevLoggerBackend.Infrastructure/Persistence/Configurations/NoteConfiguration.cs](src/DevLoggerBackend.Infrastructure/Persistence/Configurations/NoteConfiguration.cs)
- Purpose: Maps the `Note` entity to the `Notes` table.
- Code present: max length for content, unique user index, and one-to-one relationship to `User`.

#### Repositories

##### [src/DevLoggerBackend.Infrastructure/Repositories/DailyLogRepository.cs](src/DevLoggerBackend.Infrastructure/Repositories/DailyLogRepository.cs)
- Purpose: EF Core implementation of daily log persistence.
- Code present: methods to get all logs, get by user, get by id, search, add, update, and remove.
- Key behavior: the search method builds a query by keyword and date range and orders by log date descending.
- Depends on: [src/DevLoggerBackend.Infrastructure/Persistence/AppDbContext.cs](src/DevLoggerBackend.Infrastructure/Persistence/AppDbContext.cs).

##### [src/DevLoggerBackend.Infrastructure/Repositories/NoteRepository.cs](src/DevLoggerBackend.Infrastructure/Repositories/NoteRepository.cs)
- Purpose: EF Core implementation of note persistence.
- Code present: lookup by user and insertion of new notes.

##### [src/DevLoggerBackend.Infrastructure/Repositories/UserRepository.cs](src/DevLoggerBackend.Infrastructure/Repositories/UserRepository.cs)
- Purpose: EF Core implementation for user access.
- Code present: add, find by email/id, and get the first/default user.

#### Services

##### [src/DevLoggerBackend.Infrastructure/Services/BcryptPasswordHasher.cs](src/DevLoggerBackend.Infrastructure/Services/BcryptPasswordHasher.cs)
- Purpose: Implements password hashing and verification with BCrypt.
- Code present: wraps `BCrypt.Net.BCrypt` methods.

##### [src/DevLoggerBackend.Infrastructure/Services/CurrentUserService.cs](src/DevLoggerBackend.Infrastructure/Services/CurrentUserService.cs)
- Purpose: Reads the authenticated user id from the current HTTP context.
- Code present: uses `IHttpContextAccessor` and `ClaimTypes.NameIdentifier`.

##### [src/DevLoggerBackend.Infrastructure/Services/PlaceholderTokenService.cs](src/DevLoggerBackend.Infrastructure/Services/PlaceholderTokenService.cs)
- Purpose: Generates JWTs for authenticated users.
- Code present: builds a `JwtSecurityToken` using configuration values and returns the serialized token.
- Note: The class name suggests a placeholder implementation, and the project currently uses it directly for production-style auth behavior.

#### Migrations

##### [src/DevLoggerBackend.Infrastructure/Persistence/Migrations/20260227170537_InitialCreate.cs](src/DevLoggerBackend.Infrastructure/Persistence/Migrations/20260227170537_InitialCreate.cs)
- Purpose: Initial EF Core migration.
- Code present: creates `Users` and `DailyLogs` tables, adds indexes, and seeds initial demo users.

##### [src/DevLoggerBackend.Infrastructure/Persistence/Migrations/20260719130000_AddNotes.cs](src/DevLoggerBackend.Infrastructure/Persistence/Migrations/20260719130000_AddNotes.cs)
- Purpose: Adds the `Notes` table and relationship to `Users`.

##### [src/DevLoggerBackend.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs](src/DevLoggerBackend.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs)
- Purpose: Snapshot of the current EF Core model used for migrations.
- Code present: generated representation of the current schema.

---

## 4. Code Execution Flow

### Chronological Execution Order
1. The application starts in [src/DevLoggerBackend.Api/Program.cs](src/DevLoggerBackend.Api/Program.cs).
2. Configuration is loaded from appsettings and environment variables.
3. Dependency injection is configured for application and infrastructure services.
4. Controllers are mapped.
5. Authentication middleware is registered and JWT validation is defined.
6. The app builds and the startup pipeline executes.
7. Database migrations are applied if enabled.
8. An incoming request hits the middleware chain.
9. The global exception middleware wraps the request.
10. Authentication and authorization are enforced.
11. The controller routes the request to MediatR.
12. Validators run through the pipeline behavior.
13. The handler executes the business rule.
14. The repository stores or retrieves data using EF Core.
15. The context persists to PostgreSQL.
16. The response is serialized to JSON and returned to the client.

### Authentication and Authorization Flow
- The API uses JWT bearer authentication.
- The token is validated based on issuer, audience, lifetime, and signing key from configuration.
- Controllers for daily logs and notes are decorated with `[Authorize]`.
- The current user id is derived from the JWT claim `NameIdentifier` by [src/DevLoggerBackend.Infrastructure/Services/CurrentUserService.cs](src/DevLoggerBackend.Infrastructure/Services/CurrentUserService.cs).
- Handlers use this service to ensure the request is tied to the correct authenticated user.

### Validation Flow
- Validators are discovered by reflection and registered through dependency injection.
- Every MediatR request passes through [src/DevLoggerBackend.Application/Common/Behaviors/ValidationBehavior.cs](src/DevLoggerBackend.Application/Common/Behaviors/ValidationBehavior.cs).
- Validation failures throw `ValidationException`, which the API middleware converts into `400 Bad Request` responses.

### Business Logic Flow
- Auth flows handle registration and login.
- Daily log flows create, update, delete, retrieve, and search entries.
- Note flows create or update a single note per user.

---

## 5. Database Documentation

### Database Models
The schema is centered around three entities:
- `User` – an account and identity object
- `DailyLog` – a daily developer journal entry
- `Note` – a user-specific note

### Relationships
- One `User` has many `DailyLog` records.
- One `User` has at most one `Note`.
- A `DailyLog` belongs to one `User`.
- A `Note` belongs to one `User`.

### Table Layout
- `Users` table stores user identity, password hash, and role.
- `DailyLogs` stores journal entries and links them to the user who owns them.
- `Notes` stores content for a single note per user.

### Database Initialization
- The database is configured through EF Core in [src/DevLoggerBackend.Infrastructure/Persistence/AppDbContext.cs](src/DevLoggerBackend.Infrastructure/Persistence/AppDbContext.cs).
- When the app starts, migrations can be applied automatically through [src/DevLoggerBackend.Api/Extensions/WebApplicationExtensions.cs](src/DevLoggerBackend.Api/Extensions/WebApplicationExtensions.cs).
- The context seeds three sample users at startup.

### CRUD Operations
- User registration creates a new `User`.
- Daily log creation inserts a new `DailyLog`.
- Daily log updates modify an existing `DailyLog`.
- Daily log deletion removes the entity.
- Notes are created or updated for the current user.

### Query Execution Flow
- Controllers call MediatR which delegates to handlers.
- Handlers use repository abstractions.
- Repositories use EF Core `DbSet` queries.
- The query results are mapped to DTOs and returned to the client.

---

## 6. Dependencies

### Internal Dependencies
- [src/DevLoggerBackend.Api](src/DevLoggerBackend.Api) depends on [src/DevLoggerBackend.Application](src/DevLoggerBackend.Application) and [src/DevLoggerBackend.Infrastructure](src/DevLoggerBackend.Infrastructure).
- [src/DevLoggerBackend.Application](src/DevLoggerBackend.Application) depends on [src/DevLoggerBackend.Domain](src/DevLoggerBackend.Domain).
- [src/DevLoggerBackend.Infrastructure](src/DevLoggerBackend.Infrastructure) depends on [src/DevLoggerBackend.Application](src/DevLoggerBackend.Application) and [src/DevLoggerBackend.Domain](src/DevLoggerBackend.Domain).

### External Libraries and Packages
- ASP.NET Core / ASP.NET Core Authentication JWT Bearer – HTTP hosting and JWT validation
- MediatR – command/query dispatching
- FluentValidation – validation rules
- EF Core and Npgsql – ORM and PostgreSQL provider
- Serilog – structured logging
- BCrypt.Net-Next – password hashing
- Swashbuckle.AspNetCore – Swagger/OpenAPI documentation
- xUnit, Moq, FluentAssertions – testing

### Purpose of Major Dependencies
- MediatR keeps handlers decoupled from controllers and encourages CQRS.
- FluentValidation keeps business input validation centralized.
- EF Core enables type-safe persistence and migration management.
- Npgsql is the PostgreSQL driver.
- Serilog provides operational logging to console and files.
- BCrypt safely hashes passwords.

---

## 7. Configuration

### Environment Variables and Settings
Configuration is sourced from:
- [src/DevLoggerBackend.Api/appsettings.json](src/DevLoggerBackend.Api/appsettings.json)
- [src/DevLoggerBackend.Api/appsettings.Development.json](src/DevLoggerBackend.Api/appsettings.Development.json)
- environment variables such as `PORT`, `DATABASE_URL`, and `ASPNETCORE_ENVIRONMENT`

### Key Configuration Values
- `ConnectionStrings:DefaultConnection` – PostgreSQL connection string
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` – JWT settings
- `AllowedOrigins` – CORS origins permitted by the frontend
- `EnableSwaggerInProduction` – allows Swagger in production when enabled
- `ApplyMigrationsOnStartup` – controls automatic migration execution

### Runtime Configuration Notes
- The app binds to `http://localhost:5000` by default unless `PORT` is set.
- The Docker setup overrides the connection string for container networking.

### Deployment Configuration
- The project supports Docker deployment and Render-style hosting.
- The Dockerfile uses a multi-stage build and exposes port `5000`.
- The README includes Render deployment instructions with environment variables.

### Secrets Management
- Secret values such as JWT keys and connection strings are currently stored in configuration files and environment variables.
- For production systems, these should be moved to a stronger secrets store such as environment-specific secret management services.

---

## 8. Feature Documentation

### Authentication Feature
- Endpoints: `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/verify`
- Files involved: [src/DevLoggerBackend.Api/Controllers/AuthController.cs](src/DevLoggerBackend.Api/Controllers/AuthController.cs), [src/DevLoggerBackend.Application/Features/Auth/Commands/RegisterCommand.cs](src/DevLoggerBackend.Application/Features/Auth/Commands/RegisterCommand.cs), [src/DevLoggerBackend.Application/Features/Auth/Commands/LoginCommand.cs](src/DevLoggerBackend.Application/Features/Auth/Commands/LoginCommand.cs), [src/DevLoggerBackend.Infrastructure/Services/PlaceholderTokenService.cs](src/DevLoggerBackend.Infrastructure/Services/PlaceholderTokenService.cs)
- Execution flow: controller receives request -> mediator -> validator -> handler -> repository/context -> JWT created and returned

### Daily Logs Feature
- Endpoints: `GET /api/dailylogs`, `GET /api/dailylogs/{id}`, `POST /api/dailylogs`, `PUT /api/dailylogs/{id}`, `DELETE /api/dailylogs/{id}`, `POST /api/dailylogs/search`
- Files involved: [src/DevLoggerBackend.Api/Controllers/DailyLogsController.cs](src/DevLoggerBackend.Api/Controllers/DailyLogsController.cs), [src/DevLoggerBackend.Application/Features/DailyLogs](src/DevLoggerBackend.Application/Features/DailyLogs), [src/DevLoggerBackend.Infrastructure/Repositories/DailyLogRepository.cs](src/DevLoggerBackend.Infrastructure/Repositories/DailyLogRepository.cs)
- Execution flow: authenticated user sends data -> controller -> handler -> repository -> EF Core -> DTO response

### Notes Feature
- Endpoints: `GET /api/notes`, `PUT /api/notes`
- Files involved: [src/DevLoggerBackend.Api/Controllers/NotesController.cs](src/DevLoggerBackend.Api/Controllers/NotesController.cs), [src/DevLoggerBackend.Application/Features/Notes](src/DevLoggerBackend.Application/Features/Notes), [src/DevLoggerBackend.Infrastructure/Repositories/NoteRepository.cs](src/DevLoggerBackend.Infrastructure/Repositories/NoteRepository.cs)
- Execution flow: the current user’s note is fetched or updated in a single-user-per-note model

---

## 9. Architecture and Design Patterns

### Architecture Style
The project uses Clean Architecture with clear separation between:
- API / transport layer
- Application / use-case layer
- Domain model layer
- Infrastructure / persistence layer

### Design Patterns Used
- CQRS via MediatR for commands and queries
- Repository pattern for persistence abstraction
- Dependency injection for service wiring
- Unit of Work pattern through `IUnitOfWork`
- Middleware pattern for centralized exception handling
- DTO pattern for transport objects
- Validation pipeline behavior for request validation

### Folder Organization
The project is intentionally split by responsibility:
- `Api` for HTTP concerns
- `Application` for business logic and abstractions
- `Domain` for entities and enums
- `Infrastructure` for EF Core and implementation details
- `tests` for application-level unit tests

### Coding Standards and Best Practices
- Strong typing with nullable reference types enabled
- Explicit project boundaries and abstractions
- Use of DTOs to decouple API contract from domain entities
- Validation before persistence
- Consistent use of async/await for I/O-bound operations
- Centralized exception handling for predictable API responses

---

## 10. Developer Notes

### How to Run Locally
- Restore and build the solution.
- Start PostgreSQL locally or via Docker Compose.
- Set the connection string if necessary.
- Run the API with `dotnet run --project src/DevLoggerBackend.Api`.

### How to Work with Migrations
- Use `dotnet ef` from the repository root with the API project as the startup project.
- Migrations are applied at startup when enabled.

### How to Extend the Project
- Add a new feature by creating controllers in the API project, handlers/queries in the Application project, and repository implementations in Infrastructure.
- Register dependencies in the infrastructure DI class.
- Add validators to enforce input rules.

### Important Caveats
- The `VerifyAuthQuery` implementation is currently a placeholder and does not load a real user profile from JWT claims.
- The current token service is a placeholder-style implementation but is functional for baseline auth.
- The appsettings file contains concrete development credentials; this should be improved for production.

---

## 11. Additional Observations

### Performance Considerations
- Repository methods use `AsNoTracking()` where appropriate, which improves read-only query performance.
- Search operations are simple and database-driven, but adding indexes or full-text search could improve scalability for large datasets.

### Security Considerations
- Passwords are hashed with BCrypt.
- JWTs are used for authentication.
- Input is validated before handlers execute.
- However, production security should be strengthened by:
  - moving secrets out of source-controlled config
  - using stronger key rotation and secret management
  - enforcing role-based authorization more explicitly
  - carefully reviewing the `AllowedOrigins` policy

### Potential Improvements
- Implement actual user profile lookup from JWT claims in the auth verification query.
- Add role-based authorization rules for admin or lead-only endpoints.
- Introduce pagination for large daily log sets.
- Add integration tests around controllers and EF Core behavior.
- Replace placeholder token service with a production-grade token strategy if needed.
- Add explicit audit logging and audit trail features.

### Hidden Implementation Details
- The app seeds demo users during model creation.
- The `AppDbContext` automatically stamps timestamps whenever entities are added or modified.
- The API uses `CreatedAtAction` for resource creation responses.
- Notes are designed as a one-note-per-user model.

---

## Test Coverage Summary

The test project [tests/DevLoggerBackend.Application.Tests](tests/DevLoggerBackend.Application.Tests) covers selected application handlers:
- [tests/DevLoggerBackend.Application.Tests/Features/DailyLogs/Commands/CreateDailyLogCommandHandlerTests.cs](tests/DevLoggerBackend.Application.Tests/Features/DailyLogs/Commands/CreateDailyLogCommandHandlerTests.cs)
- [tests/DevLoggerBackend.Application.Tests/Features/DailyLogs/Queries/GetAllDailyLogsQueryHandlerTests.cs](tests/DevLoggerBackend.Application.Tests/Features/DailyLogs/Queries/GetAllDailyLogsQueryHandlerTests.cs)

These tests validate the basic behavior of the daily log creation and retrieval handlers using mocked repositories and services.
