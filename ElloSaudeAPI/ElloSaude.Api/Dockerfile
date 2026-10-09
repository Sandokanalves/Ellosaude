FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ElloSaude.Api/ElloSaude.Api.csproj", "ElloSaude.Api/"]
COPY ["ElloSaude.Infrastructure/ElloSaude.Infrastructure.csproj", "ElloSaude.Infrastructure/"]
COPY ["ElloSaude.Application/ElloSaude.Application.csproj", "ElloSaude.Application/"]
COPY ["ElloSaude.Domain/ElloSaude.Domain.csproj", "ElloSaude.Domain/"]
RUN dotnet restore "ElloSaude.Api/ElloSaude.Api.csproj"
COPY . .
WORKDIR "/src/ElloSaude.Api"
RUN dotnet build "ElloSaude.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ElloSaude.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ElloSaude.Api.dll"]