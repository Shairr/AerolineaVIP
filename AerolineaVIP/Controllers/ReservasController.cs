using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    public class ReservasController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        // =========================================================================
        // PANTALLA 8: EMBUDO PASO 1 — SELECCIÓN INTERACTIVA DE ASIENTOS
        // =========================================================================
        [HttpGet]
        public ActionResult SeleccionarAsientos(int? vueloId, string tarifa = "Classic", int pasajeros = 1)
        {
            if (pasajeros < 1) pasajeros = 1;
            if (pasajeros > 7) pasajeros = 7;
            if (string.IsNullOrWhiteSpace(tarifa)) tarifa = "Classic";

            var vuelo = ObtenerVueloConDetalle(vueloId);
            if (vuelo == null)
            {
                return RedirectToAction("Buscar", "Vuelos");
            }

            decimal tarifaBase = vuelo.Frecuencia?.Ruta?.TarifaReferencial ?? 450.00m;
            decimal precioUnitario = tarifa.Equals("Flex", StringComparison.OrdinalIgnoreCase)
                ? Math.Round(tarifaBase * 1.35m, 2)
                : tarifaBase;

            var asignaciones = vuelo.ReservaPasajeroVueloes
                .Where(r => r.EstadoAsignacionCodigo != "CANCELADA")
                .ToList();

            var mapa = VuelosController.GenerarMapaPhenom300E(
                vuelo.VueloId,
                vuelo.Aeronave?.Matricula ?? "AV-01",
                asignaciones,
                soloLectura: false,
                maxSeleccionables: pasajeros,
                preseleccionados: ""
            );

            var model = new SeleccionAsientosViewModel
            {
                VueloId = vuelo.VueloId,
                CodigoVuelo = "VIP" + vuelo.VueloId.ToString("D5"),
                FechaOperacion = vuelo.FechaOperacion,
                OrigenCodigo = vuelo.Frecuencia.Ruta.Aeropuerto.CodigoIATA,
                OrigenCiudad = vuelo.Frecuencia.Ruta.Aeropuerto.Localidad,
                DestinoCodigo = vuelo.Frecuencia.Ruta.Aeropuerto1.CodigoIATA,
                DestinoCiudad = vuelo.Frecuencia.Ruta.Aeropuerto1.Localidad,
                HoraSalida = vuelo.Frecuencia.HoraSalida,
                HoraLlegada = vuelo.Frecuencia.HoraLlegada,
                TarifaSeleccionada = tarifa,
                PrecioUnitario = precioUnitario,
                CantidadPasajeros = pasajeros,
                AsientosSeleccionados = "",
                Subtotal = precioUnitario * pasajeros,
                MapaCabina = mapa
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SeleccionarAsientos(SeleccionAsientosViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.AsientosSeleccionados))
            {
                ModelState.AddModelError("AsientosSeleccionados", $"Por favor seleccione los {model.CantidadPasajeros} asientos requeridos en la cabina.");
            }
            else
            {
                var asientos = model.AsientosSeleccionados
                    .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().ToUpperInvariant())
                    .Distinct()
                    .ToList();

                if (asientos.Count != model.CantidadPasajeros)
                {
                    ModelState.AddModelError("AsientosSeleccionados", $"Ha seleccionado {asientos.Count} asiento(s), pero el viaje es para {model.CantidadPasajeros} pasajero(s).");
                }
            }

            if (!ModelState.IsValid)
            {
                var vuelo = ObtenerVueloConDetalle(model.VueloId);
                var asignaciones = vuelo?.ReservaPasajeroVueloes
                    .Where(r => r.EstadoAsignacionCodigo != "CANCELADA")
                    .ToList();

                model.MapaCabina = VuelosController.GenerarMapaPhenom300E(
                    model.VueloId,
                    vuelo?.Aeronave?.Matricula ?? "AV-01",
                    asignaciones,
                    soloLectura: false,
                    maxSeleccionables: model.CantidadPasajeros,
                    preseleccionados: model.AsientosSeleccionados
                );
                return View(model);
            }

            return RedirectToAction("Pasajeros", new
            {
                vueloId = model.VueloId,
                asientos = model.AsientosSeleccionados,
                tarifa = model.TarifaSeleccionada
            });
        }

        // =========================================================================
        // PANTALLA 9: EMBUDO PASO 2 — DATOS DE PASAJEROS
        // =========================================================================
        [HttpGet]
        public ActionResult Pasajeros(int vueloId, string asientos, string tarifa = "Classic")
        {
            var vuelo = ObtenerVueloConDetalle(vueloId);
            if (vuelo == null)
            {
                return RedirectToAction("Buscar", "Vuelos");
            }

            var listaAsientos = (asientos ?? "1D")
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();

            if (!listaAsientos.Any())
            {
                listaAsientos.Add("1D");
            }

            decimal tarifaBase = vuelo.Frecuencia?.Ruta?.TarifaReferencial ?? 450.00m;
            decimal precioUnitario = tarifa.Equals("Flex", StringComparison.OrdinalIgnoreCase)
                ? Math.Round(tarifaBase * 1.35m, 2)
                : tarifaBase;

            var model = new RegistroPasajerosViewModel
            {
                VueloId = vuelo.VueloId,
                CodigoVuelo = "VIP" + vuelo.VueloId.ToString("D5"),
                FechaOperacion = vuelo.FechaOperacion,
                OrigenCodigo = vuelo.Frecuencia.Ruta.Aeropuerto.CodigoIATA,
                OrigenCiudad = vuelo.Frecuencia.Ruta.Aeropuerto.Localidad,
                DestinoCodigo = vuelo.Frecuencia.Ruta.Aeropuerto1.CodigoIATA,
                DestinoCiudad = vuelo.Frecuencia.Ruta.Aeropuerto1.Localidad,
                HoraSalida = vuelo.Frecuencia.HoraSalida,
                HoraLlegada = vuelo.Frecuencia.HoraLlegada,
                TarifaSeleccionada = tarifa,
                PrecioUnitario = precioUnitario,
                AsientosCodigos = string.Join(", ", listaAsientos),
                Subtotal = precioUnitario * listaAsientos.Count
            };

            for (int i = 0; i < listaAsientos.Count; i++)
            {
                model.Pasajeros.Add(new PasajeroItemViewModel
                {
                    Indice = i + 1,
                    AsientoAsignado = listaAsientos[i],
                    TipoDocumento = "CED",
                    Genero = "M"
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Pasajeros(RegistroPasajerosViewModel model)
        {
            if (model.Pasajeros == null || !model.Pasajeros.Any())
            {
                ModelState.AddModelError("", "Debe registrar al menos un pasajero.");
                return View(model);
            }

            // Normalización de entradas y validación rigurosa en el contexto ecuatoriano
            for (int i = 0; i < model.Pasajeros.Count; i++)
            {
                var p = model.Pasajeros[i];

                // 1. Normalización integral de entradas (trim, colapso de espacios intermedios, title case, email en minúsculas)
                p.Nombres = ValidadorEcuador.NormalizarNombrePropio(p.Nombres);
                p.Apellidos = ValidadorEcuador.NormalizarNombrePropio(p.Apellidos);
                p.NumeroDocumento = ValidadorEcuador.NormalizarDocumento(p.NumeroDocumento);
                p.Telefono = ValidadorEcuador.NormalizarTelefono(p.Telefono);
                p.Email = ValidadorEcuador.NormalizarEmail(p.Email);
                p.RequerimientoEspecial = ValidadorEcuador.NormalizarEspacios(p.RequerimientoEspecial);

                // 2. Validación de Nombres (máximo 25 caracteres, solo letras hispanas)
                if (!ValidadorEcuador.ValidarNombreApellido(p.Nombres, 25))
                {
                    ModelState.AddModelError($"Pasajeros[{i}].Nombres", $"Los nombres del Pasajero {i + 1} deben contener solo letras y no superar los 25 caracteres.");
                }

                // 3. Validación de Apellidos (máximo 25 caracteres, solo letras hispanas)
                if (!ValidadorEcuador.ValidarNombreApellido(p.Apellidos, 25))
                {
                    ModelState.AddModelError($"Pasajeros[{i}].Apellidos", $"Los apellidos del Pasajero {i + 1} deben contener solo letras y no superar los 25 caracteres.");
                }

                // 4. Validación de Documento de Identidad según tipo seleccionado
                if (p.TipoDocumento == "CED")
                {
                    if (!ValidadorEcuador.ValidarCedula(p.NumeroDocumento))
                    {
                        ModelState.AddModelError($"Pasajeros[{i}].NumeroDocumento", $"La cédula ecuatoriana del Pasajero {i + 1} no es válida según el Registro Civil (debe tener 10 dígitos, provincia 01-24 y cumplir Módulo 10).");
                    }
                }
                else if (p.TipoDocumento == "RUC")
                {
                    if (!ValidadorEcuador.ValidarRuc(p.NumeroDocumento))
                    {
                        ModelState.AddModelError($"Pasajeros[{i}].NumeroDocumento", $"El RUC del Pasajero {i + 1} debe contener 13 dígitos y terminar en un código de establecimiento válido.");
                    }
                }
                else if (p.TipoDocumento == "PAS")
                {
                    if (!ValidadorEcuador.ValidarPasaporte(p.NumeroDocumento))
                    {
                        ModelState.AddModelError($"Pasajeros[{i}].NumeroDocumento", $"El pasaporte del Pasajero {i + 1} debe ser alfanumérico entre 6 y 15 caracteres sin espacios.");
                    }
                }

                // 5. Validación de Fecha de Nacimiento y Categoría Aeronáutica (Infante 0-10, Menor 11-17, Adulto 18+)
                string errFecha;
                string categoriaEdad;
                if (!ValidadorEcuador.ValidarFechaNacimiento(p.FechaNacimiento, out errFecha, out categoriaEdad))
                {
                    ModelState.AddModelError($"Pasajeros[{i}].FechaNacimiento", $"Pasajero {i + 1}: {errFecha}");
                }
                else
                {
                    p.CategoriaEdad = categoriaEdad;
                }

                // 6. Validación de Celular Ecuatoriano (10 dígitos iniciado en 09)
                if (!ValidadorEcuador.ValidarTelefonoEcuador(p.Telefono))
                {
                    ModelState.AddModelError($"Pasajeros[{i}].Telefono", $"El teléfono del Pasajero {i + 1} debe ser un celular ecuatoriano de 10 dígitos iniciado en 09 (ej: 0991234567).");
                }

                // 7. Validación de Correo Electrónico (sin espacios ni caracteres especiales)
                if (!ValidadorEcuador.ValidarEmail(p.Email))
                {
                    ModelState.AddModelError($"Pasajeros[{i}].Email", $"Ingrese un correo electrónico válido para el Pasajero {i + 1} (sin espacios ni caracteres prohibidos).");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Guardar temporalmente en sesión para el siguiente paso del embudo
            Session["ReservaActual_Pasajeros"] = model.Pasajeros;
            Session["ReservaActual_VueloId"] = model.VueloId;
            Session["ReservaActual_Tarifa"] = model.TarifaSeleccionada;
            Session["ReservaActual_Asientos"] = model.AsientosCodigos;

            return RedirectToAction("Confirmar", new
            {
                vueloId = model.VueloId,
                tarifa = model.TarifaSeleccionada
            });
        }

        // =========================================================================
        // PANTALLA 10: EMBUDO PASO 3 — RESUMEN Y CONFIRMACIÓN DE RESERVA
        // =========================================================================
        [HttpGet]
        public ActionResult Confirmar(int? vueloId, string tarifa = "Classic")
        {
            int id = vueloId ?? (Session["ReservaActual_VueloId"] as int? ?? 1);
            var vuelo = ObtenerVueloConDetalle(id);
            if (vuelo == null)
            {
                return RedirectToAction("Buscar", "Vuelos");
            }

            var pasajeros = Session["ReservaActual_Pasajeros"] as List<PasajeroItemViewModel>;
            if (pasajeros == null || !pasajeros.Any())
            {
                // Datos demostrativos para visualización directa del wireframe
                pasajeros = new List<PasajeroItemViewModel>
                {
                    new PasajeroItemViewModel
                    {
                        Indice = 1,
                        AsientoAsignado = "1D",
                        Nombres = "Carlos Roberto",
                        Apellidos = "Mendoza Loor",
                        TipoDocumento = "CED",
                        NumeroDocumento = "1719283746",
                        FechaNacimiento = new DateTime(1988, 5, 14),
                        Genero = "M",
                        Telefono = "0991234567",
                        Email = "carlos.mendoza@aerolineavip.com",
                        RequerimientoEspecial = "Servicio de catering VIP ejecutivo"
                    }
                };
            }

            decimal tarifaBase = vuelo.Frecuencia?.Ruta?.TarifaReferencial ?? 450.00m;
            decimal precioUnitario = tarifa.Equals("Flex", StringComparison.OrdinalIgnoreCase)
                ? Math.Round(tarifaBase * 1.35m, 2)
                : tarifaBase;

            int cantidadPax = pasajeros.Count;
            decimal baseTotal = precioUnitario * cantidadPax;
            decimal tasas = 35.00m * cantidadPax; // Tasas de aeropuerto
            decimal subtotalNeto = baseTotal + tasas;
            decimal iva = Math.Round(subtotalNeto * 0.15m, 2); // IVA 15% Ecuador
            decimal total = subtotalNeto + iva;

            var random = new Random();
            string preCodigo = "PRE-" + random.Next(10000, 99999);

            var model = new ResumenReservaViewModel
            {
                CodigoPreReserva = preCodigo,
                VueloId = vuelo.VueloId,
                CodigoVuelo = "VIP" + vuelo.VueloId.ToString("D5"),
                FechaOperacion = vuelo.FechaOperacion,
                HoraSalida = vuelo.Frecuencia.HoraSalida,
                HoraLlegada = vuelo.Frecuencia.HoraLlegada,
                OrigenCodigo = vuelo.Frecuencia.Ruta.Aeropuerto.CodigoIATA,
                OrigenCiudad = vuelo.Frecuencia.Ruta.Aeropuerto.Localidad,
                DestinoCodigo = vuelo.Frecuencia.Ruta.Aeropuerto1.CodigoIATA,
                DestinoCiudad = vuelo.Frecuencia.Ruta.Aeropuerto1.Localidad,
                AeronaveMatricula = vuelo.Aeronave?.Matricula ?? "AV-01",
                TarifaSeleccionada = tarifa,
                PrecioUnitario = precioUnitario,
                CantidadPasajeros = cantidadPax,
                AsientosCodigos = string.Join(", ", pasajeros.Select(p => p.AsientoAsignado)),
                Pasajeros = pasajeros,
                TarifaBaseTotal = baseTotal,
                TasasAeroportuarias = tasas,
                SubtotalNeto = subtotalNeto,
                IvaMonto = iva,
                TotalPagar = total,
                MinutosRetencion = 15
            };

            Session["ReservaActual_Resumen"] = model;

            return View(model);
        }

        // =========================================================================
        // PANTALLA 11: EMBUDO PASO 4 — PAGO SIMULADO Y MODAL
        // =========================================================================
        [HttpGet]
        public ActionResult Pagar(string codigoPreReserva, int? vueloId, string tarifa = "Classic")
        {
            var resumen = Session["ReservaActual_Resumen"] as ResumenReservaViewModel;
            int id = vueloId ?? (resumen?.VueloId ?? 1);
            var vuelo = ObtenerVueloConDetalle(id);

            decimal total = resumen?.TotalPagar ?? 557.75m;

            var model = new PagoSimuladoViewModel
            {
                CodigoPreReserva = !string.IsNullOrWhiteSpace(codigoPreReserva) ? codigoPreReserva : (resumen?.CodigoPreReserva ?? "PRE-84920"),
                VueloId = id,
                CodigoVuelo = vuelo != null ? "VIP" + vuelo.VueloId.ToString("D5") : "VIP00001",
                FechaOperacion = vuelo?.FechaOperacion ?? DateTime.Today.AddDays(2),
                OrigenCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA ?? "UIO",
                DestinoCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA ?? "GYE",
                TotalPagar = total,
                TarifaSeleccionada = tarifa,
                CantidadPasajeros = resumen?.CantidadPasajeros ?? 1,
                AsientosCodigos = resumen?.AsientosCodigos ?? "1D",
                MarcaTarjeta = "VISA",
                TitularTarjeta = "CARLOS ROBERTO MENDOZA",
                NumeroTarjeta = "4500 1234 5678 9010",
                MesExpiracion = "12",
                AnioExpiracion = "28",
                Cvv = "889",
                AceptoTerminos = true
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcesarPago(PagoSimuladoViewModel model)
        {
            // 1. Normalización de entradas de pago
            model.TitularTarjeta = ValidadorEcuador.NormalizarEspacios(model.TitularTarjeta).ToUpperInvariant();
            string numTarjetaSoloDigitos = System.Text.RegularExpressions.Regex.Replace(model.NumeroTarjeta ?? "", @"\D", "");
            model.Cvv = (model.Cvv ?? "").Trim();

            // 2. Validación del Titular (mínimo 2 palabras, solo letras)
            if (!ValidadorEcuador.ValidarTitularTarjeta(model.TitularTarjeta))
            {
                ModelState.AddModelError("TitularTarjeta", "Ingrese el nombre y apellido completos como figura en la tarjeta (solo letras y mínimo dos palabras).");
            }

            // 3. Validación de la Tarjeta flexibilizada según dígitos mínimos requeridos
            if (!ValidadorEcuador.ValidarTarjetaDigitosMinimos(numTarjetaSoloDigitos, model.MarcaTarjeta))
            {
                int min = model.MarcaTarjeta == "DINERS" ? 14 : 15;
                ModelState.AddModelError("NumeroTarjeta", $"El número de tarjeta debe tener al menos {min} dígitos numéricos.");
            }

            // 4. Validación de Expiración
            if (!ValidadorEcuador.ValidarVencimientoTarjeta(model.MesExpiracion, model.AnioExpiracion))
            {
                ModelState.AddModelError("MesExpiracion", "La tarjeta seleccionada se encuentra vencida.");
            }

            // 5. Validación de CVV según marca
            if (!ValidadorEcuador.ValidarCvv(model.Cvv, model.MarcaTarjeta))
            {
                string msgCvv = model.MarcaTarjeta == "AMEX"
                    ? "El código de seguridad CVV debe tener 4 dígitos para American Express."
                    : "El código de seguridad CVV debe tener 3 dígitos.";
                ModelState.AddModelError("Cvv", msgCvv);
            }

            if (!ModelState.IsValid)
            {
                return View("Pagar", model);
            }

            var random = new Random();
            string codigoReserva = "VIP-" + random.Next(10000, 99999);
            string codigoOperacion = "AUT-" + random.Next(100000, 999999);

            return RedirectToAction("Exito", new
            {
                codigoReserva = codigoReserva,
                codigoOperacion = codigoOperacion,
                vueloId = model.VueloId,
                marca = model.MarcaTarjeta,
                ultimos4 = numTarjetaSoloDigitos.Length >= 4 ? numTarjetaSoloDigitos.Substring(numTarjetaSoloDigitos.Length - 4) : "9010",
                titular = model.TitularTarjeta
            });
        }

        // =========================================================================
        // PANTALLA 12: CONFIRMACIÓN DE COMPRA EXITOSA Y PASES DE ABORDAR
        // =========================================================================
        [HttpGet]
        public ActionResult Exito(string codigoReserva = "VIP-78492", string codigoOperacion = "AUT-582910", int vueloId = 1, string marca = "VISA", string ultimos4 = "9010", string titular = "CARLOS ROBERTO MENDOZA")
        {
            var vuelo = ObtenerVueloConDetalle(vueloId);
            var resumen = Session["ReservaActual_Resumen"] as ResumenReservaViewModel;
            var pasajeros = resumen?.Pasajeros;

            if (pasajeros == null || !pasajeros.Any())
            {
                pasajeros = new List<PasajeroItemViewModel>
                {
                    new PasajeroItemViewModel
                    {
                        Indice = 1,
                        AsientoAsignado = "1D",
                        Nombres = "Carlos Roberto",
                        Apellidos = "Mendoza Loor",
                        TipoDocumento = "CED",
                        NumeroDocumento = "1719283746",
                        Email = "carlos.mendoza@aerolineavip.com",
                        Telefono = "0991234567"
                    }
                };
            }

            var model = new ConfirmacionCompraViewModel
            {
                CodigoReserva = codigoReserva,
                CodigoOperacion = codigoOperacion,
                FechaCompra = DateTime.Now,
                VueloId = vuelo?.VueloId ?? 1,
                CodigoVuelo = vuelo != null ? "VIP" + vuelo.VueloId.ToString("D5") : "VIP00001",
                FechaOperacion = vuelo?.FechaOperacion ?? DateTime.Today.AddDays(2),
                HoraSalida = vuelo?.Frecuencia?.HoraSalida ?? new TimeSpan(7, 30, 0),
                HoraLlegada = vuelo?.Frecuencia?.HoraLlegada ?? new TimeSpan(8, 25, 0),
                OrigenCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA ?? "UIO",
                OrigenCiudad = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.Localidad ?? "Quito",
                DestinoCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA ?? "GYE",
                DestinoCiudad = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.Localidad ?? "Guayaquil",
                AeronaveMatricula = vuelo?.Aeronave?.Matricula ?? "AV-01",
                TarifaSeleccionada = resumen?.TarifaSeleccionada ?? "Classic",
                TotalPagado = resumen?.TotalPagar ?? 557.75m,
                MarcaTarjeta = marca,
                TarjetaEnmascarada = "**** " + ultimos4,
                TitularTarjeta = titular,
                Pasajeros = pasajeros
            };

            return View(model);
        }

        // =========================================================================
        // ACCIÓN DE APOYO: MIS RESERVAS (P13 placeholder para navegación limpia)
        // =========================================================================
        [HttpGet]
        public ActionResult MisReservas()
        {
            ViewBag.Mensaje = "Sección 'Mis Reservas' (Pantalla 13) programada para entrega completa en el Día 4.";
            return RedirectToAction("Buscar", "Vuelos");
        }

        // =========================================================================
        // MÉTODOS PRIVADOS DE AYUDA
        // =========================================================================
        private Vuelo ObtenerVueloConDetalle(int? vueloId)
        {
            var query = db.Vueloes
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Include(v => v.Aeronave.ModeloAeronave)
                .Include(v => v.ReservaPasajeroVueloes);

            if (vueloId.HasValue && vueloId.Value > 0)
            {
                var v = query.FirstOrDefault(x => x.VueloId == vueloId.Value);
                if (v != null) return v;
            }

            return query.FirstOrDefault(x => x.EstadoVueloCodigo != "CANCELADO");
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
