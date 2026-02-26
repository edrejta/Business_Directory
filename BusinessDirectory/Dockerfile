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

RUN groupadd --gid 10001 appgroup \
    && useradd --uid 10001 --gid 10001 --create-home --shell /usr/sbin/nologin appuser

ENV ASPNETCORE_URLS=http://+:8080
USER 10001:10001
EXPOSE 8080
ENTRYPOINT ["dotnet", "BusinessDirectory.API.dll"]
