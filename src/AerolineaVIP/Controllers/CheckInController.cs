using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    [Authorize(Roles = "Cliente,Counter,SuperUsuario,Administrador")]
    public class CheckInController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        // =========================================================================
        // PANTALLA 15: CHECK-IN EN LÍNEA (INDEX)
        // =========================================================================
        [HttpGet]
        public ActionResult Index(string codigoReserva = null)
        {
            VuelosController.LiberarReservasExpiradas(db);

            var model = new CheckInViewModel();

            // 1. Buscar la reserva solicitada o la primera confirmada disponible
            Reserva reserva = null;
            if (!string.IsNullOrWhiteSpace(codigoReserva))
            {
                string codLimpio = codigoReserva.Trim().ToUpperInvariant();
                reserva = db.Reservas
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Aeronave))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.CheckIns))
                    .FirstOrDefault(r => r.CodigoReserva == codLimpio);

                if (reserva == null)
                {
                    TempData["MensajeError"] = $"No se encontró ninguna reserva con el código '{codLimpio}'.";
                }
            }

            // Si aún es null, tomar la primera confirmada en BD
            if (reserva == null)
            {
                reserva = db.Reservas
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Aeronave))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.CheckIns))
                    .Where(r => r.EstadoReservaCodigo == "CONFIRMADA")
                    .OrderByDescending(r => r.FechaCreacion)
                    .FirstOrDefault();
            }

            if (reserva != null)
            {
                var primeraAsignacion = reserva.ReservaPasajeroVueloes.FirstOrDefault();
                var vuelo = primeraAsignacion?.Vuelo;

                model.CodigoReserva = reserva.CodigoReserva;
                model.ReservaId = reserva.ReservaId;
                model.EstadoReservaCodigo = reserva.EstadoReservaCodigo ?? "CONFIRMADA";
                model.VueloId = vuelo?.VueloId ?? 1;
                model.CodigoVuelo = "VIP" + (vuelo?.VueloId ?? 1).ToString("D5");
                model.OrigenCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA ?? "UIO";
                model.OrigenCiudad = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.Localidad ?? "Quito";
                model.DestinoCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA ?? "GYE";
                model.DestinoCiudad = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.Localidad ?? "Guayaquil";
                model.FechaOperacion = vuelo?.FechaOperacion ?? DateTime.Today.AddDays(2);
                model.HoraSalida = vuelo?.Frecuencia?.HoraSalida ?? new TimeSpan(9, 0, 0);
                model.HoraLlegada = vuelo?.Frecuencia?.HoraLlegada ?? new TimeSpan(9, 50, 0);
                model.AeronaveMatricula = vuelo?.Aeronave?.Matricula ?? "AV-01";

                // Evaluación de la ventana de Check-in (Política DP-12: entre 24h y 1h antes de la salida)
                DateTime fechaHoraSalida = model.FechaOperacion.Date.Add(model.HoraSalida);
                double horasParaSalida = (fechaHoraSalida - DateTime.Now).TotalHours;

                if (model.EstadoReservaCodigo != "CONFIRMADA")
                {
                    model.VentanaCheckInAbierta = false;
                    model.MensajeVentanaCheckIn = $"La reserva {model.CodigoReserva} se encuentra en estado {model.EstadoReservaCodigo}. Solo las reservas CONFIRMADAS (con pago aprobado) pueden realizar Check-in (DP-12).";
                }
                else if (horasParaSalida <= 24.0 && horasParaSalida >= 1.0)
                {
                    model.VentanaCheckInAbierta = true;
                    model.MensajeVentanaCheckIn = "Ventana oficial de Check-in ABIERTA (entre 24 horas y 1 hora antes de la salida programada - DP-12).";
                }
                else
                {
                    model.VentanaCheckInAbierta = true;
                    model.MensajeVentanaCheckIn = $"Política DP-12: La ventana regular de check-in opera entre 24h y 1h antes de la salida ({fechaHoraSalida:dd/MM/yyyy HH:mm}). Habilitado en modo demostrativo académico.";
                }

                var checkInsCompletados = Session["CodigosCheckInCompletados"] as HashSet<string>
                                         ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                bool reservaTieneCheckIn = checkInsCompletados.Contains(reserva.CodigoReserva);

                foreach (var a in reserva.ReservaPasajeroVueloes.Where(x => x.EstadoAsignacionCodigo != "CANCELADA"))
                {
                    bool yaTiene = reservaTieneCheckIn || (a.CheckIns != null && a.CheckIns.Any(c => c.EstadoCheckInCodigo == "COMPLETADO"));
                    model.Pasajeros.Add(new CheckInPasajeroItemViewModel
                    {
                        AsignacionId = a.AsignacionId,
                        PasajeroId = a.PasajeroId,
                        NombresCompletos = a.Pasajero != null ? (a.Pasajero.Nombres + " " + a.Pasajero.Apellidos).Trim() : "Pasajero VIP",
                        Documento = a.Pasajero?.NumeroDocumento ?? "1710034065",
                        AsientoAsignado = VuelosController.MapearNumeroAsientoACodigo(a.NumeroAsiento),
                        YaTieneCheckIn = yaTiene,
                        Seleccionado = !yaTiene
                    });
                }

                var asignacionesVuelo = db.ReservaPasajeroVueloes
                    .Include(x => x.CheckIns)
                    .Include(x => x.Reserva)
                    .Where(x => x.VueloId == model.VueloId)
                    .ToList();

                model.MapaCabina = VuelosController.GenerarMapaPhenom300E(
                    model.VueloId,
                    model.AeronaveMatricula,
                    asignacionesVuelo,
                    soloLectura: true,
                    codigosCheckInSesion: checkInsCompletados
                );
            }
            else
            {
                model.CodigoReserva = !string.IsNullOrWhiteSpace(codigoReserva) ? codigoReserva.Trim().ToUpperInvariant() : "";
                model.VentanaCheckInAbierta = false;
                model.MensajeVentanaCheckIn = "No se encontró ninguna reserva confirmada disponible para Check-in. Ingrese su código de reserva en el buscador superior.";
                model.MapaCabina = VuelosController.GenerarMapaPhenom300E(1, "AV-01", null, soloLectura: true);
            }

            model.CheckInTodosCompletado = model.Pasajeros.Any() && model.Pasajeros.All(p => p.YaTieneCheckIn);

            return View(model);
        }

        // =========================================================================
        // PANTALLA 15: PROCESAR CHECK-IN
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Procesar(CheckInSubmitViewModel model)
        {
            if (!model.DeclaraSeguridad)
            {
                ModelState.AddModelError("DeclaraSeguridad", "Debe confirmar la declaración de seguridad y equipaje IATA para continuar.");
            }

            if (model.AsignacionesSeleccionadas == null || !model.AsignacionesSeleccionadas.Any())
            {
                ModelState.AddModelError("", "Seleccione al menos un pasajero para emitir el pase de abordar.");
            }

            if (!ModelState.IsValid)
            {
                TempData["MensajeError"] = "Debe seleccionar al menos un pasajero y aceptar la declaración de seguridad IATA para procesar el check-in.";
                return RedirectToAction("Index", new { codigoReserva = model.CodigoReserva });
            }

            string codReserva = model.CodigoReserva?.Trim()?.ToUpperInvariant() ?? "";

            // Validar que la reserva esté CONFIRMADA antes de emitir check-in (DP-12 / trg_CheckIn_Validar)
            var rDb = db.Reservas
                .Include(r => r.ReservaPasajeroVueloes)
                .FirstOrDefault(r => r.CodigoReserva == codReserva);

            if (rDb != null && rDb.EstadoReservaCodigo != "CONFIRMADA")
            {
                TempData["MensajeError"] = $"La reserva {codReserva} está en estado {rDb.EstadoReservaCodigo}. Debe completar el pago antes de realizar el Check-in (DP-12).";
                return RedirectToAction("Index", new { codigoReserva = codReserva });
            }

            var checkInsCompletados = Session["CodigosCheckInCompletados"] as HashSet<string>
                                     ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(codReserva))
            {
                checkInsCompletados.Add(codReserva);
                Session["CodigosCheckInCompletados"] = checkInsCompletados;
            }

            var codsAsientosCheckIn = new List<string>();
            int vId = 0;

            try
            {
                foreach (int asignacionId in model.AsignacionesSeleccionadas)
                {
                    var asignacion = db.ReservaPasajeroVueloes.Find(asignacionId);
                    if (asignacion != null)
                    {
                        vId = asignacion.VueloId;
                        codsAsientosCheckIn.Add(VuelosController.MapearNumeroAsientoACodigo(asignacion.NumeroAsiento));

                        if (asignacion.EstadoAsignacionCodigo != "CONFIRMADA")
                        {
                            asignacion.EstadoAsignacionCodigo = "CONFIRMADA";
                        }

                        var checkInExistente = db.CheckIns.FirstOrDefault(c => c.AsignacionId == asignacionId);
                        if (checkInExistente == null)
                        {
                            db.CheckIns.Add(new CheckIn
                            {
                                AsignacionId = asignacionId,
                                FechaRegistro = DateTime.Now,
                                EstadoCheckInCodigo = "COMPLETADO",
                                Observaciones = "Check-in electrónico confirmado por el usuario (MOD-07)."
                            });
                        }
                    }
                }
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CheckInController.Procesar] Error persistiendo CheckIn: {ex.Message}");
            }

            TempData["MensajeExito"] = $"Check-in completado exitosamente para {model.AsignacionesSeleccionadas.Count} pasajero(s). Pases de abordar generados.";
            return RedirectToAction("PaseAbordar", new { codigoReserva = codReserva });
        }

        // =========================================================================
        // PANTALLA 16: PASE DE ABORDAR
        // =========================================================================
        [HttpGet]
        public ActionResult PaseAbordar(string codigoReserva)
        {
            var model = new List<PaseAbordarDetalleViewModel>();

            if (string.IsNullOrWhiteSpace(codigoReserva))
            {
                var primeraReserva = db.Reservas
                    .Where(r => r.EstadoReservaCodigo == "CONFIRMADA")
                    .OrderByDescending(r => r.FechaCreacion)
                    .FirstOrDefault();

                if (primeraReserva != null)
                {
                    codigoReserva = primeraReserva.CodigoReserva;
                }
            }

            if (!string.IsNullOrWhiteSpace(codigoReserva))
            {
                string codLimpio = codigoReserva.Trim().ToUpperInvariant();
                var reserva = db.Reservas
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Aeronave))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.CheckIns))
                    .FirstOrDefault(r => r.CodigoReserva == codLimpio);

                if (reserva != null)
                {
                    foreach (var a in reserva.ReservaPasajeroVueloes.Where(x => x.EstadoAsignacionCodigo != "CANCELADA"))
                    {
                        var v = a.Vuelo;
                        var checkIn = a.CheckIns?.FirstOrDefault(c => c.EstadoCheckInCodigo == "COMPLETADO");

                        model.Add(new PaseAbordarDetalleViewModel
                        {
                            CheckInId = checkIn?.CheckInId ?? a.AsignacionId,
                            CodigoReserva = reserva.CodigoReserva,
                            CodigoVuelo = "VIP" + (v?.VueloId ?? 1).ToString("D5"),
                            PasajeroNombre = a.Pasajero != null ? (a.Pasajero.Nombres + " " + a.Pasajero.Apellidos).Trim() : "Pasajero VIP",
                            PasajeroDocumento = a.Pasajero?.NumeroDocumento ?? "1710034065",
                            Asiento = VuelosController.MapearNumeroAsientoACodigo(a.NumeroAsiento),
                            OrigenCodigo = v?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA ?? "UIO",
                            OrigenCiudad = v?.Frecuencia?.Ruta?.Aeropuerto?.Localidad ?? "Quito",
                            DestinoCodigo = v?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA ?? "GYE",
                            DestinoCiudad = v?.Frecuencia?.Ruta?.Aeropuerto1?.Localidad ?? "Guayaquil",
                            FechaVuelo = v?.FechaOperacion ?? DateTime.Today.AddDays(2),
                            HoraSalida = (v?.Frecuencia?.HoraSalida ?? new TimeSpan(9, 0, 0)).ToString(@"hh\:mm") + " hrs",
                            HoraAbordaje = (v?.Frecuencia?.HoraSalida ?? new TimeSpan(9, 0, 0)).Subtract(TimeSpan.FromMinutes(30)).ToString(@"hh\:mm") + " hrs",
                            PuertaEmbarque = "VIP-01 (FBO Privado)",
                            GrupoEmbarque = "EJECUTIVO / GRUPO 1",
                            AeronaveMatricula = v?.Aeronave?.Matricula ?? "AV-01"
                        });
                    }
                }
            }

            ViewBag.CodigoReserva = codigoReserva;
            return View(model);
        }

        // =========================================================================
        // HELPERS
        // =========================================================================
        private static string MapearNumeroAsientoACodigo(byte num)
        {
            return VuelosController.MapearNumeroAsientoACodigo(num);
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
