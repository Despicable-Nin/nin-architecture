```json
"ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=MyDatabase;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
  }

```

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:latest

```
