# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

This is **Espasyo**, a crime incident management system built with Clean Architecture using ASP.NET Core 8. The system manages crime incidents with features like machine learning analysis, geocoding, and dual database support (SQL Server and SQLite).

## Architecture

### Clean Architecture Structure
- **Core/Domain** (`espasyo.Domain`): Entities, enums, and domain logic
- **Core/Application** (`espasyo.Application`): Repository interfaces, application services  
- **Infrastructure** (`espasyo.Infrastructure`): Data access, external services, dual database implementation
- **Presentation/WebAPI** (`espasyo.WebAPI`): REST API controllers, authentication
- **Presentation/Console** (`espasyo_console`): Data seeding tool

### Key Domain Entities
- **Incident**: Core crime incident entity with severity, crime type, motive, location data, and timestamps
- **Street**: Street and barangay mapping entity
- **Product**: Generic product entity

### Dual Database Architecture
The system supports both SQL Server and SQLite through configuration:
- **SQL Server**: Production database with enterprise features
- **SQLite**: Development database for local development
- Database provider is controlled via `DatabaseProvider` setting in appsettings

## Development Commands

### Building and Running

```bash
# Build entire solution
dotnet build nin-architecture.sln

# Run Web API (development with SQLite)
dotnet run --project espasyo.WebAPI

# Run with Aspire hosting (orchestrates both API and Console)
dotnet run --project nin-architecture.AppHost

# Run data seeder console
dotnet run --project espasyo_console
```

### Database Migration Commands

#### For SQL Server Migrations
```bash
# Add new migration
dotnet ef migrations add MigrationName --project espasyo.Infrastructure --startup-project espasyo.WebAPI --context ApplicationDbContext --configuration Debug --verbose --output-dir Migrations

# Update database
dotnet ef database update --project espasyo.Infrastructure --startup-project espasyo.WebAPI --context ApplicationDbContext --configuration Debug --verbose

# Remove last migration
dotnet ef migrations remove --project espasyo.Infrastructure --startup-project espasyo.WebAPI --context ApplicationDbContext --configuration Debug --verbose --force
```

#### For SQLite Migrations
```bash
# Add new migration
dotnet ef migrations add MigrationName --project espasyo.Infrastructure --startup-project espasyo.WebAPI --context SqliteApplicationDbContext --output-dir Data/Migrations/Sqlite

# Update database
dotnet ef database update --project espasyo.Infrastructure --startup-project espasyo.WebAPI --context SqliteApplicationDbContext
```

### Testing Commands

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test Domain.Tests
dotnet test Infrastructure.Tests
```

## Database Configuration

### Development (SQLite - Default)
The system uses SQLite by default in development mode. Database file: `espasyo_dev.db`

### Production (SQL Server)
Requires external SQL Server container:
```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:latest
```

### Configuration Override
Change database provider in `appsettings.json`:
```json
{
  "DatabaseProvider": "SqlServer", // or "Sqlite"
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=MyDatabase;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;",
    "SqliteConnection": "Data Source=espasyo_dev.db"
  }
}
```

## Key Services and Features

### Repository Pattern Implementation
- **IIncidentRepository**: CRUD operations, filtering, pagination, enum dictionaries
- **IStreetRepository**: Street and barangay management
- **IProductRepository**: Basic product operations

### External Services
- **GeocodeService**: Converts addresses to coordinates using external API
- **MachineLearningService**: ML.NET integration for data analysis
- **Identity Services**: ASP.NET Core Identity for authentication/authorization

### Business Logic
- **Crime Data Processing**: Complex incident filtering by date, type, severity, location
- **GIS Integration**: Latitude/longitude geocoding with rate limiting
- **Time-based Analysis**: Morning/afternoon/evening categorization

## API Access

### Swagger Documentation
When running in development: `http://localhost:5041/swagger/index.html`

### Authentication
- JWT Bearer token authentication
- Default admin user: `admin@example.com` / `Admin@123`
- Automatic role-based seeding on startup

### Key API Endpoints
- **Incidents**: Full CRUD with advanced filtering and pagination
- **Streets**: Barangay and street management
- **Users**: Authentication and user management

## Data Seeding

The `espasyo_console` project seeds sample data:
- **Streets**: Barangay and street data
- **Incidents**: 1000+ sample incidents with geocoding (rate-limited to 1 request/second)

## Aspire Integration

The project includes .NET Aspire for orchestration:
- **AppHost**: Orchestrates API and Console projects
- **ServiceDefaults**: Shared observability and configuration
- No database provisioning (uses external containers)

## Architecture Guidelines

### Domain-Driven Design
- Rich domain entities with business logic encapsulation
- Value objects for enums and complex types
- Repository pattern for data access abstraction

### Dependency Injection
- Smart provider switching based on configuration
- Separate DI registration for SQL Server vs SQLite
- Service lifetime management for external HTTP clients

### Error Handling
- Global exception filters
- Validation at entity and API levels
- Proper HTTP status code responses

## Development Notes

### Environment Behavior
- **Development**: Uses SQLite by default, full CORS policy, detailed logging
- **Production**: Uses SQL Server, restricted CORS, optimized for performance

### Rate Limiting
The geocoding service is rate-limited to prevent API abuse. Data seeding operations include 1-second delays between requests.

### Entity Framework Considerations
- Dual DbContext implementation with shared configurations
- SQLite-specific DateTimeOffset handling
- Identity framework integration with both databases