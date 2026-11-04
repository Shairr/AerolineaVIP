using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    [Authorize(Roles = "Administrador,SuperUsuario")]
    public class AdminController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: /Admin/Index (Pantalla 17 - Menú de Accesos)
        public ActionResult Index()
        {
            return View();
        }

        // GET: /Admin/Reportes (Pantalla 21 - Consultas y Reportes Administrativos Reales)
        [HttpGet]
        public ActionResult Reportes(string tipoConsulta = "OCUPACION", DateTime? fechaDesde = null, DateTime? fechaHasta = null, string ruta = "TODAS", string aeronave = "TODAS", string estado = "TODOS")
        {
            VuelosController.LiberarReservasExpiradas(db);
            var model = ConstruirModeloReportes(tipoConsulta, fechaDesde, fechaHasta, ruta, aeronave, estado);
            return View(model);
        }

        // GET: /Admin/ExportarReporteCsv (Exportación CSV para Pantalla 21)
        [HttpGet]
        public ActionResult ExportarReporteCsv(string tipoConsulta = "OCUPACION", DateTime? fechaDesde = null, DateTime? fechaHasta = null, string ruta = "TODAS", string aeronave = "TODAS", string estado = "TODOS")
        {
            var model = ConstruirModeloReportes(tipoConsulta, fechaDesde, fechaHasta, ruta, aeronave, estado);
            var sb = new StringBuilder();

            if (model.TipoConsulta == "RESERVAS")
            {
                sb.AppendLine("CodigoReserva,FechaCreacion,Ruta,FechaVuelo,Pasajeros,ImporteTotalUSD,CheckIn,EstadoReserva");
                foreach (var r in model.ReservasPorEstado)
                {
                    sb.AppendLine($"{r.CodigoReserva},{r.FechaCreacion:yyyy-MM-dd HH:mm},\"{r.Ruta}\",{r.FechaVuelo:yyyy-MM-dd},{r.CantidadPasajeros},{r.ImporteTotal:F2},{(r.TieneCheckIn ? "SI" : "NO")},{r.EstadoReserva}");
                }
            }
            else if (model.TipoConsulta == "COMPRAS")
            {
                sb.AppendLine("CompraId,CodigoReserva,FechaPago,ImporteUSD,EstadoCompra,EstadoPago,MetodoMarca,Ultimos4,CodigoOperacion");
                foreach (var c in model.ComprasPagos)
                {
                    sb.AppendLine($"{c.CompraId},{c.CodigoReserva},{c.FechaPago:yyyy-MM-dd HH:mm},{c.Importe:F2},{c.EstadoCompra},{c.EstadoPago},\"{c.MetodoYMarca}\",{c.UltimosCuatro},{c.CodigoOperacion}");
                }
            }
            else
            {
                sb.AppendLine("VueloId,CodigoVuelo,FechaOperacion,Horario,Ruta,Aeronave,Capacidad,ConfirmadosRojo,ReservadosMorado,DisponiblesVerde,PorcentajeOcupacion,EstadoVuelo");
                foreach (var o in model.OcupacionVuelos)
                {
                    sb.AppendLine($"{o.VueloId},{o.CodigoVuelo},{o.FechaOperacion:yyyy-MM-dd},\"{o.Horario}\",\"{o.Ruta}\",{o.AeronaveMatricula},{o.Capacidad},{o.OcupadosConfirmados},{o.ReservadosPendientes},{o.Disponibles},{o.PorcentajeOcupacion:F1}%,{o.EstadoVuelo}");
                }
            }

            byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            string fileName = $"Reporte_{model.TipoConsulta}_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return File(bytes, "text/csv", fileName);
        }

        private AdminReportesViewModel ConstruirModeloReportes(string tipoConsulta, DateTime? fechaDesde, DateTime? fechaHasta, string ruta, string aeronave, string estado)
        {
            string tipo = string.IsNullOrWhiteSpace(tipoConsulta) ? "OCUPACION" : tipoConsulta.Trim().ToUpperInvariant();
            DateTime desde = fechaDesde ?? new DateTime(2026, 3, 1);
            DateTime hasta = fechaHasta ?? new DateTime(2026, 12, 31);

            var model = new AdminReportesViewModel
            {
                TipoConsulta = tipo,
                FechaDesde = desde,
                FechaHasta = hasta,
                RutaFiltro = string.IsNullOrWhiteSpace(ruta) ? "TODAS" : ruta,
                AeronaveFiltro = string.IsNullOrWhiteSpace(aeronave) ? "TODAS" : aeronave,
                EstadoFiltro = string.IsNullOrWhiteSpace(estado) ? "TODOS" : estado.ToUpperInvariant()
            };

            // 1. Reporte: Ocupación por Vuelo
            var qVuelos = db.Vueloes
                .Include(v => v.Aeronave.ModeloAeronave)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Include(v => v.ReservaPasajeroVueloes)
                .Where(v => DbFunctions.TruncateTime(v.FechaOperacion) >= desde.Date && DbFunctions.TruncateTime(v.FechaOperacion) <= hasta.Date);

            if (model.RutaFiltro != "TODAS")
            {
                var partes = model.RutaFiltro.Split('-');
                if (partes.Length == 2)
                {
                    string o = partes[0].Trim();
                    string d = partes[1].Trim();
                    qVuelos = qVuelos.Where(v => v.Frecuencia.Ruta.Aeropuerto.CodigoIATA == o && v.Frecuencia.Ruta.Aeropuerto1.CodigoIATA == d);
                }
            }

            if (model.AeronaveFiltro != "TODAS")
            {
                qVuelos = qVuelos.Where(v => v.Aeronave.Matricula == model.AeronaveFiltro);
            }

            if (model.EstadoFiltro != "TODOS" && tipo == "OCUPACION")
            {
                qVuelos = qVuelos.Where(v => v.EstadoVueloCodigo == model.EstadoFiltro);
            }

            var listaVuelos = qVuelos.OrderBy(v => v.FechaOperacion).ThenBy(v => v.Frecuencia.HoraSalida).ToList();
            foreach (var v in listaVuelos)
            {
                int cap = v.Aeronave?.ModeloAeronave?.CapacidadAsientos ?? 7;
                int conf = v.ReservaPasajeroVueloes.Count(a => a.EstadoAsignacionCodigo == "CONFIRMADA");
                int pend = v.ReservaPasajeroVueloes.Count(a => a.EstadoAsignacionCodigo == "PENDIENTE");
                int disp = Math.Max(0, cap - conf - pend);
                decimal pct = cap > 0 ? Math.Round(((decimal)(conf + pend) / cap) * 100m, 1) : 0m;

                model.OcupacionVuelos.Add(new ReporteOcupacionItemViewModel
                {
                    VueloId = v.VueloId,
                    CodigoVuelo = "VIP" + v.VueloId.ToString("D5"),
                    FechaOperacion = v.FechaOperacion,
                    Horario = $"{v.Frecuencia.HoraSalida:hh\\:mm} - {v.Frecuencia.HoraLlegada:hh\\:mm}",
                    Ruta = $"{v.Frecuencia.Ruta.Aeropuerto.CodigoIATA} → {v.Frecuencia.Ruta.Aeropuerto1.CodigoIATA}",
                    AeronaveMatricula = v.Aeronave?.Matricula ?? "AV-01",
                    Capacidad = cap,
                    OcupadosConfirmados = conf,
                    ReservadosPendientes = pend,
                    Disponibles = disp,
                    EstadoVuelo = v.EstadoVueloCodigo,
                    PorcentajeOcupacion = pct
                });
            }

            // 2. Reporte: Reservas por Estado
            var qReservas = db.Reservas
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.CheckIns))
                .Include(r => r.Compras)
                .Where(r => DbFunctions.TruncateTime(r.FechaCreacion) >= desde.Date && DbFunctions.TruncateTime(r.FechaCreacion) <= hasta.Date);

            if (model.EstadoFiltro != "TODOS" && tipo == "RESERVAS")
            {
                qReservas = qReservas.Where(r => r.EstadoReservaCodigo == model.EstadoFiltro);
            }

            foreach (var r in qReservas.OrderByDescending(r => r.FechaCreacion).ToList())
            {
                var primerVuelo = r.ReservaPasajeroVueloes.FirstOrDefault()?.Vuelo;
                string rutaStr = primerVuelo != null
                    ? $"{primerVuelo.Frecuencia.Ruta.Aeropuerto.CodigoIATA} → {primerVuelo.Frecuencia.Ruta.Aeropuerto1.CodigoIATA}"
                    : "UIO → GYE";

                if (model.RutaFiltro != "TODAS")
                {
                    string filtroNorm = model.RutaFiltro.Replace("-", " → ");
                    if (!string.Equals(rutaStr, filtroNorm, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                model.ReservasPorEstado.Add(new ReporteReservaItemViewModel
                {
                    ReservaId = r.ReservaId,
                    CodigoReserva = r.CodigoReserva,
                    FechaCreacion = r.FechaCreacion,
                    EstadoReserva = r.EstadoReservaCodigo,
                    Ruta = rutaStr,
                    FechaVuelo = primerVuelo?.FechaOperacion ?? r.FechaCreacion,
                    CantidadPasajeros = r.ReservaPasajeroVueloes.Count,
                    ImporteTotal = r.Compras.FirstOrDefault()?.Importe ?? 0m,
                    TieneCheckIn = r.ReservaPasajeroVueloes.Any(a => a.CheckIns.Any(c => c.EstadoCheckInCodigo == "COMPLETADO"))
                });
            }

            // 3. Reporte: Compras y Pagos Simulados
            var qPagos = db.Pagoes
                .Include(p => p.Compra.Reserva)
                .Where(p => DbFunctions.TruncateTime(p.FechaPago) >= desde.Date && DbFunctions.TruncateTime(p.FechaPago) <= hasta.Date);

            if (model.EstadoFiltro != "TODOS" && tipo == "COMPRAS")
            {
                qPagos = qPagos.Where(p => p.EstadoPagoCodigo == model.EstadoFiltro || p.Compra.EstadoCompraCodigo == model.EstadoFiltro);
            }

            foreach (var p in qPagos.OrderByDescending(p => p.FechaPago).ToList())
            {
                model.ComprasPagos.Add(new ReporteCompraPagoItemViewModel
                {
                    CompraId = p.CompraId,
                    CodigoReserva = p.Compra?.Reserva?.CodigoReserva ?? "RV000001",
                    FechaPago = p.FechaPago,
                    Importe = p.Importe,
                    EstadoCompra = p.Compra?.EstadoCompraCodigo ?? "PAGADA",
                    EstadoPago = p.EstadoPagoCodigo,
                    MetodoYMarca = $"{p.MetodoPagoCodigo} ({p.MarcaTarjetaCodigo})",
                    UltimosCuatro = p.UltimosCuatroDigitos,
                    CodigoOperacion = p.CodigoOperacion,
                    MensajeResultado = p.MensajeResultado
                });
            }

            return model;
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
