FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY ["EcoPonto.API/EcoPonto.API.csproj", "EcoPonto.API/"]
COPY ["EcoPonto.Domain/EcoPonto.Domain.csproj", "EcoPonto.Domain/"]
COPY ["EcoPonto.Infrastructure/EcoPonto.Infrastructure.csproj", "EcoPonto.Infrastructure/"]

RUN dotnet restore "EcoPonto.API/EcoPonto.API.csproj"

# copy everything else and build
COPY . .
WORKDIR /src/EcoPonto.API
RUN dotnet publish "EcoPonto.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "EcoPonto.API.dll"]
