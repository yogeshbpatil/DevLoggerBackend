# DevLoggerBackend

Backend API for Developer Journal frontend (`developer-journal-ui`) built with .NET 8, Clean Architecture, CQRS + MediatR, EF Core, PostgreSQL, and Serilog.

## Run locally
```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/DevLoggerBackend.Api
```

API base URL: `http://localhost:5000/api`

## Docker
```bash
docker compose up --build
```

## Database migrations
```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/DevLoggerBackend.Infrastructure --startup-project src/DevLoggerBackend.Api
dotnet ef database update --project src/DevLoggerBackend.Infrastructure --startup-project src/DevLoggerBackend.Api
```

## Frontend integration
Set in Next.js `.env.local`:
```env
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000/api
```

## Render free hosting
1. Create a PostgreSQL instance on Render free tier.
2. Create a Web Service for this API from your GitHub repository.
3. Build command: `dotnet build src/DevLoggerBackend.Api/DevLoggerBackend.Api.csproj -c Release`
4. Start command: `dotnet run --project src/DevLoggerBackend.Api/DevLoggerBackend.Api.csproj -c Release --no-build`
5. Set env vars:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `ConnectionStrings__DefaultConnection=...`
   - `Jwt__Key=...`
   - `Jwt__Issuer=DevLoggerBackendApi`
   - `Jwt__Audience=DevLoggerBackendClient`
   - `Jwt__ExpiryMinutes=120`
   - `AllowedOrigins=https://your-frontend-domain`
6. Expose `/swagger` optionally in production by environment flag.
7. Run migrations on startup safely (already enabled via `Database.Migrate()`), or run a one-time migration command in CI/CD.
