## Story 1.1: Create environment and base application

As a developer,  
I want a working development environment and a minimal runnable base application so that I can start building the ShortLink API on a solid foundation.

### Acceptance Criteria

- **Given** the architecture specifies .NET 8 Web API  
  **When** I set up the development environment and create the base application  
  **Then** the .NET 8 SDK is available and the solution builds and runs  
  **And** a Web API project exists (e.g. created with `dotnet new webapi` for ShortLink.Api)  
  **And** the base API runs and returns a minimal response (e.g. default endpoint or a simple health/hello)  
  **And** OpenAPI/Swagger is enabled so that the API is discoverable  
  **And** a README or doc describes how to run the project locally (without Docker if desired)  
  **And** the repository or folder structure is ready for adding Features, Domain, and Infrastructure in the next story

### Implementation Plan

- **Environment**
  - Install .NET 8 SDK on the development machine.
  - Verify `dotnet --info` shows .NET 8 is available.
- **Solution and project**
  - Create a solution `ShortLink.sln`.
  - Create a Web API project `ShortLink.Api` targeting `net8.0`.
  - Add the project to the solution and set it as the startup project.
- **Minimal API and health endpoint**
  - Configure a minimal API in `Program.cs` with a root or `/health` endpoint that returns `200 OK` and a simple payload.
- **Swagger / OpenAPI**
  - Enable `AddEndpointsApiExplorer` and `AddSwaggerGen`.
  - In development, enable Swagger UI.
- **Documentation**
  - Add a short section to the main `README.md` (or a dedicated doc) describing:
    - Prerequisites (SDK).
    - Commands to build and run the API.

### Sample Implementation Code (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
```

