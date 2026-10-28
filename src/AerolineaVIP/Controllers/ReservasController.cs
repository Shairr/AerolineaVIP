using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    [Authorize]
    public class ReservasController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        // =========================================================================
        // PANTALLA 8: EMBUDO PASO 1 ÔÇö SELECCI├ôN INTERACTIVA DE ASIENTOS
        // =========================================================================
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
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

            var asignaciones = VuelosController.ObtenerAsignacionesCombinadas(
                vuelo.VueloId,
                vuelo.ReservaPasajeroVueloes,
                Session["MisReservasNuevas"] as List<ReservaResumenItemViewModel>
            );

            var checkInsSesion = Session["CodigosCheckInCompletados"] as HashSet<string>;
            var mapa = VuelosController.GenerarMapaPhenom300E(
                vuelo.VueloId,
                vuelo.Aeronave?.Matricula ?? "AV-01",
                asignaciones,
                soloLectura: false,
                maxSeleccionables: pasajeros,
                preseleccionados: "",
                codigosCheckInSesion: checkInsSesion
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
            var vuelo = ObtenerVueloConDetalle(model.VueloId);
            var asignaciones = VuelosController.ObtenerAsignacionesCombinadas(
                model.VueloId,
                vuelo?.ReservaPasajeroVueloes,
                Session["MisReservasNuevas"] as List<ReservaResumenItemViewModel>
            );

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

                // Validar en servidor que ninguno de los asientos seleccionados estÃ© ya ocupado
                var asientosOcupados = asignaciones
                    .Where(a => a.EstadoAsignacionCodigo == "CONFIRMADA")
                    .Select(a => VuelosController.MapearNumeroAsientoACodigo(a.NumeroAsiento))
                    .ToList();

                var enConflicto = asientos.Where(a => asientosOcupados.Contains(a, StringComparer.OrdinalIgnoreCase)).ToList();
                if (enConflicto.Any())
                {
                    ModelState.AddModelError("AsientosSeleccionados", $"El o los asientos {string.Join(", ", enConflicto)} no estÃ¡n disponibles (ocupados). Por favor seleccione asientos en verde.");
                }
            }

            if (!ModelState.IsValid)
            {
                var checkInsSesion = Session["CodigosCheckInCompletados"] as HashSet<string>;
                model.MapaCabina = VuelosController.GenerarMapaPhenom300E(
                    model.VueloId,
                    vuelo?.Aeronave?.Matricula ?? "AV-01",
                    asignaciones,
                    soloLectura: false,
                    maxSeleccionables: model.CantidadPasajeros,
                    preseleccionados: model.AsientosSeleccionados,
                    codigosCheckInSesion: checkInsSesion
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
        // PANTALLA 9: EMBUDO PASO 2 ÔÇö DATOS DE PASAJEROS
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

            // Normalizaci├│n de entradas y validaci├│n rigurosa en el contexto ecuatoriano
            for (int i = 0; i < model.Pasajeros.Count; i++)
            {
                var p = model.Pasajeros[i];

                // 1. Normalizaci├│n integral de entradas (trim, colapso de espacios intermedios, title case, email en min├║sculas)
                p.Nombres = ValidadorEcuador.NormalizarNombrePropio(p.Nombres);
                p.Apellidos = ValidadorEcuador.NormalizarNombrePropio(p.Apellidos);
                p.NumeroDocumento = ValidadorEcuador.NormalizarDocumento(p.NumeroDocumento);
                p.Telefono = ValidadorEcuador.NormalizarTelefono(p.Telefono);
                p.Email = ValidadorEcuador.NormalizarEmail(p.Email);
                p.RequerimientoEspecial = ValidadorEcuador.NormalizarEspacios(p.RequerimientoEspecial);

                // 2. Validaci├│n de Nombres (m├íximo 25 caracteres, solo letras hispanas)
                if (!ValidadorEcuador.ValidarNombreApellido(p.Nombres, 25))
                {
                    ModelState.AddModelError($"Pasajeros[{i}].Nombres", $"Los nombres del Pasajero {i + 1} deben contener solo letras y no superar los 25 caracteres.");
                }

                // 3. Validaci├│n de Apellidos (m├íximo 25 caracteres, solo letras hispanas)
                if (!ValidadorEcuador.ValidarNombreApellido(p.Apellidos, 25))
                {
                    ModelState.AddModelError($"Pasajeros[{i}].Apellidos", $"Los apellidos del Pasajero {i + 1} deben contener solo letras y no superar los 25 caracteres.");
                }

                // 4. Validaci├│n de Documento de Identidad seg├║n tipo seleccionado
                if (p.TipoDocumento == "CED")
                {
                    if (!ValidadorEcuador.ValidarCedula(p.NumeroDocumento))
                    {
                        ModelState.AddModelError($"Pasajeros[{i}].NumeroDocumento", $"La c├®dula ecuatoriana del Pasajero {i + 1} no es v├ílida seg├║n el Registro Civil (debe tener 10 d├¡gitos, provincia 01-24 y cumplir M├│dulo 10).");
                    }
                }
                else if (p.TipoDocumento == "RUC")
                {
                    if (!ValidadorEcuador.ValidarRuc(p.NumeroDocumento))
                    {
                        ModelState.AddModelError($"Pasajeros[{i}].NumeroDocumento", $"El RUC del Pasajero {i + 1} debe contener 13 d├¡gitos y terminar en un c├│digo de establecimiento v├ílido.");
                    }
                }
                else if (p.TipoDocumento == "PAS")
                {
                    if (!ValidadorEcuador.ValidarPasaporte(p.NumeroDocumento))
                    {
                        ModelState.AddModelError($"Pasajeros[{i}].NumeroDocumento", $"El pasaporte del Pasajero {i + 1} debe ser alfanum├®rico entre 6 y 15 caracteres sin espacios.");
                    }
                }

                // 5. Validaci├│n de Fecha de Nacimiento y Categor├¡a Aeron├íutica (Infante 0-10, Menor 11-17, Adulto 18+)
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

                // 6. Validaci├│n de Celular Ecuatoriano (10 d├¡gitos iniciado en 09)
                if (!ValidadorEcuador.ValidarTelefonoEcuador(p.Telefono))
                {
                    ModelState.AddModelError($"Pasajeros[{i}].Telefono", $"El tel├®fono del Pasajero {i + 1} debe ser un celular ecuatoriano de 10 d├¡gitos iniciado en 09 (ej: 0991234567).");
                }

                // 7. Validaci├│n de Correo Electr├│nico (sin espacios ni caracteres especiales)
                if (!ValidadorEcuador.ValidarEmail(p.Email))
                {
                    ModelState.AddModelError($"Pasajeros[{i}].Email", $"Ingrese un correo electr├│nico v├ílido para el Pasajero {i + 1} (sin espacios ni caracteres prohibidos).");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Guardar temporalmente en sesi├│n para el siguiente paso del embudo
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
        // PANTALLA 10: EMBUDO PASO 3 ÔÇö RESUMEN Y CONFIRMACI├ôN DE RESERVA
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
                // Datos demostrativos para visualizaci├│n directa del wireframe
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
        // PANTALLA 11: EMBUDO PASO 4 ÔÇö PAGO SIMULADO Y MODAL
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
            // 1. Normalizaci├│n de entradas de pago
            model.TitularTarjeta = ValidadorEcuador.NormalizarEspacios(model.TitularTarjeta).ToUpperInvariant();
            string numTarjetaSoloDigitos = System.Text.RegularExpressions.Regex.Replace(model.NumeroTarjeta ?? "", @"\D", "");
            model.Cvv = (model.Cvv ?? "").Trim();

            // 2. Validaci├│n del Titular (m├¡nimo 2 palabras, solo letras)
            if (!ValidadorEcuador.ValidarTitularTarjeta(model.TitularTarjeta))
            {
                ModelState.AddModelError("TitularTarjeta", "Ingrese el nombre y apellido completos como figura en la tarjeta (solo letras y m├¡nimo dos palabras).");
            }

            // 3. Validaci├│n de la Tarjeta flexibilizada seg├║n d├¡gitos m├¡nimos requeridos
            if (!ValidadorEcuador.ValidarTarjetaDigitosMinimos(numTarjetaSoloDigitos, model.MarcaTarjeta))
            {
                int min = model.MarcaTarjeta == "DINERS" ? 14 : 15;
                ModelState.AddModelError("NumeroTarjeta", $"El n├║mero de tarjeta debe tener al menos {min} d├¡gitos num├®ricos.");
            }

            // 4. Validaci├│n de Expiraci├│n
            if (!ValidadorEcuador.ValidarVencimientoTarjeta(model.MesExpiracion, model.AnioExpiracion))
            {
                ModelState.AddModelError("MesExpiracion", "La tarjeta seleccionada se encuentra vencida.");
            }

            // 5. Validaci├│n de CVV seg├║n marca
            if (!ValidadorEcuador.ValidarCvv(model.Cvv, model.MarcaTarjeta))
            {
                string msgCvv = model.MarcaTarjeta == "AMEX"
                    ? "El c├│digo de seguridad CVV debe tener 4 d├¡gitos para American Express."
                    : "El c├│digo de seguridad CVV debe tener 3 d├¡gitos.";
                ModelState.AddModelError("Cvv", msgCvv);
            }

            if (!ModelState.IsValid)
            {
                return View("Pagar", model);
            }


            var random = new Random();
            string codigoReserva = "VIP-" + random.Next(10000, 99999);
            string codigoOperacion = "AUT-" + random.Next(100000, 999999);

            // Guardar en sesiÃ³n para Mis Reservas y Check-in interactivo
            var resumen = Session["ReservaActual_Resumen"] as ResumenReservaViewModel;
            var vueloActual = ObtenerVueloConDetalle(model.VueloId);

            var reservaNueva = new ReservaResumenItemViewModel
            {
                ReservaId = random.Next(1000, 9999),
                CodigoReserva = codigoReserva,
                FechaCreacion = DateTime.Now,
                EstadoReservaCodigo = "CONFIRMADA",
                VueloId = vueloActual?.VueloId ?? model.VueloId,
                CodigoVuelo = "VIP" + (vueloActual?.VueloId ?? model.VueloId).ToString("D5"),
                OrigenCodigo = vueloActual?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA ?? "UIO",
                OrigenCiudad = vueloActual?.Frecuencia?.Ruta?.Aeropuerto?.Localidad ?? "Quito",
                DestinoCodigo = vueloActual?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA ?? "GYE",
                DestinoCiudad = vueloActual?.Frecuencia?.Ruta?.Aeropuerto1?.Localidad ?? "Guayaquil",
                FechaOperacion = vueloActual?.FechaOperacion ?? DateTime.Today.AddDays(2),
                HoraSalida = vueloActual?.Frecuencia?.HoraSalida ?? new TimeSpan(7, 30, 0),
                HoraLlegada = vueloActual?.Frecuencia?.HoraLlegada ?? new TimeSpan(8, 25, 0),
                AeronaveMatricula = vueloActual?.Aeronave?.Matricula ?? "AV-01",
                ModeloAeronave = vueloActual?.Aeronave?.ModeloAeronave?.Nombre ?? "Phenom 300E",
                CantidadPasajeros = resumen?.Pasajeros?.Count ?? 1,
                Tarifa = resumen?.TarifaSeleccionada ?? "Classic",
                TotalPagado = resumen?.TotalPagar ?? 557.75m,
                TieneCheckIn = false,
                EsCancelable = true,
                PuedeHacerCheckIn = true
            };

            if (resumen?.Pasajeros != null && resumen.Pasajeros.Any())
            {
                reservaNueva.PasajerosNombres = resumen.Pasajeros.Select(p => (p.Nombres + " " + p.Apellidos).Trim()).ToList();
                reservaNueva.AsientosCodigos = string.Join(", ", resumen.Pasajeros.Select(p => p.AsientoAsignado));
            }
            else
            {
                reservaNueva.PasajerosNombres = new List<string> { model.TitularTarjeta };
                reservaNueva.AsientosCodigos = "1D";
            }

            // Bloquear inmediatamente los asientos a nivel global y de sesiÃ³n
            var codigosAsientosList = new List<string>();
            if (resumen?.Pasajeros != null && resumen.Pasajeros.Any())
            {
                codigosAsientosList = resumen.Pasajeros
                    .Select(p => p.AsientoAsignado?.Trim()?.ToUpperInvariant())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }
            if (!codigosAsientosList.Any())
            {
                codigosAsientosList.Add("1D");
            }
            VuelosController.RegistrarAsientosOcupados(model.VueloId, codigosAsientosList);

            // Persistir reserva y asignaciones en la base de datos SQL Server
            try
            {
                string userId = User?.Identity?.IsAuthenticated == true
                    ? (Microsoft.AspNet.Identity.IdentityExtensions.GetUserId(User.Identity) ?? User.Identity.Name)
                    : "cliente01";

                var reservaDb = new Reserva
                {
                    UsuarioId = string.IsNullOrWhiteSpace(userId) ? "cliente01" : userId,
                    FechaCreacion = DateTime.Now,
                    EstadoReservaCodigo = "CONFIRMADA"
                };
                db.Reservas.Add(reservaDb);

                decimal tarifaUnit = resumen?.TarifaBaseTotal ?? 450.00m;
                if (resumen?.CantidadPasajeros > 1)
                {
                    tarifaUnit = Math.Round(resumen.TarifaBaseTotal / resumen.CantidadPasajeros, 2);
                }

                if (resumen?.Pasajeros != null && resumen.Pasajeros.Any())
                {
                    foreach (var pModel in resumen.Pasajeros)
                    {
                        var pasExistente = db.Pasajeroes.FirstOrDefault(p => 
                            p.TipoDocumentoCodigo == pModel.TipoDocumento && 
                            p.NumeroDocumento == pModel.NumeroDocumento);

                        if (pasExistente == null)
                        {
                            pasExistente = new Pasajero
                            {
                                TipoDocumentoCodigo = pModel.TipoDocumento,
                                NumeroDocumento = pModel.NumeroDocumento,
                                Nombres = pModel.Nombres,
                                Apellidos = pModel.Apellidos,
                                Correo = pModel.Email,
                                Telefono = pModel.Telefono,
                                FechaRegistro = DateTime.Now
                            };
                            db.Pasajeroes.Add(pasExistente);
                        }

                        byte numAsiento = VuelosController.MapearCodigoANumeroAsiento(pModel.AsientoAsignado);
                        db.ReservaPasajeroVueloes.Add(new ReservaPasajeroVuelo
                        {
                            Reserva = reservaDb,
                            Pasajero = pasExistente,
                            VueloId = model.VueloId,
                            NumeroAsiento = numAsiento,
                            ImporteTramo = tarifaUnit,
                            EstadoAsignacionCodigo = "CONFIRMADA",
                            FechaAsignacion = DateTime.Now
                        });
                    }
                }

                var compraDb = new Compra
                {
                    Reserva = reservaDb,
                    FechaCompra = DateTime.Now,
                    Importe = resumen?.TotalPagar ?? model.TotalPagar,
                    MonedaCodigo = "USD",
                    EstadoCompraCodigo = "PAGADA",
                    ClaveIdempotencia = Guid.NewGuid()
                };
                db.Compras.Add(compraDb);

                db.Pagoes.Add(new Pago
                {
                    Compra = compraDb,
                    FechaPago = DateTime.Now,
                    Importe = compraDb.Importe,
                    EstadoPagoCodigo = "CONFIRMADO",
                    MetodoPagoCodigo = "TARJETA_CREDITO",
                    MarcaTarjetaCodigo = model.MarcaTarjeta,
                    UltimosCuatroDigitos = numTarjetaSoloDigitos.Length >= 4 ? numTarjetaSoloDigitos.Substring(numTarjetaSoloDigitos.Length - 4) : "9010",
                    NombreTitular = model.TitularTarjeta,
                    CodigoOperacion = codigoOperacion,
                    EsSimulado = true,
                    MensajeResultado = "Pago simulado aprobado exitosamente"
                });

                db.SaveChanges();

                if (!string.IsNullOrWhiteSpace(reservaDb.CodigoReserva))
                {
                    codigoReserva = reservaDb.CodigoReserva;
                    reservaNueva.CodigoReserva = codigoReserva;
                    reservaNueva.ReservaId = reservaDb.ReservaId;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al persistir reserva en BD: " + ex.Message);
            }

            var listaReservas = Session["MisReservasNuevas"] as List<ReservaResumenItemViewModel> ?? new List<ReservaResumenItemViewModel>();
            listaReservas.Insert(0, reservaNueva);
            Session["MisReservasNuevas"] = listaReservas;

            // Guardar tambiÃ©n el detalle completo para Pantalla 14 (Gestionar)
            var gestionDetalle = new GestionarReservaViewModel
            {
                ReservaId = reservaNueva.ReservaId,
                CodigoReserva = codigoReserva,
                FechaCreacion = reservaNueva.FechaCreacion,
                EstadoReservaCodigo = "CONFIRMADA",
                VueloId = reservaNueva.VueloId,
                CodigoVuelo = reservaNueva.CodigoVuelo,
                OrigenCodigo = reservaNueva.OrigenCodigo,
                OrigenCiudad = reservaNueva.OrigenCiudad,
                DestinoCodigo = reservaNueva.DestinoCodigo,
                DestinoCiudad = reservaNueva.DestinoCiudad,
                FechaOperacion = reservaNueva.FechaOperacion,
                HoraSalida = reservaNueva.HoraSalida,
                HoraLlegada = reservaNueva.HoraLlegada,
                AeronaveMatricula = reservaNueva.AeronaveMatricula,
                ModeloAeronave = reservaNueva.ModeloAeronave,
                Tarifa = reservaNueva.Tarifa,
                TarifaBase = resumen?.TarifaBaseTotal ?? 450.00m,
                TasasAeroportuarias = resumen?.TasasAeroportuarias ?? 35.00m,
                Subtotal = (resumen?.SubtotalNeto ?? 485.00m),
                Iva = resumen?.IvaMonto ?? 72.75m,
                TotalPagado = reservaNueva.TotalPagado,
                MarcaTarjeta = model.MarcaTarjeta,
                Ultimos4 = numTarjetaSoloDigitos.Length >= 4 ? numTarjetaSoloDigitos.Substring(numTarjetaSoloDigitos.Length - 4) : "9010",
                CodigoOperacion = codigoOperacion,
                FechaPago = DateTime.Now,
                EsCancelable = true,
                PoliticaCancelacion = "CancelaciÃ³n con reembolso del 80% si se realiza al menos 24 horas antes de la salida programada del vuelo.",
                TieneCheckIn = false
            };

            if (resumen?.Pasajeros != null && resumen.Pasajeros.Any())
            {
                int dummyAsig = 9000;
                foreach (var p in resumen.Pasajeros)
                {
                    gestionDetalle.Pasajeros.Add(new PasajeroDetalleItemViewModel
                    {
                        AsignacionId = dummyAsig++,
                        PasajeroId = dummyAsig,
                        Nombres = p.Nombres,
                        Apellidos = p.Apellidos,
                        TipoDocumento = p.TipoDocumento,
                        NumeroDocumento = p.NumeroDocumento,
                        AsientoCodigo = p.AsientoAsignado,
                        Correo = p.Email,
                        Telefono = p.Telefono,
                        CategoriaEdad = "Adulto",
                        CheckInCompletado = false
                    });
                }
            }

            var dictDetalles = Session["MisReservasDetalles"] as Dictionary<string, GestionarReservaViewModel> ?? new Dictionary<string, GestionarReservaViewModel>(StringComparer.OrdinalIgnoreCase);
            dictDetalles[codigoReserva] = gestionDetalle;
            Session["MisReservasDetalles"] = dictDetalles;

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
        // PANTALLA 12: CONFIRMACI├ôN DE COMPRA EXITOSA Y PASES DE ABORDAR
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
        // PANTALLA 13: MIS RESERVAS (PORTAL CLIENTE VIP)
        // =========================================================================
        [Authorize(Roles = "Cliente,SuperUsuario,Administrador")]
        [HttpGet]
        public ActionResult MisReservas(string tab = "todas")
        {
            var model = new MisReservasViewModel
            {
                FiltroActual = string.IsNullOrWhiteSpace(tab) ? "todas" : tab.ToLowerInvariant()
            };

            var todas = new List<ReservaResumenItemViewModel>();
            var checkInsCompletados = Session["CodigosCheckInCompletados"] as HashSet<string> 
                                     ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Cargar reservas creadas en la sesiÃ³n actual
            var enSesion = Session["MisReservasNuevas"] as List<ReservaResumenItemViewModel>;
            if (enSesion != null)
            {
                foreach (var rSes in enSesion)
                {
                    if (checkInsCompletados.Contains(rSes.CodigoReserva))
                    {
                        rSes.TieneCheckIn = true;
                        rSes.PuedeHacerCheckIn = false;
                    }
                }
                todas.AddRange(enSesion);
            }

            // 2. Cargar reservas existentes en la base de datos
            var reservasDb = db.Reservas
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Aeronave.ModeloAeronave))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.CheckIns))
                .Include(r => r.Compras.Select(c => c.Pagoes))
                .OrderByDescending(r => r.FechaCreacion)
                .ToList();

            foreach (var r in reservasDb)
            {
                if (todas.Any(x => x.CodigoReserva.Equals(r.CodigoReserva, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var primeraAsignacion = r.ReservaPasajeroVueloes.FirstOrDefault();
                var vuelo = primeraAsignacion?.Vuelo;
                var compra = r.Compras.FirstOrDefault();
                var pago = compra?.Pagoes.FirstOrDefault();

                bool tieneCheckIn = checkInsCompletados.Contains(r.CodigoReserva) || r.ReservaPasajeroVueloes.Any(a => a.CheckIns != null && a.CheckIns.Any(c => c.EstadoCheckInCodigo == "COMPLETADO"));
                bool esCancelada = string.Equals(r.EstadoReservaCodigo, "CANCELADA", StringComparison.OrdinalIgnoreCase);
                DateTime fechaVuelo = vuelo?.FechaOperacion ?? DateTime.Today;

                var item = new ReservaResumenItemViewModel
                {
                    ReservaId = r.ReservaId,
                    CodigoReserva = r.CodigoReserva,
                    FechaCreacion = r.FechaCreacion,
                    EstadoReservaCodigo = r.EstadoReservaCodigo ?? "CONFIRMADA",
                    VueloId = vuelo?.VueloId ?? 1,
                    CodigoVuelo = "VIP" + (vuelo?.VueloId ?? 1).ToString("D5"),
                    OrigenCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA ?? "UIO",
                    OrigenCiudad = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.Localidad ?? "Quito",
                    DestinoCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA ?? "GYE",
                    DestinoCiudad = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.Localidad ?? "Guayaquil",
                    FechaOperacion = fechaVuelo,
                    HoraSalida = vuelo?.Frecuencia?.HoraSalida ?? new TimeSpan(7, 30, 0),
                    HoraLlegada = vuelo?.Frecuencia?.HoraLlegada ?? new TimeSpan(8, 25, 0),
                    AeronaveMatricula = vuelo?.Aeronave?.Matricula ?? "AV-01",
                    ModeloAeronave = vuelo?.Aeronave?.ModeloAeronave?.Nombre ?? "Phenom 300E",
                    CantidadPasajeros = r.ReservaPasajeroVueloes.Count,
                    Tarifa = "Classic",
                    TotalPagado = compra?.Importe ?? 557.75m,
                    TieneCheckIn = tieneCheckIn,
                    EsCancelable = !esCancelada && fechaVuelo >= DateTime.Today,
                    PuedeHacerCheckIn = !esCancelada && !tieneCheckIn
                };

                if (r.ReservaPasajeroVueloes.Any())
                {
                    item.PasajerosNombres = r.ReservaPasajeroVueloes
                        .Where(a => a.Pasajero != null)
                        .Select(a => (a.Pasajero.Nombres + " " + a.Pasajero.Apellidos).Trim())
                        .ToList();

                    item.AsientosCodigos = string.Join(", ", r.ReservaPasajeroVueloes.Select(a => MapearNumeroAsientoACodigo(a.NumeroAsiento)));
                }

                todas.Add(item);
            }

            // Calcular contadores por pestaÃ±a
            // PrÃ³ximas: reservas activas futuras PENDIENTES de check-in
            model.TotalProximas = todas.Count(r => r.EstadoReservaCodigo != "CANCELADA" && !r.TieneCheckIn && r.FechaOperacion >= DateTime.Today);
            // Completadas: reservas que ya completaron su Check-in O vuelos de fechas pasadas
            model.TotalCompletadas = todas.Count(r => r.EstadoReservaCodigo != "CANCELADA" && (r.TieneCheckIn || r.FechaOperacion < DateTime.Today));
            // Canceladas
            model.TotalCanceladas = todas.Count(r => r.EstadoReservaCodigo == "CANCELADA");

            // Filtrar segÃºn el tab solicitado
            switch (model.FiltroActual)
            {
                case "proximas":
                    model.Reservas = todas.Where(r => r.EstadoReservaCodigo != "CANCELADA" && !r.TieneCheckIn && r.FechaOperacion >= DateTime.Today).ToList();
                    break;
                case "completadas":
                    model.Reservas = todas.Where(r => r.EstadoReservaCodigo != "CANCELADA" && (r.TieneCheckIn || r.FechaOperacion < DateTime.Today)).ToList();
                    break;
                case "canceladas":
                    model.Reservas = todas.Where(r => r.EstadoReservaCodigo == "CANCELADA").ToList();
                    break;
                default:
                    model.Reservas = todas;
                    break;
            }

            return View(model);
        }

        // =========================================================================
        // PANTALLA 14: GESTIONAR RESERVA (DETALLES Y CANCELACIÃ“N)
        // =========================================================================
        [Authorize(Roles = "Cliente,SuperUsuario,Administrador")]
        [HttpGet]
        public ActionResult Gestionar(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("MisReservas");
            }

            string codigoLimpio = id.Trim().ToUpperInvariant();

            // 1. Revisar si estÃ¡ en sesiÃ³n
            var dictDetalles = Session["MisReservasDetalles"] as Dictionary<string, GestionarReservaViewModel>;
            if (dictDetalles != null && dictDetalles.ContainsKey(codigoLimpio))
            {
                var sesModel = dictDetalles[codigoLimpio];
                var checkInsCompletadosSes = Session["CodigosCheckInCompletados"] as HashSet<string> 
                                             ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (checkInsCompletadosSes.Contains(codigoLimpio))
                {
                    sesModel.TieneCheckIn = true;
                    if (sesModel.Pasajeros != null)
                    {
                        foreach (var p in sesModel.Pasajeros)
                        {
                            p.CheckInCompletado = true;
                        }
                    }
                }
                return View(sesModel);
            }

            // 2. Buscar en base de datos
            var reserva = db.Reservas
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Aeronave.ModeloAeronave))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.CheckIns))
                .Include(r => r.Compras.Select(c => c.Pagoes))
                .FirstOrDefault(r => r.CodigoReserva == codigoLimpio);

            if (reserva == null)
            {
                TempData["MensajeError"] = "No se encontrÃ³ la reserva " + id;
                return RedirectToAction("MisReservas");
            }

            var primeraAsignacion = reserva.ReservaPasajeroVueloes.FirstOrDefault();
            var vuelo = primeraAsignacion?.Vuelo;
            var compra = reserva.Compras.FirstOrDefault();
            var pago = compra?.Pagoes.FirstOrDefault();

            var checkInsCompletados = Session["CodigosCheckInCompletados"] as HashSet<string> ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool tieneCheckIn = checkInsCompletados.Contains(codigoLimpio) || reserva.ReservaPasajeroVueloes.Any(a => a.CheckIns != null && a.CheckIns.Any(c => c.EstadoCheckInCodigo == "COMPLETADO"));
            bool esCancelada = string.Equals(reserva.EstadoReservaCodigo, "CANCELADA", StringComparison.OrdinalIgnoreCase);

            decimal total = compra?.Importe ?? 557.75m;
            decimal baseTarifa = Math.Round(total / 1.15m, 2);
            decimal iva = total - baseTarifa;

            var model = new GestionarReservaViewModel
            {
                ReservaId = reserva.ReservaId,
                CodigoReserva = reserva.CodigoReserva,
                FechaCreacion = reserva.FechaCreacion,
                EstadoReservaCodigo = reserva.EstadoReservaCodigo ?? "CONFIRMADA",
                VueloId = vuelo?.VueloId ?? 1,
                CodigoVuelo = "VIP" + (vuelo?.VueloId ?? 1).ToString("D5"),
                OrigenCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA ?? "UIO",
                OrigenCiudad = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.Localidad ?? "Quito",
                DestinoCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA ?? "GYE",
                DestinoCiudad = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.Localidad ?? "Guayaquil",
                FechaOperacion = vuelo?.FechaOperacion ?? DateTime.Today.AddDays(2),
                HoraSalida = vuelo?.Frecuencia?.HoraSalida ?? new TimeSpan(7, 30, 0),
                HoraLlegada = vuelo?.Frecuencia?.HoraLlegada ?? new TimeSpan(8, 25, 0),
                AeronaveMatricula = vuelo?.Aeronave?.Matricula ?? "AV-01",
                ModeloAeronave = vuelo?.Aeronave?.ModeloAeronave?.Nombre ?? "Phenom 300E",
                Tarifa = "Classic",
                TarifaBase = baseTarifa,
                TasasAeroportuarias = 35.00m,
                Subtotal = baseTarifa,
                Iva = iva,
                TotalPagado = total,
                MarcaTarjeta = pago?.MarcaTarjetaCodigo ?? "VISA",
                Ultimos4 = pago?.UltimosCuatroDigitos ?? "9010",
                CodigoOperacion = pago?.CodigoOperacion ?? "AUT-582910",
                FechaPago = pago?.FechaPago ?? reserva.FechaCreacion,
                EsCancelable = !esCancelada && (vuelo?.FechaOperacion ?? DateTime.Today) >= DateTime.Today,
                PoliticaCancelacion = "CancelaciÃ³n con reembolso del 80% si se realiza al menos 24 horas antes de la salida del vuelo.",
                TieneCheckIn = tieneCheckIn
            };

            foreach (var a in reserva.ReservaPasajeroVueloes)
            {
                model.Pasajeros.Add(new PasajeroDetalleItemViewModel
                {
                    AsignacionId = a.AsignacionId,
                    PasajeroId = a.PasajeroId,
                    Nombres = a.Pasajero?.Nombres ?? "Pasajero",
                    Apellidos = a.Pasajero?.Apellidos ?? "VIP",
                    TipoDocumento = a.Pasajero?.TipoDocumentoCodigo ?? "CED",
                    NumeroDocumento = a.Pasajero?.NumeroDocumento ?? "1719283746",
                    AsientoCodigo = MapearNumeroAsientoACodigo(a.NumeroAsiento),
                    Correo = a.Pasajero?.Correo ?? "cliente@aerolineavip.com",
                    Telefono = a.Pasajero?.Telefono ?? "0991234567",
                    CategoriaEdad = "Adulto",
                    CheckInCompletado = tieneCheckIn || (a.CheckIns != null && a.CheckIns.Any(c => c.EstadoCheckInCodigo == "COMPLETADO"))
                });
            }

            return View(model);
        }

        // =========================================================================
        // ACCIÃ“N: CANCELAR RESERVA
        // =========================================================================
        [Authorize(Roles = "Cliente,SuperUsuario,Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancelar(string codigoReserva, string motivo)
        {
            if (string.IsNullOrWhiteSpace(codigoReserva))
            {
                return RedirectToAction("MisReservas");
            }

            string cod = codigoReserva.Trim().ToUpperInvariant();

            // 1. Si existe en la BD
            var reservaDb = db.Reservas.Include(r => r.ReservaPasajeroVueloes).FirstOrDefault(r => r.CodigoReserva == cod);
            if (reservaDb != null)
            {
                reservaDb.EstadoReservaCodigo = "CANCELADA";
                foreach (var a in reservaDb.ReservaPasajeroVueloes)
                {
                    a.EstadoAsignacionCodigo = "CANCELADA";
                }
                var cods = reservaDb.ReservaPasajeroVueloes.Select(a => VuelosController.MapearNumeroAsientoACodigo(a.NumeroAsiento)).ToList();
                VuelosController.LiberarAsientos(reservaDb.ReservaPasajeroVueloes.FirstOrDefault()?.VueloId ?? 1, cods);
                db.SaveChanges();
            }

            // 2. Si existe en sesiÃ³n
            var enSesion = Session["MisReservasNuevas"] as List<ReservaResumenItemViewModel>;
            if (enSesion != null)
            {
                var target = enSesion.FirstOrDefault(r => r.CodigoReserva.Equals(cod, StringComparison.OrdinalIgnoreCase));
                if (target != null)
                {
                    target.EstadoReservaCodigo = "CANCELADA";
                    target.EsCancelable = false;
                    target.PuedeHacerCheckIn = false;
                    if (!string.IsNullOrWhiteSpace(target.AsientosCodigos))
                    {
                        var cods = target.AsientosCodigos.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        VuelosController.LiberarAsientos(target.VueloId, cods);
                    }
                }
            }

            var dictDetalles = Session["MisReservasDetalles"] as Dictionary<string, GestionarReservaViewModel>;
            if (dictDetalles != null && dictDetalles.ContainsKey(cod))
            {
                dictDetalles[cod].EstadoReservaCodigo = "CANCELADA";
                dictDetalles[cod].EsCancelable = false;
            }

            TempData["MensajeExito"] = $"La reserva {cod} ha sido cancelada correctamente conforme a las polÃ­ticas tarifarias.";
            return RedirectToAction("MisReservas", new { tab = "canceladas" });
        }

        // =========================================================================
        // PANTALLA 16: COMPROBANTE FISCAL SRI / FACTURA ELECTRÃ“NICA
        // =========================================================================
        [Authorize(Roles = "Cliente,SuperUsuario,Administrador")]
        [HttpGet]
        public ActionResult Comprobante(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("MisReservas");
            }

            string cod = id.Trim().ToUpperInvariant();

            // Buscar en sesiÃ³n o BD
            var dictDetalles = Session["MisReservasDetalles"] as Dictionary<string, GestionarReservaViewModel>;
            GestionarReservaViewModel reserva = null;
            if (dictDetalles != null && dictDetalles.ContainsKey(cod))
            {
                reserva = dictDetalles[cod];
            }

            Reserva reservaDb = null;
            if (reserva == null)
            {
                reservaDb = db.Reservas
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.CheckIns))
                    .Include(r => r.Compras.Select(c => c.Pagoes))
                    .FirstOrDefault(r => r.CodigoReserva == cod);
            }

            if (reserva == null && reservaDb == null)
            {
                TempData["MensajeError"] = "No se encontrÃ³ el comprobante para la reserva " + id;
                return RedirectToAction("MisReservas");
            }

            var comp = new ComprobanteFiscalViewModel();
            DateTime fechaEmision = reserva?.FechaCreacion ?? reservaDb.FechaCreacion;
            comp.FechaEmision = fechaEmision;
            comp.CodigoReserva = cod;
            comp.NumeroComprobante = "001-002-0000" + Math.Abs(cod.GetHashCode() % 90000 + 10000);
            comp.ClaveAccesoSRI = fechaEmision.ToString("ddMMyyyy") + "01179284719200120010020000" + Math.Abs(cod.GetHashCode() % 90000 + 10000) + "123456781";
            comp.NumeroAutorizacion = comp.ClaveAccesoSRI;

            if (reserva != null)
            {
                var primerPasajero = reserva.Pasajeros.FirstOrDefault();
                comp.ClienteNombre = primerPasajero != null ? (primerPasajero.Nombres + " " + primerPasajero.Apellidos).Trim() : "CLIENTE VIP";
                comp.ClienteIdentificacion = primerPasajero?.NumeroDocumento ?? "1719283746";
                comp.ClienteCorreo = primerPasajero?.Correo ?? "cliente@aerolineavip.com";
                comp.ClienteTelefono = primerPasajero?.Telefono ?? "0991234567";

                comp.CodigoVuelo = reserva.CodigoVuelo;
                comp.Ruta = $"{reserva.OrigenCiudad} ({reserva.OrigenCodigo}) - {reserva.DestinoCiudad} ({reserva.DestinoCodigo})";
                comp.FechaVuelo = reserva.FechaOperacion;
                comp.HoraSalida = reserva.HoraSalida.ToString(@"hh\:mm");
                comp.Asientos = string.Join(", ", reserva.Pasajeros.Select(p => p.AsientoCodigo));
                comp.PasajerosNombres = reserva.Pasajeros.Select(p => $"{p.Nombres} {p.Apellidos}").ToList();

                comp.SubtotalSinImpuestos = reserva.Subtotal;
                comp.Subtotal15 = reserva.Subtotal;
                comp.Subtotal0 = 0.00m;
                comp.TarifaIvaPorcentaje = 15.00m;
                comp.ValorIva = reserva.Iva;
                comp.ImporteTotal = reserva.TotalPagado;

                comp.MarcaTarjeta = reserva.MarcaTarjeta;
                comp.Ultimos4 = reserva.Ultimos4;
                comp.CodigoAutorizacionBancaria = reserva.CodigoOperacion;

                foreach (var p in reserva.Pasajeros)
                {
                    comp.Items.Add(new ComprobanteItemDetalleViewModel
                    {
                        Codigo = "TRN-" + reserva.CodigoVuelo,
                        Descripcion = $"Transporte AÃ©reo VIP ({reserva.Tarifa}) - {reserva.OrigenCodigo}/{reserva.DestinoCodigo} - Pasajero: {p.Nombres} {p.Apellidos} (Asiento {p.AsientoCodigo})",
                        Cantidad = 1,
                        PrecioUnitario = Math.Round(reserva.Subtotal / (reserva.Pasajeros.Count > 0 ? reserva.Pasajeros.Count : 1), 2),
                        Total = Math.Round(reserva.Subtotal / (reserva.Pasajeros.Count > 0 ? reserva.Pasajeros.Count : 1), 2)
                    });

                    comp.PasesAbordar.Add(new PaseAbordarDetalleViewModel
                    {
                        CodigoReserva = cod,
                        CodigoVuelo = reserva.CodigoVuelo,
                        PasajeroNombre = $"{p.Nombres} {p.Apellidos}",
                        PasajeroDocumento = p.NumeroDocumento,
                        Asiento = p.AsientoCodigo,
                        OrigenCodigo = reserva.OrigenCodigo,
                        OrigenCiudad = reserva.OrigenCiudad,
                        DestinoCodigo = reserva.DestinoCodigo,
                        DestinoCiudad = reserva.DestinoCiudad,
                        FechaVuelo = reserva.FechaOperacion,
                        HoraSalida = reserva.HoraSalida.ToString(@"hh\:mm"),
                        HoraAbordaje = reserva.HoraSalida.Subtract(TimeSpan.FromMinutes(30)).ToString(@"hh\:mm"),
                        AeronaveMatricula = reserva.AeronaveMatricula
                    });
                }
            }
            else
            {
                var primerAsig = reservaDb.ReservaPasajeroVueloes.FirstOrDefault();
                var pas = primerAsig?.Pasajero;
                var vuelo = primerAsig?.Vuelo;
                var compra = reservaDb.Compras.FirstOrDefault();
                var pago = compra?.Pagoes.FirstOrDefault();

                comp.ClienteNombre = pas != null ? (pas.Nombres + " " + pas.Apellidos).Trim() : "CLIENTE VIP";
                comp.ClienteIdentificacion = pas?.NumeroDocumento ?? "1719283746";
                comp.ClienteCorreo = pas?.Correo ?? "cliente@aerolineavip.com";
                comp.ClienteTelefono = pas?.Telefono ?? "0991234567";

                comp.CodigoVuelo = "VIP" + (vuelo?.VueloId ?? 1).ToString("D5");
                comp.Ruta = $"{vuelo?.Frecuencia?.Ruta?.Aeropuerto?.Localidad ?? "Quito"} ({vuelo?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA ?? "UIO"}) - {vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.Localidad ?? "Guayaquil"} ({vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA ?? "GYE"})";
                comp.FechaVuelo = vuelo?.FechaOperacion ?? DateTime.Today.AddDays(2);
                comp.HoraSalida = (vuelo?.Frecuencia?.HoraSalida ?? new TimeSpan(7, 30, 0)).ToString(@"hh\:mm");
                comp.Asientos = string.Join(", ", reservaDb.ReservaPasajeroVueloes.Select(a => MapearNumeroAsientoACodigo(a.NumeroAsiento)));
                comp.PasajerosNombres = reservaDb.ReservaPasajeroVueloes.Where(a => a.Pasajero != null).Select(a => $"{a.Pasajero.Nombres} {a.Pasajero.Apellidos}").ToList();

                decimal total = compra?.Importe ?? 557.75m;
                decimal subtotal = Math.Round(total / 1.15m, 2);
                decimal iva = total - subtotal;

                comp.SubtotalSinImpuestos = subtotal;
                comp.Subtotal15 = subtotal;
                comp.Subtotal0 = 0.00m;
                comp.TarifaIvaPorcentaje = 15.00m;
                comp.ValorIva = iva;
                comp.ImporteTotal = total;

                comp.MarcaTarjeta = pago?.MarcaTarjetaCodigo ?? "VISA";
                comp.Ultimos4 = pago?.UltimosCuatroDigitos ?? "9010";
                comp.CodigoAutorizacionBancaria = pago?.CodigoOperacion ?? "AUT-582910";

                foreach (var a in reservaDb.ReservaPasajeroVueloes)
                {
                    string seat = MapearNumeroAsientoACodigo(a.NumeroAsiento);
                    string nom = a.Pasajero != null ? $"{a.Pasajero.Nombres} {a.Pasajero.Apellidos}" : "Pasajero VIP";
                    comp.Items.Add(new ComprobanteItemDetalleViewModel
                    {
                        Codigo = "TRN-" + comp.CodigoVuelo,
                        Descripcion = $"Transporte AÃ©reo VIP (Classic) - {vuelo?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA}/{vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA} - Pasajero: {nom} (Asiento {seat})",
                        Cantidad = 1,
                        PrecioUnitario = a.ImporteTramo > 0 ? a.ImporteTramo : Math.Round(subtotal / (reservaDb.ReservaPasajeroVueloes.Count > 0 ? reservaDb.ReservaPasajeroVueloes.Count : 1), 2),
                        Total = a.ImporteTramo > 0 ? a.ImporteTramo : Math.Round(subtotal / (reservaDb.ReservaPasajeroVueloes.Count > 0 ? reservaDb.ReservaPasajeroVueloes.Count : 1), 2)
                    });

                    comp.PasesAbordar.Add(new PaseAbordarDetalleViewModel
                    {
                        CodigoReserva = cod,
                        CodigoVuelo = comp.CodigoVuelo,
                        PasajeroNombre = nom,
                        PasajeroDocumento = a.Pasajero?.NumeroDocumento ?? "1719283746",
                        Asiento = seat,
                        OrigenCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA ?? "UIO",
                        OrigenCiudad = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.Localidad ?? "Quito",
                        DestinoCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA ?? "GYE",
                        DestinoCiudad = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.Localidad ?? "Guayaquil",
                        FechaVuelo = vuelo?.FechaOperacion ?? DateTime.Today.AddDays(2),
                        HoraSalida = (vuelo?.Frecuencia?.HoraSalida ?? new TimeSpan(7, 30, 0)).ToString(@"hh\:mm"),
                        HoraAbordaje = (vuelo?.Frecuencia?.HoraSalida ?? new TimeSpan(7, 30, 0)).Subtract(TimeSpan.FromMinutes(30)).ToString(@"hh\:mm"),
                        AeronaveMatricula = vuelo?.Aeronave?.Matricula ?? "AV-01"
                    });
                }
            }

            return View(comp);
        }

        private static string MapearNumeroAsientoACodigo(byte numero)
        {
            switch (numero)
            {
                case 1: return "1D";
                case 2: return "2D";
                case 3: return "2I";
                case 4: return "3D";
                case 5: return "3I";
                case 6: return "4D";
                case 7: return "4I";
                default: return numero.ToString();
            }
        }
        // M├ëTODOS PRIVADOS DE AYUDA
        // =========================================================================
        private Vuelo ObtenerVueloConDetalle(int? vueloId)
        {
            var query = db.Vueloes
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Include(v => v.Aeronave.ModeloAeronave)
                .AsNoTracking()
                .Include(v => v.ReservaPasajeroVueloes.Select(rpv => rpv.CheckIns))
                .Include(v => v.ReservaPasajeroVueloes.Select(rpv => rpv.Reserva))
                .Include(v => v.ReservaPasajeroVueloes.Select(rpv => rpv.Pasajero));

            if (vueloId.HasValue && vueloId.Value > 0)
            {
                var v = query.FirstOrDefault(x => x.VueloId == vueloId.Value);
                if (v != null) return v;
            }

            return query.FirstOrDefault(x => x.EstadoVueloCodigo != "CANCELADO" && DbFunctions.TruncateTime(x.FechaOperacion) >= DateTime.Today)
                ?? query.FirstOrDefault(x => x.EstadoVueloCodigo != "CANCELADO");
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