(function () {
    "use strict";

    const boton = document.querySelector(".boton-menu");
    const nav = document.getElementById("nav-principal");
    boton.addEventListener("click", () => {
        const abierto = boton.getAttribute("aria-expanded") === "true";
        boton.setAttribute("aria-expanded", String(!abierto));
        nav.classList.toggle("abierto", !abierto);
    });
    document.addEventListener("keydown", (evento) => {
        if (evento.key === "Escape" && nav.classList.contains("abierto")) {
            boton.setAttribute("aria-expanded", "false");
            nav.classList.remove("abierto");
            boton.focus();
        }
    });

    const relativo = new Intl.RelativeTimeFormat("es-AR", { numeric: "auto" });
    const completo = new Intl.DateTimeFormat("es-AR", { dateStyle: "long", timeStyle: "short" });
    document.querySelectorAll(".pie-centro time").forEach((elemento) => {
        const fecha = new Date(elemento.dateTime);
        if (Number.isNaN(fecha.getTime())) return;
        const minutos = Math.round((fecha.getTime() - Date.now()) / 60000);
        const horas = Math.round(minutos / 60);
        elemento.title = completo.format(fecha);
        elemento.textContent = Math.abs(minutos) < 60
            ? relativo.format(Math.min(minutos, -1), "minute")
            : Math.abs(horas) < 24
                ? relativo.format(horas, "hour")
                : relativo.format(Math.round(horas / 24), "day");
    });
})();
