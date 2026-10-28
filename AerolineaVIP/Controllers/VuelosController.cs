using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    public class VuelosController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Vuelos/Buscar (Pantalla 5)
        [HttpGet]
        public ActionResult Buscar()
        {
            var model = new BusquedaVueloViewModel();
            CargarAeropuertosEnModelo(model);

            // Obtener rutas y días de operación reales de la BD para la guía informativa
            ViewBag.RutasFrecuencias = ObtenerRutasConFrecuencias();

            // Sugerir la fecha del primer vuelo futuro disponible en la BD
            var proximoVuelo = db.Vueloes
                .Where(v => v.EstadoVueloCodigo != "CANCELADO" && v.FechaOperacion >= DateTime.Today)
                .OrderBy(v => v.FechaOperacion)
                .FirstOrDefault();

            model.FechaSalida = proximoVuelo != null ? proximoVuelo.FechaOperacion : DateTime.Today.AddDays(1);
            model.FechaRegreso = model.FechaSalida.Value.AddDays(3);

            return View(model);
        }

        // GET: Vuelos/Resultados (Pantalla 6)
        [HttpGet]
        public ActionResult Resultados(string origen = "UIO", string destino = "GYE", DateTime? fechaSalida = null, DateTime? fechaRegreso = null, string tipoViaje = "solo-ida", int pasajeros = 1)
        {
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

            // Consulta base de vuelos reales en Entity Framework con todas sus relaciones
            var query = db.Vueloes
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Include(v => v.Aeronave.ModeloAeronave)
                .Include(v => v.ReservaPasajeroVueloes)
                .Where(v => v.Frecuencia.Ruta.Aeropuerto.CodigoIATA == origen
                         && v.Frecuencia.Ruta.Aeropuerto1.CodigoIATA == destino
                         && v.EstadoVueloCodigo != "CANCELADO");

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
                vuelosDb = query.OrderBy(v => v.FechaOperacion)
                                .ThenBy(v => v.Frecuencia.HoraSalida)
                                .Take(6)
                                .ToList();

                resultado.FechaConVuelos = vuelosDb.Any();
            }

            // Mapeo a ViewModels con cálculo dinámico de disponibilidad y tarifas de BD
            foreach (var v in vuelosDb)
            {
                int capacidad = v.Aeronave?.ModeloAeronave?.CapacidadAsientos ?? 7;
                int ocupados = v.ReservaPasajeroVueloes.Count(r => r.EstadoAsignacionCodigo != "CANCELADA");
                int disponibles = Math.Max(0, capacidad - ocupados);

                decimal tarifaBase = v.Frecuencia?.Ruta?.TarifaReferencial ?? 450.00m;
                decimal tarifaFlex = Math.Round(tarifaBase * 1.35m, 2);

                TimeSpan duracion = (v.Frecuencia.HoraLlegada >= v.Frecuencia.HoraSalida)
                    ? (v.Frecuencia.HoraLlegada - v.Frecuencia.HoraSalida)
                    : (v.Frecuencia.HoraLlegada.Add(TimeSpan.FromHours(24)) - v.Frecuencia.HoraSalida);

                string duracionStr = duracion.Hours > 0
                    ? $"{duracion.Hours}h {duracion.Minutes:D2}m"
                    : $"{duracion.Minutes} min";

                bool esDirecto = v.Frecuencia.Ruta.Aeropuerto1.CodigoIATA != "GPS";

                resultado.Vuelos.Add(new VueloItemViewModel
                {
                    VueloId = v.VueloId,
                    FechaOperacion = v.FechaOperacion,
                    DiaSemanaNombre = ObtenerNombreDiaSemana(v.Frecuencia.DiaSemana),
                    HoraSalida = v.Frecuencia.HoraSalida,
                    HoraLlegada = v.Frecuencia.HoraLlegada,
                    DuracionTexto = duracionStr,
                    OrigenCodigo = v.Frecuencia.Ruta.Aeropuerto.CodigoIATA,
                    OrigenNombre = v.Frecuencia.Ruta.Aeropuerto.Nombre,
                    OrigenCiudad = v.Frecuencia.Ruta.Aeropuerto.Localidad,
                    DestinoCodigo = v.Frecuencia.Ruta.Aeropuerto1.CodigoIATA,
                    DestinoNombre = v.Frecuencia.Ruta.Aeropuerto1.Nombre,
                    DestinoCiudad = v.Frecuencia.Ruta.Aeropuerto1.Localidad,
                    AeronaveMatricula = v.Aeronave?.Matricula ?? "AV-01",
                    ModeloAeronave = v.Aeronave?.ModeloAeronave?.Nombre ?? "Phenom 300E",
                    CapacidadTotal = capacidad,
                    AsientosOcupados = ocupados,
                    AsientosDisponibles = disponibles,
                    TarifaClassic = tarifaBase,
                    TarifaFlex = tarifaFlex,
                    EstadoVueloCodigo = v.EstadoVueloCodigo,
                    EsVueloDirecto = esDirecto,
                    EscalaInfo = esDirecto ? null : "Escala técnica en Guayaquil (GYE)"
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

            return list;
        }

        private List<DiaTarifaViewModel> GenerarCalendarioSemanalReal(string origen, string destino, DateTime fechaBase)
        {
            var calendario = new List<DiaTarifaViewModel>();
            DateTime inicioSemana = fechaBase.AddDays(-(int)fechaBase.DayOfWeek + 1); // Lunes de esa semana

            for (int i = 0; i < 7; i++)
            {
                DateTime dia = inicioSemana.AddDays(i);
                var vuelosDia = db.Vueloes
                    .Where(v => v.Frecuencia.Ruta.Aeropuerto.CodigoIATA == origen
                             && v.Frecuencia.Ruta.Aeropuerto1.CodigoIATA == destino
                             && v.EstadoVueloCodigo != "CANCELADO"
                             && DbFunctions.TruncateTime(v.FechaOperacion) == dia.Date)
                    .ToList();

                decimal? minTarifa = vuelosDia.Any()
                    ? vuelosDia.Min(v => v.Frecuencia.Ruta.TarifaReferencial)
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
        public ActionResult Disponibilidad(int? id)
        {
            var query = db.Vueloes
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Include(v => v.Aeronave.ModeloAeronave)
                .Include(v => v.ReservaPasajeroVueloes);

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

            // Obtener ocupaciones de este vuelo
            var asignaciones = vuelo.ReservaPasajeroVueloes
                .Where(r => r.EstadoAsignacionCodigo != "CANCELADA")
                .ToList();

            var mapa = GenerarMapaPhenom300E(vuelo.VueloId, vuelo.Aeronave?.Matricula ?? "AV-01", asignaciones, soloLectura: true);

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

        public static MapaCabinaViewModel GenerarMapaPhenom300E(int vueloId, string matricula, List<ReservaPasajeroVuelo> asignaciones, bool soloLectura, int maxSeleccionables = 1, string preseleccionados = "")
        {
            var listaSeleccionados = (preseleccionados ?? "")
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToUpperInvariant())
                .ToList();

            var asientosDefecto = new[]
            {
                new { Num = 1, Cod = "1D", Fila = (byte)1, Lado = "D", Ubic = "Fila 1, lado derecho, ventanilla (junto a cabina de mando)" },
                new { Num = 2, Cod = "2D", Fila = (byte)2, Lado = "D", Ubic = "Fila 2, lado derecho, ventanilla" },
                new { Num = 3, Cod = "2I", Fila = (byte)2, Lado = "I", Ubic = "Fila 2, lado izquierdo, ventanilla (tras puerta de acceso)" },
                new { Num = 4, Cod = "3D", Fila = (byte)3, Lado = "D", Ubic = "Fila 3, lado derecho, ventanilla (mesa ejecutiva)" },
                new { Num = 5, Cod = "3I", Fila = (byte)3, Lado = "I", Ubic = "Fila 3, lado izquierdo, ventanilla (mesa ejecutiva)" },
                new { Num = 6, Cod = "4D", Fila = (byte)4, Lado = "D", Ubic = "Fila 4, lado derecho, ventanilla (junto al baño VIP)" },
                new { Num = 7, Cod = "4I", Fila = (byte)4, Lado = "I", Ubic = "Fila 4, lado izquierdo, ventanilla" }
            };

            var mapa = new MapaCabinaViewModel
            {
                VueloId = vueloId,
                AeronaveMatricula = matricula,
                ModeloAeronave = "Embraer Phenom 300E",
                EsSoloLectura = soloLectura,
                MaxAsientosSeleccionables = maxSeleccionables,
                AsientosSeleccionadosCodigos = preseleccionados
            };

            foreach (var item in asientosDefecto)
            {
                var asig = asignaciones?.FirstOrDefault(a => a.NumeroAsiento == item.Num);

                string visual = "DISPONIBLE";
                string color = "Verde";

                if (asig != null)
                {
                    if (asig.EstadoAsignacionCodigo == "CONFIRMADA")
                    {
                        visual = "OCUPADO";
                        color = "Rojo";
                    }
                    else if (asig.EstadoAsignacionCodigo == "PENDIENTE")
                    {
                        visual = "RESERVADO";
                        color = "Morado";
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
                    EsAccesible = (visual == "DISPONIBLE")
                });
            }

            return mapa;
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
