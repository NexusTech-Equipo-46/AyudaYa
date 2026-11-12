(function () {
    "use strict";
    const A = window.AyudaYa;
    const AU = window.AyudaYaUsuario;

    let datos = null;
    try { datos = JSON.parse(sessionStorage.getItem("ayudaya-ultima-solicitud")); } catch (e) { datos = null; }
    const ejemplo = window.DATOS_USUARIO.todas.find((s) => s.id === "s-1004");
    const s = datos && Array.isArray(datos.recursos) ? datos : {
        urgencia: ejemplo.urgencia, zona: ejemplo.zona_nombre,
        personas: ejemplo.personas_afectadas, recursos: ejemplo.recursos
    };

    document.getElementById("insignias").innerHTML = AU.insigniaSolicitud("pendiente") + AU.insigniaUrgencia(s.urgencia);
    document.getElementById("meta").textContent = s.zona + " · " + AU.plural(s.personas, "persona afectada", "personas afectadas");
    document.getElementById("recursos").innerHTML = s.recursos.map((r) =>
        "<li>" + AU.chipRecurso({ tipo_recurso: r.tipo_recurso, estado: "pendiente" }) +
        ' <span class="texto-suave">· ' + A.escapar(AU.plural(r.personas, "persona", "personas")) + "</span></li>"
    ).join("");

    const titulo = document.getElementById("titulo-pendiente");
    titulo.setAttribute("tabindex", "-1");
    titulo.focus();
})();
