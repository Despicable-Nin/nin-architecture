```json
"ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=MyDatabase;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
  }

```

### Pull Docker Image for SQL Server
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:latest

```

### Add Migration (Sample)
```
"C:\Program Files\dotnet\dotnet.exe" ef migrations add --project espasyo.Infrastructure\espasyo.Infrastructure.csproj --startup-project espasyo.WebAPI\espasyo.WebAPI.csproj --context nin.Infrastructure.Data.ApplicationDbContext --configuration Debug --verbose add-incident --output-dir Migrations
```

### Update Database (Sample)
```
"C:\Program Files\dotnet\dotnet.exe" ef database update --project espasyo.Infrastructure\espasyo.Infrastructure.csproj --startup-project espasyo.WebAPI\espasyo.WebAPI.csproj --context nin.Infrastructure.Data.ApplicationDbContext --configuration Debug --verbose 20250217012332_add-incident
```

### Remove Last Migration (Sample)
```js
"C:\Program Files\dotnet\dotnet.exe" ef migrations remove --project espasyo.Infrastructure\espasyo.Infrastructure.csproj --startup-project espasyo.WebAPI\espasyo.WebAPI.csproj --context nin.Infrastructure.Data.ApplicationDbContext --configuration Debug --verbose --force
```

### Swagger Open API Documentation
```js
http://localhost:5041/swagger/index.html
```