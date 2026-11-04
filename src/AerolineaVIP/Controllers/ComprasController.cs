using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    /// <summary>
    /// Controlador dedicado al Módulo de Compra y Pago Simulado (MOD-06 — Pantallas 11 y 12).
    /// Soporta tanto las rutas oficiales /Compras/Pagar y /Compras/Confirmacion como la integración con ReservasController.
    /// </summary>
    [Authorize(Roles = "Cliente,Counter,SuperUsuario,Administrador")]
    public class ComprasController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Compras/Pagar (Pantalla 11)
        [HttpGet]
        public ActionResult Pagar(int? id = null, int? reservaId = null, string codigoPreReserva = null, int? vueloId = null, string tarifa = "Classic")
        {
            VuelosController.LiberarReservasExpiradas(db);

            int? targetReservaId = reservaId ?? id;
            var resumen = Session["ReservaActual_Resumen"] as ResumenReservaViewModel;
            Reserva reservaDb = null;

            if (targetReservaId.HasValue && targetReservaId.Value > 0)
            {
                reservaDb = db.Reservas
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                    .Include(r => r.Compras)
                    .FirstOrDefault(r => r.ReservaId == targetReservaId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(codigoPreReserva))
            {
                string codUpper = codigoPreReserva.Trim().ToUpperInvariant();
                reservaDb = db.Reservas
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                    .Include(r => r.Compras)
                    .FirstOrDefault(r => r.CodigoReserva == codUpper);
            }
            else if (resumen != null && resumen.ReservaId > 0)
            {
                reservaDb = db.Reservas
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                    .Include(r => r.Compras)
                    .FirstOrDefault(r => r.ReservaId == resumen.ReservaId);
            }

            if (reservaDb != null)
            {
                var primerAsig = reservaDb.ReservaPasajeroVueloes.FirstOrDefault();
                var vueloDb = primerAsig?.Vuelo;
                var compraDb = reservaDb.Compras.FirstOrDefault();
                var primerPax = primerAsig?.Pasajero;

                var modelDb = new PagoSimuladoViewModel
                {
                    ReservaId = reservaDb.ReservaId,
                    CodigoPreReserva = reservaDb.CodigoReserva,
                    VueloId = vueloDb?.VueloId ?? (vueloId ?? 1),
                    CodigoVuelo = "VIP" + (vueloDb?.VueloId ?? (vueloId ?? 1)).ToString("D5"),
                    FechaOperacion = vueloDb?.FechaOperacion ?? DateTime.Today.AddDays(2),
                    OrigenCodigo = vueloDb?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA ?? "UIO",
                    DestinoCodigo = vueloDb?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA ?? "GYE",
                    TotalPagar = compraDb?.Importe ?? (resumen?.TotalPagar ?? 557.75m),
                    TarifaSeleccionada = tarifa,
                    CantidadPasajeros = reservaDb.ReservaPasajeroVueloes.Count,
                    AsientosCodigos = string.Join(", ", reservaDb.ReservaPasajeroVueloes.Select(a => VuelosController.MapearNumeroAsientoACodigo(a.NumeroAsiento))),
                    MetodoPago = "TARJETA_CREDITO",
                    MarcaTarjeta = "VISA",
                    TitularTarjeta = primerPax != null ? $"{primerPax.Nombres} {primerPax.Apellidos}".ToUpperInvariant() : "",
                    NumeroTarjeta = "",
                    MesExpiracion = "",
                    AnioExpiracion = "",
                    Cvv = "",
                    AceptoTerminos = false
                };

                return View("~/Views/Reservas/Pagar.cshtml", modelDb);
            }

            int vId = vueloId ?? (resumen?.VueloId ?? 1);
            var vuelo = db.Vueloes
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .FirstOrDefault(v => v.VueloId == vId);

            var model = new PagoSimuladoViewModel
            {
                ReservaId = resumen?.ReservaId ?? 0,
                CodigoPreReserva = !string.IsNullOrWhiteSpace(codigoPreReserva) ? codigoPreReserva : (resumen?.CodigoPreReserva ?? "RV000013"),
                VueloId = vId,
                CodigoVuelo = vuelo != null ? "VIP" + vuelo.VueloId.ToString("D5") : "VIP00001",
                FechaOperacion = vuelo?.FechaOperacion ?? DateTime.Today.AddDays(2),
                OrigenCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA ?? "UIO",
                DestinoCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA ?? "GYE",
                TotalPagar = resumen?.TotalPagar ?? 557.75m,
                TarifaSeleccionada = tarifa,
                CantidadPasajeros = resumen?.CantidadPasajeros ?? 1,
                AsientosCodigos = resumen?.AsientosCodigos ?? "1D",
                MetodoPago = "TARJETA_CREDITO",
                MarcaTarjeta = "VISA",
                TitularTarjeta = "",
                NumeroTarjeta = "",
                MesExpiracion = "",
                AnioExpiracion = "",
                Cvv = "",
                AceptoTerminos = false
            };

            return View("~/Views/Reservas/Pagar.cshtml", model);
        }

        // POST: Compras/ProcesarPago (Pantalla 11)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcesarPago(PagoSimuladoViewModel model)
        {
            using (var reservasCtrl = new ReservasController())
            {
                reservasCtrl.ControllerContext = this.ControllerContext;
                return reservasCtrl.EjecutarFlujoPagoSimulado(model, redirigirACompras: true);
            }
        }

        // GET: Compras/Confirmacion (Pantalla 12)
        [HttpGet]
        public ActionResult Confirmacion(string codigoReserva = "RV000001", string codigoOperacion = "SIM-RV000001-01", int vueloId = 1, string marca = "VISA", string ultimos4 = "9010", string titular = "CARLOS ROBERTO MENDOZA", string metodoPago = "TARJETA_CREDITO")
        {
            using (var reservasCtrl = new ReservasController())
            {
                reservasCtrl.ControllerContext = this.ControllerContext;
                var model = reservasCtrl.ConstruirConfirmacionCompraViewModel(codigoReserva, codigoOperacion, vueloId, marca, ultimos4, titular, metodoPago);
                return View("~/Views/Reservas/Exito.cshtml", model);
            }
        }

        // GET: Compras/Exito (Alias de Pantalla 12 para evitar 404 en redirecciones compartidas)
        [HttpGet]
        public ActionResult Exito(string codigoReserva = "RV000001", string codigoOperacion = "SIM-RV000001-01", int vueloId = 1, string marca = "VISA", string ultimos4 = "9010", string titular = "CARLOS ROBERTO MENDOZA", string metodoPago = "TARJETA_CREDITO")
        {
            return Confirmacion(codigoReserva, codigoOperacion, vueloId, marca, ultimos4, titular, metodoPago);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
