using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace AerolineaVIP.Models
{
    /// <summary>
    /// Utilidades de normalización y validación según la legislación, 
    /// Registro Civil del Ecuador, ARCOTEL y estándares bancarios internacionales.
    /// </summary>
    public static class ValidadorEcuador
    {
        // =========================================================================
        // 1. MÉTODOS DE NORMALIZACIÓN DE ENTRADAS
        // =========================================================================

        /// <summary>
        /// Elimina espacios sobrantes a izquierda y derecha, y colapsa múltiples espacios internos a uno solo.
        /// </summary>
        public static string NormalizarEspacios(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            return Regex.Replace(texto.Trim(), @"\s+", " ");
        }

        /// <summary>
        /// Normaliza nombres y apellidos a formato Title Case (primera letra en mayúscula, resto en minúscula).
        /// </summary>
        public static string NormalizarNombrePropio(string texto)
        {
            string limpio = NormalizarEspacios(texto);
            if (string.IsNullOrEmpty(limpio)) return string.Empty;

            TextInfo textInfo = new CultureInfo("es-EC", false).TextInfo;
            return textInfo.ToTitleCase(limpio.ToLowerInvariant());
        }

        /// <summary>
        /// Normaliza el correo electrónico a minúsculas y sin espacios.
        /// </summary>
        public static string NormalizarEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return string.Empty;
            return email.Trim().ToLowerInvariant().Replace(" ", "");
        }

        /// <summary>
        /// Normaliza documentos eliminando espacios y convirtiendo a mayúsculas.
        /// </summary>
        public static string NormalizarDocumento(string doc)
        {
            if (string.IsNullOrWhiteSpace(doc)) return string.Empty;
            return Regex.Replace(doc.Trim().ToUpperInvariant(), @"[\s\-]", "");
        }

        /// <summary>
        /// Normaliza números de teléfono eliminando caracteres no numéricos o prefijo +593.
        /// </summary>
        public static string NormalizarTelefono(string tel)
        {
            if (string.IsNullOrWhiteSpace(tel)) return string.Empty;
            string soloDigitos = Regex.Replace(tel.Trim(), @"\D", "");

            // Si comienza con 593 y tiene 12 dígitos (5939XXXXXXXX), convertir a formato nacional 09XXXXXXXX
            if (soloDigitos.StartsWith("593") && soloDigitos.Length == 12)
            {
                soloDigitos = "0" + soloDigitos.Substring(3);
            }

            return soloDigitos;
        }

        // =========================================================================
        // 2. VALIDACIONES DE IDENTIDAD Y DATOS PERSONALES
        // =========================================================================

        /// <summary>
        /// Valida una Cédula de Identidad ecuatoriana mediante el Algoritmo Módulo 10 del Registro Civil.
        /// Excluye explícitamente casos especiales de dígitos repetidos como "1111111111".
        /// </summary>
        public static bool ValidarCedula(string cedula)
        {
            string clean = NormalizarDocumento(cedula);

            // Debe tener exactamente 10 dígitos numéricos
            if (string.IsNullOrEmpty(clean) || clean.Length != 10 || !Regex.IsMatch(clean, @"^\d{10}$"))
            {
                return false;
            }

            // EXCLUSIÓN EXPLÍCITA: Casos de dígitos repetidos ("1111111111", "0000000000", etc.)
            if (clean.Distinct().Count() == 1)
            {
                return false;
            }

            // Exclusión específica del caso 1111111111
            if (clean == "1111111111")
            {
                return false;
            }

            // 1. Código de provincia: 2 primeros dígitos entre 01 y 24, o 30 (consulados en el exterior)
            int provincia = int.Parse(clean.Substring(0, 2));
            if (!((provincia >= 1 && provincia <= 24) || provincia == 30))
            {
                return false;
            }

            // 2. Tercer dígito menor a 6 para personas naturales (cédulas)
            int tercerDigito = int.Parse(clean.Substring(2, 1));
            if (tercerDigito >= 6)
            {
                return false;
            }

            // 3. Algoritmo Módulo 10 con coeficientes 2, 1, 2, 1, 2, 1, 2, 1, 2
            int[] coeficientes = { 2, 1, 2, 1, 2, 1, 2, 1, 2 };
            int suma = 0;

            for (int i = 0; i < 9; i++)
            {
                int valor = int.Parse(clean[i].ToString()) * coeficientes[i];
                if (valor >= 10)
                {
                    valor -= 9;
                }
                suma += valor;
            }

            int digitoVerificador = int.Parse(clean[9].ToString());
            int residuo = suma % 10;
            int resultadoEsperado = (residuo == 0) ? 0 : (10 - residuo);

            return digitoVerificador == resultadoEsperado;
        }

        /// <summary>
        /// Valida un RUC ecuatoriano de 13 dígitos.
        /// </summary>
        public static bool ValidarRuc(string ruc)
        {
            string clean = NormalizarDocumento(ruc);
            if (string.IsNullOrEmpty(clean) || clean.Length != 13 || !Regex.IsMatch(clean, @"^\d{13}$"))
            {
                return false;
            }

            // Persona natural: primeros 10 dígitos deben formar cédula válida y terminar en 001..999
            string cedulaParte = clean.Substring(0, 10);
            string establecimiento = clean.Substring(10, 3);

            if (establecimiento == "000") return false;

            int tercerDigito = int.Parse(clean.Substring(2, 1));
            if (tercerDigito < 6)
            {
                return ValidarCedula(cedulaParte);
            }

            // Para sociedades públicas (6) o privadas (9)
            return establecimiento == "001";
        }

        /// <summary>
        /// Valida pasaporte internacional (6 a 15 caracteres alfanuméricos, sin espacios ni caracteres especiales).
        /// </summary>
        public static bool ValidarPasaporte(string pasaporte)
        {
            string clean = NormalizarDocumento(pasaporte);
            return !string.IsNullOrEmpty(clean) && Regex.IsMatch(clean, @"^[A-Z0-9]{6,15}$");
        }

        /// <summary>
        /// Valida teléfono móvil celular de Ecuador: 10 dígitos iniciando en 09.
        /// </summary>
        public static bool ValidarTelefonoEcuador(string telefono)
        {
            string clean = NormalizarTelefono(telefono);
            return !string.IsNullOrEmpty(clean) && Regex.IsMatch(clean, @"^09\d{8}$");
        }

        /// <summary>
        /// Valida nombres y apellidos: solo caracteres alfabéticos en español, sin números ni símbolos raros.
        /// Longitud máxima fijada en 25 caracteres según la especificación del usuario.
        /// </summary>
        public static bool ValidarNombreApellido(string texto, int maxLength = 25)
        {
            string clean = NormalizarEspacios(texto);
            if (string.IsNullOrEmpty(clean) || clean.Length < 2 || clean.Length > maxLength)
            {
                return false;
            }

            // Solo letras (incluye á, é, í, ó, ú, ñ, ü, Á, É, Í, Ó, Ú, Ñ, Ü), apóstrofe y espacios simples
            return Regex.IsMatch(clean, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\']+$");
        }

        /// <summary>
        /// Valida correo electrónico sin espacios ni caracteres prohibidos.
        /// </summary>
        public static bool ValidarEmail(string email)
        {
            string clean = NormalizarEmail(email);
            if (string.IsNullOrEmpty(clean) || clean.Contains(" ") || clean.Length > 100)
            {
                return false;
            }

            // Formato estándar RFC con caracteres seguros
            return Regex.IsMatch(clean, @"^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$");
        }

        /// <summary>
        /// Valida la fecha de nacimiento en el contexto aeronáutico:
        /// - No futura
        /// - Edad entre 0 y 110 años
        /// - Clasificación por rangos: Infante (0-10 años), Menor (11-17 años), Adulto (18+ años).
        /// </summary>
        public static bool ValidarFechaNacimiento(DateTime? fecha, out string error, out string categoriaEdad, bool debeSerMayorEdad = false)
        {
            error = null;
            categoriaEdad = "Adulto";

            if (!fecha.HasValue)
            {
                error = "La fecha de nacimiento es obligatoria.";
                return false;
            }

            DateTime fechaVal = fecha.Value.Date;
            DateTime hoy = DateTime.Today;

            if (fechaVal > hoy)
            {
                error = "La fecha de nacimiento no puede ser una fecha futura.";
                return false;
            }

            int edad = hoy.Year - fechaVal.Year;
            if (fechaVal.Date > hoy.AddYears(-edad)) edad--;

            if (edad > 110)
            {
                error = "La edad del pasajero no puede exceder los 110 años.";
                return false;
            }

            // Categorías oficiales acordadas:
            // Infante/Niño: 0 a 10 años
            // Menor: 11 a 17 años
            // Adulto: 18 o más años
            if (edad <= 10)
            {
                categoriaEdad = "Infante (0-10 años)";
            }
            else if (edad >= 11 && edad <= 17)
            {
                categoriaEdad = "Menor (11-17 años)";
            }
            else
            {
                categoriaEdad = "Adulto (18+ años)";
            }

            return true;
        }

        /// <summary>
        /// Determina si una fecha de nacimiento corresponde a una persona mayor de edad (18 años o más).
        /// </summary>
        public static bool EsMayorDeEdad(DateTime? fecha)
        {
            if (!fecha.HasValue) return false;
            DateTime fechaVal = fecha.Value.Date;
            DateTime hoy = DateTime.Today;
            int edad = hoy.Year - fechaVal.Year;
            if (fechaVal.Date > hoy.AddYears(-edad)) edad--;
            return edad >= 18;
        }

        // =========================================================================
        // 3. VALIDACIONES BANCARIAS DE PAGO (SIN MENCIONES ACADÉMICAS)
        // =========================================================================

        /// <summary>
        /// Valida el nombre del titular: al menos dos palabras (nombre y apellido), solo letras.
        /// </summary>
        public static bool ValidarTitularTarjeta(string titular)
        {
            string clean = NormalizarEspacios(titular);
            if (string.IsNullOrEmpty(clean) || clean.Length < 5 || clean.Length > 50)
            {
                return false;
            }

            if (!Regex.IsMatch(clean, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\']+$"))
            {
                return false;
            }

            // Debe contener al menos dos palabras
            string[] palabras = clean.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return palabras.Length >= 2;
        }

        /// <summary>
        /// Valida el número de tarjeta flexibilizado según dígitos mínimos:
        /// - Diners: al menos 14 dígitos numéricos.
        /// - American Express / Visa / Mastercard y otras: al menos 15 dígitos numéricos.
        /// - Máximo general: 19 dígitos numéricos.
        /// </summary>
        public static bool ValidarTarjetaDigitosMinimos(string numeroTarjeta, string marcaEsperada = null)
        {
            if (string.IsNullOrWhiteSpace(numeroTarjeta)) return false;
            string clean = Regex.Replace(numeroTarjeta, @"\D", "");

            int minDigitos = 15;
            if (!string.IsNullOrWhiteSpace(marcaEsperada) && marcaEsperada.Trim().ToUpperInvariant().Contains("DINERS"))
            {
                minDigitos = 14;
            }

            return clean.Length >= minDigitos && clean.Length <= 19;
        }

        /// <summary>
        /// Valida el número de tarjeta según la directriz de requerir únicamente los dígitos mínimos requeridos.
        /// </summary>
        public static bool ValidarTarjetaLuhn(string numeroTarjeta, string marcaEsperada = null)
        {
            return ValidarTarjetaDigitosMinimos(numeroTarjeta, marcaEsperada);
        }

        /// <summary>
        /// Valida la vigencia de la tarjeta (mes y año). No puede ser una fecha anterior al mes/año actual.
        /// </summary>
        public static bool ValidarVencimientoTarjeta(string mesStr, string anioStr)
        {
            if (string.IsNullOrWhiteSpace(mesStr) || string.IsNullOrWhiteSpace(anioStr)) return false;

            if (!int.TryParse(mesStr, out int mes) || mes < 1 || mes > 12) return false;
            if (!int.TryParse(anioStr, out int anio)) return false;

            // Si el año viene en formato de 2 dígitos (ej: 28 -> 2028)
            if (anio < 100) anio += 2000;

            DateTime hoy = DateTime.Today;
            int anioActual = hoy.Year;
            int mesActual = hoy.Month;

            if (anio < anioActual) return false;
            if (anio == anioActual && mes < mesActual) return false;
            if (anio > anioActual + 15) return false; // Límite razonable

            return true;
        }

        /// <summary>
        /// Valida el código de seguridad CVV: 4 dígitos para Amex, 3 dígitos para Visa/Mastercard/Diners.
        /// </summary>
        public static bool ValidarCvv(string cvv, string marca)
        {
            if (string.IsNullOrWhiteSpace(cvv)) return false;
            string clean = cvv.Trim();

            bool esAmex = !string.IsNullOrEmpty(marca) && (marca.Equals("AMEX", StringComparison.OrdinalIgnoreCase) || marca.Contains("AMERICAN"));
            int longitudEsperada = esAmex ? 4 : 3;

            return clean.Length == longitudEsperada && Regex.IsMatch(clean, @"^\d+$");
        }
    }
}
