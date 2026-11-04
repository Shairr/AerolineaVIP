using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    public class VuelosController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        /// <summary>
        /// Libera automáticamente las reservas en estado PENDIENTE cuya ventana de retención (20 min - DP-07) haya expirado,
        /// pasando la reserva a EXPIRADA y los asientos a LIBERADA (Disponibles en Verde).
        /// </summary>
        public static void LiberarReservasExpiradas(AerolineaVIPEntities context)
        {
            if (context == null) return;
            try
            {
                DateTime ahora = DateTime.Now;
                var expiradas = context.Reservas
                    .Include(r => r.ReservaPasajeroVueloes)
                    .Where(r => r.EstadoReservaCodigo == "PENDIENTE"
                             && r.FechaExpiracion.HasValue
                             && r.FechaExpiracion.Value < ahora)
                    .ToList();

                if (expiradas.Any())
                {
                    foreach (var res in expiradas)
                    {
                        res.EstadoReservaCodigo = "EXPIRADA";
                        res.FechaActualizacion = ahora;
                        res.Observaciones = (res.Observaciones ?? "") + " | Expirada automáticamente por superar 20 min sin pago (DP-07).";
                        foreach (var rpv in res.ReservaPasajeroVueloes.Where(a => a.EstadoAsignacionCodigo == "PENDIENTE"))
                        {
                            rpv.EstadoAsignacionCodigo = "LIBERADA";
                            LiberarAsientos(rpv.VueloId, new[] { MapearNumeroAsientoACodigo(rpv.NumeroAsiento) });
                        }
                    }
                    context.SaveChanges();
                }
            }
            catch
            {
                // Ignorar errores de concurrencia en limpieza automática
            }
        }

        // GET: Vuelos/Buscar (Pantalla 5)
        [HttpGet]
        public ActionResult Buscar(string origen = null, string destino = null, DateTime? fechaSalida = null, int? pasajeros = null, string tipoViaje = null)
        {
            LiberarReservasExpiradas(db);

            var model = new BusquedaVueloViewModel();
            CargarAeropuertosEnModelo(model);

            if (!string.IsNullOrWhiteSpace(origen)) model.OrigenCodigo = origen;
            if (!string.IsNullOrWhiteSpace(destino)) model.DestinoCodigo = destino;
            if (pasajeros.HasValue && pasajeros.Value >= 1 && pasajeros.Value <= 7) model.CantidadPasajeros = pasajeros.Value;
            if (!string.IsNullOrWhiteSpace(tipoViaje)) model.TipoViaje = tipoViaje;

            // Obtener rutas y días de operación reales de la BD para la guía informativa
            ViewBag.RutasFrecuencias = ObtenerRutasConFrecuencias();

            // Sugerir la fecha del primer vuelo futuro disponible en la BD
            var proximoVuelo = db.Vueloes
                .Where(v => v.EstadoVueloCodigo != "CANCELADO" && v.FechaOperacion >= DateTime.Today)
                .OrderBy(v => v.FechaOperacion)
                .FirstOrDefault();

            model.FechaSalida = fechaSalida ?? (proximoVuelo != null ? proximoVuelo.FechaOperacion : DateTime.Today.AddDays(1));
            model.FechaRegreso = model.FechaSalida.Value.AddDays(3);

            return View(model);
        }

        // GET: Vuelos/Resultados (Pantalla 6)
        [HttpGet]
        public ActionResult Resultados(string origen = "UIO", string destino = "GYE", DateTime? fechaSalida = null, DateTime? fechaRegreso = null, string tipoViaje = "solo-ida", int pasajeros = 1)
        {
            LiberarReservasExpiradas(db);

            if (string.IsNullOrWhiteSpace(origen)) origen = "UIO";
            if (string.IsNullOrWhiteSpace(destino)) destino = "GYE";
            if (pasajeros < 1) pasajeros = 1;
            if (pasajeros > 7) pasajeros = 7;

            var resultado = new ResultadoBusquedaViewModel
            {
                Criterios = new BusquedaVueloViewModel
                {
                    OrigenCodigo = origen,
                    DestinoCodigo = destino,
                    FechaSalida = fechaSalida,
                    FechaRegreso = fechaRegreso,
                    TipoViaje = tipoViaje,
                    CantidadPasajeros = pasajeros
                },
                RutasDisponibles = ObtenerRutasConFrecuencias()
            };

            CargarAeropuertosEnModelo(resultado.Criterios);

            bool esConexionUioGps = (origen == "UIO" && destino == "GPS") || (origen == "GPS" && destino == "UIO");
            string origenConsulta = origen;
            string destinoConsulta = destino;

            if (esConexionUioGps)
            {
                // DP-04 / DP-09 / DP-10: UIO <-> GPS opera los domingos con conexión en Guayaquil (GYE) y tarifa combinada USD 1,100
                origenConsulta = (origen == "UIO") ? "GYE" : "GPS";
                destinoConsulta = (origen == "UIO") ? "GPS" : "GYE";
            }

            // Consulta base de vuelos reales en Entity Framework con todas sus relaciones (solo aeronaves DISPONIBLES - RNP-08)
            var query = db.Vueloes
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Include(v => v.Aeronave.ModeloAeronave)
                .AsNoTracking()
                .Include(v => v.ReservaPasajeroVueloes.Select(rpv => rpv.CheckIns))
                .Include(v => v.ReservaPasajeroVueloes.Select(rpv => rpv.Reserva))
                .Where(v => v.Frecuencia.Ruta.Aeropuerto.CodigoIATA == origenConsulta
                         && v.Frecuencia.Ruta.Aeropuerto1.CodigoIATA == destinoConsulta
                         && v.EstadoVueloCodigo != "CANCELADO"
                         && v.Aeronave.EstadoAeronaveCodigo == "DISPONIBLE");

            List<Vuelo> vuelosDb = new List<Vuelo>();

            if (fechaSalida.HasValue)
            {
                var fechaBuscada = fechaSalida.Value.Date;
                vuelosDb = query.Where(v => DbFunctions.TruncateTime(v.FechaOperacion) == fechaBuscada)
                                .OrderBy(v => v.Frecuencia.HoraSalida)
                                .ToList();

                if (vuelosDb.Any())
                {
                    resultado.FechaConVuelos = true;
                }
                else
                {
                    // Si no hay vuelos en esa fecha exacta, consultar vuelos reales más cercanos en la misma ruta
                    vuelosDb = query.Where(v => v.FechaOperacion >= fechaBuscada || v.FechaOperacion >= DateTime.Today)
                                    .OrderBy(v => v.FechaOperacion)
                                    .ThenBy(v => v.Frecuencia.HoraSalida)
                                    .Take(6)
                                    .ToList();

                    resultado.FechaConVuelos = false;
                    resultado.MensajeInformativo = "No se encontraron operaciones en la fecha seleccionada. A continuación se presentan los vuelos programados en la base de datos más cercanos para esta ruta.";
                }
            }
            else
            {
                // Sin fecha específica: mostrar los próximos vuelos programados en la BD
                vuelosDb = query.Where(v => DbFunctions.TruncateTime(v.FechaOperacion) >= DateTime.Today)
                                .OrderBy(v => v.FechaOperacion)
                                .ThenBy(v => v.Frecuencia.HoraSalida)
                                .Take(6)
                                .ToList();

                if (!vuelosDb.Any())
                {
                    vuelosDb = query.OrderBy(v => v.FechaOperacion)
                                    .ThenBy(v => v.Frecuencia.HoraSalida)
                                    .Take(6)
                                    .ToList();
                }

                resultado.FechaConVuelos = vuelosDb.Any();
            }

            var aeroUio = esConexionUioGps ? db.Aeropuertoes.FirstOrDefault(a => a.CodigoIATA == "UIO") : null;
            var aeroGps = esConexionUioGps ? db.Aeropuertoes.FirstOrDefault(a => a.CodigoIATA == "GPS") : null;

            // Mapeo a ViewModels con cálculo dinámico de disponibilidad y tarifas de BD
            foreach (var v in vuelosDb)
            {
                int capacidad = v.Aeronave?.ModeloAeronave?.CapacidadAsientos ?? 7;
                int ocupados = v.ReservaPasajeroVueloes.Count(r => r.EstadoAsignacionCodigo != "CANCELADA" && r.EstadoAsignacionCodigo != "LIBERADA");
                int disponibles = Math.Max(0, capacidad - ocupados);

                decimal tarifaBase = esConexionUioGps
                    ? 1100.00m
                    : (v.Frecuencia?.Ruta?.TarifaReferencial ?? 450.00m);
                decimal tarifaFlex = Math.Round(tarifaBase * 1.35m, 2);

                TimeSpan horaSalidaMostrada = v.Frecuencia.HoraSalida;
                TimeSpan horaLlegadaMostrada = v.Frecuencia.HoraLlegada;

                if (esConexionUioGps)
                {
                    if (origen == "UIO")
                    {
                        horaSalidaMostrada = new TimeSpan(7, 0, 0); // Tramo 1 UIO->GYE sale 07:00, conecta GYE->GPS 09:00-11:00
                    }
                    else
                    {
                        horaLlegadaMostrada = new TimeSpan(16, 0, 0); // Tramo 1 GPS->GYE 12:00-14:00, conecta GYE->UIO 15:00-16:00
                    }
                }

                TimeSpan duracion = (horaLlegadaMostrada >= horaSalidaMostrada)
                    ? (horaLlegadaMostrada - horaSalidaMostrada)
                    : (horaLlegadaMostrada.Add(TimeSpan.FromHours(24)) - horaSalidaMostrada);

                string duracionStr = duracion.Hours > 0
                    ? $"{duracion.Hours}h {duracion.Minutes:D2}m"
                    : $"{duracion.Minutes} min";

                bool esDirecto = !esConexionUioGps && v.Frecuencia.Ruta.Aeropuerto1.CodigoIATA != "GPS";

                resultado.Vuelos.Add(new VueloItemViewModel
                {
                    VueloId = v.VueloId,
                    FechaOperacion = v.FechaOperacion,
                    DiaSemanaNombre = ObtenerNombreDiaSemana(v.Frecuencia.DiaSemana),
                    HoraSalida = horaSalidaMostrada,
                    HoraLlegada = horaLlegadaMostrada,
                    DuracionTexto = duracionStr,
                    OrigenCodigo = esConexionUioGps ? origen : v.Frecuencia.Ruta.Aeropuerto.CodigoIATA,
                    OrigenNombre = esConexionUioGps
                        ? (origen == "UIO" ? (aeroUio?.Nombre ?? "Aeropuerto Internacional Mariscal Sucre") : (aeroGps?.Nombre ?? "Aeropuerto Ecológico Galápagos Seymour"))
                        : v.Frecuencia.Ruta.Aeropuerto.Nombre,
                    OrigenCiudad = esConexionUioGps
                        ? (origen == "UIO" ? "Quito" : "Isla Baltra - Galápagos")
                        : v.Frecuencia.Ruta.Aeropuerto.Localidad,
                    DestinoCodigo = esConexionUioGps ? destino : v.Frecuencia.Ruta.Aeropuerto1.CodigoIATA,
                    DestinoNombre = esConexionUioGps
                        ? (destino == "GPS" ? (aeroGps?.Nombre ?? "Aeropuerto Ecológico Galápagos Seymour") : (aeroUio?.Nombre ?? "Aeropuerto Internacional Mariscal Sucre"))
                        : v.Frecuencia.Ruta.Aeropuerto1.Nombre,
                    DestinoCiudad = esConexionUioGps
                        ? (destino == "GPS" ? "Isla Baltra - Galápagos" : "Quito")
                        : v.Frecuencia.Ruta.Aeropuerto1.Localidad,
                    AeronaveMatricula = v.Aeronave?.Matricula ?? "AV-01",
                    ModeloAeronave = v.Aeronave?.ModeloAeronave?.Nombre ?? "Phenom 300E",
                    CapacidadTotal = capacidad,
                    AsientosOcupados = ocupados,
                    AsientosDisponibles = disponibles,
                    TarifaClassic = tarifaBase,
                    TarifaFlex = tarifaFlex,
                    EstadoVueloCodigo = v.EstadoVueloCodigo,
                    EsVueloDirecto = esDirecto,
                    EscalaInfo = esConexionUioGps
                        ? "Itinerario con escala en Guayaquil (GYE) · Turnaround 60 min (DP-03/DP-04)"
                        : (esDirecto ? null : "Vuelo directo insular (GYE ↔ GPS)")
                });
            }

            // Construir franja de calendario semanal de tarifas reales
            DateTime fechaReferencia = fechaSalida ?? (resultado.Vuelos.FirstOrDefault()?.FechaOperacion ?? DateTime.Today);
            resultado.CalendarioSemanal = GenerarCalendarioSemanalReal(origen, destino, fechaReferencia);

            return View(resultado);
        }

        private void CargarAeropuertosEnModelo(BusquedaVueloViewModel model)
        {
            var aeropuertos = db.Aeropuertoes
                .Where(a => a.Activo)
                .OrderBy(a => a.Localidad)
                .ToList();

            model.AeropuertosDisponibles = aeropuertos.Select(a => new SelectListItem
            {
                Value = a.CodigoIATA,
                Text = $"{a.Localidad} ({a.CodigoIATA}) - {a.Nombre}"
            }).ToList();
        }

        private List<RutaInfoViewModel> ObtenerRutasConFrecuencias()
        {
            var rutas = db.Rutas
                .Include(r => r.Aeropuerto)
                .Include(r => r.Aeropuerto1)
                .Include(r => r.Frecuencias)
                .Where(r => r.Activa)
                .ToList();

            var list = new List<RutaInfoViewModel>();
            foreach (var r in rutas)
            {
                var dias = r.Frecuencias
                    .Where(f => f.Activa)
                    .Select(f => f.DiaSemana)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                string diasTexto = FormatearDiasSemana(dias);

                var horas = r.Frecuencias
                    .Where(f => f.Activa)
                    .Select(f => f.HoraSalida.ToString(@"hh\:mm"))
                    .Distinct()
                    .OrderBy(h => h)
                    .ToList();

                list.Add(new RutaInfoViewModel
                {
                    RutaId = r.RutaId,
                    OrigenCodigo = r.Aeropuerto.CodigoIATA,
                    OrigenCiudad = r.Aeropuerto.Localidad,
                    DestinoCodigo = r.Aeropuerto1.CodigoIATA,
                    DestinoCiudad = r.Aeropuerto1.Localidad,
                    TarifaReferencial = r.TarifaReferencial,
                    DiasOperacion = diasTexto,
                    HorariosResumen = string.Join(", ", horas)
                });
            }

            // Añadir información de rutas en conexión dominical UIO <-> GPS vía GYE (DP-04 / DP-09 / DP-10)
            list.Add(new RutaInfoViewModel
            {
                RutaId = 901,
                OrigenCodigo = "UIO",
                OrigenCiudad = "Quito",
                DestinoCodigo = "GPS",
                DestinoCiudad = "Isla Baltra - Galápagos (vía GYE)",
                TarifaReferencial = 1100.00m,
                DiasOperacion = "Domingo",
                HorariosResumen = "07:00 (Conexión en GYE 09:00)"
            });

            list.Add(new RutaInfoViewModel
            {
                RutaId = 902,
                OrigenCodigo = "GPS",
                OrigenCiudad = "Isla Baltra - Galápagos",
                DestinoCodigo = "UIO",
                DestinoCiudad = "Quito (vía GYE)",
                TarifaReferencial = 1100.00m,
                DiasOperacion = "Domingo",
                HorariosResumen = "12:00 (Conexión en GYE 15:00)"
            });

            return list;
        }

        private List<DiaTarifaViewModel> GenerarCalendarioSemanalReal(string origen, string destino, DateTime fechaBase)
        {
            var calendario = new List<DiaTarifaViewModel>();
            int diffLunes = ((int)fechaBase.DayOfWeek == 0) ? -6 : (1 - (int)fechaBase.DayOfWeek);
            DateTime inicioSemana = fechaBase.Date.AddDays(diffLunes);

            bool esConexionUioGps = (origen == "UIO" && destino == "GPS") || (origen == "GPS" && destino == "UIO");
            string origenConsulta = esConexionUioGps ? (origen == "UIO" ? "GYE" : "GPS") : origen;
            string destinoConsulta = esConexionUioGps ? (origen == "UIO" ? "GPS" : "GYE") : destino;

            for (int i = 0; i < 7; i++)
            {
                DateTime dia = inicioSemana.AddDays(i);
                var vuelosDia = db.Vueloes
                    .Where(v => v.Frecuencia.Ruta.Aeropuerto.CodigoIATA == origenConsulta
                             && v.Frecuencia.Ruta.Aeropuerto1.CodigoIATA == destinoConsulta
                             && v.EstadoVueloCodigo != "CANCELADO"
                             && v.Aeronave.EstadoAeronaveCodigo == "DISPONIBLE"
                             && DbFunctions.TruncateTime(v.FechaOperacion) == dia.Date)
                    .ToList();

                decimal? minTarifa = vuelosDia.Any()
                    ? (esConexionUioGps ? 1100.00m : vuelosDia.Min(v => v.Frecuencia.Ruta.TarifaReferencial))
                    : (decimal?)null;

                calendario.Add(new DiaTarifaViewModel
                {
                    Fecha = dia,
                    DiaSemanaCorto = ObtenerDiaCorto(dia.DayOfWeek),
                    FechaCorta = dia.ToString("dd MMM"),
                    PrecioMinimo = minTarifa,
                    TieneVuelos = vuelosDia.Any(),
                    EsSeleccionado = dia.Date == fechaBase.Date
                });
            }

            return calendario;
        }

        private static string ObtenerNombreDiaSemana(byte diaSemana)
        {
            switch (diaSemana)
            {
                case 1: return "Lunes";
                case 2: return "Martes";
                case 3: return "Miércoles";
                case 4: return "Jueves";
                case 5: return "Viernes";
                case 6: return "Sábado";
                case 7: return "Domingo";
                default: return "Día " + diaSemana;
            }
        }

        private static string ObtenerDiaCorto(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Monday: return "Lun";
                case DayOfWeek.Tuesday: return "Mar";
                case DayOfWeek.Wednesday: return "Mié";
                case DayOfWeek.Thursday: return "Jue";
                case DayOfWeek.Friday: return "Vie";
                case DayOfWeek.Saturday: return "Sáb";
                case DayOfWeek.Sunday: return "Dom";
                default: return "";
            }
        }

        private static string FormatearDiasSemana(List<byte> dias)
        {
            if (dias == null || !dias.Any()) return "A consultar";
            var nombres = dias.Select(d => ObtenerNombreDiaSemana(d)).ToList();
            if (nombres.Count == 1) return nombres[0];
            return string.Join(", ", nombres.Take(nombres.Count - 1)) + " y " + nombres.Last();
        }

        // GET: Vuelos/Disponibilidad/5 (Pantalla 7)
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult Disponibilidad(int? id)
        {
            LiberarReservasExpiradas(db);

            var query = db.Vueloes
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Include(v => v.Aeronave.ModeloAeronave)
                .AsNoTracking()
                .Include(v => v.ReservaPasajeroVueloes.Select(rpv => rpv.CheckIns))
                .Include(v => v.ReservaPasajeroVueloes.Select(rpv => rpv.Reserva));

            Vuelo vuelo = null;
            if (id.HasValue && id.Value > 0)
            {
                vuelo = query.FirstOrDefault(v => v.VueloId == id.Value);
            }

            if (vuelo == null)
            {
                vuelo = query.FirstOrDefault(v => v.EstadoVueloCodigo != "CANCELADO");
            }

            if (vuelo == null)
            {
                return RedirectToAction("Buscar");
            }

            TimeSpan duracion = (vuelo.Frecuencia.HoraLlegada >= vuelo.Frecuencia.HoraSalida)
                ? (vuelo.Frecuencia.HoraLlegada - vuelo.Frecuencia.HoraSalida)
                : (vuelo.Frecuencia.HoraLlegada.Add(TimeSpan.FromHours(24)) - vuelo.Frecuencia.HoraSalida);

            string duracionStr = duracion.Hours > 0
                ? $"{duracion.Hours}h {duracion.Minutes:D2}m"
                : $"{duracion.Minutes} min";

            decimal tarifaBase = vuelo.Frecuencia?.Ruta?.TarifaReferencial ?? 450.00m;
            decimal tarifaFlex = Math.Round(tarifaBase * 1.35m, 2);

            // Obtener ocupaciones de este vuelo (combinando DB, sesión y memoria global)
            var asignaciones = ObtenerAsignacionesCombinadas(
                vuelo.VueloId,
                vuelo.ReservaPasajeroVueloes,
                Session["MisReservasNuevas"] as List<ReservaResumenItemViewModel>
            );

            var checkInsSes = Session["CodigosCheckInCompletados"] as HashSet<string>;
            var mapa = GenerarMapaPhenom300E(vuelo.VueloId, vuelo.Aeronave?.Matricula ?? "AV-01", asignaciones, soloLectura: true, codigosCheckInSesion: checkInsSes);

            int ocupados = mapa.Asientos.Count(a => a.EstadoVisual == "OCUPADO");
            int reservados = mapa.Asientos.Count(a => a.EstadoVisual == "RESERVADO");
            int disponibles = mapa.Asientos.Count(a => a.EstadoVisual == "DISPONIBLE");

            var model = new DisponibilidadVueloViewModel
            {
                VueloId = vuelo.VueloId,
                CodigoVuelo = "VIP" + vuelo.VueloId.ToString("D5"),
                FechaOperacion = vuelo.FechaOperacion,
                DiaSemanaNombre = ObtenerNombreDiaSemana(vuelo.Frecuencia.DiaSemana),
                HoraSalida = vuelo.Frecuencia.HoraSalida,
                HoraLlegada = vuelo.Frecuencia.HoraLlegada,
                DuracionTexto = duracionStr,
                OrigenCodigo = vuelo.Frecuencia.Ruta.Aeropuerto.CodigoIATA,
                OrigenCiudad = vuelo.Frecuencia.Ruta.Aeropuerto.Localidad,
                OrigenNombre = vuelo.Frecuencia.Ruta.Aeropuerto.Nombre,
                DestinoCodigo = vuelo.Frecuencia.Ruta.Aeropuerto1.CodigoIATA,
                DestinoCiudad = vuelo.Frecuencia.Ruta.Aeropuerto1.Localidad,
                DestinoNombre = vuelo.Frecuencia.Ruta.Aeropuerto1.Nombre,
                AeronaveMatricula = vuelo.Aeronave?.Matricula ?? "AV-01",
                ModeloAeronave = vuelo.Aeronave?.ModeloAeronave?.Nombre ?? "Phenom 300E",
                CapacidadTotal = 7,
                AsientosOcupados = ocupados,
                AsientosReservados = reservados,
                AsientosDisponibles = disponibles,
                EstadoVueloCodigo = vuelo.EstadoVueloCodigo,
                TarifaClassic = tarifaBase,
                TarifaFlex = tarifaFlex,
                MapaCabina = mapa
            };

            return View(model);
        }

        // Control de ocupaciÃ³n y liberaciÃ³n gestionado directamente en SQL Server (dbo.ReservaPasajeroVuelo y dbo.CheckIn)
        public static void RegistrarAsientosOcupados(int vueloId, IEnumerable<string> codigos)
        {
            // Persistencia directa en base de datos; no requiere bloqueo de memoria
        }

        public static void RegistrarCheckInCompletado(string codigoReserva, int vueloId, IEnumerable<string> codigos = null)
        {
            // Persistencia directa en base de datos; no requiere bloqueo de memoria
        }

        public static void LiberarAsientos(int vueloId, IEnumerable<string> codigos)
        {
            // Persistencia directa en base de datos; no requiere bloqueo de memoria
        }

        public static MapaCabinaViewModel GenerarMapaPhenom300E(
            int vueloId, 
            string matricula, 
            List<ReservaPasajeroVuelo> asignaciones, 
            bool soloLectura, 
            int maxSeleccionables = 1, 
            string preseleccionados = "",
            IEnumerable<string> codigosCheckInSesion = null)
        {
            var listaSeleccionados = (preseleccionados ?? "")
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToUpperInvariant())
                .ToList();

            // Lectura dinamica directa desde dbo.Asiento en SQL Server
            List<Asiento> asientosDesdeBd = null;
            try
            {
                using (var dbContext = new AerolineaVIPEntities())
                {
                    asientosDesdeBd = dbContext.Asientoes.OrderBy(a => a.NumeroAsiento).ToList();
                }
            }
            catch { }

            var listaAsientosDefinicion = (asientosDesdeBd != null && asientosDesdeBd.Any())
                ? asientosDesdeBd.Select(a => new { Num = (int)a.NumeroAsiento, Cod = a.Codigo, Fila = a.Fila, Lado = a.Lado, Ubic = a.Ubicacion }).ToList()
                : new[]
                {
                    new { Num = 1, Cod = "1D", Fila = (byte)1, Lado = "D", Ubic = "Fila 1, lado derecho, ventanilla (junto a cabina de mando)" },
                    new { Num = 2, Cod = "2D", Fila = (byte)2, Lado = "D", Ubic = "Fila 2, lado derecho, ventanilla" },
                    new { Num = 3, Cod = "2I", Fila = (byte)2, Lado = "I", Ubic = "Fila 2, lado izquierdo, ventanilla (tras puerta de acceso)" },
                    new { Num = 4, Cod = "3D", Fila = (byte)3, Lado = "D", Ubic = "Fila 3, lado derecho, ventanilla (mesa ejecutiva)" },
                    new { Num = 5, Cod = "3I", Fila = (byte)3, Lado = "I", Ubic = "Fila 3, lado izquierdo, ventanilla (mesa ejecutiva)" },
                    new { Num = 6, Cod = "4D", Fila = (byte)4, Lado = "D", Ubic = "Fila 4, lado derecho, ventanilla (junto al baÃ±o VIP)" },
                    new { Num = 7, Cod = "4I", Fila = (byte)4, Lado = "I", Ubic = "Fila 4, lado izquierdo, ventanilla" }
                }.ToList();

            var mapa = new MapaCabinaViewModel
            {
                VueloId = vueloId,
                AeronaveMatricula = matricula,
                ModeloAeronave = "Embraer Phenom 300E",
                EsSoloLectura = soloLectura,
                MaxAsientosSeleccionables = maxSeleccionables,
                AsientosSeleccionadosCodigos = preseleccionados
            };

            foreach (var item in listaAsientosDefinicion)
            {
                var asig = asignaciones?.FirstOrDefault(a => a.NumeroAsiento == item.Num
                    && a.EstadoAsignacionCodigo != "CANCELADA"
                    && a.EstadoAsignacionCodigo != "LIBERADA");

                string visual = "DISPONIBLE";
                string color = "Verde";
                bool tieneCheckIn = false;

                if (asig != null)
                {
                    // Verificar si cuenta con check-in completado
                    if (asig.CheckIns != null && asig.CheckIns.Any(c => c.EstadoCheckInCodigo == "COMPLETADO"))
                    {
                        tieneCheckIn = true;
                    }
                    else if (asig.Reserva != null && !string.IsNullOrWhiteSpace(asig.Reserva.CodigoReserva))
                    {
                        string codRes = asig.Reserva.CodigoReserva.Trim().ToUpperInvariant();
                        if (
                            (codigosCheckInSesion != null && codigosCheckInSesion.Contains(codRes, StringComparer.OrdinalIgnoreCase)))
                        {
                            tieneCheckIn = true;
                        }
                    }



                    // Regla oficial DP-11 / RG-18 / Sección 6.5 del Enunciado:
                    // - Disponible (Verde): sin asignación activa (o CANCELADA / LIBERADA)
                    // - Reservado (Morado): reserva creada pendiente de pago (EstadoAsignacionCodigo == "PENDIENTE")
                    // - Ocupado (Rojo): compra confirmada / pago aprobado (EstadoAsignacionCodigo == "CONFIRMADA")
                    bool esPendientePago = string.Equals(asig.EstadoAsignacionCodigo, "PENDIENTE", StringComparison.OrdinalIgnoreCase)
                        || (asig.Reserva != null && string.Equals(asig.Reserva.EstadoReservaCodigo, "PENDIENTE", StringComparison.OrdinalIgnoreCase));

                    if (esPendientePago && !tieneCheckIn)
                    {
                        visual = "RESERVADO";
                        color = "Morado";
                    }
                    else
                    {
                        visual = "OCUPADO";
                        color = "Rojo";
                    }
                }

                bool seleccionado = listaSeleccionados.Contains(item.Cod.ToUpperInvariant());

                mapa.Asientos.Add(new AsientoEstadoViewModel
                {
                    NumeroAsiento = item.Num,
                    CodigoAsiento = item.Cod,
                    Fila = item.Fila,
                    Lado = item.Lado,
                    Ubicacion = item.Ubic,
                    EstadoVisual = visual,
                    ColorEstado = color,
                    EsSeleccionado = seleccionado,
                    EsAccesible = (visual == "DISPONIBLE"),
                    TieneCheckIn = tieneCheckIn
                });
            }

            return mapa;
        }

        public static byte MapearCodigoANumeroAsiento(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return 1;
            switch (codigo.Trim().ToUpperInvariant())
            {
                case "1D": return 1;
                case "2D": return 2;
                case "2I": return 3;
                case "3D": return 4;
                case "3I": return 5;
                case "4D": return 6;
                case "4I": return 7;
                default:
                    byte num;
                    return byte.TryParse(codigo, out num) ? num : (byte)1;
            }
        }

        public static string MapearNumeroAsientoACodigo(byte num)
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
                default: return num.ToString();
            }
        }

        public static List<ReservaPasajeroVuelo> ObtenerAsignacionesCombinadas(
            int vueloId, 
            IEnumerable<ReservaPasajeroVuelo> asignacionesDb, 
            IEnumerable<ReservaResumenItemViewModel> reservasSesion = null)
        {
            var resultado = asignacionesDb != null 
                ? asignacionesDb.Where(a => a.EstadoAsignacionCodigo != "CANCELADA" && a.EstadoAsignacionCodigo != "LIBERADA").ToList() 
                : new List<ReservaPasajeroVuelo>();

            // 1. Integrar reservas de la sesión del usuario si aún no están en el listado de BD
            if (reservasSesion != null)
            {
                foreach (var r in reservasSesion)
                {
                    if (r.VueloId == vueloId && r.EstadoReservaCodigo != "CANCELADA" && r.EstadoReservaCodigo != "EXPIRADA" && !string.IsNullOrWhiteSpace(r.AsientosCodigos))
                    {
                        var cods = r.AsientosCodigos.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var cod in cods)
                        {
                            byte num = MapearCodigoANumeroAsiento(cod);
                            var asigExistente = resultado.FirstOrDefault(a => a.NumeroAsiento == num);
                            if (asigExistente == null)
                            {
                                var nuevaAsig = new ReservaPasajeroVuelo
                                {
                                    VueloId = vueloId,
                                    NumeroAsiento = num,
                                    EstadoAsignacionCodigo = (r.EstadoReservaCodigo == "PENDIENTE") ? "PENDIENTE" : "CONFIRMADA",
                                    ImporteTramo = 450.00m,
                                    FechaAsignacion = r.FechaCreacion,
                                    Reserva = new Reserva
                                    {
                                        CodigoReserva = r.CodigoReserva,
                                        EstadoReservaCodigo = r.EstadoReservaCodigo
                                    }
                                };
                                if (r.TieneCheckIn)
                                {
                                    nuevaAsig.CheckIns = new List<CheckIn>
                                    {
                                        new CheckIn { EstadoCheckInCodigo = "COMPLETADO", FechaRegistro = DateTime.Now }
                                    };
                                }
                                resultado.Add(nuevaAsig);
                            }
                            else if (r.TieneCheckIn)
                            {
                                if (asigExistente.CheckIns == null)
                                {
                                    asigExistente.CheckIns = new List<CheckIn>();
                                }
                                if (!asigExistente.CheckIns.Any(c => c.EstadoCheckInCodigo == "COMPLETADO"))
                                {
                                    asigExistente.CheckIns.Add(new CheckIn
                                    {
                                        EstadoCheckInCodigo = "COMPLETADO",
                                        FechaRegistro = DateTime.Now
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return resultado;
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
