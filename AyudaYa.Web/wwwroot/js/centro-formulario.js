// Formulario de centro: con "24 horas" no hacen falta días ni horas.
(function () {
    "use strict";

    const todoElDia = document.getElementById("TodoElDia");
    const turno = document.getElementById("horario-turno");
    if (!todoElDia || !turno) return;

    function actualizar() { turno.hidden = todoElDia.checked; }
    todoElDia.addEventListener("change", actualizar);
    actualizar();
})();
