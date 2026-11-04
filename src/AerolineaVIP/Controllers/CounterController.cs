using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    /// <summary>
    /// MÓDULO MOD-09: Asistencia de Counter (Mostrador)
    /// Centraliza la atención presencial de ventanilla para búsqueda autorizada de reservas,
    /// reasignación de asientos disponibles, cobro en mostrador, cancelación y asistencia directa de check-in.
    /// </summary>
    [Authorize(Roles = "Counter,Administrador,SuperUsuario")]
    public class CounterController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: /Counter/Index
        public ActionResult Index(string busqueda = null, string fecha = null)
        {
            VuelosController.LiberarReservasExpiradas(db);

            DateTime fechaFiltro = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(fecha) && DateTime.TryParse(fecha, out DateTime fParseada))
            {
                fechaFiltro = fParseada;
            }
            else
            {
                var proximoVuelo = db.Vueloes
                    .Where(v => v.EstadoVueloCodigo != "CANCELADO" && DbFunctions.TruncateTime(v.FechaOperacion) >= DateTime.Today)
                    .OrderBy(v => v.FechaOperacion)
                    .FirstOrDefault();

                if (proximoVuelo != null)
                {
                    fechaFiltro = proximoVuelo.FechaOperacion.Date;
                }
            }

            // 1. Vuelos programados para la fecha seleccionada
            var vuelosDelDia = db.Vueloes
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Include(v => v.Aeronave.ModeloAeronave)
                .Include(v => v.EstadoVuelo)
                .Include(v => v.ReservaPasajeroVueloes)
                .Where(v => DbFunctions.TruncateTime(v.FechaOperacion) == DbFunctions.TruncateTime(fechaFiltro))
                .OrderBy(v => v.Frecuencia.HoraSalida)
                .ToList();

            // 2. Consulta de reservas (si hay término filtra por Código, Cédula o Apellidos; si está vacío muestra las 15 más recientes)
            var queryReservas = db.Reservas
                .Include(r => r.EstadoReserva)
                .Include(r => r.ReservaPasajeroVueloes.Select(rpv => rpv.Pasajero))
                .Include(r => r.ReservaPasajeroVueloes.Select(rpv => rpv.Vuelo.Frecuencia.Ruta.Aeropuerto))
                .Include(r => r.ReservaPasajeroVueloes.Select(rpv => rpv.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                .Include(r => r.ReservaPasajeroVueloes.Select(rpv => rpv.CheckIns))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string b = busqueda.Trim().ToUpperInvariant();
                queryReservas = queryReservas.Where(r =>
                    r.CodigoReserva.Contains(b) ||
                    r.ReservaPasajeroVueloes.Any(rpv => rpv.Pasajero.NumeroDocumento.Contains(b) ||
                                                       rpv.Pasajero.Apellidos.ToUpper().Contains(b) ||
                                                       rpv.Pasajero.Nombres.ToUpper().Contains(b)));
            }

            var reservasEncontradas = queryReservas
                .OrderByDescending(r => r.FechaCreacion)
                .Take(20)
                .ToList();

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

            if (asignacion.Reserva.EstadoReservaCodigo != "CONFIRMADA")
            {
                TempData["MensajeError"] = $"La reserva {asignacion.Reserva.CodigoReserva} se encuentra en estado {asignacion.Reserva.EstadoReservaCodigo}. Debe procesarse el pago antes del Check-in (DP-12).";
                return RedirectToAction("Index", new { busqueda = asignacion.Reserva.CodigoReserva });
            }

            if (asignacion.CheckIns.Any(c => c.EstadoCheckInCodigo == "COMPLETADO"))
            {
                TempData["MensajeError"] = "El pasajero ya tiene su check-in completado.";
                return RedirectToAction("Index", new { busqueda = asignacion.Reserva.CodigoReserva });
            }

            if (asignacion.EstadoAsignacionCodigo != "CONFIRMADA")
            {
                asignacion.EstadoAsignacionCodigo = "CONFIRMADA";
            }

            var checkIn = new CheckIn
            {
                AsignacionId = asignacion.AsignacionId,
                FechaRegistro = DateTime.Now,
                EstadoCheckInCodigo = "COMPLETADO",
                Observaciones = $"Check-in presencial asistido en Counter por {User.Identity.Name} (MOD-09)."
            };

            db.CheckIns.Add(checkIn);
            db.SaveChanges();

            string codSeat = VuelosController.MapearNumeroAsientoACodigo(asignacion.NumeroAsiento);
            VuelosController.RegistrarCheckInCompletado(asignacion.Reserva.CodigoReserva, asignacion.VueloId, new[] { codSeat });

            TempData["MensajeExito"] = $"Check-in presencial completado para {asignacion.Pasajero.Nombres} {asignacion.Pasajero.Apellidos} (Asiento {codSeat} - Código BP-{checkIn.CheckInId:D6}).";
            return RedirectToAction("Index", new { busqueda = asignacion.Reserva.CodigoReserva });
        }

        // POST: /Counter/CambiarAsiento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarAsiento(int asignacionId, byte nuevoAsiento, string busquedaRetorno = null)
        {
            if (nuevoAsiento < 1 || nuevoAsiento > 7)
            {
                TempData["MensajeError"] = "El número de asiento debe estar entre 1 (1D) y 7 (4I).";
                return RedirectToAction("Index", new { busqueda = busquedaRetorno });
            }

            var asignacion = db.ReservaPasajeroVueloes
                .Include(a => a.Reserva)
                .Include(a => a.Pasajero)
                .Include(a => a.CheckIns)
                .FirstOrDefault(a => a.AsignacionId == asignacionId);

            if (asignacion == null)
            {
                TempData["MensajeError"] = "No se encontró la asignación de asiento solicitada.";
                return RedirectToAction("Index", new { busqueda = busquedaRetorno });
            }

            if (asignacion.CheckIns.Any(c => c.EstadoCheckInCodigo == "COMPLETADO"))
            {
                TempData["MensajeError"] = "No se puede reasignar el asiento de un pasajero que ya completó el Check-in.";
                return RedirectToAction("Index", new { busqueda = asignacion.Reserva.CodigoReserva });
            }

            bool ocupado = db.ReservaPasajeroVueloes.Any(a =>
                a.VueloId == asignacion.VueloId &&
                a.AsignacionId != asignacion.AsignacionId &&
                a.NumeroAsiento == nuevoAsiento &&
                a.EstadoAsignacionCodigo != "CANCELADA" &&
                a.EstadoAsignacionCodigo != "LIBERADA");

            string codNuevo = VuelosController.MapearNumeroAsientoACodigo(nuevoAsiento);
            if (ocupado)
            {
                TempData["MensajeError"] = $"El asiento {codNuevo} ya se encuentra ocupado o reservado en este vuelo.";
                return RedirectToAction("Index", new { busqueda = asignacion.Reserva.CodigoReserva });
            }

            string codAnterior = VuelosController.MapearNumeroAsientoACodigo(asignacion.NumeroAsiento);
            asignacion.NumeroAsiento = nuevoAsiento;
            db.SaveChanges();

            VuelosController.LiberarAsientos(asignacion.VueloId, new[] { codAnterior });
            if (asignacion.EstadoAsignacionCodigo == "CONFIRMADA")
            {
                VuelosController.RegistrarAsientosOcupados(asignacion.VueloId, new[] { codNuevo });
            }

            TempData["MensajeExito"] = $"Asiento de {asignacion.Pasajero?.Nombres} {asignacion.Pasajero?.Apellidos} actualizado de {codAnterior} a {codNuevo}.";
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
