FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/OnlineComplaintManagementSystem/OnlineComplaintManagementSystem.csproj src/OnlineComplaintManagementSystem/
RUN dotnet restore src/OnlineComplaintManagementSystem/OnlineComplaintManagementSystem.csproj

COPY src/OnlineComplaintManagementSystem/ src/OnlineComplaintManagementSystem/
RUN dotnet publish src/OnlineComplaintManagementSystem/OnlineComplaintManagementSystem.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "dotnet OnlineComplaintManagementSystem.dll --urls http://0.0.0.0:${PORT:-8080}"]