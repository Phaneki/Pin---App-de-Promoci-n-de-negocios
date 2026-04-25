# 1. Usar el SDK de .NET para compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copiar archivos y restaurar dependencias
COPY *.sln ./
COPY *.csproj ./
RUN dotnet restore

# Copiar todo lo demás y publicar
COPY . ./
RUN dotnet publish -c Release -o out

# 2. Crear la imagen final de ejecución (más ligera)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Instalar librería requerida por Npgsql para autenticación con PostgreSQL en Render
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
COPY --from=build-env /app/out .

# Exponer el puerto que usa Render
EXPOSE 80
ENTRYPOINT ["dotnet", "Pin---App-de-Promoci-n-de-negocios.dll"]
