using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);


//run this first on command-line
//docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:latest


var api = builder.AddProject<Projects.espasyo_WebAPI>("espasyo-webapi");

builder.AddProject<Projects.espasyo_console>("espasyo-console")
    .WaitFor(api);

builder.Build().Run();
