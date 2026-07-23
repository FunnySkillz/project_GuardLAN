FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY GuardLAN.API.sln ./GuardLAN.API.sln
COPY src/GuardLan.Api/GuardLan.Api.csproj ./src/GuardLan.Api/GuardLan.Api.csproj
COPY src/GuardLan.Application/GuardLan.Application.csproj ./src/GuardLan.Application/GuardLan.Application.csproj
COPY src/GuardLan.Domain/GuardLan.Domain.csproj ./src/GuardLan.Domain/GuardLan.Domain.csproj
COPY src/GuardLan.Infrastructure/GuardLan.Infrastructure.csproj ./src/GuardLan.Infrastructure/GuardLan.Infrastructure.csproj
COPY src/GuardLan.Worker/GuardLan.Worker.csproj ./src/GuardLan.Worker/GuardLan.Worker.csproj

RUN dotnet restore ./src/GuardLan.Api/GuardLan.Api.csproj

COPY src ./src
RUN dotnet publish ./src/GuardLan.Api/GuardLan.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./
USER app
ENTRYPOINT ["dotnet", "GuardLan.Api.dll"]
