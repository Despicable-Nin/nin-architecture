## Pre-requisites
### Pull Docker Image for SQL Server
```bash

docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:latest

```

### Setting Up the Connection String
This has been already taken care of, unless you wanted to use a different one. It is highly suggested leaving the `appsettings.json` as it is.
```json

"ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=MyDatabase;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
}

```
### EntityFrameworkCore Command Line(s)
Make sure to change path to `espasyo.Infrastructure` project folder.
#### Add Migration (Sample)
This is done whenever entities (*Domain*) are updated.
```efcore
dotnet ef migrations add --project espasyo.Infrastructure\espasyo.Infrastructure.csproj --startup-project espasyo.WebAPI\espasyo.WebAPI.csproj --context nin.Infrastructure.Data.ApplicationDbContext --configuration Debug --verbose add-incident --output-dir Migrations
```
#### Update Database (Sample)
This is done to commit the added migration to the database.
```efcore
dotnet ef database update --project espasyo.Infrastructure\espasyo.Infrastructure.csproj --startup-project espasyo.WebAPI\espasyo.WebAPI.csproj --context nin.Infrastructure.Data.ApplicationDbContext --configuration Debug --verbose 20250217012332_add-incident
```
#### Remove Last Migration (Sample)
```efcore
dotnet ef migrations remove --project espasyo.Infrastructure\espasyo.Infrastructure.csproj --startup-project espasyo.WebAPI\espasyo.WebAPI.csproj --context nin.Infrastructure.Data.ApplicationDbContext --configuration Debug --verbose --force
```

## Swagger Open API Documentation
```http
http://localhost:5041/swagger/index.html
```
## Running & Building the Application
Make sure Docker is running (IMPORTANT)

### Visual Studio: (Make sure `espasyo.WebAPI` is the start-up project.)
- Open terminal / cmd / powershell to path of `espasyo.WebAPI`. Run *Update Database* (See above)
   - Note: Replace `20250217012332_add-incident` only if you added a new migration. Replace it with the latest migration the file name is prefixed as `yyyyMMddHHmmss_`. 
   - Example: `20250217012332_add-incident`
- Open context menu (right-click) on `espasyo.WebAPI` then select *Build* (or *Rebuild*)
- Press F5 or open *Debug* then click *Start Debugging*
- This then browse to *Swagger Open API Documentation* (See above)

### Jetbrains Rider: TBD

### Visual Studio Code: 
  - Open terminal / cmd / powershell to path of `espasyo.WebAPI`. 
  - Run *Update Database* (See above)
  - Run `dotnet build`
  - Run `dotnet run`

## Seeding Sample Data
 - Run the Web API project (espasyo.WebAPI)
 - Run the Console project (espasyo_console)
   - This will seed at least 1000 entries. 

 ***Note***: This may take long as it fetches GIS geocoding (latlong) per second (API has rate-limiting)
 
#### Code Snippet For ***espasyo_console***
##### Program.cs
```csharp

for (var i = 1; i <= 1000; i++) // Manually edit to desired. Warning: 1000 loops will take 1000 x (timeofApiFetch) seconds
{
    await SendIncidentRequest(url, i);
    await Task.Delay(1000); // Delay of 1 second between requests
}
```