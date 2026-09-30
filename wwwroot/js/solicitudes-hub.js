"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/solicitudes", {
        transport: signalR.HttpTransportType.WebSockets,
        skipNegotiation: true
    })
    .withAutomaticReconnect()
    .build();

function mostrarEstadoConexion(texto, claseCss) {
    const indicador = document.getElementById("estado-conexion-ws");
    if (!indicador) return;
    indicador.textContent = texto;
    indicador.className = "small " + claseCss;
}

function mostrarAviso(mensaje, estado) {
    const contenedor = document.getElementById("notificaciones-contenedor");
    if (!contenedor) return;

    const alertClass = estado === "Aprobado" ? "alert-success" : "alert-danger";
    const div = document.createElement("div");
    div.className = `alert ${alertClass} alert-dismissible fade show`;
    div.innerHTML = `${mensaje} <button type="button" class="btn-close" data-bs-dismiss="alert"></button>`;
    contenedor.appendChild(div);
}

async function refrescarEstadoVigente() {
    // Al reconectar: consulta el estado real de cada solicitud visible en pantalla
    const filas = document.querySelectorAll("[data-solicitud-id]");
    for (const fila of filas) {
        const id = fila.getAttribute("data-solicitud-id");
        try {
            const respuesta = await fetch(`/Solicitudes/Estado/${id}`);
            if (respuesta.ok) {
                const data = await respuesta.json();
                actualizarFilaEnPantalla(data.solicitudId, data.estado, data.motivoRechazo);
            }
        } catch (e) {
            console.error("No se pudo consultar el estado vigente de la solicitud " + id, e);
        }
    }
}

function actualizarFilaEnPantalla(solicitudId, estado, motivoRechazo) {
    const el = document.querySelector(`[data-solicitud-id="${solicitudId}"] [data-estado-badge]`);
    if (el) {
        el.textContent = estado;
        el.className = "badge bg-" + (estado === "Aprobado" ? "success" : estado === "Rechazado" ? "danger" : "warning");
    }

    const motivoEl = document.querySelector(`[data-solicitud-id="${solicitudId}"] [data-motivo-rechazo]`);
    if (motivoEl && motivoRechazo) {
        motivoEl.textContent = motivoRechazo;
        motivoEl.closest("[data-motivo-contenedor]")?.classList.remove("d-none");
    }
}

connection.on("SolicitudEstadoActualizado", function (data) {
    actualizarFilaEnPantalla(data.solicitudId, data.estado, data.motivoRechazo);

    const mensaje = data.estado === "Aprobado"
        ? `Tu solicitud #${data.solicitudId} fue aprobada.`
        : `Tu solicitud #${data.solicitudId} fue rechazada${data.motivoRechazo ? ": " + data.motivoRechazo : "."}`;

    mostrarAviso(mensaje, data.estado);
});

connection.onreconnecting(() => mostrarEstadoConexion("🟡 Reconectando...", "text-warning"));

connection.onreconnected(() => {
    mostrarEstadoConexion("🟢 Conectado", "text-success");
    refrescarEstadoVigente();
});

connection.onclose(() => mostrarEstadoConexion("🔴 Desconectado", "text-danger"));

connection.start()
    .then(() => {
        mostrarEstadoConexion("🟢 Conectado", "text-success");
        refrescarEstadoVigente();
    })
    .catch(function (err) {
        mostrarEstadoConexion("🔴 No se pudo conectar", "text-danger");
        console.error(err.toString());
    });