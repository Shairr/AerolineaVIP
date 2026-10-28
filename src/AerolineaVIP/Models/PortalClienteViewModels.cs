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
        public DateTime FechaCreacion { get; set; }
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
    }

    public class MisReservasViewModel
    {
        public string FiltroActual { get; set; } = "todas"; // todas, proximas, completadas, canceladas
        public List<ReservaResumenItemViewModel> Reservas { get; set; } = new List<ReservaResumenItemViewModel>();

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
        public string Tarifa { get; set; } = "Classic";

        public List<PasajeroDetalleItemViewModel> Pasajeros { get; set; } = new List<PasajeroDetalleItemViewModel>();

        public decimal TarifaBase { get; set; }
        public decimal TasasAeroportuarias { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Iva { get; set; }
        public decimal TotalPagado { get; set; }

        public string MarcaTarjeta { get; set; } = "VISA";
        public string Ultimos4 { get; set; } = "9010";
        public string CodigoOperacion { get; set; } = "AUT-582910";
        public DateTime? FechaPago { get; set; }

        public bool EsCancelable { get; set; }
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

        public MapaCabinaViewModel MapaCabina { get; set; }
        public List<CheckInPasajeroItemViewModel> Pasajeros { get; set; } = new List<CheckInPasajeroItemViewModel>();

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
    // PANTALLA 16: COMPROBANTE FISCAL / BOLETO PDF
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
        public string TipoComprobante { get; set; } = "FACTURA ELECTRÓNICA";
        public string NumeroComprobante { get; set; } // Ej: 001-002-000084920
        public DateTime FechaEmision { get; set; }
        public string ClaveAccesoSRI { get; set; }
        public string NumeroAutorizacion { get; set; }
        public string EstadoComprobante { get; set; } = "AUTORIZADO";

        // Emisor
        public string EmisorRazonSocial { get; set; } = "AEROLÍNEA VIP S.A.";
        public string EmisorRuc { get; set; } = "1792847192001";
        public string EmisorDireccion { get; set; } = "Av. Amazonas N34-12 y República, Quito - Ecuador";
        public string EmisorContribuyenteEspecial { get; set; } = "Resolución Nro. NAC-DNCRASC20-00000001";
        public string EmisorObligadoContabilidad { get; set; } = "SÍ";

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

        // Desglose Tributario (Ecuador IVA 15%)
        public List<ComprobanteItemDetalleViewModel> Items { get; set; } = new List<ComprobanteItemDetalleViewModel>();
        public decimal SubtotalSinImpuestos { get; set; }
        public decimal Subtotal15 { get; set; }
        public decimal Subtotal0 { get; set; }
        public decimal TarifaIvaPorcentaje { get; set; } = 15.00m;
        public decimal ValorIva { get; set; }
        public decimal ImporteTotal { get; set; }

        // Forma de Pago
        public string FormaPago { get; set; } = "TARJETA DE CRÉDITO";
        public string MarcaTarjeta { get; set; } = "VISA";
        public string Ultimos4 { get; set; } = "9010";
        public string CodigoAutorizacionBancaria { get; set; } = "AUT-582910";

        // Pase de Abordar Asociado
        public List<PaseAbordarDetalleViewModel> PasesAbordar { get; set; } = new List<PaseAbordarDetalleViewModel>();
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
