using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AerolineaVIP.Models
{
    // =========================================================================
    // PANTALLA 13: MIS RESERVAS (PORTAL CLIENTE)
    // =========================================================================
    public class ReservaResumenItemViewModel
    {
        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public string UsuarioId { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaExpiracion { get; set; }
        public string EstadoReservaCodigo { get; set; } // CONFIRMADA, PENDIENTE, CANCELADA, EXPIRADA

        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public string OrigenCodigo { get; set; }
        public string OrigenCiudad { get; set; }
        public string DestinoCodigo { get; set; }
        public string DestinoCiudad { get; set; }
        public DateTime FechaOperacion { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public string AeronaveMatricula { get; set; }
        public string ModeloAeronave { get; set; }

        public int CantidadPasajeros { get; set; }
        public List<string> PasajerosNombres { get; set; } = new List<string>();
        public string AsientosCodigos { get; set; }
        public string Tarifa { get; set; } = "Classic"; // Classic o Flex
        public decimal TotalPagado { get; set; }

        public bool TieneCheckIn { get; set; }
        public bool EsCancelable { get; set; }
        public bool PuedeHacerCheckIn { get; set; }
        private bool? _estaPendientePago;
        public bool EstaPendientePago
        {
            get => _estaPendientePago ?? (string.Equals(EstadoReservaCodigo, "PENDIENTE", StringComparison.OrdinalIgnoreCase)
                                          && (!FechaExpiracion.HasValue || FechaExpiracion.Value > DateTime.Now));
            set => _estaPendientePago = value;
        }
    }

    public class MisReservasViewModel
    {
        public string FiltroActual { get; set; } = "todas"; // todas, pendientes, proximas, completadas, canceladas
        public List<ReservaResumenItemViewModel> Reservas { get; set; } = new List<ReservaResumenItemViewModel>();

        public int TotalPendientes { get; set; }
        public int TotalProximas { get; set; }
        public int TotalCompletadas { get; set; }
        public int TotalCanceladas { get; set; }
    }

    // =========================================================================
    // PANTALLA 14: GESTIONAR / CANCELAR RESERVA
    // =========================================================================
    public class PasajeroDetalleItemViewModel
    {
        public int PasajeroId { get; set; }
        public int AsignacionId { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string TipoDocumento { get; set; }
        public string NumeroDocumento { get; set; }
        public string AsientoCodigo { get; set; }
        public string CategoriaEdad { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public bool CheckInCompletado { get; set; }
    }

    public class GestionarReservaViewModel
    {
        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaExpiracion { get; set; }
        public string EstadoReservaCodigo { get; set; }

        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public string OrigenCodigo { get; set; }
        public string OrigenCiudad { get; set; }
        public string DestinoCodigo { get; set; }
        public string DestinoCiudad { get; set; }
        public DateTime FechaOperacion { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public string AeronaveMatricula { get; set; }
        public string ModeloAeronave { get; set; }
        public string EstadoVueloCodigo { get; set; } = "PROGRAMADO";
        public string Tarifa { get; set; } = "Classic";

        public List<PasajeroDetalleItemViewModel> Pasajeros { get; set; } = new List<PasajeroDetalleItemViewModel>();

        public decimal TarifaBase { get; set; }
        public decimal TasasAeroportuarias { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Iva { get; set; }
        public decimal TotalPagado { get; set; }

        public string MetodoPago { get; set; } = "TARJETA_CREDITO";
        public string MarcaTarjeta { get; set; } = "VISA";
        public string Ultimos4 { get; set; } = "9010";
        public string CodigoOperacion { get; set; } = "SIM-RV000001-01";
        public DateTime? FechaPago { get; set; }
        public string NumeroComprobanteFactura { get; set; }
        public string NumeroNotaCredito { get; set; }

        public bool EsCancelable { get; set; }
        public string MotivoNoCancelable { get; set; }
        public decimal PorcentajePenalidad { get; set; }
        public decimal MontoPenalidad { get; set; }
        public decimal MontoReembolsoEstimado { get; set; }
        public decimal MontoReembolso
        {
            get => MontoReembolsoEstimado;
            set => MontoReembolsoEstimado = value;
        }
        public string PoliticaCancelacion { get; set; }
        public bool TieneCheckIn { get; set; }
    }

    // =========================================================================
    // PANTALLA 15: CHECK-IN EN LÍNEA
    // =========================================================================
    public class CheckInPasajeroItemViewModel
    {
        public int AsignacionId { get; set; }
        public int PasajeroId { get; set; }
        public string NombresCompletos { get; set; }
        public string Documento { get; set; }
        public string AsientoAsignado { get; set; }
        public bool YaTieneCheckIn { get; set; }
        public bool Seleccionado { get; set; }
    }

    public class CheckInViewModel
    {
        public string CodigoReserva { get; set; }
        public int ReservaId { get; set; }
        public string EstadoReservaCodigo { get; set; } = "CONFIRMADA";
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public string EstadoVueloCodigo { get; set; } = "CONFIRMADO";
        public string OrigenCodigo { get; set; }
        public string OrigenCiudad { get; set; }
        public string DestinoCodigo { get; set; }
        public string DestinoCiudad { get; set; }
        public DateTime FechaOperacion { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public string AeronaveMatricula { get; set; }

        public bool VentanaWebAbierta { get; set; } = true;
        public bool VentanaCheckInAbierta
        {
            get => VentanaWebAbierta;
            set => VentanaWebAbierta = value;
        }
        public string MensajeVentanaCheckIn { get; set; }

        public MapaCabinaViewModel MapaCabina { get; set; }
        public List<CheckInPasajeroItemViewModel> Pasajeros { get; set; } = new List<CheckInPasajeroItemViewModel>();
        public List<string> CodigosReservasSugeridos { get; set; } = new List<string>();

        [Display(Name = "Declaro que no transporto mercancías peligrosas ni sustancias prohibidas (IATA).")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Debe aceptar la declaración de seguridad para realizar el check-in")]
        public bool DeclaraSeguridad { get; set; }

        public bool CheckInTodosCompletado { get; set; }
    }

    public class CheckInSubmitViewModel
    {
        [Required]
        public string CodigoReserva { get; set; }
        public List<int> AsignacionesSeleccionadas { get; set; } = new List<int>();

        [Range(typeof(bool), "true", "true", ErrorMessage = "Debe aceptar la declaración de seguridad para realizar el check-in")]
        public bool DeclaraSeguridad { get; set; }
    }

    // =========================================================================
    // PANTALLA 16: VISUALIZACIÓN DE COMPROBANTES ACADÉMICOS Y PDF (DP-05)
    // =========================================================================
    public class ComprobanteItemDetalleViewModel
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public int Cantidad { get; set; } = 1;
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; } = 0.00m;
        public decimal Total { get; set; }
    }

    public class ComprobanteFiscalViewModel
    {
        public int ComprobanteId { get; set; }
        public string TipoComprobanteCodigo { get; set; } = "FACTURA"; // CONFIRMACION, FACTURA, NOTA_CREDITO
        public string TipoComprobante { get; set; } = "COMPROBANTE DE VENTA ACADÉMICO";
        public string NumeroComprobante { get; set; } // Ej: ACAD-000001
        public DateTime FechaEmision { get; set; }
        public string ClaveAccesoSRI { get; set; }
        public string NumeroAutorizacion { get; set; }
        public string EstadoComprobante { get; set; } = "EMITIDO";

        // Emisor (desde dbo.DatosEmisor según DP-05)
        public string EmisorRazonSocial { get; set; } = "AEROLINEA VIP S.A. - PROYECTO ACADEMICO DIT";
        public string EmisorRuc { get; set; } = "1799999999001";
        public string EmisorDireccion { get; set; } = "Aeropuerto Internacional Mariscal Sucre, Tababela - Quito, Ecuador";
        public string EmisorTelefono { get; set; } = "(02) 399-0000";
        public string EmisorCorreo { get; set; } = "facturacion@aerolineavip.ec";
        public string EmisorContribuyenteEspecial { get; set; } = "SIMULACIÓN ACADÉMICA - SIN VALIDEZ TRIBUTARIA";
        public string EmisorObligadoContabilidad { get; set; } = "SÍ";
        public string LeyendaComprobante { get; set; } = "Comprobante académico de una operación simulada. No constituye factura tributaria ni documento autorizado.";
        public string LeyendaAcademica
        {
            get => LeyendaComprobante;
            set => LeyendaComprobante = value;
        }

        // Receptor / Cliente
        public string ClienteNombre { get; set; }
        public string ClienteIdentificacion { get; set; }
        public string ClienteCorreo { get; set; }
        public string ClienteTelefono { get; set; }

        // Vuelo & Reserva
        public string CodigoReserva { get; set; }
        public string CodigoVuelo { get; set; }
        public string Ruta { get; set; }
        public DateTime FechaVuelo { get; set; }
        public string HoraSalida { get; set; }
        public string Asientos { get; set; }
        public List<string> PasajerosNombres { get; set; } = new List<string>();

        // Desglose Referencial (Ecuador IVA 15% DP-05)
        public List<ComprobanteItemDetalleViewModel> Items { get; set; } = new List<ComprobanteItemDetalleViewModel>();
        public decimal SubtotalSinImpuestos { get; set; }
        public decimal Subtotal15 { get; set; }
        public decimal Subtotal0 { get; set; }
        public decimal TarifaIvaPorcentaje { get; set; } = 15.00m;
        public decimal ValorIva { get; set; }
        public decimal ImporteTotal { get; set; }

        // Forma de Pago Simulado
        public string FormaPago { get; set; } = "Tarjeta de crédito (simulación académica)";
        public string MarcaTarjeta { get; set; } = "VISA";
        public string Ultimos4 { get; set; } = "9010";
        public string CodigoAutorizacionBancaria { get; set; } = "SIM-RV000001-01";
        public string EstadoPagoCodigo { get; set; } = "APROBADO";

        // Pase de Abordar Asociado
        public List<PaseAbordarDetalleViewModel> PasesAbordar { get; set; } = new List<PaseAbordarDetalleViewModel>();
    }

    public class ComprobanteListadoItemViewModel
    {
        public int ComprobanteId { get; set; }
        public int CompraId { get; set; }
        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public string TipoComprobanteCodigo { get; set; }
        public string TipoComprobanteDescripcion { get; set; }
        public string NumeroComprobante { get; set; }
        public DateTime FechaEmision { get; set; }
        public string EstadoComprobanteCodigo { get; set; }
        public decimal TotalOperacion { get; set; }
        public decimal MontoTotal
        {
            get => TotalOperacion;
            set => TotalOperacion = value;
        }
        public string MonedaCodigo { get; set; } = "USD";
        public string MarcaTarjeta { get; set; }
        public string TarjetaEnmascarada { get; set; }
        public string CodigoOperacion { get; set; }
        public string PasajeroPrincipal { get; set; }
        public string ReceptorNombre
        {
            get => PasajeroPrincipal;
            set => PasajeroPrincipal = value;
        }
        public string ReceptorDocumento { get; set; }
        public string ReceptorCorreo { get; set; }
        public string Observaciones { get; set; }
        public DateTime FechaVuelo { get; set; }
        public string RutaResumen { get; set; }
    }

    public class ComprobantesIndexViewModel
    {
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string TipoComprobante { get; set; } = "TODOS";
        public string TipoFiltro
        {
            get => TipoComprobante;
            set => TipoComprobante = value;
        }
        public string CodigoReserva { get; set; }
        public string CodigoReservaFiltro
        {
            get => CodigoReserva;
            set => CodigoReserva = value;
        }
        public int? ComprobanteSeleccionadoId { get; set; }

        public string EmisorRazonSocial { get; set; }
        public string EmisorRuc { get; set; }
        public string EmisorDireccion { get; set; }
        public string EmisorTelefono { get; set; }
        public string EmisorCorreo { get; set; }
        public string LeyendaComprobante { get; set; } = "Comprobante académico de una operación simulada. No constituye factura tributaria ni documento autorizado.";
        public string LeyendaAcademica
        {
            get => LeyendaComprobante;
            set => LeyendaComprobante = value;
        }

        public List<ComprobanteListadoItemViewModel> Comprobantes { get; set; } = new List<ComprobanteListadoItemViewModel>();
        public ComprobanteFiscalViewModel DetalleSeleccionado { get; set; }
        public ComprobanteListadoItemViewModel ComprobanteSeleccionado { get; set; }
    }

    public class PaseAbordarDetalleViewModel
    {
        public int CheckInId { get; set; }
        public string CodigoReserva { get; set; }
        public string CodigoVuelo { get; set; }
        public string PasajeroNombre { get; set; }
        public string PasajeroDocumento { get; set; }
        public string Asiento { get; set; }
        public string OrigenCodigo { get; set; }
        public string OrigenCiudad { get; set; }
        public string DestinoCodigo { get; set; }
        public string DestinoCiudad { get; set; }
        public DateTime FechaVuelo { get; set; }
        public string HoraSalida { get; set; }
        public string HoraAbordaje { get; set; }
        public string PuertaEmbarque { get; set; } = "VIP-01";
        public string GrupoEmbarque { get; set; } = "EJECUTIVO / GRUPO 1";
        public string AeronaveMatricula { get; set; } = "AV-01";
    }
}
