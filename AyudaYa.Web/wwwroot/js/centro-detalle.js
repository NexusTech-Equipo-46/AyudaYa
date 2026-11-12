(function () {
    "use strict";

    const relativo = new Intl.RelativeTimeFormat("es-AR", { numeric: "auto" });
    const completo = new Intl.DateTimeFormat("es-AR", { dateStyle: "long", timeStyle: "short" });
    document.querySelectorAll("time.fecha-relativa, time.fecha-completa").forEach((elemento) => {
        const fecha = new Date(elemento.dateTime);
        if (Number.isNaN(fecha.getTime())) return;
        elemento.title = completo.format(fecha);
        if (elemento.classList.contains("fecha-completa")) {
            elemento.textContent = completo.format(fecha);
            return;
        }
        const minutos = Math.round((fecha.getTime() - Date.now()) / 60000);
        const horas = Math.round(minutos / 60);
        elemento.textContent = Math.abs(minutos) < 60
            ? relativo.format(Math.min(minutos, -1), "minute")
            : Math.abs(horas) < 24
                ? relativo.format(horas, "hour")
                : relativo.format(Math.round(horas / 24), "day");
    });

    const contenedor = document.getElementById("mapa-centro");
    const aviso = document.getElementById("mapa-no-disponible");
    if (!contenedor || !window.L) return;

    try {
        const ubicacion = [Number(contenedor.dataset.latitud), Number(contenedor.dataset.longitud)];
        contenedor.hidden = false;
        const mapa = L.map(contenedor, { scrollWheelZoom: false, keyboard: false }).setView(ubicacion, 15);
        const mosaicos = L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        });
        mosaicos.on("tileerror", () => { aviso.hidden = false; });
        mosaicos.addTo(mapa);
        L.marker(ubicacion, { title: contenedor.dataset.nombre, alt: contenedor.dataset.nombre, keyboard: false }).addTo(mapa);
        aviso.hidden = true;
    } catch (_) {
        contenedor.hidden = true;
        aviso.hidden = false;
    }
})();
