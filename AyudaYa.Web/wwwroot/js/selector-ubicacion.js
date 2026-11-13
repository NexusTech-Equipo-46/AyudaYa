// Selector de ubicación con mapa. Mantiene sincronizados un marcador y los
// campos Latitud/Longitud, que siguen siendo editables a mano (accesibilidad
// y respaldo si el mapa no carga).
//   #selector-mapa   contenedor del mapa
//   #Latitud #Longitud   campos numéricos
//   #ZonaId          (opcional) select con data-lat / data-lon: centra el mapa en la zona
//   #usar-mi-ubicacion   (opcional) botón que usa la geolocalización del navegador
(function () {
    "use strict";

    const contenedor = document.getElementById("selector-mapa");
    const latitud = document.getElementById("Latitud");
    const longitud = document.getElementById("Longitud");
    if (!contenedor || !latitud || !longitud || !window.L) {
        if (contenedor) contenedor.hidden = true;
        return;
    }

    const zona = document.getElementById("ZonaId");
    const botonUbicacion = document.getElementById("usar-mi-ubicacion");

    // Centro por defecto: el de la zona elegida y, si no hay, el centro del país.
    function centroDeZona() {
        if (!zona || !zona.selectedOptions.length) return null;
        const o = zona.selectedOptions[0];
        const lat = parseFloat(o.dataset.lat), lon = parseFloat(o.dataset.lon);
        return Number.isFinite(lat) && Number.isFinite(lon) ? [lat, lon] : null;
    }

    function valorActual() {
        const lat = parseFloat(latitud.value), lon = parseFloat(longitud.value);
        return Number.isFinite(lat) && Number.isFinite(lon) && Math.abs(lat) <= 90 && Math.abs(lon) <= 180 ? [lat, lon] : null;
    }

    const inicial = valorActual() || centroDeZona();
    const mapa = L.map(contenedor, { scrollWheelZoom: false }).setView(inicial || [-38.4, -63.6], inicial ? 15 : 4);
    MapaAyudaYa.capaBase().addTo(mapa);

    let marcador = null;

    function colocar(lat, lon, centrar) {
        const punto = [lat, lon];
        if (!marcador) {
            marcador = L.marker(punto, { draggable: true, keyboard: true, title: "Tu ubicación", alt: "Tu ubicación" }).addTo(mapa);
            marcador.on("dragend", () => {
                const p = marcador.getLatLng();
                escribir(p.lat, p.lng);
            });
        } else {
            marcador.setLatLng(punto);
        }
        escribir(lat, lon);
        if (centrar) mapa.setView(punto, Math.max(mapa.getZoom(), 16));
    }

    function escribir(lat, lon) {
        latitud.value = lat.toFixed(6);
        longitud.value = lon.toFixed(6);
    }

    if (valorActual()) colocar(valorActual()[0], valorActual()[1], false);

    mapa.on("click", (e) => colocar(e.latlng.lat, e.latlng.lng, false));

    // Si se escriben las coordenadas a mano, el marcador las sigue.
    [latitud, longitud].forEach((campo) => campo.addEventListener("change", () => {
        const v = valorActual();
        if (v) { colocar(v[0], v[1], true); }
    }));

    // Al cambiar de zona y sin marcador puesto, el mapa se centra en la zona.
    if (zona) {
        zona.addEventListener("change", () => {
            const c = centroDeZona();
            if (c && !marcador) mapa.setView(c, 15);
        });
    }

    if (botonUbicacion && "geolocation" in navigator) {
        botonUbicacion.hidden = false;
        botonUbicacion.addEventListener("click", () => {
            botonUbicacion.disabled = true;
            navigator.geolocation.getCurrentPosition(
                (pos) => { botonUbicacion.disabled = false; colocar(pos.coords.latitude, pos.coords.longitude, true); },
                () => {
                    botonUbicacion.disabled = false;
                    alert("No pudimos obtener tu ubicación. Tocá el mapa para marcarla.");
                },
                { enableHighAccuracy: true, timeout: 15000 }
            );
        });
    }
})();
