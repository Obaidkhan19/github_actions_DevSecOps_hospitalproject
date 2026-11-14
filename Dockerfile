
# syntax=docker/dockerfile:1

ARG DOTNET_VERSION=10.0


FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build

WORKDIR /src

COPY src/HospitalManagement.Web/HospitalManagement.Web.csproj src/HospitalManagement.Web/

RUN dotnet restore src/HospitalManagement.Web/HospitalManagement.Web.csproj

COPY src/HospitalManagement.Web/ src/HospitalManagement.Web/

RUN dotnet publish src/HospitalManagement.Web/HospitalManagement.Web.csproj \
      --configuration Release \
      --no-restore \
      --output /app/publish \
      /p:UseAppHost=false


FROM build AS test

COPY tests/HospitalManagement.Tests/HospitalManagement.Tests.csproj tests/HospitalManagement.Tests/

RUN dotnet restore tests/HospitalManagement.Tests/HospitalManagement.Tests.csproj

COPY tests/HospitalManagement.Tests/ tests/HospitalManagement.Tests/

RUN dotnet test tests/HospitalManagement.Tests/HospitalManagement.Tests.csproj \
      --configuration Release \
      --no-restore



FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS final

# FIX: Install ICU globalization library
RUN apk add --no-cache icu-libs

# Enable full .NET globalization
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

LABEL org.opencontainers.image.title="hospital-web" \
      org.opencontainers.image.description="Hospital Management System (ASP.NET Core)" \
      org.opencontainers.image.source="https://github.com/Obaidkhan19/github_actions_devsecops_hospitalproject"

WORKDIR /app

ARG APP_UID=1654

ENV ASPNETCORE_HTTP_PORTS=8080 \
    HOME=/home/app

RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
 && chown -R ${APP_UID}:${APP_UID} /home/app

COPY --from=build /app/publish ./

USER ${APP_UID}

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
  CMD wget -qO /dev/null http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "HospitalManagement.Web.dll"]
