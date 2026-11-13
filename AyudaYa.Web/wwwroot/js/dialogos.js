// Diálogos modales (<dialog>) para confirmar acciones que piden un motivo.
//   <button data-abrir-dialogo="id" data-valores='{"campo":"valor"}'>
//     Copia cada valor al campo [name=campo] del diálogo y al elemento
//     [data-dialogo-texto=campo], si existe.
//   <button data-cerrar-dialogo>  cierra el diálogo que lo contiene.
(function () {
    "use strict";

    let abridor = null;

    document.addEventListener("click", (e) => {
        const abrir = e.target.closest("[data-abrir-dialogo]");
        if (abrir) {
            const dialogo = document.getElementById(abrir.dataset.abrirDialogo);
            if (!dialogo || typeof dialogo.showModal !== "function") return;

            let valores = {};
            try { valores = JSON.parse(abrir.dataset.valores || "{}"); } catch (_) { valores = {}; }
            Object.entries(valores).forEach(([clave, valor]) => {
                const campo = dialogo.querySelector('[name="' + clave + '"]');
                if (campo) campo.value = valor;
                dialogo.querySelectorAll('[data-dialogo-texto="' + clave + '"]').forEach((el) => { el.textContent = valor; });
            });

            abridor = abrir;
            dialogo.showModal();
            const primero = dialogo.querySelector("textarea, input:not([type=hidden]), select");
            if (primero) primero.focus();
            return;
        }

        const cerrar = e.target.closest("[data-cerrar-dialogo]");
        if (cerrar) {
            const dialogo = cerrar.closest("dialog");
            if (dialogo) dialogo.close();
        }
    });

    // Al cerrar con Esc o "No, volver", el foco vuelve al botón que lo abrió.
    document.querySelectorAll("dialog").forEach((d) => d.addEventListener("close", () => {
        if (abridor && document.body.contains(abridor)) abridor.focus();
    }));
})();
