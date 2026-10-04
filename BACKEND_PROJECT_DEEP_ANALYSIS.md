# DevLoggerBackend - Deep Technical Analysis

## Analysis scope and method

This document is based on the source, project files, configuration, migration files, tests, operational documentation, and runtime artifacts present in the repository at analysis time. The canonical application tree was analyzed from the repository root. Generated build output under `bin/` and `obj/`, Visual Studio state under `.vs/`, Git internals under `.git/`, and the tool-managed duplicate checkout under `.kilo/worktrees/` are identified but are not treated as application source.

The repository contains credential-like values in configuration, Docker settings, and seeded data. They are intentionally redacted here. No password, signing key, connection-string secret, token, or password hash is reproduced.

## 1. Executive summary

DevLoggerBackend is a single ASP.NET Core HTTP API for a developer/office journal. It supports:

- user registration and login;
- signed JWT authentication;
- authenticated daily-log CRUD and keyword/date search;
- one note per user, with read and upsert operations;
- PostgreSQL persistence through Entity Framework Core and Npgsql;
- structured console/file logging through Serilog;
- Prometheus HTTP metrics;
- Swagger/OpenAPI in development and, by the checked-in configuration, production;
- startup migration application;
- a small unit-test project covering two daily-log handlers.

The implementation is organized into four runtime projects:

1. `DevLoggerBackend.Api` owns hosting, HTTP controllers, middleware, configuration, authentication setup, Swagger, CORS, and metrics.
2. `DevLoggerBackend.Application` owns MediatR requests/handlers, validators, DTOs, application abstractions, and application exceptions.
3. `DevLoggerBackend.Domain` owns entities, the shared timestamp/id base class, and the user-role enum.
4. `DevLoggerBackend.Infrastructure` owns EF Core, PostgreSQL configuration, migrations, repository implementations, BCrypt hashing, JWT generation, and current-user resolution.

The actual request pattern is controller -> MediatR request -> validation pipeline -> handler -> repository/service abstraction -> EF Core `AppDbContext` -> PostgreSQL. The pattern is Clean/Layered Architecture with feature-oriented CQRS and the Mediator pattern; it is not a full domain-driven design implementation because the domain entities are mostly persistence-oriented data models and contain no domain methods.

The most important confirmed observations are:

- All four projects target `net10.0`; the README still describes the backend as .NET 8.
- JWT authentication is implemented with HMAC-SHA256, issuer/audience/lifetime/signature validation, and a `NameIdentifier` claim containing the user GUID.
- All current journal handlers enforce user ownership in application code before returning or mutating records.
- `IUnitOfWork` is implemented directly by `AppDbContext`; no explicit transaction abstraction or transaction boundary is defined.
- `AddHealthChecks()` is registered, but no health-check endpoint is mapped.
- The checked-in base configuration contains hard-coded database/JWT credential material and enables Swagger and migration-on-startup by default; these are security/deployment concerns.
- The code builds successfully. The test suite contains two passing tests, but coverage is narrow and there are no API, integration, repository, authentication, validation, migration, or middleware tests.

## 2. Complete repository structure

The canonical, application-relevant structure is:

```text
DevLoggerBackend/
├── DevLoggerBackend.sln
├── README.md
├── docker-compose.yml
├── DOCKER-GRAFANA-SUMMARY.md
├── OFFICE-JOURNAL-BACKEND-SUMMARY.md
├── monitoring/
│   └── prometheus.yml
├── src/
│   ├── DevLoggerBackend.Api/
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── DailyLogsController.cs
│   │   │   └── NotesController.cs
│   │   ├── Extensions/
│   │   │   └── WebApplicationExtensions.cs
│   │   ├── Middleware/
│   │   │   └── GlobalExceptionHandlingMiddleware.cs
│   │   ├── Models/
│   │   │   └── ErrorResponse.cs
│   │   ├── Properties/
│   │   │   └── launchSettings.json
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── DevLoggerBackend.Api.csproj
│   │   ├── Dockerfile
│   │   ├── Program.cs
│   │   └── logs/                         # ignored runtime log files observed locally
│   │       ├── devlogger-20260802.log
│   │       └── devlogger-20260929.log
│   ├── DevLoggerBackend.Application/
│   │   ├── Abstractions/
│   │   │   ├── Persistence/IUnitOfWork.cs
│   │   │   ├── Repositories/
│   │   │   │   ├── IDailyLogRepository.cs
│   │   │   │   ├── INoteRepository.cs
│   │   │   │   └── IUserRepository.cs
│   │   │   └── Services/
│   │   │       ├── ICurrentUserService.cs
│   │   │       ├── IPasswordHasher.cs
│   │   │       └── ITokenService.cs
│   │   ├── Common/
│   │   │   ├── Behaviors/ValidationBehavior.cs
│   │   │   ├── Exceptions/
│   │   │   │   ├── ConflictException.cs
│   │   │   │   ├── NotFoundException.cs
│   │   │   │   └── UnauthorizedException.cs
│   │   │   └── Models/PagedResult.cs
│   │   ├── Features/
│   │   │   ├── Auth/
│   │   │   │   ├── Commands/LoginCommand.cs
│   │   │   │   ├── Commands/RegisterCommand.cs
│   │   │   │   ├── Dtos/{LoginRequestDto,LoginResponseDto,RegisterRequestDto,UserDto}.cs
│   │   │   │   ├── Queries/VerifyAuthQuery.cs
│   │   │   │   └── Validators/{LoginCommandValidator,RegisterCommandValidator}.cs
│   │   │   ├── DailyLogs/
│   │   │   │   ├── Commands/{CreateDailyLogCommand,DeleteDailyLogCommand,UpdateDailyLogCommand}.cs
│   │   │   │   ├── Dtos/{CreateDailyLogDto,DailyLogDto,SearchDailyLogsRequestDto}.cs
│   │   │   │   ├── Queries/{GetAllDailyLogsQuery,GetDailyLogByIdQuery,SearchDailyLogsQuery}.cs
│   │   │   │   └── Validators/{CreateDailyLogCommandValidator,UpdateDailyLogCommandValidator}.cs
│   │   │   └── Notes/
│   │   │       ├── Commands/SaveNoteCommand.cs
│   │   │       ├── Dtos/NoteDto.cs
│   │   │       ├── Queries/GetNoteQuery.cs
│   │   │       └── Validators/SaveNoteCommandValidator.cs
│   │   ├── DependencyInjection.cs
│   │   └── DevLoggerBackend.Application.csproj
│   ├── DevLoggerBackend.Domain/
│   │   ├── Common/BaseEntity.cs
│   │   ├── Entities/{DailyLog,Note,User}.cs
│   │   ├── Enums/UserRole.cs
│   │   └── DevLoggerBackend.Domain.csproj
│   └── DevLoggerBackend.Infrastructure/
│       ├── Migrations/
│       │   ├── 20260725125919_InitialCreate.cs
│       │   ├── 20260725125919_InitialCreate.Designer.cs
│       │   └── AppDbContextModelSnapshot.cs
│       ├── Persistence/
│       │   ├── AppDbContext.cs
│       │   └── Configurations/{DailyLogConfiguration,NoteConfiguration,UserConfiguration}.cs
│       ├── Repositories/{DailyLogRepository,NoteRepository,UserRepository}.cs
│       ├── Services/{BcryptPasswordHasher,CurrentUserService,PlaceholderTokenService}.cs
│       ├── DependencyInjection.cs
│       └── DevLoggerBackend.Infrastructure.csproj
├── tests/
│   └── DevLoggerBackend.Application.Tests/
│       ├── Features/DailyLogs/Commands/CreateDailyLogCommandHandlerTests.cs
│       ├── Features/DailyLogs/Queries/GetAllDailyLogsQueryHandlerTests.cs
│       └── DevLoggerBackend.Application.Tests.csproj
├── .github/workflows/                    # directory exists but contains no workflow files
├── .gitignore
└── .gitattributes
```

The repository has 82 tracked files according to `git ls-files`. The `logs/` files are ignored by the repository rules and are runtime artifacts rather than tracked source. `bin/` and `obj/` directories were present/created as generated build output and are ignored. `.vs/`, `.git/`, and `.kilo/` are workspace/tool metadata; `.kilo/worktrees/ionized-canidae` contains a duplicate managed checkout and is not an additional runtime project.

## 3. Folder-by-folder responsibilities

### Root

- `DevLoggerBackend.sln` groups four source projects and one test project in Visual Studio solution folders named `src` and `tests`.
- `README.md` gives local build/run instructions, Docker usage, EF migration commands, frontend base URL guidance, and Render deployment notes. Its .NET 8 description is stale relative to the project files, which target .NET 10.
- `docker-compose.yml` defines only PostgreSQL and the API. It does not define Prometheus or Grafana services.
- `DOCKER-GRAFANA-SUMMARY.md` documents a Prometheus/Grafana setup that is external to the checked-in Compose services.
- `OFFICE-JOURNAL-BACKEND-SUMMARY.md` is a pre-existing project summary. It is documentation, not runtime input.
- `monitoring/prometheus.yml` is the Prometheus scraper configuration.
- `.gitignore` excludes .NET build output, IDE state, logs, test results, packages, and common temporary files. In particular, `*.log` and `logs/` are ignored.
- `.gitattributes` enables automatic text line-ending normalization and contains commented Git merge/diff guidance.
- `.github/workflows` is empty, so no CI/CD workflow is defined in the visible repository.

### API project

`DevLoggerBackend.Api` is the composition root. It knows about the Application and Infrastructure projects, the HTTP framework, authentication, Swagger, Serilog, and Prometheus. It should not contain database query details; the checked-in code keeps those details in Infrastructure.

`Controllers/` contains attribute-routed MVC controllers. `Extensions/` centralizes reusable pipeline/startup extension methods. `Middleware/` contains the global exception wrapper. `Models/` contains the API error envelope. `Properties/launchSettings.json` is local launch metadata. `logs/` contains ignored output generated by local executions.

### Application project

`DevLoggerBackend.Application` is the use-case layer. `Abstractions/` defines ports that Infrastructure implements. `Common/` contains cross-cutting validation, exceptions, and an unused generic paging model. `Features/` groups requests, handlers, DTOs, and validators by business feature rather than by technical type alone.

### Domain project

`DevLoggerBackend.Domain` is the dependency-light model layer. It contains no external package references and defines persistence entities, the common audit/id base class, and `UserRole`. The entities expose public setters and navigation properties and do not contain domain methods or invariants.

### Infrastructure project

`DevLoggerBackend.Infrastructure` implements the Application ports. `Persistence/` configures EF Core and entity mappings. `Migrations/` contains one initial PostgreSQL migration plus generated model metadata. `Repositories/` contains EF Core query/write adapters. `Services/` contains BCrypt, JWT, and HTTP-context implementations. `DependencyInjection.cs` is the Infrastructure composition extension.

### Tests

`DevLoggerBackend.Application.Tests` is a unit-test project referencing Application and Domain only. It uses xUnit, Moq, and FluentAssertions. The tests do not start ASP.NET, EF Core, PostgreSQL, or the actual DI container.

## 4. Project and dependency analysis

### Project references

```text
DevLoggerBackend.Api
├── DevLoggerBackend.Application
└── DevLoggerBackend.Infrastructure
    └── DevLoggerBackend.Application
        └── DevLoggerBackend.Domain

DevLoggerBackend.Infrastructure
└── DevLoggerBackend.Domain

DevLoggerBackend.Application.Tests
├── DevLoggerBackend.Application
└── DevLoggerBackend.Domain
```

The API references Infrastructure directly because it is the composition root and must call `AddInfrastructure`. Infrastructure references Application to implement its abstractions. Application references Domain to use entities and enums. Domain has no project dependency.

### Package references

`DevLoggerBackend.Api.csproj` targets `net10.0`, enables nullable reference types and implicit usings, emits XML documentation, and references:

- `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.0;
- `Microsoft.EntityFrameworkCore.Design` 10.0.0 as a private design-time dependency;
- `prometheus-net.AspNetCore` 8.2.1;
- `Serilog.AspNetCore` 8.0.3;
- `Serilog.Sinks.Console` 5.0.1;
- `Serilog.Sinks.File` 5.0.0;
- `Swashbuckle.AspNetCore` 6.8.1;
- `System.IdentityModel.Tokens.Jwt` 8.16.0.

`DevLoggerBackend.Application.csproj` references Domain and uses FluentValidation 11.11.0, its DI extensions, MediatR 12.4.1, and Microsoft DI abstractions 8.0.2.

`DevLoggerBackend.Infrastructure.csproj` references Application and Domain and uses BCrypt.Net-Next 4.0.3, EF Core and EF Core Design 10.0.0, Npgsql EF Core PostgreSQL 10.0.0, configuration and DI abstractions 10.0.0, and System.IdentityModel.Tokens.Jwt 8.16.0. It also uses the `Microsoft.AspNetCore.App` framework reference for HTTP abstractions.

The test project targets `net10.0`, is non-packable, and uses coverlet 6.0.4, FluentAssertions 6.12.1, Microsoft.NET.Test.Sdk 17.11.1, Moq 4.20.72, xUnit 2.9.2, and the Visual Studio xUnit runner 2.8.2.

The build completed successfully with two NU1510 warnings: Infrastructure explicitly references `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Configuration.Abstractions` even though those assemblies are already supplied by the framework/reference graph. These warnings do not currently prevent compilation.

## 5. File-by-file analysis

### Root and operational files

#### `DevLoggerBackend.sln`

The solution defines the API, Application, Domain, Infrastructure, and Application.Tests projects. It nests the four runtime projects beneath `src` and the tests beneath `tests`, and defines Debug/Release configurations for Any CPU, x64, and x86. It contains no runtime behavior.

#### `README.md`

This is operational documentation. It instructs developers to restore, build, test, and run the API, identifies `http://localhost:5000/api` as the local base URL, documents `docker compose up --build`, shows `dotnet ef` migration commands, and describes Render environment variables. It does not configure the application. The instruction to run migrations safely should be reconciled with the checked-in `ApplyMigrationsOnStartup` setting and deployment policy.

#### `docker-compose.yml`

The `postgres` service uses PostgreSQL 16, exposes port 5432, and persists data in the named `postgres-data` volume. The `api` service builds from the repository root using `src/DevLoggerBackend.Api/Dockerfile`, depends on the Postgres container, exposes port 5000, sets Development environment, injects a container-local connection string, and sets one CORS origin. `depends_on` controls startup order only; it does not prove that PostgreSQL is ready when the API begins migration. The Compose credentials are development values and should not be treated as production secrets.

#### `monitoring/prometheus.yml`

Prometheus scrapes itself and `host.docker.internal:5000` every 15 seconds. The target assumes the API is running on the host at port 5000, which is different from scraping the Compose `api` service by its Docker service name. This configuration is suitable for a host-run API plus containerized Prometheus, not automatically for the two-service Compose setup.

#### `DOCKER-GRAFANA-SUMMARY.md`

This documentation describes the intended chain `ASP.NET Core API -> /metrics -> Prometheus -> Grafana`, example PromQL panels, and local URLs. It claims Prometheus/Grafana integration was completed, but the repository's `docker-compose.yml` contains no Prometheus or Grafana services or provisioning files. The actual code contribution to this chain is the Prometheus middleware/package and `MapMetrics()` in `Program.cs`.

#### `OFFICE-JOURNAL-BACKEND-SUMMARY.md`

This is an existing technical summary containing architecture, file, flow, and observation notes. It is not loaded by the application. Its statements were checked against current source during this analysis; where source behavior is authoritative, this document follows the source.

#### `.gitattributes` and `.gitignore`

These files control repository behavior only. `.gitignore` excludes logs and generated output, which explains why the local `src/DevLoggerBackend.Api/logs/` files are present but not in the tracked file list. No application settings are read from either file.

### API files

#### `src/DevLoggerBackend.Api/Program.cs`

This is the top-level host bootstrapper. It:

1. creates `WebApplicationBuilder`;
2. reads `PORT` and binds to `http://0.0.0.0:{PORT}` when set, otherwise binds to `http://localhost:5000`;
3. configures Serilog from application configuration and DI services, enriches from log context, and adds console plus daily rolling-file sinks;
4. invokes `AddApplication()` and `AddInfrastructure(configuration)`;
5. adds MVC controllers;
6. reads `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience` and configures the JWT bearer default authenticate/challenge schemes;
7. enables issuer, audience, lifetime, signing-key validation with a symmetric UTF-8 key;
8. adds endpoint discovery and Swagger with a Bearer security definition/requirement;
9. registers health checks;
10. builds an `AllowedOrigins` CORS policy allowing any method/header and credentials for configured origins;
11. builds the app;
12. enables Swagger when Development or `EnableSwaggerInProduction` is true and, in Development, attempts to open the local Swagger URL using the platform browser command;
13. decides whether to apply migrations from `ApplyMigrationsOnStartup`, defaulting to Development when the setting is absent;
14. applies migrations when enabled;
15. adds Prometheus HTTP metrics middleware;
16. applies `UseApiPipeline()`;
17. maps the Prometheus metrics endpoint; and
18. calls `Run()`.

The browser-opening callback intentionally swallows exceptions. `Jwt:Key` is null-forgiven when converted to bytes, so a missing key will fail at startup rather than produce a useful configuration validation error. Health checks are registered but no `MapHealthChecks(...)` call exists in the source.

#### `src/DevLoggerBackend.Api/Extensions/WebApplicationExtensions.cs`

`UseApiPipeline()` registers `GlobalExceptionHandlingMiddleware`, CORS, authentication, authorization, and controller endpoint mapping in that order. `ApplyDatabaseMigrationsAsync()` creates a service scope, obtains a startup logger and `AppDbContext`, and calls `Database.MigrateAsync()`. PostgreSQL SQL state `28P01` is logged and tolerated only in Development; in other environments the exception is rethrown and prevents successful startup. Other migration/connection errors are not specially handled.

#### `src/DevLoggerBackend.Api/Middleware/GlobalExceptionHandlingMiddleware.cs`

The middleware wraps the remainder of the request pipeline in a `try/catch`. It logs unhandled exceptions through `ILogger`, also writes the exception message, inner exception message, and full exception text to standard output, and calls `HandleExceptionAsync()`. It maps FluentValidation exceptions to 400 with grouped field errors, `NotFoundException` to 404, `UnauthorizedException` to 401, `ConflictException` to 409, and all other exceptions to 500 with a generic message. The response includes the ASP.NET trace identifier.

It serializes with a new default `System.Text.Json` serializer rather than the MVC serializer options. Therefore error property casing/options can differ from normal controller responses. Full exception printing is useful during development but can expose implementation details in captured logs.

#### `src/DevLoggerBackend.Api/Models/ErrorResponse.cs`

`ErrorResponse` is the error envelope with `StatusCode`, `Message`, `TraceId`, and nullable `Dictionary<string,string[]> Errors`. It has no behavior and is only instantiated by the global exception middleware.

#### `src/DevLoggerBackend.Api/Controllers/AuthController.cs`

The controller is routed as `api/AuthController` through `api/[controller]`, conventionally used as `/api/Auth`. It injects `IMediator` in its constructor. `Register()` accepts `RegisterRequestDto`, sends `RegisterCommand`, and returns 201 with a success message. `Login()` accepts `LoginRequestDto`, sends `LoginCommand`, and returns the `LoginResponseDto` with 200. `Verify()` is `[Authorize]`, receives `IUserRepository` and `ICurrentUserService` directly through `[FromServices]`, reads the `NameIdentifier`-derived current GUID, loads the user, maps a small anonymous profile response, and throws `UnauthorizedException` if the claim/user is unavailable. It does not send the existing `VerifyAuthQuery`; that query is a separate unused scaffold.

#### `src/DevLoggerBackend.Api/Controllers/DailyLogsController.cs`

The controller is `[Authorize]`, `[ApiController]`, and routed as `/api/DailyLogs`. It sends MediatR requests for list, detail, create, update, delete, and search. `GetAll()` returns 200 with a read-only list. `GetById(Guid)` returns 200 or allows the exception middleware to produce 404. `Create()` returns 201 using `CreatedAtAction` pointing to `GetById`. `Update()` returns 200. `Delete()` returns 204. `Search()` is a POST with a `SearchDailyLogsRequestDto` body and returns 200. Ownership, validation, persistence, and exception mapping are not performed in the controller.

#### `src/DevLoggerBackend.Api/Controllers/NotesController.cs`

The controller is `[Authorize]`, `[ApiController]`, and routed as `/api/Notes`. `Get()` sends `GetNoteQuery` and returns 204 when the current user has no note or 200 with `NoteDto`. `Save()` accepts `SaveNoteDto`, sends `SaveNoteCommand`, and returns 200 with the upserted note.

#### `src/DevLoggerBackend.Api/appsettings.json`

The base configuration defines a PostgreSQL `DefaultConnection`, JWT key/issuer/audience/expiry settings, two allowed frontend origins, production Swagger enabled, startup migrations enabled, and Serilog default/override levels. Secret-like values are present in the file and are not reproduced here. Because this file is a checked-in default and `ApplyMigrationsOnStartup` is explicitly true, deployment configuration must override it deliberately.

#### `src/DevLoggerBackend.Api/appsettings.Development.json`

This file overrides only Serilog's default minimum level to `Debug`. It is merged when the host environment is Development.

#### `src/DevLoggerBackend.Api/Properties/launchSettings.json`

The project launch profile uses the Project command, opens Swagger, binds to `http://localhost:5000`, and sets `ASPNETCORE_ENVIRONMENT=Development`. It is local tooling metadata and is not a production deployment configuration source.

#### `src/DevLoggerBackend.Api/DevLoggerBackend.Api.csproj`

The web SDK project targets .NET 10, enables nullable and implicit usings, emits documentation XML while suppressing missing-XML-doc warnings, and references Application and Infrastructure. Its package list provides JWT bearer authentication, EF design-time services, Prometheus metrics, Serilog, Swagger, and JWT token primitives.

#### `src/DevLoggerBackend.Api/Dockerfile`

The multi-stage Dockerfile uses the .NET 10 SDK to copy the repository, restore the solution, and publish the API without an app host. The final image uses the .NET 10 ASP.NET runtime, copies published output, exposes port 5000, sets `ASPNETCORE_URLS=http://+:5000`, and runs `DevLoggerBackend.Api.dll`.

### Application shared files

#### `src/DevLoggerBackend.Application/DependencyInjection.cs`

`AddApplication()` scans the Application assembly for MediatR handlers, scans it for FluentValidation validators, and registers `ValidationBehavior<,>` as a transient open-generic MediatR pipeline behavior.

#### `Common/Behaviors/ValidationBehavior.cs`

For each MediatR request, the behavior resolves all `IValidator<TRequest>` instances. It executes them concurrently with the request cancellation token, aggregates non-null failures, throws FluentValidation `ValidationException` when any failure exists, and otherwise invokes the next handler. This is the only registered pipeline behavior.

#### `Common/Exceptions/{ConflictException,NotFoundException,UnauthorizedException}.cs`

Each file defines a thin custom exception with a message-only constructor. The API middleware maps these exception types to 409, 404, and 401 respectively. They do not carry error codes, metadata, or inner exceptions.

#### `Common/Models/PagedResult.cs`

`PagedResult<T>` defines `Items`, `TotalCount`, `PageNumber`, and `PageSize` as init-only properties. No current query or controller uses it; current list/search endpoints return complete lists.

#### `Abstractions/Persistence/IUnitOfWork.cs`

The interface exposes only `SaveChangesAsync(CancellationToken)`. It is a persistence commit port, not a transaction API.

#### `Abstractions/Repositories/IDailyLogRepository.cs`

The port exposes all-log retrieval, user-scoped retrieval, ID retrieval, user-scoped search by keyword/date range, add, update, and remove. The application handlers use every method except `GetAllAsync`; the implementation nevertheless provides it.

#### `Abstractions/Repositories/INoteRepository.cs`

The port exposes current-user lookup and add. Updates occur by mutating the tracked entity returned from `GetByUserIdAsync`; there is no explicit update method.

#### `Abstractions/Repositories/IUserRepository.cs`

The port exposes add, email lookup, ID lookup, and an alphabetically first/default-user lookup. The default-user operation is not used by current application code.

#### `Abstractions/Services/ICurrentUserService.cs`

The interface exposes nullable `Guid? UserId`, allowing handlers to distinguish an authenticated request with a usable subject from a request with no recognized identity claim.

#### `Abstractions/Services/IPasswordHasher.cs`

The interface defines `Hash` and `Verify` operations so Application depends on password behavior rather than BCrypt directly.

#### `Abstractions/Services/ITokenService.cs`

The interface defines `GenerateToken(User user)`. The current implementation creates a signed JWT.

### Auth feature files

#### `Features/Auth/Commands/RegisterCommand.cs`

`RegisterCommand` carries name, email, password, and confirmation and returns MediatR `Unit`. The handler checks for an existing email, throws `ConflictException` if found, creates a new user with trimmed name, trimmed/lowercased email, BCrypt hash, Developer role, and UTC timestamps, adds it through `IUserRepository`, and commits through `IUnitOfWork`.

#### `Features/Auth/Commands/LoginCommand.cs`

`LoginCommand` carries email/password. The handler looks up by normalized email, verifies the password using `IPasswordHasher`, throws the same generic `UnauthorizedException` for missing user or invalid password, maps user identity/role to `UserDto`, and obtains a JWT from `ITokenService`. Senior Developer and Team Lead are formatted with spaces for the response; other enum names are returned as enum text.

#### `Features/Auth/Dtos/*.cs`

`LoginRequestDto` and `RegisterRequestDto` are mutable HTTP body models with default empty strings. `LoginResponseDto` contains a `UserDto` and token string. `UserDto` contains string ID, name, email, and role. DTOs have no data-annotation validation; validation is attached to MediatR commands.

#### `Features/Auth/Validators/*.cs`

`LoginCommandValidator` requires a nonempty, email-shaped email and a nonempty password. `RegisterCommandValidator` requires a nonempty name of at least two characters, a valid nonempty email, a password of at least eight characters, and a nonempty confirmation equal to the password. Maximum name/email lengths are not enforced here even though the database has name/email limits.

#### `Features/Auth/Queries/VerifyAuthQuery.cs`

This file defines `VerifyAuthQuery` and a handler returning `UserDto?`, but the handler always returns `null` and contains a TODO to load the authenticated user from JWT claims. No controller sends this query. The working `/api/Auth/verify` path is implemented directly in `AuthController` instead.

### Daily-log feature files

#### `Features/DailyLogs/Commands/CreateDailyLogCommand.cs`

The command wraps `CreateDailyLogDto`. The handler parses `LogDate`, creates a `DailyLog`, trims nullable textual fields where applicable, converts a blank Git URL to null, obtains the authenticated user GUID, adds the entity, commits, and maps it to `DailyLogDto`. It injects `IUserRepository` but never uses it; this is a confirmed unused dependency in the constructor/field set. Date parsing is repeated despite the validator already checking it.

#### `Features/DailyLogs/Commands/UpdateDailyLogCommand.cs`

The handler requires an authenticated user, loads the log by ID, and treats missing or foreign records as 404 to avoid exposing ownership information. It parses the date, replaces all editable fields, trims selected strings, normalizes blank GitLink to null, sets `UpdatedAtUtc`, calls repository `Update`, commits, and returns the mapped DTO.

#### `Features/DailyLogs/Commands/DeleteDailyLogCommand.cs`

The handler resolves the current user, loads the record by ID, checks ownership, removes it, commits, and returns `Unit`. Missing and foreign records both become the same 404 response.

#### `Features/DailyLogs/Dtos/*.cs`

`CreateDailyLogDto` contains string date/content fields and nullable GitLink. `DailyLogDto` exposes string ID/date/timestamps and all log content; `FromEntity` formats dates as `yyyy-MM-dd` and timestamps with round-trip `O` format. `SearchDailyLogsRequestDto` carries optional keyword, `DateFrom`, and `DateTo` strings.

#### `Features/DailyLogs/Queries/GetAllDailyLogsQuery.cs`

The handler resolves the current user, calls `GetByUserIdAsync`, maps each entity with `DailyLogDto.FromEntity`, and returns a list. Sorting is performed by the repository, not by the handler.

#### `Features/DailyLogs/Queries/GetDailyLogByIdQuery.cs`

The handler resolves the current user, fetches by ID, checks ownership, throws 404 for missing/foreign data, and maps the entity to a DTO.

#### `Features/DailyLogs/Queries/SearchDailyLogsQuery.cs`

The handler resolves the current user, parses nonblank date filters into nullable `DateOnly` values, silently treats invalid search dates as null, passes user/keyword/date bounds to the repository, and maps the results. It has no validator, so invalid date input does not produce a validation error.

#### `Features/DailyLogs/Validators/*.cs`

Create and update validators require a nonempty parseable date, reject dates after the current UTC date, require `TasksWorked`, and require an optional GitLink to be an absolute HTTP/HTTPS URI. The error messages state `YYYY-MM-DD`, but `DateOnly.TryParse` is used without an exact format, so the implementation accepts whatever formats the current culture/parser accepts. The validators do not impose maximum lengths on the large text fields or GitLink, although the database caps GitLink at 2048 characters.

### Notes feature files

#### `Features/Notes/Commands/SaveNoteCommand.cs`

The handler resolves the current user and looks up the user's existing note. If absent, it creates and adds a `Note`; if present, it mutates `Content`. It commits once and maps the entity to `NoteDto`. The database unique index enforces the one-note-per-user rule in addition to the read-then-insert application logic.

#### `Features/Notes/Dtos/NoteDto.cs`

`NoteDto` is a positional record containing GUID ID, content, and `UpdatedAtUtc`; `FromEntity` performs the mapping. `SaveNoteDto` is a positional record containing content.

#### `Features/Notes/Queries/GetNoteQuery.cs`

The handler resolves the current user, retrieves the note by user ID, and returns either null or a mapped DTO. The controller translates null to 204.

#### `Features/Notes/Validators/SaveNoteCommandValidator.cs`

The validator rejects null content and limits content to 100,000 characters. Empty content is allowed because there is no `NotEmpty()` rule.

### Domain files

#### `Domain/Common/BaseEntity.cs`

`BaseEntity` supplies GUID `Id`, `CreatedAtUtc`, and `UpdatedAtUtc` to all entities. `AppDbContext.SaveChangesAsync` is responsible for assigning/updating these audit timestamps.

#### `Domain/Entities/User.cs`

`User` extends `BaseEntity` with Name, Email, PasswordHash, Role, a collection of DailyLogs, and an optional Note navigation. It is the aggregate root for the relational relationships in the current model, although there are no aggregate methods.

#### `Domain/Entities/DailyLog.cs`

`DailyLog` extends `BaseEntity` with a `DateOnly` LogDate, five required string content fields, optional GitLink, UserId, and nullable User navigation.

#### `Domain/Entities/Note.cs`

`Note` extends `BaseEntity` with Content, UserId, and nullable User navigation. The relational mapping makes the user-to-note relationship one-to-one.

#### `Domain/Enums/UserRole.cs`

The integer-backed roles are Developer=1, SeniorDeveloper=2, TeamLead=3, and Manager=4. They are stored as integers by EF Core and emitted as strings in JWT role claims and selected API responses.

#### `Domain/DevLoggerBackend.Domain.csproj`

This SDK project targets .NET 10 with nullable and implicit usings. It has no package or project dependencies.

### Infrastructure files

#### `Infrastructure/DependencyInjection.cs`

`AddInfrastructure` resolves `DATABASE_URL` first and the named `ConnectionStrings:DefaultConnection` second. An absolute database URL is parsed into an Npgsql connection string with SSL required; a non-URI value is passed through unchanged. Missing configuration throws a clear `InvalidOperationException` during startup. EF Core is registered with Npgsql and `EnableRetryOnFailure`. `AppDbContext` is scoped and is also registered as `IUnitOfWork`; all repositories, the hash service, token service, and current-user service are scoped. `AddHttpContextAccessor()` supplies the HTTP context dependency.

#### `Infrastructure/Persistence/AppDbContext.cs`

`AppDbContext` exposes Users, DailyLogs, and Notes DbSets and implements `IUnitOfWork`. Its override of `SaveChangesAsync` assigns the same current UTC timestamp to both audit fields for Added `BaseEntity` objects and updates `UpdatedAtUtc` for Modified ones before delegating to EF. `OnModelCreating` applies all assembly configurations, seeds three users, and calls the base implementation. The seed timestamp is fixed, and the seeded password hashes are BCrypt hashes; no plaintext passwords are present in the model.

#### `Infrastructure/Persistence/Configurations/*.cs`

`UserConfiguration` maps `Users`, requires Name/Email/PasswordHash, limits Name to 200 and Email to 320, and creates a unique Email index.

`DailyLogConfiguration` maps `DailyLogs`, requires the main content fields, supplies an empty-string default for Tips, limits GitLink to 2048, maps required many-to-one User with cascade delete, and indexes LogDate. EF also creates the foreign-key index on UserId.

`NoteConfiguration` maps `Notes`, requires Content and limits it to 100,000, creates a unique UserId index, and maps a required one-to-one User relationship with cascade delete.

#### `Infrastructure/Repositories/DailyLogRepository.cs`

`GetAllAsync` returns all logs as no-tracking records ordered by descending LogDate but is not called by current handlers. `GetByUserIdAsync` applies user filtering, no tracking, and descending LogDate order. `GetByIdAsync` returns the first matching entity with tracking, which supports later update/remove operations. `SearchByUserIdAsync` starts with a user filter, optionally searches lowercased content fields, GitLink, and `LogDate.ToString()`, optionally applies inclusive date bounds, uses no tracking, orders descending by LogDate, and materializes asynchronously. Add uses `AddAsync`; Update and Remove call the corresponding DbSet methods.

#### `Infrastructure/Repositories/NoteRepository.cs`

`GetByUserIdAsync` uses `SingleOrDefaultAsync`, relying on the unique UserId index; Add calls `AddAsync`. Existing note updates happen through the tracked entity and need no repository Update method.

#### `Infrastructure/Repositories/UserRepository.cs`

`AddAsync` adds a user. `GetByEmailAsync` trims/lowercases the supplied email and compares it with a lowercased database email. `GetByIdAsync` matches the GUID. `GetDefaultUserAsync` returns the alphabetically first email user and is currently unused. The lowercasing in the email predicate may affect index use depending on PostgreSQL/provider translation and collation/index design; no normalized-email column is present.

#### `Infrastructure/Services/BcryptPasswordHasher.cs`

This is the `IPasswordHasher` adapter around BCrypt.Net-Next `HashPassword` and `Verify`.

#### `Infrastructure/Services/PlaceholderTokenService.cs`

Despite its name, this implementation generates a real JWT. It reads key, issuer, audience, and integer expiry minutes from configuration, creates NameIdentifier, Email, and Role claims, signs with HMAC-SHA256, sets UTC expiry, and serializes the token. Missing or nonnumeric expiry configuration causes `int.Parse` to throw when login attempts token generation.

#### `Infrastructure/Services/CurrentUserService.cs`

This service reads `ClaimTypes.NameIdentifier` from `HttpContext.User`. Missing claim produces null; a present claim is parsed with `Guid.Parse`. The JWT generator uses this exact claim type, so the handler ownership checks depend on that contract.

#### `Infrastructure/DevLoggerBackend.Infrastructure.csproj`

This project targets .NET 10 with nullable and implicit usings, references Application and Domain, and carries the PostgreSQL, EF Core, BCrypt, JWT, configuration, DI, and ASP.NET framework dependencies required by the implementations.

#### `Infrastructure/Migrations/20260725125919_InitialCreate.cs`

The only migration creates Users, DailyLogs, and Notes with PostgreSQL UUID/date/timestamp types, required columns, limits, foreign keys, cascading deletes, unique indexes for user email and note ownership, and indexes for DailyLog LogDate/UserId. It inserts three example users with precomputed password hashes and roles Developer, SeniorDeveloper, and TeamLead. `Down` drops DailyLogs and Notes before Users. No stored procedures, views, raw SQL, triggers, or later migrations are present.

#### `Infrastructure/Migrations/20260725125919_InitialCreate.Designer.cs` and `AppDbContextModelSnapshot.cs`

These are EF-generated model metadata. They mirror the current three-entity schema, relationships, indexes, seed rows, Npgsql provider annotations, and EF product version 10.0.0. They are not hand-authored request logic.

### Test files

#### `tests/DevLoggerBackend.Application.Tests/Features/DailyLogs/Commands/CreateDailyLogCommandHandlerTests.cs`

This unit test mocks the daily-log repository, user repository, unit of work, and current-user service, supplies a valid payload, invokes `CreateDailyLogCommandHandler`, asserts the mapped task text, and verifies exactly one repository add and one save. It does not assert user ID, field trimming, invalid dates, future-date rejection, GitLink normalization, or validation pipeline behavior. The mocked `IUserRepository` is unused, matching the production handler's unused dependency.

#### `tests/DevLoggerBackend.Application.Tests/Features/DailyLogs/Queries/GetAllDailyLogsQueryHandlerTests.cs`

This unit test supplies one in-memory log from a mocked user-scoped repository and a mocked current user, invokes the handler, and asserts one mapped result and its task text. The test name says sorted, but it supplies only one record and therefore does not verify ordering; ordering is actually implemented in the repository, which this unit test mocks.

#### `tests/DevLoggerBackend.Application.Tests/DevLoggerBackend.Application.Tests.csproj`

This is a non-packable .NET 10 xUnit project with implicit xUnit using and references only Application and Domain. It intentionally avoids Infrastructure, so database behavior is not covered.

## 6. Application startup flow

The exact startup sequence in the source is:

```text
Process starts
  -> WebApplication.CreateBuilder(args)
  -> PORT decides host URL (0.0.0.0:PORT or localhost:5000)
  -> Serilog host logger configured
  -> AddApplication()
       -> MediatR handler scan
       -> FluentValidation scan
       -> ValidationBehavior registration
  -> AddInfrastructure(configuration)
       -> connection string resolution
       -> scoped AppDbContext/IUnitOfWork
       -> repositories and concrete services
  -> MVC controllers
  -> JWT bearer authentication
  -> Swagger/OpenAPI
  -> health-check service registration
  -> CORS policy
  -> builder.Build()
  -> optional Swagger UI/browser open
  -> optional scoped Database.MigrateAsync()
  -> HTTP metrics middleware
  -> global exception/CORS/auth/authorization/controller pipeline
  -> /metrics mapping
  -> Run()
```

Database migrations run before the server begins accepting normal requests when enabled. Development-only PostgreSQL authentication failures are tolerated, allowing the host to start without a migrated/usable database. The application does not define an explicit readiness gate for the database.

## 7. Architecture and design patterns

### Clean/Layered Architecture

The dependency direction is inward: API depends on Application and Infrastructure; Infrastructure depends on Application abstractions and Domain; Application depends on Domain; Domain depends on nothing. This is consistent with Clean/Layered Architecture, although the API directly references Infrastructure as expected for a composition root.

### Feature-oriented CQRS

Commands and queries are separated under each feature. Commands mutate state (`Register`, `CreateDailyLog`, `UpdateDailyLog`, `DeleteDailyLog`, `SaveNote`); queries read state (`GetAllDailyLogs`, `GetDailyLogById`, `SearchDailyLogs`, `GetNote`). The separation is at the request/handler level, not at the database or deployment level: all handlers use the same DbContext/database and there are no separate read/write models.

### Mediator pattern

Controllers send MediatR requests rather than calling handlers directly. MediatR scans the Application assembly and resolves one handler per request. Auth verification is a deviation: its current controller path directly injects and calls repository/service ports instead of using the unused `VerifyAuthQuery`.

### Repository pattern

Application declares repository interfaces and Infrastructure implements them with EF Core. The repositories are thin query/write adapters. They do not contain a generic repository base class, specification objects, or domain query objects.

### Unit of Work

`IUnitOfWork` is implemented by `AppDbContext`, so handler calls to `SaveChangesAsync` commit all tracked changes in the scoped context. This provides a commit abstraction but not explicit transaction begin/commit/rollback methods. EF Core's normal save operation supplies its own database transaction behavior for a save operation; no multi-step explicit transaction is visible in the code.

### Dependency injection

All significant runtime components are constructor-injected except `AuthController.Verify`, which uses method-level `[FromServices]` injection. No handler manually constructs a repository, DbContext, password hasher, or token service. The token service and current-user service read configuration/HTTP context internally, as their infrastructure responsibilities require.

### Domain model

There are entities, navigation properties, timestamps, relationships, and an enum, but no aggregate methods, value objects, domain events, domain services, or domain-level invariants. Domain-driven design concepts are therefore limited to the entity/relationship model.

### Patterns not present

No background worker, message bus, event handler, outbox, cache, external email/SMS/payment integration, explicit policy authorization, refresh token, distributed tracing, or explicit metrics registration beyond Prometheus HTTP metrics was found. If a feature cannot be located in the source, its implementation could not be determined from the available source code.

## 8. Dependency injection map

| Registration | Lifetime | Implementation | Consumers / purpose |
|---|---:|---|---|
| `IPipelineBehavior<,>` | Transient | `ValidationBehavior<,>` | All MediatR requests; runs validators before handlers |
| `IUnitOfWork` | Scoped | `AppDbContext` | Register, daily-log mutations, note save |
| `IDailyLogRepository` | Scoped | `DailyLogRepository` | Daily-log handlers |
| `INoteRepository` | Scoped | `NoteRepository` | Note query/save handlers |
| `IUserRepository` | Scoped | `UserRepository` | Auth handlers and `/verify` |
| `IPasswordHasher` | Scoped | `BcryptPasswordHasher` | Registration and login |
| `ITokenService` | Scoped | `PlaceholderTokenService` | Login |
| `ICurrentUserService` | Scoped | `CurrentUserService` | Auth verification and all authenticated journal handlers |
| `IHttpContextAccessor` | Framework registration | Framework accessor | `CurrentUserService` |
| `AppDbContext` | Scoped | EF Core context | Repositories and unit of work |

MediatR handlers and validators are discovered by assembly scanning. MVC controllers are discovered by `AddControllers`. Authentication and authorization services are registered through ASP.NET Core extension methods. The source contains no singleton application service and no transient repository. The only confirmed manually created object in the request path is `new` entity/DTO/command construction by handlers/controllers; infrastructure services are resolved through DI.

## 9. API surface

The controllers use `[ApiController]`, so normal body binding and framework model-state behavior apply in addition to MediatR validation. The routes are conventionally case-insensitive in ASP.NET Core, but their declared route templates are shown exactly below.

| Method | Declared route | Auth | Request | MediatR/use case | Success |
|---|---|---|---|---|---|
| POST | `/api/Auth/register` | Public | `RegisterRequestDto` | `RegisterCommand` | 201 + message |
| POST | `/api/Auth/login` | Public | `LoginRequestDto` | `LoginCommand` | 200 + `LoginResponseDto` |
| GET | `/api/Auth/verify` | JWT required | none | Direct repository/current-user lookup | 200 + anonymous profile |
| GET | `/api/DailyLogs` | JWT required | none | `GetAllDailyLogsQuery` | 200 + list |
| GET | `/api/DailyLogs/{id:guid}` | JWT required | GUID route | `GetDailyLogByIdQuery` | 200 + DTO |
| POST | `/api/DailyLogs` | JWT required | `CreateDailyLogDto` | `CreateDailyLogCommand` | 201 + DTO/location |
| PUT | `/api/DailyLogs/{id:guid}` | JWT required | `CreateDailyLogDto` | `UpdateDailyLogCommand` | 200 + DTO |
| DELETE | `/api/DailyLogs/{id:guid}` | JWT required | GUID route | `DeleteDailyLogCommand` | 204 |
| POST | `/api/DailyLogs/search` | JWT required | `SearchDailyLogsRequestDto` | `SearchDailyLogsQuery` | 200 + list |
| GET | `/api/Notes` | JWT required | none | `GetNoteQuery` | 200 or 204 |
| PUT | `/api/Notes` | JWT required | `SaveNoteDto` | `SaveNoteCommand` | 200 + DTO |

Known error behavior is:

- validation failures from MediatR or manually thrown FluentValidation exceptions -> 400;
- missing/foreign daily logs -> 404;
- duplicate registration -> 409;
- missing/invalid current identity in application logic -> 401;
- invalid/missing JWT at the authentication layer -> framework challenge behavior, normally 401;
- unexpected exceptions/database failures -> 500 envelope from the middleware, unless they occur before the middleware pipeline is active during startup.

There are no versioned routes, admin routes, role-specific routes, pagination parameters in active endpoints, or deprecated routes identified in source. The unused `VerifyAuthQuery` is a stale query scaffold rather than an endpoint.

## 10. Request/response execution flows

### Registration

```text
POST /api/Auth/register
  -> AuthController.Register
  -> RegisterCommand(name, email, password, confirmPassword)
  -> ValidationBehavior
       -> RegisterCommandValidator
  -> RegisterCommandHandler
       -> IUserRepository.GetByEmailAsync
       -> ConflictException if existing
       -> new User with normalized email, BCrypt hash, Developer role
       -> IUserRepository.AddAsync
       -> IUnitOfWork.SaveChangesAsync
            -> AppDbContext timestamp interception
            -> PostgreSQL INSERT
  -> 201 { message }
```

The handler explicitly sets timestamps, then `AppDbContext` sets Added timestamps again to one `now` value. The unique email database index is a second line of defense against duplicates, but a concurrent duplicate can still surface as a database exception rather than the custom 409 path.

### Login and authenticated request

```text
POST /api/Auth/login
  -> LoginCommandValidator
  -> LoginCommandHandler
       -> UserRepository.GetByEmailAsync
       -> BCrypt.Verify
       -> PlaceholderTokenService.GenerateToken
            -> NameIdentifier, Email, Role claims
            -> HMAC-SHA256 signed JWT
  -> 200 LoginResponseDto

Authenticated journal request
  -> JwtBearer authentication validates signature/issuer/audience/lifetime
  -> [Authorize] succeeds
  -> CurrentUserService reads NameIdentifier claim
  -> handler compares that GUID to resource.UserId
  -> repository filters by current user or handler checks ownership
```

### Daily-log create

```text
POST /api/DailyLogs
  -> model binding CreateDailyLogDto
  -> CreateDailyLogCommand
  -> CreateDailyLogCommandValidator
  -> CreateDailyLogCommandHandler
       -> parse DateOnly
       -> resolve current UserId
       -> create DailyLog
       -> DailyLogRepository.AddAsync
       -> AppDbContext.SaveChangesAsync
       -> DailyLogDto.FromEntity
  -> 201 CreatedAtAction(/api/DailyLogs/{id})
```

### Daily-log read/search

`GetAllDailyLogsQueryHandler` resolves the current user and calls a filtered, no-tracking, descending-date repository query. The detail handler fetches by ID and then checks ownership in application code. Search parses optional bounds, passes them to `SearchByUserIdAsync`, and the repository applies user, keyword, inclusive date-range, no-tracking, and descending-date logic. All results are mapped to string-based DTOs.

### Daily-log update/delete

Both operations fetch by ID, require the current identity, and treat a missing or foreign row as 404. Update mutates all editable fields and saves; delete marks the entity Deleted and saves. A single scoped DbContext is used per request.

### Notes

`GET /api/Notes` resolves the user and performs a unique single-row lookup. `PUT /api/Notes` performs read-then-add-or-mutate and saves. The unique database index enforces one row per user. The source does not define a retry or conflict-recovery path for two simultaneous first saves.

## 11. CQRS, MediatR, validation, and handler map

| Request | Type | Handler | Validator | Persistence/service dependencies |
|---|---|---|---|---|
| `RegisterCommand` | Command | `RegisterCommandHandler` | `RegisterCommandValidator` | `IUserRepository`, `IPasswordHasher`, `IUnitOfWork` |
| `LoginCommand` | Command | `LoginCommandHandler` | `LoginCommandValidator` | `IUserRepository`, `IPasswordHasher`, `ITokenService` |
| `VerifyAuthQuery` | Query scaffold | `VerifyAuthQueryHandler` | None | None; returns null and is unused |
| `CreateDailyLogCommand` | Command | `CreateDailyLogCommandHandler` | `CreateDailyLogCommandValidator` | `IDailyLogRepository`, `IUnitOfWork`, `ICurrentUserService`, unused `IUserRepository` |
| `UpdateDailyLogCommand` | Command | `UpdateDailyLogCommandHandler` | `UpdateDailyLogCommandValidator` | `IDailyLogRepository`, `IUnitOfWork`, `ICurrentUserService` |
| `DeleteDailyLogCommand` | Command | `DeleteDailyLogCommandHandler` | None | `IDailyLogRepository`, `IUnitOfWork`, `ICurrentUserService` |
| `GetAllDailyLogsQuery` | Query | `GetAllDailyLogsQueryHandler` | None | `IDailyLogRepository`, `ICurrentUserService` |
| `GetDailyLogByIdQuery` | Query | `GetDailyLogByIdQueryHandler` | None | `IDailyLogRepository`, `ICurrentUserService` |
| `SearchDailyLogsQuery` | Query | `SearchDailyLogsQueryHandler` | None | `IDailyLogRepository`, `ICurrentUserService` |
| `SaveNoteCommand` | Command | `SaveNoteCommandHandler` | `SaveNoteCommandValidator` | `INoteRepository`, `IUnitOfWork`, `ICurrentUserService` |
| `GetNoteQuery` | Query | `GetNoteQueryHandler` | None | `INoteRepository`, `ICurrentUserService` |

For a request with a validator, the only visible behavior order is MediatR validation first and handler second. There are no authorization, transaction, logging, performance, or caching pipeline behaviors. ASP.NET authentication/authorization occurs before controller execution, outside the MediatR pipeline.

## 12. Database architecture

### Technology and connection

The database is PostgreSQL 16 in the provided Compose file. EF Core 10.0.0 with `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.0 maps the model. `DATABASE_URL` has precedence over `ConnectionStrings:DefaultConnection`. URI-style `DATABASE_URL` values are parsed into host, port, username, password, database, and `SslMode.Require`; non-URI values are passed through as Npgsql connection strings.

### Relational model

```text
Users (Id PK, Name, Email UNIQUE, PasswordHash, Role, CreatedAtUtc, UpdatedAtUtc)
  1 ├── * DailyLogs (Id PK, UserId FK CASCADE, LogDate, content..., timestamps)
  1 └── 1 Notes     (Id PK, UserId FK UNIQUE CASCADE, Content, timestamps)
```

Important schema details:

- all primary keys are UUID/GUID;
- `DailyLog.LogDate` is PostgreSQL `date`/CLR `DateOnly`;
- audit timestamps are PostgreSQL `timestamp with time zone` and are intended to be UTC;
- User Name maximum is 200; Email maximum is 320; GitLink maximum is 2048; Note Content maximum is 100,000;
- DailyLog main text fields are required but have no configured maximum length;
- User Email and Note UserId are unique;
- DailyLog LogDate and UserId are indexed, with the UserId index generated by the foreign key;
- relationships delete dependent daily logs/notes when a user is deleted;
- role is stored as integer values.

### Migration/seed flow

There is one initial migration, `20260725125919_InitialCreate`, and one model snapshot. On enabled startup, `MigrateAsync()` creates/updates the schema and inserts three demo users through EF seed data. The exact plaintext credentials for seed hashes are not documented here. No later migration, SQL script, stored procedure, trigger, or manual seeding service exists.

### Query/write behavior

Queries use EF LINQ and asynchronous materialization. List/search queries use `AsNoTracking`; ID lookups are tracked because update/delete handlers need to mutate/remove entities. Writes are staged through repositories and committed by `SaveChangesAsync`. No repository opens a connection or transaction directly.

### Transactions and concurrency

No explicit transaction or concurrency token is configured. Each command normally performs one `SaveChangesAsync` call in the scoped context. EF's normal save behavior handles the database operation, but the application has no cross-command transaction coordinator and no optimistic concurrency column.

## 13. Authentication and authorization

Authentication uses ASP.NET Core JWT bearer. The defaults are both JWT bearer. Token validation enables issuer, audience, lifetime, and signing-key validation. `PlaceholderTokenService` signs with the configured symmetric key using HMAC-SHA256 and sets the configured minute expiry.

The generated claims are:

- `ClaimTypes.NameIdentifier` -> user GUID;
- `ClaimTypes.Email` -> normalized user email;
- `ClaimTypes.Role` -> enum name.

`CurrentUserService` reads NameIdentifier and parses it as a GUID. DailyLogs and Notes controllers require `[Authorize]`; Auth verify also requires it. Registration and login are public because the Auth controller has no controller-level authorize attribute and those actions have no action-level attribute.

The role enum and role claim exist, but no `[Authorize(Roles = ...)]`, policy registration, permission table, or role-based branch is used by current endpoints. An authenticated user is therefore authorized to use all journal operations, with ownership enforced by user ID filtering/checks. There is no refresh-token flow, token revocation store, password reset, email verification, lockout, or external identity provider in the source.

## 14. Middleware and request pipeline

The effective order from `Program.cs` is:

```text
UseHttpMetrics()
  -> GlobalExceptionHandlingMiddleware
      -> UseCors("FrontendPolicy")
          -> UseAuthentication()
              -> UseAuthorization()
                  -> MapControllers()
  -> MapMetrics()
```

`UseHttpMetrics()` is registered before the custom pipeline. The global exception middleware is inside that metrics layer and wraps the downstream request path. CORS executes before authentication/authorization in the custom extension. Authentication populates `HttpContext.User`; authorization evaluates `[Authorize]`; MVC model binding and controller execution occur after that. Endpoint routing is mapped by `MapControllers()` inside the extension. Metrics endpoints are mapped after the extension and are not controller actions.

There is no custom compression, correlation-ID middleware, response envelope middleware, request-size middleware, rate limiting, health endpoint, or response caching. `TraceIdentifier` is used in errors, but no explicit correlation ID is generated or added to response headers/log scopes.

## 15. Configuration inventory

| Setting | Defined in | Consumed by | Behavior |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | `appsettings.json`, environment override | Infrastructure DI | PostgreSQL fallback when `DATABASE_URL` is absent |
| `DATABASE_URL` | environment/hosting | Infrastructure DI | Highest-precedence database source; absolute URI becomes SSL-required Npgsql string |
| `PORT` | environment/hosting | `Program.cs` | Host bind port; falls back to localhost:5000 |
| `Jwt:Key` | appsettings/environment | Program and token service | Symmetric signing/validation key; must be secret and externally managed |
| `Jwt:Issuer` | appsettings/environment | Program and token service | JWT issuer |
| `Jwt:Audience` | appsettings/environment | Program and token service | JWT audience |
| `Jwt:ExpiryMinutes` | appsettings/environment | token service | Parsed integer token lifetime |
| `AllowedOrigins` | appsettings/environment | Program | CORS origins with credentials enabled |
| `EnableSwaggerInProduction` | appsettings/environment | Program | Enables Swagger outside Development when true |
| `ApplyMigrationsOnStartup` | appsettings/environment | Program | Controls startup `MigrateAsync`; absent means Development-only default |
| `Serilog:MinimumLevel:*` | appsettings files | Serilog | Base and override log levels |
| `ASPNETCORE_ENVIRONMENT` | launch settings/Compose/hosting | ASP.NET host and Program | Selects Development behavior/configuration |
| `ConnectionStrings__DefaultConnection` | Compose/hosting | .NET configuration | Environment-form override of named connection string |
| `AllowedOrigins__0` | Compose/hosting | .NET configuration | Environment-form origin entry |

Configuration classes/options records are not present; settings are read directly by string key. There is no startup validation for required JWT settings, expiry range, origin shape, or key strength.

The checked-in configuration includes a database password and a JWT signing key. They are confirmed sensitive configuration values and should be rotated/moved to environment/secret storage. The Docker Compose password is also a development credential. This document intentionally does not disclose any of them.

## 16. External integrations, messaging, and background work

The application has these operational integrations:

- PostgreSQL via Npgsql/EF Core;
- Prometheus-compatible metrics via `prometheus-net.AspNetCore`;
- console and rolling-file logging via Serilog sinks;
- Swagger/OpenAPI generation via Swashbuckle;
- Docker/Compose for local packaging and orchestration;
- optional external hosting/database configuration described for Render.

There is no application client for email, SMS, payments, object storage, Redis, RabbitMQ, MassTransit, Kafka, a secret vault, or a third-party identity provider. There are no `BackgroundService`/`IHostedService` implementations, consumers, event handlers, scheduled jobs, queues, retry policies for messages, or dead-letter handling. EF's Npgsql `EnableRetryOnFailure` is the only explicit database retry configuration found.

Grafana is discussed in documentation, but no Grafana dashboard JSON, provisioning file, Compose service, or application code is present. Prometheus scraping must therefore be deployed/configured separately.

## 17. Logging, metrics, and observability

Serilog is installed as the host logger. It reads configuration levels, reads DI services, enriches from `LogContext`, writes to console, and writes to `logs/devlogger-.log` with daily rolling intervals. Development lowers the default level to Debug. Microsoft ASP.NET Core and EF Core are overridden to Warning by the base settings unless overridden.

The exception middleware logs unhandled exceptions with `ILogger` and also writes a full diagnostic block to console. Local logs show PostgreSQL authentication failures during migration and, on one run, a port-binding conflict; these are historical runtime observations, not additional code paths.

Prometheus integration consists of `app.UseHttpMetrics()` and `app.MapMetrics()`. The documentation names common HTTP/process/.NET/EF/Npgsql metrics, while the exact emitted set is supplied by the package at runtime. The `/metrics` endpoint is not protected by `[Authorize]` in the visible code. `AddHealthChecks()` is called, but no health check is configured and no health endpoint is mapped.

No explicit OpenTelemetry, distributed tracing, metrics business counters, correlation middleware, or structured request ID propagation was found. Error responses do include the framework `TraceIdentifier`.

## 18. Validation and error handling

Validation runs only for MediatR request types with registered validators. It is asynchronous, aggregates all validator failures, and stops the handler when any failure exists. Auth, create-log, update-log, and save-note commands have validators. Delete, reads, and search have no validator.

Handlers also perform imperative checks:

- registration duplicate email -> ConflictException;
- login bad credentials -> UnauthorizedException;
- all journal operations without current identity -> UnauthorizedException;
- missing/foreign log -> NotFoundException;
- create/update date parse failure -> FluentValidation.ValidationException;
- note absence is normal and becomes HTTP 204.

The global middleware is the single mapping point for those application exceptions. ASP.NET authentication failures happen in the framework before controller execution. Database unique violations, malformed GUID claims, missing JWT configuration, and other unrecognized failures become generic 500 responses if they occur inside the request pipeline.

## 19. Business rules

Confirmed business rules and their locations:

- new users default to `UserRole.Developer` (`RegisterCommandHandler`);
- email is trimmed/lowercased at registration and normalized for lookup (`RegisterCommandHandler`, `UserRepository`);
- duplicate email registration is rejected at application level and constrained at database level;
- passwords must be at least eight characters and confirmation must match (`RegisterCommandValidator`); storage uses BCrypt hashes;
- login uses a generic invalid-email-or-password message, avoiding distinction between missing account and wrong password;
- only authenticated users may read/write journal records (`[Authorize]` plus `CurrentUserService` checks);
- journal records are user-owned; list/search queries filter by user, while detail/update/delete additionally compare the entity owner;
- daily-log date cannot be future by the validator; the handler parses it again;
- daily-log GitLink, when supplied, must be absolute HTTP/HTTPS in create/update validation;
- blank GitLink is persisted as null;
- daily-log response dates use `yyyy-MM-dd`, and response timestamps use round-trip format;
- one note is allowed per user, enforced through one-to-one mapping and unique index;
- note content maximum is 100,000 characters; empty content is permitted;
- deleting a user cascades to daily logs and note at database relationship level.

## 20. Data flow and important relationships

### Interface-to-implementation map

```text
IUnitOfWork             -> AppDbContext
IDailyLogRepository     -> DailyLogRepository
INoteRepository         -> NoteRepository
IUserRepository         -> UserRepository
IPasswordHasher         -> BcryptPasswordHasher
ITokenService           -> PlaceholderTokenService
ICurrentUserService     -> CurrentUserService
```

### Controller-to-handler map

```text
AuthController.Register -> RegisterCommandHandler
AuthController.Login    -> LoginCommandHandler
AuthController.Verify   -> IUserRepository + ICurrentUserService directly

DailyLogsController.GetAll   -> GetAllDailyLogsQueryHandler
DailyLogsController.GetById  -> GetDailyLogByIdQueryHandler
DailyLogsController.Create   -> CreateDailyLogCommandHandler
DailyLogsController.Update   -> UpdateDailyLogCommandHandler
DailyLogsController.Delete   -> DeleteDailyLogCommandHandler
DailyLogsController.Search   -> SearchDailyLogsQueryHandler

NotesController.Get  -> GetNoteQueryHandler
NotesController.Save -> SaveNoteCommandHandler
```

### Handler-to-persistence map

```text
Register -> IUserRepository -> AppDbContext.Users -> Users
Login    -> IUserRepository -> AppDbContext.Users -> Users
          -> IPasswordHasher / ITokenService

Daily log reads -> IDailyLogRepository -> AppDbContext.DailyLogs -> DailyLogs
Daily log writes -> IDailyLogRepository + IUnitOfWork -> EF change tracker -> DailyLogs

Note read/write -> INoteRepository + IUnitOfWork -> AppDbContext.Notes -> Notes
```

### Entity-to-response map

`DailyLogDto.FromEntity` maps entity GUID/date/timestamps to strings and copies journal content. `NoteDto.FromEntity` maps the entity to a positional record and preserves `DateTime` as a JSON date value. Auth maps `User` to string-based `UserDto`; `Verify()` uses an anonymous object instead of `UserDto`.

## 21. Potential issues and observations

The categories below distinguish direct observations from conclusions that depend on deployment/provider behavior.

### Confirmed issues or risks

1. **Sensitive values are checked into `appsettings.json` and Compose configuration.** A database credential and JWT signing key are present in the repository configuration. They should be treated as exposed, rotated, and replaced by secret/environment management.
2. **Swagger is enabled in production by the checked-in setting.** This exposes an interactive API description in production unless deployment overrides it.
3. **Migrations run on startup by default.** This couples application startup to database schema changes and can create multi-instance deployment races or startup outages. The code rethrows PostgreSQL authentication failures outside Development.
4. **Health checks are not exposed.** `AddHealthChecks()` is registered, but there is no mapped health route, so an orchestrator cannot call a built-in health endpoint from this application.
5. **No role-based authorization is applied.** Roles are stored and placed in claims, but all authenticated roles currently receive the same endpoint authorization.
6. **The note upsert has a read-then-insert race.** Two simultaneous first saves for one user can both observe no row; the unique index protects the database, but the request has no unique-violation recovery path.
7. **The repository search has no pagination.** Current list/search endpoints materialize all matching rows into memory and return them in one response; `PagedResult<T>` exists but is unused.
8. **The visible test suite is very small.** Only two handler tests pass; important security, persistence, endpoint, and failure paths are untested.

### Confirmed dead/scaffolded code

- `VerifyAuthQueryHandler` always returns null and is not called.
- `IUserRepository.GetDefaultUserAsync` and `UserRepository.GetDefaultUserAsync` are not called.
- `IDailyLogRepository.GetAllAsync` and its implementation are not called by current handlers.
- `PagedResult<T>` is not used.
- `CreateDailyLogCommandHandler` injects `IUserRepository` but never uses it.

### Potential performance/provider concerns

- `UserRepository.GetByEmailAsync` compares `x.Email.ToLower()` to a normalized value, which may prevent use of the ordinary unique email index unless the provider translates/optimizes it accordingly. A normalized stored column or provider-appropriate case-insensitive strategy is not present.
- `SearchByUserIdAsync` lowercases multiple columns and searches with `Contains`, which is unlikely to use ordinary B-tree indexes for substring search. It also searches `LogDate.ToString()`, whose SQL translation/provider behavior should be verified against the deployed Npgsql version.
- DailyLog has separate UserId and LogDate indexes but no composite user/date index. The usefulness of the existing indexes depends on query plans and data volume.
- List/search responses have no limit or pagination and can grow without a bounded response size.
- Repository ID lookups are tracked even for read-only detail and verify paths; this is not incorrect, but no-tracking could reduce tracking overhead for read-only paths.

### Potential correctness/maintainability concerns

- Validation messages promise `YYYY-MM-DD`, but validators use general `DateOnly.TryParse` rather than an exact invariant format. Search invalid dates are silently ignored, while create/update invalid dates are rejected.
- Name and email database maxima are not mirrored in request validators, so overlong values reach the database and may become 500 responses instead of clean validation failures.
- DailyLog content fields are required at database level but only `TasksWorked` is required in the command validators; empty strings are valid for the other fields.
- `CurrentUserService` uses `Guid.Parse` and can throw for a malformed signed claim. A defensive `TryParse` path is not present.
- The API error middleware uses a separate default JSON serializer, so casing/options may differ from MVC responses.
- The class name `PlaceholderTokenService` is misleading because the implementation is the real JWT generator.
- `CreateDailyLogCommandHandler` explicitly sets timestamps that `AppDbContext` overwrites for Added entities, creating duplicated responsibility.
- The test named `Handle_ShouldReturnLogsSortedFromRepositoryResult` does not test sorting and the handler itself does not sort.

### Historical runtime observations

The ignored local logs show a PostgreSQL authentication failure during startup migration. Development correctly continued after that specific error, as coded. One earlier run also failed to bind port 5000 because another process already used it. These are environmental observations, not proof of a code defect in every deployment.

## 22. Technical debt

The principal debt is incomplete feature cleanup around auth verification, unused repository methods, and the placeholder-named token class; inconsistent validation and timestamp responsibilities; direct configuration key access without typed options/startup validation; and a minimal test suite. Operational debt includes the lack of a health route, no visible CI workflow, no checked-in Prometheus/Grafana provisioning, and ambiguity between local/Compose/production migration and secret practices.

The architecture is understandable, but its conventions are not fully uniform: most endpoints use MediatR while verify bypasses it; repositories are used for reads/writes but unit-of-work has only one method; and domain entities are anemic while the system is described as Clean Architecture. These are observations, not necessarily defects for the current project size.

## 23. Security observations

- Rotate any repository-exposed JWT/database credentials before treating the repository as safe for shared or production use.
- Supply a strong, environment-specific JWT key through secret storage; the current source does not enforce key length or presence with friendly validation.
- Do not enable production Swagger unless intentionally protected or publicly acceptable.
- The metrics endpoint is not visibly protected; confirm whether metric data is safe for public exposure.
- Ownership filtering is consistently present for current journal operations, which is a positive isolation property.
- Login returns a generic failure message, which reduces account enumeration through that message.
- Passwords are hashed with BCrypt rather than stored as plaintext; seeded values are hashes, not plaintext credentials.
- There is no token revocation, refresh-token storage, account lockout, password reset, email verification, audit trail, or rate limiting in source.
- Exception details are written in full to console. Ensure production log access is controlled and avoid returning those details to clients; the response path itself uses a generic message for unknown exceptions.
- CORS allows credentials for configured origins. Deployment must keep `AllowedOrigins` narrow and trusted.

## 24. Performance and operational observations

- EF connection resiliency is enabled with Npgsql `EnableRetryOnFailure`.
- Read lists/search use no tracking and descending date ordering.
- The absence of pagination is the largest obvious unbounded response risk.
- Search performs multiple case conversions and substring predicates; query plans should be checked with production-sized data.
- Startup migration uses a scoped DbContext and async migration call, but Compose `depends_on` does not provide database readiness.
- Rolling file logs have a day interval but no visible size/retention configuration.
- Prometheus scraping is configured at 15 seconds in `monitoring/prometheus.yml`; the Compose file does not run Prometheus itself.
- No caching, async background queue, batching, bulk operations, or response compression is present.

## 25. Important configuration and run requirements

For local host execution, the source expects .NET 10 SDK, a reachable PostgreSQL database matching the effective connection string, a valid JWT key/issuer/audience/expiry configuration, and port 5000 unless `PORT` is set. The launch profile selects Development and opens Swagger.

For Compose execution, start PostgreSQL and the API with `docker compose up --build`; the API uses the environment-injected Compose connection string and port 5000. Because startup migration is enabled in the checked-in base configuration and the Compose environment selects Development, migration/authentication behavior follows the source described above. Database readiness may still need orchestration handling.

For hosted execution, set `ASPNETCORE_ENVIRONMENT`, `DATABASE_URL` or `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpiryMinutes`, and narrowly scoped allowed origins. Decide explicitly whether Swagger and startup migrations should be enabled. If using `DATABASE_URL` as a URI, the code forces SSL required; confirm that this matches the provider.

## 26. Complete end-to-end execution flow

```text
Configuration sources
  -> WebApplicationBuilder / environment merge
  -> Program.cs host, Serilog, JWT, CORS, Swagger, metrics, DI
  -> AddApplication + AddInfrastructure
  -> optional startup EF migration
  -> Kestrel starts

HTTP request
  -> Prometheus HTTP metrics middleware
  -> GlobalExceptionHandlingMiddleware
  -> CORS
  -> JWT authentication (if bearer token supplied)
  -> authorization policy/attribute evaluation
  -> MVC route/model binding
  -> controller
  -> MediatR command/query (except direct Auth verify)
  -> ValidationBehavior and feature validator, when registered
  -> handler
  -> current-user service / application business checks
  -> repository/service abstraction
  -> Infrastructure implementation
  -> scoped AppDbContext and EF Core LINQ/change tracker
  -> PostgreSQL
  -> handler maps entity to DTO
  -> controller creates HTTP result
  -> JSON serialization
  -> metrics completion and exception middleware completion
  -> client response
```

At every journal read or write, the current user GUID is either used in the query or compared against the entity owner. At every mutation, `SaveChangesAsync` is the commit point and updates audit timestamps through the DbContext override. Unknown exceptions are converted to a generic 500 response with a trace ID when they occur after the middleware has started.

## 27. Final verification checklist

- [x] Complete canonical application folder structure inspected.
- [x] Root files, project files, source files, migration files, tests, configuration, Docker, monitoring, and existing documentation inspected.
- [x] All tracked C# source files read, including generated migration metadata.
- [x] Dependency/project references and NuGet package declarations traced.
- [x] Startup, DI, middleware order, authentication, authorization, controllers, MediatR, validators, repositories, EF Core, and response mapping traced.
- [x] API routes and success/error behavior documented.
- [x] Database schema, relationships, indexes, seed data, migration, transaction boundary, and connection resolution documented.
- [x] External/operational integrations, metrics, logging, Docker, and absence of background messaging documented.
- [x] Tests and important coverage gaps documented.
- [x] Potential issues distinguished from confirmed observations and recommendations.
- [x] Sensitive values redacted from this document.
- [x] No existing application code, configuration, project, test, or folder was modified for this analysis.
- [x] `BACKEND_PROJECT_DEEP_ANALYSIS.md` created as the only intended source-tree change.

## 28. Final project understanding summary

This backend is a small, coherent ASP.NET Core journal API whose runtime behavior is concentrated in `Program.cs`, the three API controllers, MediatR feature handlers, `AppDbContext`, and the Infrastructure DI/repository/services. The central security boundary is JWT authentication plus current-user ownership checks. The central persistence boundary is the Application interfaces implemented by EF Core repositories and a DbContext-backed unit of work. PostgreSQL is the only business-data store, and all current operations are synchronous at the request level even though they use asynchronous I/O.

The project is structurally ready for additional features using the existing feature/CQRS conventions, but production hardening should address secret management, exact configuration validation, migration policy, health/readiness exposure, pagination/search scalability, and broader automated coverage. Where the source contains a scaffold or no implementation, such as the unused verify query, background processing, or health endpoint, this document has explicitly recorded that it could not be treated as active functionality.
