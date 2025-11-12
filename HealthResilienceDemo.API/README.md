# HealthResilienceDemo (.NET 8)

## Overview
Minimal sample .NET 8 Web API demonstrating:
- PostgreSQL integration (EF Core)
- Redis connection (StackExchange.Redis)
- Health Checks (ASP.NET Core Health Checks)
- Polly resilience policies (Retry, Circuit Breaker, Fallback) for HttpClient
- Swagger UI

## How to run
1. Ensure .NET 8 SDK is installed.
2. Ensure PostgreSQL and Redis are running and update `appsettings.json` connection strings.
3. From `HealthResilienceDemo.API` folder:
   - `dotnet restore`
   - `dotnet build`
   - `dotnet run`
4. Open browser:
   - Swagger: `https://localhost:5001/swagger`
   - Health: `https://localhost:5001/health/ready`
   - Health UI: `https://localhost:5001/health-ui`

## Notes
- The project includes packages with versions that are compatible with .NET 8 at the time of generation.
- If you want migrations for EF Core, run `dotnet ef migrations add Initial` after installing `dotnet-ef`.
