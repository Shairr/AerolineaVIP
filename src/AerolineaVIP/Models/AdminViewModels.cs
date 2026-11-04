using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace AerolineaVIP.Models
{
    // =========================================================================
    // PANTALLA 20: GESTIÓN DE USUARIOS, PASAJEROS Y RESERVAS
    // =========================================================================
    public class UsuarioRolItemViewModel
    {
        public string UserId { get; set; }
        public string UsuarioId
        {
            get => UserId;
            set => UserId = value;
        }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string RolActual { get; set; }
        public bool Activo { get; set; } = true;
        public bool EstaActivo
        {
            get => Activo;
            set => Activo = value;
        }
    }

    public class AdminGestionIntegralViewModel
    {
        public string TabActiva { get; set; } = "pasajeros"; // usuarios, pasajeros, reservas
        public bool EsSuperUsuario { get; set; }

        // Filtros
        public string Busqueda { get; set; }
        public string BusquedaUsuario { get; set; }
        public string BusquedaPasajero { get; set; }
        public string BusquedaReserva { get; set; }
        public string EstadoReservaFiltro { get; set; } = "TODOS";
        public string FiltroEstadoReserva
        {
            get => EstadoReservaFiltro;
            set => EstadoReservaFiltro = value;
        }

        // Colecciones de las 3 pestañas
        public List<UsuarioRolItemViewModel> Usuarios { get; set; } = new List<UsuarioRolItemViewModel>();
        public List<Pasajero> Pasajeros { get; set; } = new List<Pasajero>();
        public List<Reserva> Reservas { get; set; } = new List<Reserva>();
        public List<string> RolesDisponibles { get; set; } = new List<string> { "SuperUsuario", "Administrador", "Counter", "Cliente" };
    }

    // =========================================================================
    // PANTALLA 21: CONSULTAS Y REPORTES ADMINISTRATIVOS (MOD-10)
    // =========================================================================
    public class ReporteOcupacionItemViewModel
    {
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string Horario { get; set; }
        public string RutaTexto { get; set; }
        public string Ruta
        {
            get => RutaTexto;
            set => RutaTexto = value;
        }
        public string AeronaveMatricula { get; set; }
        public int Capacidad { get; set; } = 7;
        public int ReservadosMorado { get; set; }
        public int ReservadosPendientes
        {
            get => ReservadosMorado;
            set => ReservadosMorado = value;
        }
        public int ConfirmadosRojo { get; set; }
        public int OcupadosConfirmados
        {
            get => ConfirmadosRojo;
            set => ConfirmadosRojo = value;
        }
        public int TotalOcupados => ReservadosMorado + ConfirmadosRojo;
        public int DisponiblesVerde
        {
            get => Math.Max(0, Capacidad - TotalOcupados);
            set { }
        }
        public int Disponibles
        {
            get => DisponiblesVerde;
            set { }
        }
        public string EstadoVueloCodigo { get; set; }
        public string EstadoVuelo
        {
            get => EstadoVueloCodigo;
            set => EstadoVueloCodigo = value;
        }
        public string EstadoVueloDescripcion { get; set; }
        public decimal PorcentajeOcupacion { get; set; }
    }

    public class ReporteReservaItemViewModel
    {
        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaExpiracion { get; set; }
        public string UsuarioId { get; set; }
        public string EstadoReservaCodigo { get; set; }
        public string EstadoReserva
        {
            get => EstadoReservaCodigo;
            set => EstadoReservaCodigo = value;
        }
        public int CantidadPasajeros { get; set; }
        public string PasajerosResumen { get; set; }
        public string RutaResumen { get; set; }
        public string Ruta
        {
            get => RutaResumen;
            set => RutaResumen = value;
        }
        public DateTime FechaVuelo { get; set; }
        public string AsientosResumen { get; set; }
        public decimal ImporteTotal { get; set; }
        public bool TieneCheckIn { get; set; }
    }

    public class ReporteCompraPagoItemViewModel
    {
        public int CompraId { get; set; }
        public string CodigoReserva { get; set; }
        public DateTime FechaCompra { get; set; }
        public DateTime FechaPago { get; set; }
        public decimal Importe { get; set; }
        public string Moneda { get; set; } = "USD";
        public string EstadoCompraCodigo { get; set; }
        public string EstadoCompra
        {
            get => EstadoCompraCodigo;
            set => EstadoCompraCodigo = value;
        }
        public string MetodoPago { get; set; }
        public string MarcaTarjeta { get; set; }
        public string MetodoYMarca { get; set; }
        public string TarjetaEnmascarada { get; set; }
        public string UltimosCuatro
        {
            get => TarjetaEnmascarada;
            set => TarjetaEnmascarada = value;
        }
        public string CodigoOperacion { get; set; }
        public string EstadoPagoCodigo { get; set; }
        public string EstadoPago
        {
            get => EstadoPagoCodigo;
            set => EstadoPagoCodigo = value;
        }
        public string MensajeResultado { get; set; }
        public string NumeroComprobante { get; set; }
    }

    public class AdminReportesViewModel
    {
        public string TipoConsulta { get; set; } = "ocupacion"; // ocupacion, reservas, compras
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int? RutaId { get; set; }
        public int? AeronaveId { get; set; }
        public string RutaFiltro { get; set; }
        public string AeronaveFiltro { get; set; }
        public string EstadoFiltro { get; set; } = "TODOS";
        public DateTime FechaCorte { get; set; } = DateTime.Now;

        public List<SelectListItem> RutasDisponibles { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AeronavesDisponibles { get; set; } = new List<SelectListItem>();

        public List<ReporteOcupacionItemViewModel> FilasOcupacion { get; set; } = new List<ReporteOcupacionItemViewModel>();
        public List<ReporteOcupacionItemViewModel> OcupacionVuelos
        {
            get => FilasOcupacion;
            set => FilasOcupacion = value;
        }

        public List<ReporteReservaItemViewModel> FilasReservas { get; set; } = new List<ReporteReservaItemViewModel>();
        public List<ReporteReservaItemViewModel> ReservasPorEstado
        {
            get => FilasReservas;
            set => FilasReservas = value;
        }

        public List<ReporteCompraPagoItemViewModel> FilasCompras { get; set; } = new List<ReporteCompraPagoItemViewModel>();
        public List<ReporteCompraPagoItemViewModel> ComprasPagos
        {
            get => FilasCompras;
            set => FilasCompras = value;
        }
    }
}
