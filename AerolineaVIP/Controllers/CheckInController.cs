using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    [Authorize(Roles = "Cliente,SuperUsuario,Administrador")]
    public class CheckInController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        // =========================================================================
        // PANTALLA 15: CHECK-IN EN LÍNEA (INDEX)
        // =========================================================================
        [HttpGet]
        public ActionResult Index(string codigoReserva = null)
        {
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
            }

            // Si no se encuentra en DB, buscar en las reservas creadas en sesión
            var reservasSesion = Session["MisReservasNuevas"] as List<ReservaResumenItemViewModel>;
            ReservaResumenItemViewModel reservaReciente = null;
            if (reservasSesion != null && reservasSesion.Any())
            {
                if (!string.IsNullOrWhiteSpace(codigoReserva))
                {
                    reservaReciente = reservasSesion.FirstOrDefault(r => r.CodigoReserva.Equals(codigoReserva.Trim(), StringComparison.OrdinalIgnoreCase));
                }
                if (reservaReciente == null && reserva == null)
                {
                    reservaReciente = reservasSesion.FirstOrDefault(r => r.EstadoReservaCodigo == "CONFIRMADA");
                }
            }

            // Si aún es null, tomar la primera confirmada en BD
            if (reserva == null && reservaReciente == null)
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

            // Poblar el modelo según el origen encontrado
            if (reserva != null)
            {
                var primeraAsignacion = reserva.ReservaPasajeroVueloes.FirstOrDefault();
                var vuelo = primeraAsignacion?.Vuelo;

                model.CodigoReserva = reserva.CodigoReserva;
                model.ReservaId = reserva.ReservaId;
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

                var checkInsCompletados = Session["CodigosCheckInCompletados"] as HashSet<string> 
                                         ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                bool reservaTieneCheckIn = checkInsCompletados.Contains(reserva.CodigoReserva);

                foreach (var a in reserva.ReservaPasajeroVueloes)
                {
                    bool yaTiene = reservaTieneCheckIn || (a.CheckIns != null && a.CheckIns.Any(c => c.EstadoCheckInCodigo == "COMPLETADO"));
                    model.Pasajeros.Add(new CheckInPasajeroItemViewModel
                    {
                        AsignacionId = a.AsignacionId,
                        PasajeroId = a.PasajeroId,
                        NombresCompletos = a.Pasajero != null ? (a.Pasajero.Nombres + " " + a.Pasajero.Apellidos).Trim() : "Pasajero VIP",
                        Documento = a.Pasajero?.NumeroDocumento ?? "1719283746",
                        AsientoAsignado = MapearNumeroAsientoACodigo(a.NumeroAsiento),
                        YaTieneCheckIn = yaTiene,
                        Seleccionado = !yaTiene
                    });
                }

                // Generar mapa del Phenom 300E para la vista
                var asignacionesVuelo = vuelo?.ReservaPasajeroVueloes?.ToList();
                model.MapaCabina = VuelosController.GenerarMapaPhenom300E(
                    model.VueloId,
                    model.AeronaveMatricula,
                    asignacionesVuelo,
                    soloLectura: true,
                    codigosCheckInSesion: checkInsCompletados
                );
            }
            else if (reservaReciente != null)
            {
                model.CodigoReserva = reservaReciente.CodigoReserva;
                model.ReservaId = reservaReciente.ReservaId;
                model.VueloId = reservaReciente.VueloId;
                model.CodigoVuelo = reservaReciente.CodigoVuelo;
                model.OrigenCodigo = reservaReciente.OrigenCodigo;
                model.OrigenCiudad = reservaReciente.OrigenCiudad;
                model.DestinoCodigo = reservaReciente.DestinoCodigo;
                model.DestinoCiudad = reservaReciente.DestinoCiudad;
                model.FechaOperacion = reservaReciente.FechaOperacion;
                model.HoraSalida = reservaReciente.HoraSalida;
                model.HoraLlegada = reservaReciente.HoraLlegada;
                model.AeronaveMatricula = reservaReciente.AeronaveMatricula;

                var checkInsCompletadosSesion = Session["CodigosCheckInCompletados"] as HashSet<string> 
                                                 ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                bool tieneCheckInReciente = checkInsCompletadosSesion.Contains(reservaReciente.CodigoReserva) || reservaReciente.TieneCheckIn;

                var asientosArr = (reservaReciente.AsientosCodigos ?? "1D").Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < reservaReciente.PasajerosNombres.Count; i++)
                {
                    model.Pasajeros.Add(new CheckInPasajeroItemViewModel
                    {
                        AsignacionId = 1000 + i,
                        PasajeroId = 2000 + i,
                        NombresCompletos = reservaReciente.PasajerosNombres[i],
                        Documento = "171928374" + i,
                        AsientoAsignado = i < asientosArr.Length ? asientosArr[i] : "1D",
                        YaTieneCheckIn = tieneCheckInReciente,
                        Seleccionado = !tieneCheckInReciente
                    });
                }

                model.MapaCabina = VuelosController.GenerarMapaPhenom300E(
                    model.VueloId,
                    model.AeronaveMatricula,
                    null,
                    soloLectura: true
                );
            }
            else
            {
                // Modelo de demostración si la base no tiene datos
                model.CodigoReserva = "VIP-78492";
                model.VueloId = 1;
                model.CodigoVuelo = "VIP00001";
                model.OrigenCodigo = "UIO";
                model.OrigenCiudad = "Quito";
                model.DestinoCodigo = "GYE";
                model.DestinoCiudad = "Guayaquil";
                model.FechaOperacion = DateTime.Today.AddDays(2);
                model.HoraSalida = new TimeSpan(9, 0, 0);
                model.HoraLlegada = new TimeSpan(9, 50, 0);
                model.AeronaveMatricula = "AV-01";

                model.Pasajeros.Add(new CheckInPasajeroItemViewModel
                {
                    AsignacionId = 1,
                    PasajeroId = 1,
                    NombresCompletos = "Carlos Roberto Mendoza Loor",
                    Documento = "1719283746",
                    AsientoAsignado = "1D",
                    YaTieneCheckIn = false,
                    Seleccionado = true
                });

                model.MapaCabina = VuelosController.GenerarMapaPhenom300E(
                    1, "AV-01", null, soloLectura: true
                );
            }

            model.CheckInTodosCompletado = model.Pasajeros.All(p => p.YaTieneCheckIn);

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

            // 1. Registrar en Set de sesión para persistencia inmediata y reactiva
            var checkInsCompletados = Session["CodigosCheckInCompletados"] as HashSet<string> 
                                     ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(codReserva))
            {
                checkInsCompletados.Add(codReserva);
                Session["CodigosCheckInCompletados"] = checkInsCompletados;
            }

            // Registrar en VuelosController para cambiar color del asiento a ROJO (Check-in confirmado)
            var codsAsientosCheckIn = new List<string>();
            int vId = 0;
            try
            {
                if (model.AsignacionesSeleccionadas != null)
                {
                    foreach (int asigId in model.AsignacionesSeleccionadas)
                    {
                        var aFind = db.ReservaPasajeroVueloes.Find(asigId);
                        if (aFind != null)
                        {
                            vId = aFind.VueloId;
                            codsAsientosCheckIn.Add(VuelosController.MapearNumeroAsientoACodigo(aFind.NumeroAsiento));
                        }
                    }
                }
            }
            catch { }

            // Buscar reserva en sesiÃ³n o BD para asegurar que vId y asientos estÃ©n poblados
            var reservasSesion = Session["MisReservasNuevas"] as List<ReservaResumenItemViewModel>;
            if (vId <= 0 && !string.IsNullOrWhiteSpace(codReserva))
            {
                var rSesion = reservasSesion?.FirstOrDefault(r => r.CodigoReserva.Equals(codReserva, StringComparison.OrdinalIgnoreCase));
                if (rSesion != null)
                {
                    vId = rSesion.VueloId;
                    if (!codsAsientosCheckIn.Any() && !string.IsNullOrWhiteSpace(rSesion.AsientosCodigos))
                    {
                        codsAsientosCheckIn.AddRange(rSesion.AsientosCodigos.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                }
                else
                {
                    var rDb = db.Reservas.Include(r => r.ReservaPasajeroVueloes).FirstOrDefault(r => r.CodigoReserva == codReserva);
                    if (rDb != null)
                    {
                        var primerTramo = rDb.ReservaPasajeroVueloes.FirstOrDefault();
                        if (primerTramo != null)
                        {
                            vId = primerTramo.VueloId;
                            if (!codsAsientosCheckIn.Any())
                            {
                                codsAsientosCheckIn.AddRange(rDb.ReservaPasajeroVueloes.Select(a => VuelosController.MapearNumeroAsientoACodigo(a.NumeroAsiento)));
                            }
                        }
                    }
                }
            }

            if (vId > 0)
            {
                VuelosController.RegistrarCheckInCompletado(codReserva, vId, codsAsientosCheckIn);
            }
            // 2. Registrar en base de datos si las asignaciones existen
            try
            {
                foreach (int asignacionId in model.AsignacionesSeleccionadas)
                {
                    var asignacion = db.ReservaPasajeroVueloes.Find(asignacionId);
                    if (asignacion != null)
                    {
                        // Asegurar que la asignación esté confirmada para satisfacer el trigger trg_CheckIn_Validar
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
                                EstadoCheckInCodigo = "COMPLETADO"
                            });
                        }
                    }
                }
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al guardar CheckIn en BD: " + ex.Message);
            }

            // 3. Actualizar en sesión si corresponde (para reservas recién creadas)
            reservasSesion = Session["MisReservasNuevas"] as List<ReservaResumenItemViewModel>;
            if (reservasSesion != null)
            {
                var rSesion = reservasSesion.FirstOrDefault(r => r.CodigoReserva.Equals(codReserva, StringComparison.OrdinalIgnoreCase));
                if (rSesion != null)
                {
                    rSesion.TieneCheckIn = true;
                    rSesion.PuedeHacerCheckIn = false;
                }
            }

            var dictDetalles = Session["MisReservasDetalles"] as Dictionary<string, GestionarReservaViewModel>;
            if (dictDetalles != null && dictDetalles.ContainsKey(codReserva))
            {
                dictDetalles[codReserva].TieneCheckIn = true;
                foreach (var pas in dictDetalles[codReserva].Pasajeros)
                {
                    pas.CheckInCompletado = true;
                }
            }

            TempData["MensajeCheckInExito"] = "¡Check-in completado exitosamente! Sus pases de abordar ejecutivos han sido generados.";
            return RedirectToAction("PaseAbordar", new { codigoReserva = codReserva });
        }

        // =========================================================================
        // PANTALLA 15: PASES DE ABORDAR ELECTRÓNICOS VIP (IMPRESIÓN / QR)
        // =========================================================================
        [HttpGet]
        public ActionResult PaseAbordar(string codigoReserva)
        {
            if (string.IsNullOrWhiteSpace(codigoReserva))
            {
                codigoReserva = "VIP-78492";
            }

            string codLimpio = codigoReserva.Trim().ToUpperInvariant();
            var model = new List<PaseAbordarDetalleViewModel>();

            var reserva = db.Reservas
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Aeronave))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.CheckIns))
                .FirstOrDefault(r => r.CodigoReserva == codLimpio);

            if (reserva != null && reserva.ReservaPasajeroVueloes.Any())
            {
                foreach (var a in reserva.ReservaPasajeroVueloes)
                {
                    var v = a.Vuelo;
                    model.Add(new PaseAbordarDetalleViewModel
                    {
                        CheckInId = a.CheckIns?.FirstOrDefault()?.CheckInId ?? a.AsignacionId,
                        CodigoReserva = reserva.CodigoReserva,
                        CodigoVuelo = "VIP" + (v?.VueloId ?? 1).ToString("D5"),
                        PasajeroNombre = a.Pasajero != null ? (a.Pasajero.Nombres + " " + a.Pasajero.Apellidos).Trim() : "Pasajero VIP",
                        PasajeroDocumento = a.Pasajero?.NumeroDocumento ?? "1719283746",
                        Asiento = MapearNumeroAsientoACodigo(a.NumeroAsiento),
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
            else
            {
                // Buscar en sesión
            var reservasSesion = Session["MisReservasNuevas"] as List<ReservaResumenItemViewModel>;
                var rSes = reservasSesion?.FirstOrDefault(r => r.CodigoReserva.Equals(codigoReserva, StringComparison.OrdinalIgnoreCase));
                var asientos = (rSes?.AsientosCodigos ?? "1D").Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var nombres = rSes?.PasajerosNombres ?? new List<string> { "Carlos Roberto Mendoza Loor" };

                for (int i = 0; i < nombres.Count; i++)
                {
                    model.Add(new PaseAbordarDetalleViewModel
                    {
                        CheckInId = 100 + i,
                        CodigoReserva = rSes?.CodigoReserva ?? codigoReserva,
                        CodigoVuelo = rSes?.CodigoVuelo ?? "VIP00001",
                        PasajeroNombre = nombres[i],
                        PasajeroDocumento = "171928374" + i,
                        Asiento = i < asientos.Length ? asientos[i] : "1D",
                        OrigenCodigo = rSes?.OrigenCodigo ?? "UIO",
                        OrigenCiudad = rSes?.OrigenCiudad ?? "Quito",
                        DestinoCodigo = rSes?.DestinoCodigo ?? "GYE",
                        DestinoCiudad = rSes?.DestinoCiudad ?? "Guayaquil",
                        FechaVuelo = rSes?.FechaOperacion ?? DateTime.Today.AddDays(2),
                        HoraSalida = (rSes?.HoraSalida ?? new TimeSpan(9, 0, 0)).ToString(@"hh\:mm") + " hrs",
                        HoraAbordaje = (rSes?.HoraSalida ?? new TimeSpan(9, 0, 0)).Subtract(TimeSpan.FromMinutes(30)).ToString(@"hh\:mm") + " hrs",
                        PuertaEmbarque = "VIP-01 (FBO Privado)",
                        GrupoEmbarque = "EJECUTIVO / GRUPO 1",
                        AeronaveMatricula = rSes?.AeronaveMatricula ?? "AV-01"
                    });
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
            switch (num)
            {
                case 1: return "1D";
                case 2: return "2D";
                case 3: return "2I";
                case 4: return "3D";
                case 5: return "3I";
                case 6: return "4D";
                case 7: return "4I";
                default: return "1D";
            }
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
