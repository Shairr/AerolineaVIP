using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AerolineaVIP.Models
{
    /// <summary>
    /// Representa el estado individual de cada uno de los 7 asientos del Embraer Phenom 300E
    /// </summary>
    public class AsientoEstadoViewModel
    {
        public int NumeroAsiento { get; set; }
        public string CodigoAsiento { get; set; } // 1D, 2D, 2I, 3D, 3I, 4D, 4I
        public byte Fila { get; set; }             // 1, 2, 3, 4
        public string Lado { get; set; }           // "D" (Derecho) o "I" (Izquierdo)
        public string Ubicacion { get; set; }
        public string EstadoVisual { get; set; }   // DISPONIBLE, RESERVADO, OCUPADO
        public string ColorEstado { get; set; }    // Verde, Morado, Rojo
        public bool EsSeleccionado { get; set; }
        public bool EsAccesible { get; set; } = true;
    }

    /// <summary>
    /// Modelo compartido para renderizar la cabina _MapaAsientos.cshtml en modo solo lectura o interactivo
    /// </summary>
    public class MapaCabinaViewModel
    {
        public int VueloId { get; set; }
        public string AeronaveMatricula { get; set; } = "AV-01";
        public string ModeloAeronave { get; set; } = "Embraer Phenom 300E";
        public bool EsSoloLectura { get; set; } = false;
        public int MaxAsientosSeleccionables { get; set; } = 1;
        public string AsientosSeleccionadosCodigos { get; set; } = "";
        public List<AsientoEstadoViewModel> Asientos { get; set; } = new List<AsientoEstadoViewModel>();
    }

    /// <summary>
    /// Pantalla 7: Ficha técnica y mapa de asientos en solo lectura
    /// </summary>
    public class DisponibilidadVueloViewModel
    {
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string DiaSemanaNombre { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public string DuracionTexto { get; set; }

        public string OrigenCodigo { get; set; }
        public string OrigenCiudad { get; set; }
        public string OrigenNombre { get; set; }

        public string DestinoCodigo { get; set; }
        public string DestinoCiudad { get; set; }
        public string DestinoNombre { get; set; }

        public string AeronaveMatricula { get; set; }
        public string ModeloAeronave { get; set; }
        public int CapacidadTotal { get; set; } = 7;
        public int AsientosOcupados { get; set; }
        public int AsientosReservados { get; set; }
        public int AsientosDisponibles { get; set; }
        public string EstadoVueloCodigo { get; set; }

        public decimal TarifaClassic { get; set; }
        public decimal TarifaFlex { get; set; }

        public MapaCabinaViewModel MapaCabina { get; set; } = new MapaCabinaViewModel();
    }

    /// <summary>
    /// Pantalla 8 (Paso 1): Selección interactiva de asientos
    /// </summary>
    public class SeleccionAsientosViewModel
    {
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string OrigenCodigo { get; set; }
        public string OrigenCiudad { get; set; }
        public string DestinoCodigo { get; set; }
        public string DestinoCiudad { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public string TarifaSeleccionada { get; set; } = "Classic"; // Classic o Flex
        public decimal PrecioUnitario { get; set; }
        public int CantidadPasajeros { get; set; } = 1;

        [Required(ErrorMessage = "Debe seleccionar los asientos para todos los pasajeros.")]
        public string AsientosSeleccionados { get; set; } // Ej: "1D, 2D"

        public decimal Subtotal { get; set; }
        public MapaCabinaViewModel MapaCabina { get; set; } = new MapaCabinaViewModel();
    }

    /// <summary>
    /// Datos individuales de un pasajero en el proceso de reserva
    /// </summary>
    public class PasajeroItemViewModel
    {
        public int Indice { get; set; }
        public string AsientoAsignado { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(25, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 25 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\']+$", ErrorMessage = "El nombre solo puede contener letras y espacios")]
        [Display(Name = "Nombres completos")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Los apellidos son obligatorios")]
        [StringLength(25, MinimumLength = 2, ErrorMessage = "Los apellidos deben tener entre 2 y 25 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\']+$", ErrorMessage = "Los apellidos solo pueden contener letras y espacios")]
        [Display(Name = "Apellidos completos")]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "Seleccione el tipo de documento")]
        [Display(Name = "Tipo de documento")]
        public string TipoDocumento { get; set; } = "CED"; // CED, PAS, RUC

        [Required(ErrorMessage = "El número de documento es obligatorio")]
        [Display(Name = "Número de identificación")]
        public string NumeroDocumento { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de nacimiento")]
        public DateTime? FechaNacimiento { get; set; }

        [Display(Name = "Categoría de Pasajero")]
        public string CategoriaEdad { get; set; } = "Adulto";

        [Display(Name = "Género")]
        public string Genero { get; set; } = "M";

        [Required(ErrorMessage = "El teléfono de contacto es obligatorio")]
        [RegularExpression(@"^09\d{8}$", ErrorMessage = "El teléfono debe ser un celular ecuatoriano válido de 10 dígitos (ej: 0991234567)")]
        [Display(Name = "Teléfono de contacto")]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [StringLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Ingrese un correo electrónico válido sin espacios ni caracteres especiales")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [StringLength(250, ErrorMessage = "El requerimiento especial no puede superar los 250 caracteres")]
        [Display(Name = "Requerimiento especial o catering VIP")]
        public string RequerimientoEspecial { get; set; }
    }

    /// <summary>
    /// Pantalla 9 (Paso 2): Formulario para captura de datos de pasajeros
    /// </summary>
    public class RegistroPasajerosViewModel
    {
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string OrigenCodigo { get; set; }
        public string OrigenCiudad { get; set; }
        public string DestinoCodigo { get; set; }
        public string DestinoCiudad { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public string TarifaSeleccionada { get; set; } = "Classic";
        public decimal PrecioUnitario { get; set; }
        public string AsientosCodigos { get; set; }
        public decimal Subtotal { get; set; }

        public List<PasajeroItemViewModel> Pasajeros { get; set; } = new List<PasajeroItemViewModel>();
    }

    /// <summary>
    /// Pantalla 10 (Paso 3): Resumen y confirmación de reserva previa a pago
    /// </summary>
    public class ResumenReservaViewModel
    {
        public string CodigoPreReserva { get; set; }
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public DateTime FechaOperacion { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public string OrigenCodigo { get; set; }
        public string OrigenCiudad { get; set; }
        public string DestinoCodigo { get; set; }
        public string DestinoCiudad { get; set; }
        public string AeronaveMatricula { get; set; } = "AV-01";
        public string TarifaSeleccionada { get; set; } = "Classic";
        public decimal PrecioUnitario { get; set; }
        public int CantidadPasajeros { get; set; }
        public string AsientosCodigos { get; set; }

        public List<PasajeroItemViewModel> Pasajeros { get; set; } = new List<PasajeroItemViewModel>();

        public decimal TarifaBaseTotal { get; set; }
        public decimal TasasAeroportuarias { get; set; }
        public decimal SubtotalNeto { get; set; }
        public decimal IvaMonto { get; set; } // 15% IVA Ecuador
        public decimal TotalPagar { get; set; }

        public int MinutosRetencion { get; set; } = 15;
    }

    /// <summary>
    /// Pantalla 11 (Paso 4): Pasarela de pago seguro con tarjeta
    /// </summary>
    public class PagoSimuladoViewModel
    {
        public string CodigoPreReserva { get; set; }
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string OrigenCodigo { get; set; }
        public string DestinoCodigo { get; set; }
        public decimal TotalPagar { get; set; }
        public string TarifaSeleccionada { get; set; }
        public int CantidadPasajeros { get; set; }
        public string AsientosCodigos { get; set; }

        [Required(ErrorMessage = "Seleccione la marca de tarjeta")]
        [Display(Name = "Marca de tarjeta")]
        public string MarcaTarjeta { get; set; } = "VISA"; // VISA, MC, AMEX, DINERS

        [Required(ErrorMessage = "Ingrese el nombre del titular como figura en la tarjeta")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "El nombre del titular debe contener entre 5 y 50 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\']+$", ErrorMessage = "El nombre del titular solo puede contener letras y espacios")]
        [Display(Name = "Nombre del titular")]
        public string TitularTarjeta { get; set; }

        [Required(ErrorMessage = "Ingrese el número de tarjeta")]
        [Display(Name = "Número de tarjeta")]
        public string NumeroTarjeta { get; set; }

        [Required(ErrorMessage = "Indique el mes de expiración")]
        [Display(Name = "Mes de expiración (MM)")]
        public string MesExpiracion { get; set; } = "12";

        [Required(ErrorMessage = "Indique el año de expiración")]
        [Display(Name = "Año de expiración (AA)")]
        public string AnioExpiracion { get; set; } = "28";

        [Required(ErrorMessage = "Ingrese el código de seguridad")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "El código CVV debe tener 3 dígitos (o 4 para American Express)")]
        [Display(Name = "CVV / CVC")]
        public string Cvv { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Debe aceptar los términos de la transacción")]
        [Display(Name = "Acepto los términos y condiciones de la compra del pasaje.")]
        public bool AceptoTerminos { get; set; }
    }

    /// <summary>
    /// Pantalla 12: Confirmación de pago y emisión de boletos
    /// </summary>
    public class ConfirmacionCompraViewModel
    {
        public string CodigoReserva { get; set; } // Ej: VIP-74829
        public string CodigoOperacion { get; set; } // Ej: SIM-938210
        public DateTime FechaCompra { get; set; } = DateTime.Now;

        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public DateTime FechaOperacion { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public string OrigenCodigo { get; set; }
        public string OrigenCiudad { get; set; }
        public string DestinoCodigo { get; set; }
        public string DestinoCiudad { get; set; }
        public string AeronaveMatricula { get; set; }
        public string TarifaSeleccionada { get; set; }
        public decimal TotalPagado { get; set; }
        public string MarcaTarjeta { get; set; }
        public string TarjetaEnmascarada { get; set; }
        public string TitularTarjeta { get; set; }

        public List<PasajeroItemViewModel> Pasajeros { get; set; } = new List<PasajeroItemViewModel>();
    }
}
