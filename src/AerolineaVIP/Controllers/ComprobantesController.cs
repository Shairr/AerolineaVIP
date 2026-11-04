using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models;
using Microsoft.AspNet.Identity;

namespace AerolineaVIP.Controllers
{
    /// <summary>
    /// Controlador dedicado al Módulo de Comprobantes Electrónicos Académicos (MOD-08 — Pantalla 16).
    /// Permite consultar, filtrar por fechas/tipo/reserva, previsualizar con cabecera DatosEmisor (DP-05) e imprimir/descargar PDF.
    /// </summary>
    [Authorize(Roles = "Cliente,Counter,SuperUsuario,Administrador")]
    public class ComprobantesController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Comprobantes/Index (Pantalla 16)
        [HttpGet]
        public ActionResult Index(string id = null, int? comprobanteId = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null, string tipo = "TODOS", string codigoReserva = null)
        {
            // Si el parámetro de ruta {id} es un código de reserva (ej: RV000001), usarlo como filtro inicial
            int? idSeleccionado = comprobanteId;
            if (!string.IsNullOrWhiteSpace(id))
            {
                if (int.TryParse(id, out int parsedId))
                {
                    idSeleccionado = parsedId;
                }
                else if (string.IsNullOrWhiteSpace(codigoReserva))
                {
                    codigoReserva = id.Trim().ToUpperInvariant();
                }
            }

            bool esPersonalInterno = User.IsInRole("SuperUsuario") || User.IsInRole("Administrador") || User.IsInRole("Counter");
            string currentUserId = User.Identity.GetUserId() ?? "";
            string currentUserName = User.Identity.Name ?? "";
            bool esClienteDemo = string.Equals(currentUserName, "cliente01", StringComparison.OrdinalIgnoreCase);
            string demoGuid = "7c1e4a20-0001-4b6e-9a01-000000000001";

            var query = db.Comprobantes
                .Include(c => c.Compra.Reserva.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                .Include(c => c.Compra.Reserva.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                .Include(c => c.Compra.Reserva.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                .Include(c => c.Compra.Pagoes)
                .AsQueryable();

            if (!esPersonalInterno)
            {
                query = query.Where(c =>
                    c.Compra.Reserva.UsuarioId == currentUserId ||
                    c.Compra.Reserva.UsuarioId == currentUserName ||
                    (esClienteDemo && c.Compra.Reserva.UsuarioId == demoGuid));
            }

            if (fechaDesde.HasValue)
            {
                DateTime desde = fechaDesde.Value.Date;
                query = query.Where(c => DbFunctions.TruncateTime(c.FechaEmision) >= desde);
            }

            if (fechaHasta.HasValue)
            {
                DateTime hasta = fechaHasta.Value.Date;
                query = query.Where(c => DbFunctions.TruncateTime(c.FechaEmision) <= hasta);
            }

            if (!string.IsNullOrWhiteSpace(tipo) && !tipo.Equals("TODOS", StringComparison.OrdinalIgnoreCase))
            {
                string tipoUpper = tipo.Trim().ToUpperInvariant();
                query = query.Where(c => c.TipoComprobanteCodigo == tipoUpper);
            }

            if (!string.IsNullOrWhiteSpace(codigoReserva))
            {
                string codResUpper = codigoReserva.Trim().ToUpperInvariant();
                query = query.Where(c => c.Compra.Reserva.CodigoReserva.Contains(codResUpper) || c.NumeroComprobante.Contains(codResUpper));
            }

            var listaDb = query
                .OrderByDescending(c => c.FechaEmision)
                .ThenByDescending(c => c.ComprobanteId)
                .ToList();

            var datosEmisor = db.DatosEmisors.FirstOrDefault();

            var viewModel = new ComprobantesIndexViewModel
            {
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                TipoFiltro = string.IsNullOrWhiteSpace(tipo) ? "TODOS" : tipo.ToUpperInvariant(),
                CodigoReservaFiltro = codigoReserva,
                EmisorRazonSocial = datosEmisor?.RazonSocial ?? "AEROLINEA VIP S.A. - PROYECTO ACADEMICO DIT",
                EmisorRuc = datosEmisor?.RucSimulado ?? "1799999999001",
                EmisorDireccion = datosEmisor?.DireccionMatriz ?? "Aeropuerto Internacional Mariscal Sucre, Tababela - Quito, Ecuador",
                EmisorTelefono = datosEmisor?.Telefono ?? "(02) 399-0000",
                EmisorCorreo = datosEmisor?.CorreoContacto ?? "facturacion@aerolineavip.ec",
                LeyendaAcademica = datosEmisor?.LeyendaComprobante ?? "Comprobante académico de una operación simulada. No constituye factura tributaria ni documento autorizado."
            };

            foreach (var c in listaDb)
            {
                var reserva = c.Compra?.Reserva;
                var primerAsig = reserva?.ReservaPasajeroVueloes?.FirstOrDefault();
                var vuelo = primerAsig?.Vuelo;
                string rutaStr = vuelo != null
                    ? $"{vuelo.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA ?? "UIO"} → {vuelo.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA ?? "GYE"}"
                    : "UIO → GYE";

                viewModel.Comprobantes.Add(new ComprobanteListadoItemViewModel
                {
                    ComprobanteId = c.ComprobanteId,
                    NumeroComprobante = c.NumeroComprobante,
                    TipoComprobanteCodigo = c.TipoComprobanteCodigo,
                    EstadoComprobanteCodigo = c.EstadoComprobanteCodigo,
                    FechaEmision = c.FechaEmision,
                    CodigoReserva = reserva?.CodigoReserva ?? "RV000001",
                    ReceptorNombre = c.ReceptorNombre,
                    ReceptorDocumento = c.ReceptorDocumento,
                    ReceptorCorreo = c.ReceptorCorreo,
                    MontoTotal = c.MontoTotal,
                    Observaciones = c.Observaciones,
                    RutaResumen = rutaStr,
                    FechaVuelo = vuelo?.FechaOperacion ?? c.FechaEmision
                });
            }

            if (viewModel.Comprobantes.Any())
            {
                viewModel.ComprobanteSeleccionado = idSeleccionado.HasValue
                    ? (viewModel.Comprobantes.FirstOrDefault(x => x.ComprobanteId == idSeleccionado.Value) ?? viewModel.Comprobantes.First())
                    : viewModel.Comprobantes.First();
            }

            return View(viewModel);
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
