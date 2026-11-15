# Imagen de AyudaYa para hosting en contenedor (por ejemplo Render).
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

COPY --from=build /out .

# La imagen de .NET ya trae un usuario sin privilegios (APP_UID): no se corre como root.
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:10000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=0 \
    DOTNET_TieredPGO=0
EXPOSE 10000

# La configuración (conexión a la base, etc.) llega por variables de entorno:
#   ConnectionStrings__AyudaYa, ForwardedHeaders__Habilitar=true,
#   DataProtection__PersistirEnBase=true, AllowedHosts=<dominio>
ENTRYPOINT ["dotnet", "AyudaYa.Web.dll"]
