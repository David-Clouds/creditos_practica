"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/solicitudes")
    .withAutomaticReconnect()
    .build();

connection.on("NotificacionSolicitud", function (data) {
    mostrarNotificacion(data.mensaje, data.estado);
});

function mostrarNotificacion(mensaje, estado) {
    const contenedor = document.getElementById("notificaciones-contenedor");
    if (!contenedor) return;

    const alertClass = estado === "Aprobado" ? "alert-success" : "alert-danger";
    const div = document.createElement("div");
    div.className = `alert ${alertClass} alert-dismissible fade show`;
    div.innerHTML = `${mensaje} <button type="button" class="btn-close" data-bs-dismiss="alert"></button>`;
    contenedor.appendChild(div);
}

connection.start().catch(function (err) {
    console.error(err.toString());
});