# Backend
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Airport.Web/Airport.Web.csproj", "Airport.Web/"]
COPY ["Airport.Models/Airport.Models.csproj", "Airport.Models/"]
COPY ["Airport.Contracts/Airport.Contracts.csproj", "Airport.Contracts/"]
COPY ["Airport.Domain/Airport.Domain.csproj", "Airport.Domain/"]
COPY ["Airport.Persistence/Airport.Persistence.csproj", "Airport.Persistence/"]
COPY ["Airport.Services.Abstractions/Airport.Services.Abstractions.csproj", "Airport.Services.Abstractions/"]
COPY ["Airport.Services/Airport.Services.csproj", "Airport.Services/"]
COPY ["Airport.Presentation/Airport.Presentation.csproj", "Airport.Presentation/"]
RUN dotnet restore "Airport.Web/Airport.Web.csproj"
COPY . .
WORKDIR "/src/Airport.Web"
RUN dotnet build "Airport.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Airport.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Airport.Web.dll"]