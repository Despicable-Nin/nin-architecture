using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);


//run this first on command-line
//docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:latest

var sql = builder.AddSqlServer("localhost")
    .WithEnvironment("MSSQL_SA_PASSWORD", "YourStrong!Passw0rd")
    .WithEnvironment("ACCEPT_EULA","Y")
    .WithEndpoint(1433, 1433, "tcp", "sqlserver-tcp-endpoint") // Unique name
    .AddDatabase("MyDatabase");

var api = builder.AddProject<Projects.espasyo_WebAPI>("espasyo-webapi").WithReference(sql)
    .WaitFor(sql);

builder.AddProject<Projects.espasyo_console>("espasyo-console")
    .WaitFor(api);

builder.Build().Run();
