FROM node:22-alpine AS web
WORKDIR /src/web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Orders.Domain/Orders.Domain.csproj src/Orders.Domain/
COPY src/Orders.Application/Orders.Application.csproj src/Orders.Application/
COPY src/Orders.Infrastructure/Orders.Infrastructure.csproj src/Orders.Infrastructure/
COPY src/Orders.Api/Orders.Api.csproj src/Orders.Api/
RUN dotnet restore src/Orders.Api/Orders.Api.csproj
COPY src/ src/
RUN dotnet publish src/Orders.Api/Orders.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1
COPY --from=build /app ./
COPY --from=web /src/src/Orders.Api/wwwroot ./wwwroot
USER app
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=5s --start-period=15s --retries=3 \
    CMD ["dotnet", "Orders.Api.dll", "--healthcheck"]
ENTRYPOINT ["dotnet", "Orders.Api.dll"]
