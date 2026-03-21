FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5013

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ArtistManagementSystem.csproj", "./"]
RUN dotnet restore "ArtistManagementSystem.csproj"
COPY . .
RUN dotnet build "ArtistManagementSystem.csproj" -c Release -o /app/build
RUN dotnet publish "ArtistManagementSystem.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ArtistManagementSystem.dll"]
