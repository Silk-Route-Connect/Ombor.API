FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["NuGet.Config", "."]

COPY ["src/Ombor.API/Ombor.API.csproj", "src/Ombor.API/"]
COPY ["src/Ombor.Application/Ombor.Application.csproj", "src/Ombor.Application/"]
COPY ["src/Ombor.Contracts/Ombor.Contracts.csproj", "src/Ombor.Contracts/"]
COPY ["src/Ombor.Domain/Ombor.Domain.csproj", "src/Ombor.Domain/"]
COPY ["src/Ombor.Infrastructure/Ombor.Infrastructure.csproj", "src/Ombor.Infrastructure/"]
COPY ["src/Ombor.TestDataGenerator/Ombor.TestDataGenerator.csproj", "src/Ombor.TestDataGenerator/"]

RUN dotnet restore src/Ombor.API/Ombor.API.csproj

COPY . .
RUN dotnet publish src/Ombor.API/Ombor.API.csproj \
  --configuration Release \
  --no-restore \
  --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "Ombor.API.dll"]
