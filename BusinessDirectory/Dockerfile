FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BusinessDirectory.sln ./
COPY BusinessDirectory/BusinessDirectory.API.csproj BusinessDirectory/
COPY Application/BusinessDirectory.Application.csproj Application/
COPY Infrastructure/BusinessDirectory.Infrastructure.csproj Infrastructure/
COPY Core/BusinessDirectory.Domain.csproj Core/

RUN dotnet restore BusinessDirectory/BusinessDirectory.API.csproj

COPY . .
RUN dotnet publish BusinessDirectory/BusinessDirectory.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BusinessDirectory.API.dll"]
