using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace AerolineaVIP.Models
{
    public partial class Reserva
    {
        [NotMapped]
        public DateTime? FechaActualizacion { get; set; }

        [NotMapped]
        public DateTime? FechaCancelacion { get; set; }

        [NotMapped]
        public string Observaciones { get; set; }
    }

    public partial class Compra
    {
        [NotMapped]
        public DateTime? FechaAnulacion { get; set; }

        [NotMapped]
        public string Observaciones { get; set; }
    }

    public partial class Comprobante
    {
        private string _receptorNombre;
        private string _receptorDocumento;
        private string _receptorCorreo;
        private decimal? _montoTotal;
        private string _observaciones;

        [NotMapped]
        public string ReceptorNombre
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_receptorNombre)) return _receptorNombre;
                var pas = Compra?.Reserva?.ReservaPasajeroVueloes?.FirstOrDefault()?.Pasajero;
                return pas != null ? $"{pas.Nombres} {pas.Apellidos}".Trim() : "Cliente VIP";
            }
            set => _receptorNombre = value;
        }

        [NotMapped]
        public string ReceptorDocumento
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_receptorDocumento)) return _receptorDocumento;
                var pas = Compra?.Reserva?.ReservaPasajeroVueloes?.FirstOrDefault()?.Pasajero;
                return pas?.NumeroDocumento ?? "1710034065";
            }
            set => _receptorDocumento = value;
        }

        [NotMapped]
        public string ReceptorCorreo
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_receptorCorreo)) return _receptorCorreo;
                var pas = Compra?.Reserva?.ReservaPasajeroVueloes?.FirstOrDefault()?.Pasajero;
                return pas?.Correo ?? "cliente@aerolineavip.com";
            }
            set => _receptorCorreo = value;
        }

        [NotMapped]
        public decimal MontoTotal
        {
            get => _montoTotal ?? (Compra?.Importe ?? 0m);
            set => _montoTotal = value;
        }

        [NotMapped]
        public string Observaciones
        {
            get => _observaciones ?? $"Comprobante académico ({TipoComprobanteCodigo}) sin validez tributaria (DP-05).";
            set => _observaciones = value;
        }
    }

    public partial class CheckIn
    {
        [NotMapped]
        public string Observaciones { get; set; }
    }

    public partial class Aeronave
    {
        [NotMapped]
        public DateTime? FechaRegistro { get; set; }
    }

    public partial class DatosEmisor
    {
        [NotMapped]
        public bool Activo { get; set; } = true;

        [NotMapped]
        public DateTime? FechaActualizacion { get; set; }

        [NotMapped]
        public string RazonSocial
        {
            get => NombreComercial;
            set => NombreComercial = value;
        }

        [NotMapped]
        public string RucSimulado
        {
            get => Identificacion;
            set => Identificacion = value;
        }

        [NotMapped]
        public string DireccionMatriz
        {
            get => Direccion;
            set => Direccion = value;
        }

        [NotMapped]
        public string CorreoContacto
        {
            get => Correo;
            set => Correo = value;
        }
    }

    public partial class Vuelo
    {
        [NotMapped]
        public DateTime? FechaCreacion { get; set; }
    }

    public partial class Itinerario
    {
        [NotMapped]
        public string CodigoItinerario
        {
            get => Referencia;
            set => Referencia = value;
        }

        [NotMapped]
        public string Descripcion
        {
            get => Nombre;
            set => Nombre = value;
        }
    }

    public partial class TramoItinerario
    {
        [NotMapped]
        public byte NumeroOrden
        {
            get => Orden;
            set => Orden = value;
        }
    }
}
