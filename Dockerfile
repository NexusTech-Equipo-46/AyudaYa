# Imagen de AyudaYa para hosting en contenedor (por ejemplo Hugging Face Spaces).
# Solo compila la web y sus dependencias; las pruebas, la documentación y los
# scripts no entran (ver .dockerignore).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaurar primero con los .csproj aprovecha la caché de capas.
COPY AyudaYa.Web/AyudaYa.Web.csproj AyudaYa.Web/
COPY AyudaYa.Negocio/AyudaYa.Negocio.csproj AyudaYa.Negocio/
COPY AyudaYa.Data/AyudaYa.Data.csproj AyudaYa.Data/
COPY AyudaYa.Dominio/AyudaYa.Dominio.csproj AyudaYa.Dominio/
RUN dotnet restore AyudaYa.Web/AyudaYa.Web.csproj

COPY AyudaYa.Web/ AyudaYa.Web/
COPY AyudaYa.Negocio/ AyudaYa.Negocio/
COPY AyudaYa.Data/ AyudaYa.Data/
COPY AyudaYa.Dominio/ AyudaYa.Dominio/
RUN dotnet publish AyudaYa.Web/AyudaYa.Web.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Hugging Face Spaces ejecuta el contenedor con el usuario 1000 y escucha en 7860.
RUN useradd --create-home --uid 1000 ayudaya
COPY --from=build /out .
USER 1000

ENV ASPNETCORE_URLS=http://+:7860 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=0 \
    DOTNET_TieredPGO=0
EXPOSE 7860

# La configuración (conexión a la base, etc.) llega por variables de entorno:
#   ConnectionStrings__AyudaYa, ForwardedHeaders__Habilitar=true,
#   DataProtection__PersistirEnBase=true, AllowedHosts=<dominio>
ENTRYPOINT ["dotnet", "AyudaYa.Web.dll"]
