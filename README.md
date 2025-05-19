Below is an updated version of your documentation that now incorporates the Aspire Hosting setup you ended up fixing. In this version, Aspire doesn’t attempt to provision a new SQL Server container—instead, you’ll run the SQL Server container externally (using your already‑pulled Docker image) and then simply bind your Web API (and Console) projects using Aspire Hosting. Make sure your connection string in *appsettings.json* is correct so your Web API can connect to the externally running SQL Server.

---

## Pre-requisites

### Pull Docker Image for SQL Server

Run the following command in your CLI to start the SQL Server container. (This makes the container available on port 1433 of your local machine.)

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:latest
```

### Setting Up the Connection String

This has been already taken care of—unless you want to use a different connection string. It is highly suggested to leave the *appsettings.json* as it is. For example:

```json
"ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=MyDatabase;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
}
```

The Web API will use this value (typically via its configuration mechanism) in its data context setup.

### EntityFrameworkCore Command Line(s)

Make sure to change the path to the `espasyo.Infrastructure` project folder when executing EF Core commands.

#### Add Migration (Sample)

This is done whenever entities (*Domain*) are updated.

```bash
dotnet ef migrations add --project espasyo.Infrastructure/espasyo.Infrastructure.csproj --startup-project espasyo.WebAPI/espasyo.WebAPI.csproj --context nin.Infrastructure.Data.ApplicationDbContext --configuration Debug --verbose add-incident --output-dir Migrations
```

#### Update Database (Sample)

This commits the added migration to the database:

```bash
dotnet ef database update --project espasyo.Infrastructure/espasyo.Infrastructure.csproj --startup-project espasyo.WebAPI/espasyo.WebAPI.csproj --context nin.Infrastructure.Data.ApplicationDbContext --configuration Debug --verbose 20250217012332_add-incident
```

#### Remove Last Migration (Sample)

```bash
dotnet ef migrations remove --project espasyo.Infrastructure/espasyo.Infrastructure.csproj --startup-project espasyo.WebAPI/espasyo.WebAPI.csproj --context nin.Infrastructure.Data.ApplicationDbContext --configuration Debug --verbose --force
```

---

## Swagger Open API Documentation

After your application starts successfully, you can view the Swagger documentation at:

```http
http://localhost:5041/swagger/index.html
```

---

## Running & Building the Application

Make sure Docker is running (this is important because the SQL Server container must be up).

### Visual Studio  
*(Make sure `espasyo.WebAPI` is set as the start-up project.)*

1. Open a terminal (or CMD/PowerShell) to the path of `espasyo.WebAPI`. Run the **Update Database** command as shown above.
   - **Note:** Replace `20250217012332_add-incident` only if you add a new migration. Replace it with the latest migration whose file name is prefixed as `yyyyMMddHHmmss_` (e.g. `20250217012332_add-incident`).
2. In the Solution Explorer, right-click the `espasyo.WebAPI` project then select **Build** (or **Rebuild**).
3. Press **F5** or go to **Debug > Start Debugging**.
4. This will automatically open the Swagger Open API Documentation page in your browser.

### Visual Studio Code

1. Open a terminal (or CMD/PowerShell) to the `espasyo.WebAPI` project directory.
2. Run the **Update Database** command as shown above.
3. Execute `dotnet build`.
4. Execute `dotnet run`.

---

## Aspire Hosting Integration

Since you’re running the SQL Server container externally, your Aspire setup does not need to pull or provision SQL Server; it only orchestrates your application projects. In your *Program.cs* for Aspire Hosting, your code may look like this:

```csharp
using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

// Instead of using builder.AddSqlServer, we just register our application projects.
// The Web API project is assumed to use the connection string from configuration (appsettings.json)
// so that it connects to the external SQL Server container.
var api = builder.AddProject<Projects.espasyo_WebAPI>("espasyo-webapi");

builder.AddProject<Projects.espasyo_console>("espasyo-console")
    .WaitFor(api);

builder.Build().Run();
```

In this setup:
- **Aspire Hosting** simply ensures that your Web API and Console projects are started in the desired order.
- Your **Web API** reads the connection string from *appsettings.json* (where `"DefaultConnection"` points to `Server=localhost,1433;...`) and connects to the externally running SQL Server container (which you started with the Docker run command).
- No extra container provisioning is attempted by Aspire—your manually managed SQL Server container is used.

---

## Seeding Sample Data

- Run the Web API project (`espasyo.WebAPI`).
- Run the Console project (`espasyo_console`).
  - This will seed at least 1000 entries.
  
  **Note:** This may take a while because it fetches GIS geocoding (latitude/longitude) per second (the API is rate-limited).

#### Code Snippet for ***espasyo_console***  
**Program.cs**

```csharp
for (var i = 1; i <= 1000; i++) // Manually edit to desired. Note: 1000 iterations will take at least 1000 seconds (given the 1 second delay per request).
{
    await SendIncidentRequest(url, i);
    await Task.Delay(1000); // Delay of 1 second between requests
}
```