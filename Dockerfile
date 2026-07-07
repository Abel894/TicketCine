# Etapa 1: Compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar los archivos .csproj de cada proyecto primero (optimiza el cache de Docker)
COPY TicketCine.Web/*.csproj TicketCine.Web/
COPY TicketCine.Application/*.csproj TicketCine.Application/
COPY TicketCine.Domain/*.csproj TicketCine.Domain/
COPY TicketCine.Infrastructure/*.csproj TicketCine.Infrastructure/

# Restaurar dependencias
RUN dotnet restore TicketCine.Web/TicketCine.Web.csproj

# Copiar todo el código fuente
COPY . .

# Publicar la aplicación
WORKDIR /src/TicketCine.Web
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: Imagen final (más liviana, solo runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render asigna el puerto dinámicamente vía variable de entorno PORT
ENV ASPNETCORE_URLS=http://+:$PORT
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "TicketCine.Web.dll"]