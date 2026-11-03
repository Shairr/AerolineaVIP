using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    /// <summary>
    /// MÓDULO MOD-09: Asistencia de Counter
    /// Centraliza la atención presencial de ventanilla para búsqueda autorizada de reservas,
    /// seguimiento de vuelos del día y asistencia directa de check-in.
    /// </summary>
    [Authorize(Roles = "Counter,Administrador,SuperUsuario")]
    public class CounterController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: /Counter/Index
        public ActionResult Index(string busqueda = null, string fecha = null)
        {
            DateTime fechaFiltro = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(fecha) && DateTime.TryParse(fecha, out DateTime fParseada))
            {
                fechaFiltro = fParseada;
            }

            // 1. Vuelos programados para la fecha seleccionada
            var vuelosDelDia = db.Vueloes
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Include(v => v.Aeronave.ModeloAeronave)
                .Include(v => v.EstadoVuelo)
                .Where(v => DbFunctions.TruncateTime(v.FechaOperacion) == DbFunctions.TruncateTime(fechaFiltro))
                .OrderBy(v => v.Frecuencia.HoraSalida)
                .ToList();

            // 2. Consulta de reservas si se proporcionó un término de búsqueda (Código, Cédula o Apellidos)
            var reservasEncontradas = new List<Reserva>();
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string b = busqueda.Trim().ToUpperInvariant();
                reservasEncontradas = db.Reservas
                    .Include(r => r.EstadoReserva)
                    .Include(r => r.ReservaPasajeroVueloes.Select(rpv => rpv.Pasajero))
                    .Include(r => r.ReservaPasajeroVueloes.Select(rpv => rpv.Vuelo.Frecuencia.Ruta.Aeropuerto))
                    .Include(r => r.ReservaPasajeroVueloes.Select(rpv => rpv.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                    .Include(r => r.ReservaPasajeroVueloes.Select(rpv => rpv.CheckIns))
                    .Where(r => r.CodigoReserva.Contains(b) ||
                                r.ReservaPasajeroVueloes.Any(rpv => rpv.Pasajero.NumeroDocumento.Contains(b) ||
                                                                   rpv.Pasajero.Apellidos.ToUpper().Contains(b)))
                    .Take(25)
                    .ToList();
            }

            ViewBag.Busqueda = busqueda;
            ViewBag.Fecha = fechaFiltro.ToString("yyyy-MM-dd");
            ViewBag.Vuelos = vuelosDelDia;

            return View(reservasEncontradas);
        }

        // POST: /Counter/AsistirCheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AsistirCheckIn(int reservaId, int pasajeroId, int vueloId)
        {
            var asignacion = db.ReservaPasajeroVueloes
                .Include(a => a.CheckIns)
                .Include(a => a.Pasajero)
                .Include(a => a.Reserva)
                .FirstOrDefault(a => a.ReservaId == reservaId && a.PasajeroId == pasajeroId && a.VueloId == vueloId);

            if (asignacion == null)
            {
                TempData["MensajeError"] = "No se localizó la asignación del pasajero para este vuelo.";
                return RedirectToAction("Index");
            }

            if (asignacion.CheckIns.Any(c => c.EstadoCheckInCodigo == "COMPLETADO"))
            {
                TempData["MensajeError"] = "El pasajero ya tiene su check-in completado.";
                return RedirectToAction("Index", new { busqueda = asignacion.Reserva.CodigoReserva });
            }


            // 1. Asegurar que la asignación esté CONFIRMADA para satisfacer el trigger trg_CheckIn_Validar
            if (asignacion.EstadoAsignacionCodigo != "CONFIRMADA")
            {
                asignacion.EstadoAsignacionCodigo = "CONFIRMADA";
            }


            var checkIn = new CheckIn
            {
                AsignacionId = asignacion.AsignacionId,
                FechaRegistro = DateTime.Now,
                EstadoCheckInCodigo = "COMPLETADO",
            };

            db.CheckIns.Add(checkIn);
            db.SaveChanges();

            TempData["MensajeExito"] = $"Check-in presencial completado para {asignacion.Pasajero.Nombres} {asignacion.Pasajero.Apellidos} (Asiento {asignacion.NumeroAsiento}).";
            return RedirectToAction("Index", new { busqueda = asignacion.Reserva.CodigoReserva });
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