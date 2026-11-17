// Capa base común de los mapas (Leaflet se sirve desde /lib/leaflet, sin CDN).
(function () {
    "use strict";

    // Mosaicos de OpenStreetMap: requieren la atribución y el encabezado Referer
    // (el navegador lo envía al servir la página por http/https).
    function capaBase() {
        return L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        });
    }

    window.MapaAyudaYa = { capaBase };
})();
