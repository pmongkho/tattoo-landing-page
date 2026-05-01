# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY dotnet-server.csproj ./
RUN dotnet restore dotnet-server.csproj

COPY . ./
RUN dotnet publish dotnet-server.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish ./

# Render sets PORT at runtime; default to 10000 locally.
ENV PORT=10000
EXPOSE 10000

CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT} dotnet dotnet-server.dll"]
