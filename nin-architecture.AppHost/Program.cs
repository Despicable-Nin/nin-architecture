using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", buider =>
    {
        buider.AllowAnyHeader()
          .AllowAnyMethod()
          .AllowAnyOrigin();
    });
});

builder.AddProject<Projects.espasyo_WebAPI>("espasyo-webapi");

builder.AddProject<Projects.espasyo_console>("espasyo-console");

builder.Build().Run();
