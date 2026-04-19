# 1. Usar el SDK de .NET para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copiar archivos y restaurar dependencias
COPY *.sln ./
COPY *.csproj ./
RUN dotnet restore

# Copiar todo lo demás y publicar
COPY . ./
RUN dotnet publish -c Release -o out

# 2. Crear la imagen final de ejecución (más ligera)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Exponer el puerto que usa Render
EXPOSE 80
ENTRYPOINT ["dotnet", "TuProyecto.dll"]