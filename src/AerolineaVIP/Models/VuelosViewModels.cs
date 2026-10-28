using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace AerolineaVIP.Models
{
    public class BusquedaVueloViewModel
    {
        [Required(ErrorMessage = "Seleccione la ciudad de origen")]
        [Display(Name = "Origen")]
        public string OrigenCodigo { get; set; } = "UIO";

        [Required(ErrorMessage = "Seleccione la ciudad de destino")]
        [Display(Name = "Destino")]
        public string DestinoCodigo { get; set; } = "GYE";

        [Required(ErrorMessage = "Indique la fecha de salida")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de salida")]
        public DateTime? FechaSalida { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de regreso")]
        public DateTime? FechaRegreso { get; set; }

        [Display(Name = "Tipo de viaje")]
        public string TipoViaje { get; set; } = "solo-ida";

        [Range(1, 7, ErrorMessage = "La capacidad máxima por vuelo privado es de 7 pasajeros")]
        [Display(Name = "Pasajeros")]
        public int CantidadPasajeros { get; set; } = 1;

        public List<SelectListItem> AeropuertosDisponibles { get; set; } = new List<SelectListItem>();
    }

    public class VueloItemViewModel
    {
        public int VueloId { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string DiaSemanaNombre { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public string DuracionTexto { get; set; }

        public string OrigenCodigo { get; set; }
        public string OrigenNombre { get; set; }
        public string OrigenCiudad { get; set; }

        public string DestinoCodigo { get; set; }
        public string DestinoNombre { get; set; }
        public string DestinoCiudad { get; set; }

        public string AeronaveMatricula { get; set; }
        public string ModeloAeronave { get; set; }
        public int CapacidadTotal { get; set; } = 7;
        public int AsientosOcupados { get; set; }
        public int AsientosDisponibles { get; set; }

        public decimal TarifaClassic { get; set; }
        public decimal TarifaFlex { get; set; }
        public string EstadoVueloCodigo { get; set; }

        public bool EsVueloDirecto { get; set; } = true;
        public string EscalaInfo { get; set; }
    }

    public class DiaTarifaViewModel
    {
        public DateTime Fecha { get; set; }
        public string DiaSemanaCorto { get; set; }
        public string FechaCorta { get; set; }
        public decimal? PrecioMinimo { get; set; }
        public bool TieneVuelos { get; set; }
        public bool EsSeleccionado { get; set; }
    }

    public class RutaInfoViewModel
    {
        public int RutaId { get; set; }
        public string OrigenCodigo { get; set; }
        public string OrigenCiudad { get; set; }
        public string DestinoCodigo { get; set; }
        public string DestinoCiudad { get; set; }
        public decimal TarifaReferencial { get; set; }
        public string DiasOperacion { get; set; }
        public string HorariosResumen { get; set; }
    }

    public class ResultadoBusquedaViewModel
    {
        public BusquedaVueloViewModel Criterios { get; set; } = new BusquedaVueloViewModel();
        public List<VueloItemViewModel> Vuelos { get; set; } = new List<VueloItemViewModel>();
        public List<DiaTarifaViewModel> CalendarioSemanal { get; set; } = new List<DiaTarifaViewModel>();
        public List<RutaInfoViewModel> RutasDisponibles { get; set; } = new List<RutaInfoViewModel>();
        public string MensajeInformativo { get; set; }
        public bool FechaConVuelos { get; set; }
    }
}
