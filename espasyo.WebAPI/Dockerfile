# Use the ASP.NET Core runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the .NET SDK for building the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["espasyo.WebAPI/espasyo.WebAPI.csproj", "espasyo.WebAPI/"]
COPY ["espasyo.Application/espasyo.Application.csproj", "espasyo.Application/"]
COPY ["espasyo.Domain/espasyo.Domain.csproj", "espasyo.Domain/"]
COPY ["espasyo.Infrastructure/espasyo.Infrastructure.csproj", "espasyo.Infrastructure/"]
RUN dotnet restore "./espasyo.WebAPI/espasyo.WebAPI.csproj"
COPY . .
WORKDIR "/src/espasyo.WebAPI"
RUN dotnet build "./espasyo.WebAPI.csproj" -c Release -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
RUN dotnet publish "./espasyo.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the base image to run the application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "espasyo.WebAPI.dll"]
