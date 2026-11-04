using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models;
using Microsoft.AspNet.Identity;

namespace AerolineaVIP.Controllers
{
    [Authorize(Roles = "Cliente,Counter,SuperUsuario,Administrador")]
    public class ReservasController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        // =========================================================================
        // PANTALLA 8: EMBUDO PASO 1 — SELECCIÓN INTERACTIVA DE ASIENTOS
        // =========================================================================
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult SeleccionAsiento(int? vueloId, string tarifa = "Classic", int pasajeros = 1, decimal? tarifaMonto = null)
        {
            return SeleccionarAsientos(vueloId, tarifa, pasajeros, tarifaMonto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SeleccionAsiento(SeleccionAsientosViewModel model)
        {
            return SeleccionarAsientos(model);
        }

        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult SeleccionarAsientos(int? vueloId, string tarifa = "Classic", int pasajeros = 1, decimal? tarifaMonto = null)
        {
            VuelosController.LiberarReservasExpiradas(db);

            if (pasajeros < 1) pasajeros = 1;
            if (pasajeros > 7) pasajeros = 7;
            if (string.IsNullOrWhiteSpace(tarifa)) tarifa = "Classic";

            var vuelo = ObtenerVueloConDetalle(vueloId);
            if (vuelo == null)
            {
                return RedirectToAction("Buscar", "Vuelos");
            }

            decimal tarifaBase = tarifaMonto ?? (vuelo.Frecuencia?.Ruta?.TarifaReferencial ?? 450.00m);
            decimal precioUnitario = tarifaMonto.HasValue
                ? tarifaMonto.Value
                : (tarifa.Equals("Flex", StringComparison.OrdinalIgnoreCase)
                    ? Math.Round(tarifaBase * 1.35m, 2)
                    : tarifaBase);

            // Limpiar reserva en progreso previa si cambia de vuelo
            Session.Remove("ReservaActual_ReservaId");
            Session.Remove("ReservaActual_CodigoReserva");

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

            return View("SeleccionarAsientos", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SeleccionarAsientos(SeleccionAsientosViewModel model)
        {
            VuelosController.LiberarReservasExpiradas(db);

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

                // Validar en servidor que ninguno de los asientos seleccionados esté reservado (Morado - PENDIENTE) ni ocupado (Rojo - CONFIRMADA)
                var asientosNoDisponibles = asignaciones
                    .Where(a => a.EstadoAsignacionCodigo == "CONFIRMADA" || a.EstadoAsignacionCodigo == "PENDIENTE")
                    .Select(a => VuelosController.MapearNumeroAsientoACodigo(a.NumeroAsiento))
                    .ToList();

                var enConflicto = asientos.Where(a => asientosNoDisponibles.Contains(a, StringComparer.OrdinalIgnoreCase)).ToList();
                if (enConflicto.Any())
                {
                    ModelState.AddModelError("AsientosSeleccionados", $"El o los asientos {string.Join(", ", enConflicto)} no están disponibles (reservados u ocupados). Por favor seleccione únicamente asientos en verde.");
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
                return View("SeleccionarAsientos", model);
            }

            Session["ReservaActual_PrecioUnitario"] = model.PrecioUnitario;

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
            decimal? precioSesion = Session["ReservaActual_PrecioUnitario"] as decimal?;
            decimal precioUnitario = precioSesion ?? (tarifa.Equals("Flex", StringComparison.OrdinalIgnoreCase)
                ? Math.Round(tarifaBase * 1.35m, 2)
                : tarifaBase);

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
                    TipoDocumento = "CEDULA",
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

            var documentosEnReserva = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Normalización de entradas y validación rigurosa en el contexto ecuatoriano
            for (int i = 0; i < model.Pasajeros.Count; i++)
            {
                var p = model.Pasajeros[i];

                // 1. Normalización integral de entradas
                p.Nombres = ValidadorEcuador.NormalizarNombrePropio(p.Nombres);
                p.Apellidos = ValidadorEcuador.NormalizarNombrePropio(p.Apellidos);
                p.NumeroDocumento = ValidadorEcuador.NormalizarDocumento(p.NumeroDocumento);
                p.Telefono = ValidadorEcuador.NormalizarTelefono(p.Telefono);
                p.Email = ValidadorEcuador.NormalizarEmail(p.Email);
                p.RequerimientoEspecial = ValidadorEcuador.NormalizarEspacios(p.RequerimientoEspecial);

                // Homologar código de documento hacia valores de catálogo SQL (CEDULA / PASAPORTE)
                string tipoDocUpper = (p.TipoDocumento ?? "CEDULA").Trim().ToUpperInvariant();
                if (tipoDocUpper == "CED") p.TipoDocumento = "CEDULA";
                else if (tipoDocUpper == "PAS") p.TipoDocumento = "PASAPORTE";

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
                if (p.TipoDocumento == "CEDULA" || p.TipoDocumento == "CED")
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
                else if (p.TipoDocumento == "PASAPORTE" || p.TipoDocumento == "PAS")
                {
                    if (!ValidadorEcuador.ValidarPasaporte(p.NumeroDocumento))
                    {
                        ModelState.AddModelError($"Pasajeros[{i}].NumeroDocumento", $"El pasaporte del Pasajero {i + 1} debe ser alfanumérico entre 6 y 15 caracteres sin espacios.");
                    }
                }

                // Validar que no se duplique el documento dentro de la misma solicitud (RG-10)
                if (!string.IsNullOrWhiteSpace(p.NumeroDocumento))
                {
                    if (!documentosEnReserva.Add(p.NumeroDocumento))
                    {
                        ModelState.AddModelError($"Pasajeros[{i}].NumeroDocumento", $"El documento {p.NumeroDocumento} está duplicado en esta reserva. Un pasajero no puede ocupar dos asientos en el mismo vuelo (RG-10).");
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

                // 7. Validación de Correo Electrónico
                if (!ValidadorEcuador.ValidarEmail(p.Email))
                {
                    ModelState.AddModelError($"Pasajeros[{i}].Email", $"Ingrese un correo electrónico válido para el Pasajero {i + 1} (sin espacios ni caracteres prohibidos).");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Limpiar reserva previa si se modificaron pasajeros
            Session.Remove("ReservaActual_ReservaId");
            Session.Remove("ReservaActual_CodigoReserva");

            // Guardar temporalmente en sesión para el siguiente paso del embudo
            Session["ReservaActual_Pasajeros"] = model.Pasajeros;
            Session["ReservaActual_VueloId"] = model.VueloId;
            Session["ReservaActual_Tarifa"] = model.TarifaSeleccionada;
            Session["ReservaActual_Asientos"] = model.AsientosCodigos;
            Session["ReservaActual_PrecioUnitario"] = model.PrecioUnitario;

            return RedirectToAction("Confirmar", new
            {
                vueloId = model.VueloId,
                tarifa = model.TarifaSeleccionada
            });
        }

        // =========================================================================
        // PANTALLA 10: EMBUDO PASO 3 — RESUMEN Y CREACIÓN DE RESERVA PENDIENTE (DP-07)
        // =========================================================================
        [HttpGet]
        public ActionResult Confirmar(int? vueloId, string tarifa = "Classic")
        {
            VuelosController.LiberarReservasExpiradas(db);

            int id = vueloId ?? (Session["ReservaActual_VueloId"] as int? ?? 1);
            var vuelo = ObtenerVueloConDetalle(id);
            if (vuelo == null)
            {
                return RedirectToAction("Buscar", "Vuelos");
            }

            var pasajeros = Session["ReservaActual_Pasajeros"] as List<PasajeroItemViewModel>;
            if (pasajeros == null || !pasajeros.Any())
            {
                TempData["MensajeError"] = "Debe registrar los datos de los pasajeros antes de confirmar la reserva.";
                return RedirectToAction("Pasajeros", new { vueloId = id, asientos = (Session["ReservaActual_Asientos"] as string ?? "1D"), tarifa = tarifa });
            }

            decimal tarifaBaseRuta = vuelo.Frecuencia?.Ruta?.TarifaReferencial ?? 450.00m;
            decimal? precioSesion = Session["ReservaActual_PrecioUnitario"] as decimal?;
            decimal precioUnitario = precioSesion ?? (tarifa.Equals("Flex", StringComparison.OrdinalIgnoreCase)
                ? Math.Round(tarifaBaseRuta * 1.35m, 2)
                : tarifaBaseRuta);

            int cantidadPax = pasajeros.Count;
            decimal baseTotal = precioUnitario * cantidadPax;
            decimal tasas = 35.00m * cantidadPax; // Tasas de aeropuerto
            decimal subtotalNeto = baseTotal + tasas;
            decimal iva = Math.Round(subtotalNeto * 0.15m, 2); // IVA 15% Ecuador
            decimal total = subtotalNeto + iva;

            // Persistir o recuperar la reserva en estado PENDIENTE con retención de 20 minutos (DP-07 / RG-11)
            int? reservaIdExistente = Session["ReservaActual_ReservaId"] as int?;
            Reserva reservaDb = null;

            if (reservaIdExistente.HasValue)
            {
                reservaDb = db.Reservas
                    .Include(r => r.ReservaPasajeroVueloes)
                    .Include(r => r.Compras)
                    .FirstOrDefault(r => r.ReservaId == reservaIdExistente.Value && r.EstadoReservaCodigo == "PENDIENTE");
            }

            if (reservaDb == null)
            {
                try
                {
                    string userId = ObtenerUsuarioActualId();
                    DateTime ahora = DateTime.Now;
                    DateTime expiracion = ahora.AddMinutes(20); // DP-07: 20 minutos de retención

                    reservaDb = new Reserva
                    {
                        UsuarioId = userId,
                        FechaCreacion = ahora,
                        FechaExpiracion = expiracion,
                        EstadoReservaCodigo = "PENDIENTE",
                        Observaciones = $"Reserva creada en Paso 3 ({tarifa}). Retención de asientos por 20 minutos (DP-07)."
                    };
                    db.Reservas.Add(reservaDb);

                    decimal importePorPasajero = Math.Round(total / cantidadPax, 2);

                    foreach (var pModel in pasajeros)
                    {
                        string tipoDocSql = MapearTipoDocumentoSql(pModel.TipoDocumento);
                        string numDocSql = (pModel.NumeroDocumento ?? "1710034065").Trim();

                        var pasExistente = db.Pasajeroes.FirstOrDefault(p =>
                            p.TipoDocumentoCodigo == tipoDocSql &&
                            p.NumeroDocumento == numDocSql);

                        DateTime fechaNacValida = (pModel.FechaNacimiento.HasValue && pModel.FechaNacimiento.Value >= new DateTime(1900, 1, 1) && pModel.FechaNacimiento.Value <= DateTime.Today)
                            ? pModel.FechaNacimiento.Value.Date
                            : new DateTime(1990, 1, 1);

                        if (pasExistente == null)
                        {
                            pasExistente = new Pasajero
                            {
                                TipoDocumentoCodigo = tipoDocSql,
                                NumeroDocumento = numDocSql,
                                Nombres = string.IsNullOrWhiteSpace(pModel.Nombres) ? "Pasajero" : pModel.Nombres,
                                Apellidos = string.IsNullOrWhiteSpace(pModel.Apellidos) ? "VIP" : pModel.Apellidos,
                                FechaNacimiento = fechaNacValida,
                                Correo = string.IsNullOrWhiteSpace(pModel.Email) ? "cliente@aerolineavip.com" : pModel.Email,
                                Telefono = string.IsNullOrWhiteSpace(pModel.Telefono) ? "0991234567" : pModel.Telefono,
                                FechaRegistro = ahora
                            };
                            db.Pasajeroes.Add(pasExistente);
                        }
                        else
                        {
                            pasExistente.Nombres = string.IsNullOrWhiteSpace(pModel.Nombres) ? pasExistente.Nombres : pModel.Nombres;
                            pasExistente.Apellidos = string.IsNullOrWhiteSpace(pModel.Apellidos) ? pasExistente.Apellidos : pModel.Apellidos;
                            pasExistente.Correo = string.IsNullOrWhiteSpace(pModel.Email) ? pasExistente.Correo : pModel.Email;
                            pasExistente.Telefono = string.IsNullOrWhiteSpace(pModel.Telefono) ? pasExistente.Telefono : pModel.Telefono;
                        }

                        byte numAsiento = VuelosController.MapearCodigoANumeroAsiento(pModel.AsientoAsignado);

                        // Verificar que el asiento no esté ocupado en BD antes de insertar
                        bool ocupadoEnBd = db.ReservaPasajeroVueloes.Any(a =>
                            a.VueloId == vuelo.VueloId &&
                            a.NumeroAsiento == numAsiento &&
                            a.EstadoAsignacionCodigo != "CANCELADA" &&
                            a.EstadoAsignacionCodigo != "LIBERADA");

                        if (!ocupadoEnBd)
                        {
                            db.ReservaPasajeroVueloes.Add(new ReservaPasajeroVuelo
                            {
                                Reserva = reservaDb,
                                Pasajero = pasExistente,
                                VueloId = vuelo.VueloId,
                                NumeroAsiento = numAsiento,
                                ImporteTramo = importePorPasajero,
                                EstadoAsignacionCodigo = "PENDIENTE",
                                FechaAsignacion = ahora
                            });
                        }
                    }

                    var compraDb = new Compra
                    {
                        Reserva = reservaDb,
                        FechaCompra = ahora,
                        Importe = total,
                        MonedaCodigo = "USD",
                        EstadoCompraCodigo = "PENDIENTE",
                        ClaveIdempotencia = Guid.NewGuid(),
                        Observaciones = $"Orden de compra pendiente de pago ({tarifa})"
                    };
                    db.Compras.Add(compraDb);

                    db.SaveChanges();
                    db.Entry(reservaDb).Reload();

                    Session["ReservaActual_ReservaId"] = reservaDb.ReservaId;
                    Session["ReservaActual_CodigoReserva"] = reservaDb.CodigoReserva;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Aviso al crear pre-reserva PENDIENTE: " + ex.Message);
                }
            }

            string codigoOficial = reservaDb?.CodigoReserva ?? ("RV" + new Random().Next(100, 999).ToString("D6"));
            DateTime? fechaExp = reservaDb?.FechaExpiracion ?? DateTime.Now.AddMinutes(20);

            var model = new ResumenReservaViewModel
            {
                ReservaId = reservaDb?.ReservaId ?? 0,
                CodigoPreReserva = codigoOficial,
                FechaExpiracion = fechaExp,
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
                MinutosRetencion = 20
            };

            Session["ReservaActual_Resumen"] = model;

            return View(model);
        }

        // =========================================================================
        // PANTALLA 11: EMBUDO PASO 4 — PAGO SIMULADO Y MODAL (DP-06)
        // =========================================================================
        [HttpGet]
        public ActionResult Pagar(string codigoPreReserva = null, int? vueloId = null, string tarifa = "Classic", int? reservaId = null)
        {
            VuelosController.LiberarReservasExpiradas(db);

            var resumen = Session["ReservaActual_Resumen"] as ResumenReservaViewModel;
            Reserva reservaDb = null;

            if (reservaId.HasValue && reservaId.Value > 0)
            {
                reservaDb = db.Reservas
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                    .Include(r => r.Compras)
                    .FirstOrDefault(r => r.ReservaId == reservaId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(codigoPreReserva))
            {
                string codUpper = codigoPreReserva.Trim().ToUpperInvariant();
                reservaDb = db.Reservas
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                    .Include(r => r.Compras)
                    .FirstOrDefault(r => r.CodigoReserva == codUpper);
            }

            if (reservaDb != null)
            {
                var primerAsig = reservaDb.ReservaPasajeroVueloes.FirstOrDefault();
                var vueloDb = primerAsig?.Vuelo;
                var compraDb = reservaDb.Compras.FirstOrDefault();
                var primerPax = primerAsig?.Pasajero;

                var modelDb = new PagoSimuladoViewModel
                {
                    ReservaId = reservaDb.ReservaId,
                    CodigoPreReserva = reservaDb.CodigoReserva,
                    VueloId = vueloDb?.VueloId ?? (vueloId ?? 1),
                    CodigoVuelo = "VIP" + (vueloDb?.VueloId ?? (vueloId ?? 1)).ToString("D5"),
                    FechaOperacion = vueloDb?.FechaOperacion ?? DateTime.Today.AddDays(2),
                    OrigenCodigo = vueloDb?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA ?? "UIO",
                    DestinoCodigo = vueloDb?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA ?? "GYE",
                    TotalPagar = compraDb?.Importe ?? (resumen?.TotalPagar ?? 557.75m),
                    TarifaSeleccionada = tarifa,
                    CantidadPasajeros = reservaDb.ReservaPasajeroVueloes.Count,
                    AsientosCodigos = string.Join(", ", reservaDb.ReservaPasajeroVueloes.Select(a => VuelosController.MapearNumeroAsientoACodigo(a.NumeroAsiento))),
                    MetodoPago = "TARJETA_CREDITO",
                    MarcaTarjeta = "VISA",
                    TitularTarjeta = primerPax != null ? $"{primerPax.Nombres} {primerPax.Apellidos}".ToUpperInvariant() : "CARLOS ROBERTO MENDOZA",
                    NumeroTarjeta = "",
                    MesExpiracion = "",
                    AnioExpiracion = "",
                    Cvv = "",
                    AceptoTerminos = false
                };

                return View(modelDb);
            }

            int id = vueloId ?? (resumen?.VueloId ?? 1);
            var vuelo = ObtenerVueloConDetalle(id);
            decimal total = resumen?.TotalPagar ?? 557.75m;

            var model = new PagoSimuladoViewModel
            {
                ReservaId = resumen?.ReservaId ?? 0,
                CodigoPreReserva = !string.IsNullOrWhiteSpace(codigoPreReserva) ? codigoPreReserva : (resumen?.CodigoPreReserva ?? "RV000013"),
                VueloId = id,
                CodigoVuelo = vuelo != null ? "VIP" + vuelo.VueloId.ToString("D5") : "VIP00001",
                FechaOperacion = vuelo?.FechaOperacion ?? DateTime.Today.AddDays(2),
                OrigenCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA ?? "UIO",
                DestinoCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA ?? "GYE",
                TotalPagar = total,
                TarifaSeleccionada = tarifa,
                CantidadPasajeros = resumen?.CantidadPasajeros ?? 1,
                AsientosCodigos = resumen?.AsientosCodigos ?? "1D",
                MetodoPago = "TARJETA_CREDITO",
                MarcaTarjeta = "VISA",
                TitularTarjeta = "",
                    NumeroTarjeta = "",
                    MesExpiracion = "",
                    AnioExpiracion = "",
                    Cvv = "",
                    AceptoTerminos = false
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcesarPago(PagoSimuladoViewModel model)
        {
            return EjecutarFlujoPagoSimulado(model, redirigirACompras: false);
        }

        internal ActionResult EjecutarFlujoPagoSimulado(PagoSimuladoViewModel model, bool redirigirACompras)
        {
            VuelosController.LiberarReservasExpiradas(db);

            // 1. Normalización de entradas de pago
            model.TitularTarjeta = ValidadorEcuador.NormalizarEspacios(model.TitularTarjeta).ToUpperInvariant();
            string numTarjetaSoloDigitos = System.Text.RegularExpressions.Regex.Replace(model.NumeroTarjeta ?? "", @"\D", "");
            model.Cvv = (model.Cvv ?? "").Trim();
            if (string.IsNullOrWhiteSpace(model.MetodoPago)) model.MetodoPago = "TARJETA_CREDITO";
            if (string.IsNullOrWhiteSpace(model.MarcaTarjeta)) model.MarcaTarjeta = "VISA";

            // 2. Validación del Titular (mínimo 2 palabras, solo letras)
            if (!ValidadorEcuador.ValidarTitularTarjeta(model.TitularTarjeta))
            {
                ModelState.AddModelError("TitularTarjeta", "Ingrese el nombre y apellido completos como figura en la tarjeta (solo letras y mínimo dos palabras).");
            }

            // 3. Validación de la Tarjeta según dígitos requeridos
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
                return View(redirigirACompras ? "~/Views/Compras/Pagar.cshtml" : "Pagar", model);
            }

            string ultimos4 = numTarjetaSoloDigitos.Length >= 4
                ? numTarjetaSoloDigitos.Substring(numTarjetaSoloDigitos.Length - 4)
                : "9010";

            bool esRechazoSimulado = ultimos4 == "0000"; // Política DP-06: tarjeta terminada en 0000 => RECHAZADO

            // Localizar la reserva existente en BD (o crearla si se ingresó directo sin pasar por Confirmar)
            Reserva reservaDb = null;
            if (model.ReservaId > 0)
            {
                reservaDb = db.Reservas
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                    .Include(r => r.Compras.Select(c => c.Pagoes))
                    .Include(r => r.Compras.Select(c => c.Comprobantes))
                    .FirstOrDefault(r => r.ReservaId == model.ReservaId);
            }
            if (reservaDb == null && !string.IsNullOrWhiteSpace(model.CodigoPreReserva))
            {
                string codLookup = model.CodigoPreReserva.Trim().ToUpperInvariant();
                reservaDb = db.Reservas
                    .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                    .Include(r => r.Compras.Select(c => c.Pagoes))
                    .Include(r => r.Compras.Select(c => c.Comprobantes))
                    .FirstOrDefault(r => r.CodigoReserva == codLookup);
            }

            var resumen = Session["ReservaActual_Resumen"] as ResumenReservaViewModel;

            // Si el pago es RECHAZADO (termina en 0000 - DP-06)
            if (esRechazoSimulado)
            {
                try
                {
                    if (reservaDb != null)
                    {
                        var compraPend = reservaDb.Compras.FirstOrDefault();
                        if (compraPend != null)
                        {
                            int intentoRechazo = compraPend.Pagoes.Count + 1;
                            string codOpRechazo = $"SIM-{reservaDb.CodigoReserva}-{intentoRechazo:D2}";

                            db.Pagoes.Add(new Pago
                            {
                                CompraId = compraPend.CompraId,
                                FechaPago = DateTime.Now,
                                Importe = compraPend.Importe,
                                EstadoPagoCodigo = "RECHAZADO",
                                MetodoPagoCodigo = model.MetodoPago,
                                MarcaTarjetaCodigo = model.MarcaTarjeta,
                                UltimosCuatroDigitos = "0000",
                                NombreTitular = model.TitularTarjeta,
                                CodigoOperacion = codOpRechazo,
                                EsSimulado = true,
                                MensajeResultado = "Pago rechazado por el emisor simulado (tarjeta terminada en 0000 - DP-06)."
                            });
                            db.SaveChanges();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al registrar intento rechazado: " + ex.Message);
                }

                model.MensajeRechazo = $"Transacción RECHAZADA por el banco emisor simulado (Política DP-06: numeración terminada en 0000). Su reserva {model.CodigoPreReserva} se mantiene en estado PENDIENTE. Por favor intente con otra tarjeta.";
                ModelState.AddModelError("", model.MensajeRechazo);
                return View(redirigirACompras ? "~/Views/Compras/Pagar.cshtml" : "Pagar", model);
            }

            // PAGO APROBADO (DP-06: numeración válida distinta de 0000)
            string codigoReserva = model.CodigoPreReserva;
            string codigoOperacion = "SIM-RV000013-01";
            string numCompConfirmacion = "";
            string numCompFactura = "";

            try
            {
                DateTime ahora = DateTime.Now;
                string userId = ObtenerUsuarioActualId();

                if (reservaDb == null)
                {
                    reservaDb = new Reserva
                    {
                        UsuarioId = userId,
                        FechaCreacion = ahora,
                        FechaExpiracion = null,
                        EstadoReservaCodigo = "CONFIRMADA",
                        Observaciones = "Reserva confirmada con pago aprobado (DP-06)."
                    };
                    db.Reservas.Add(reservaDb);

                    var listaPax = resumen?.Pasajeros ?? new List<PasajeroItemViewModel>
                    {
                        new PasajeroItemViewModel
                        {
                            AsientoAsignado = "1D",
                            Nombres = "Carlos Roberto",
                            Apellidos = "Mendoza Loor",
                            TipoDocumento = "CEDULA",
                            NumeroDocumento = "1710034065",
                            FechaNacimiento = new DateTime(1988, 5, 14),
                            Email = "carlos.mendoza@aerolineavip.com",
                            Telefono = "0991234567"
                        }
                    };

                    decimal importeUnit = Math.Round((resumen?.TotalPagar ?? model.TotalPagar) / Math.Max(1, listaPax.Count), 2);
                    foreach (var pModel in listaPax)
                    {
                        string tipoDocSql = MapearTipoDocumentoSql(pModel.TipoDocumento);
                        string numDocSql = (pModel.NumeroDocumento ?? "1710034065").Trim();
                        var pasExistente = db.Pasajeroes.FirstOrDefault(p =>
                            p.TipoDocumentoCodigo == tipoDocSql &&
                            p.NumeroDocumento == numDocSql);

                        DateTime fechaNacValida = (pModel.FechaNacimiento.HasValue && pModel.FechaNacimiento.Value >= new DateTime(1900, 1, 1) && pModel.FechaNacimiento.Value <= DateTime.Today)
                            ? pModel.FechaNacimiento.Value.Date
                            : new DateTime(1990, 1, 1);

                        if (pasExistente == null)
                        {
                            pasExistente = new Pasajero
                            {
                                TipoDocumentoCodigo = tipoDocSql,
                                NumeroDocumento = numDocSql,
                                Nombres = string.IsNullOrWhiteSpace(pModel.Nombres) ? "Pasajero" : pModel.Nombres,
                                Apellidos = string.IsNullOrWhiteSpace(pModel.Apellidos) ? "VIP" : pModel.Apellidos,
                                FechaNacimiento = fechaNacValida,
                                Correo = string.IsNullOrWhiteSpace(pModel.Email) ? "cliente@aerolineavip.com" : pModel.Email,
                                Telefono = string.IsNullOrWhiteSpace(pModel.Telefono) ? "0991234567" : pModel.Telefono,
                                FechaRegistro = ahora
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
                            ImporteTramo = importeUnit,
                            EstadoAsignacionCodigo = "CONFIRMADA",
                            FechaAsignacion = ahora
                        });
                    }

                    var nuevaCompra = new Compra
                    {
                        Reserva = reservaDb,
                        FechaCompra = ahora,
                        Importe = resumen?.TotalPagar ?? model.TotalPagar,
                        MonedaCodigo = "USD",
                        EstadoCompraCodigo = "PAGADA",
                        ClaveIdempotencia = Guid.NewGuid(),
                        Observaciones = "Compra pagada exitosamente."
                    };
                    db.Compras.Add(nuevaCompra);
                    db.SaveChanges();
                    db.Entry(reservaDb).Reload();
                }
                else
                {
                    // Transicionar la reserva PENDIENTE existente a CONFIRMADA (DP-06 / RG-14)
                    reservaDb.EstadoReservaCodigo = "CONFIRMADA";
                    reservaDb.FechaExpiracion = null;
                    reservaDb.FechaActualizacion = ahora;

                    foreach (var rpv in reservaDb.ReservaPasajeroVueloes.Where(a => a.EstadoAsignacionCodigo != "CANCELADA"))
                    {
                        rpv.EstadoAsignacionCodigo = "CONFIRMADA";
                    }
                }

                var compraDb = reservaDb.Compras.FirstOrDefault();
                if (compraDb == null)
                {
                    compraDb = new Compra
                    {
                        ReservaId = reservaDb.ReservaId,
                        FechaCompra = ahora,
                        Importe = model.TotalPagar,
                        MonedaCodigo = "USD",
                        EstadoCompraCodigo = "PAGADA",
                        ClaveIdempotencia = Guid.NewGuid()
                    };
                    db.Compras.Add(compraDb);
                    db.SaveChanges();
                }
                else
                {
                    compraDb.EstadoCompraCodigo = "PAGADA";
                }

                codigoReserva = reservaDb.CodigoReserva;
                int intento = (compraDb.Pagoes?.Count ?? 0) + 1;
                codigoOperacion = $"SIM-{codigoReserva}-{intento:D2}";

                // Verificar que no tenga ya un pago APROBADO para no violar trg_Pago_Validar
                bool yaTienePagoAprobado = compraDb.Pagoes != null && compraDb.Pagoes.Any(p => p.EstadoPagoCodigo == "APROBADO");
                if (!yaTienePagoAprobado)
                {
                    db.Pagoes.Add(new Pago
                    {
                        CompraId = compraDb.CompraId,
                        FechaPago = ahora,
                        Importe = compraDb.Importe,
                        EstadoPagoCodigo = "APROBADO", // Catálogo oficial dbo.EstadoPago
                        MetodoPagoCodigo = model.MetodoPago,
                        MarcaTarjetaCodigo = model.MarcaTarjeta,
                        UltimosCuatroDigitos = ultimos4,
                        NombreTitular = model.TitularTarjeta,
                        CodigoOperacion = codigoOperacion,
                        EsSimulado = true,
                        MensajeResultado = "Pago simulado aprobado exitosamente (DP-06)."
                    });
                }

                // Emitir comprobantes oficiales en dbo.Comprobante (DP-05: CONFIRMACION + FACTURA)
                var primerPasajeroDb = reservaDb.ReservaPasajeroVueloes.Select(a => a.Pasajero).FirstOrDefault();
                string receptorNom = primerPasajeroDb != null ? $"{primerPasajeroDb.Nombres} {primerPasajeroDb.Apellidos}".Trim() : model.TitularTarjeta;
                string receptorDoc = primerPasajeroDb?.NumeroDocumento ?? "1710034065";
                string receptorMail = primerPasajeroDb?.Correo ?? "cliente@aerolineavip.com";

                numCompConfirmacion = "CNF-" + compraDb.CompraId.ToString("D6");
                numCompFactura = "ACAD-" + compraDb.CompraId.ToString("D6");

                if (!db.Comprobantes.Any(c => c.NumeroComprobante == numCompConfirmacion))
                {
                    db.Comprobantes.Add(new Comprobante
                    {
                        CompraId = compraDb.CompraId,
                        TipoComprobanteCodigo = "CONFIRMACION",
                        EstadoComprobanteCodigo = "EMITIDO",
                        NumeroComprobante = numCompConfirmacion,
                        FechaEmision = ahora,
                        ReceptorNombre = receptorNom,
                        ReceptorDocumento = receptorDoc,
                        ReceptorCorreo = receptorMail,
                        MontoTotal = compraDb.Importe,
                        Observaciones = "Confirmación de compra electrónica emitida automáticamente (DP-05)."
                    });
                }

                if (!db.Comprobantes.Any(c => c.NumeroComprobante == numCompFactura))
                {
                    db.Comprobantes.Add(new Comprobante
                    {
                        CompraId = compraDb.CompraId,
                        TipoComprobanteCodigo = "FACTURA",
                        EstadoComprobanteCodigo = "EMITIDO",
                        NumeroComprobante = numCompFactura,
                        FechaEmision = ahora,
                        ReceptorNombre = receptorNom,
                        ReceptorDocumento = receptorDoc,
                        ReceptorCorreo = receptorMail,
                        MontoTotal = compraDb.Importe,
                        Observaciones = "Comprobante académico de venta emitido automáticamente sin validez tributaria (DP-05)."
                    });
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al persistir pago en BD: " + ex.Message);
            }

            // Registrar asientos ocupados y limpiar pre-reserva temporal en sesión
            var codigosAsientosList = (model.AsientosCodigos ?? "1D")
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToUpperInvariant())
                .ToList();

            VuelosController.RegistrarAsientosOcupados(model.VueloId, codigosAsientosList);
            Session.Remove("ReservaActual_ReservaId");
            Session.Remove("ReservaActual_CodigoReserva");

            if (redirigirACompras)
            {
                return RedirectToAction("Confirmacion", "Compras", new
                {
                    codigoReserva = codigoReserva,
                    codigoOperacion = codigoOperacion,
                    vueloId = model.VueloId,
                    marca = model.MarcaTarjeta,
                    ultimos4 = ultimos4,
                    titular = model.TitularTarjeta,
                    metodoPago = model.MetodoPago
                });
            }

            return RedirectToAction("Exito", new
            {
                codigoReserva = codigoReserva,
                codigoOperacion = codigoOperacion,
                vueloId = model.VueloId,
                marca = model.MarcaTarjeta,
                ultimos4 = ultimos4,
                titular = model.TitularTarjeta,
                metodoPago = model.MetodoPago
            });
        }

        // =========================================================================
        // PANTALLA 12: CONFIRMACIÓN DE COMPRA EXITOSA Y PASES DE ABORDAR
        // =========================================================================
        [HttpGet]
        public ActionResult Exito(string codigoReserva = "RV000001", string codigoOperacion = "SIM-RV000001-01", int vueloId = 1, string marca = "VISA", string ultimos4 = "9010", string titular = "CARLOS ROBERTO MENDOZA", string metodoPago = "TARJETA_CREDITO")
        {
            var model = ConstruirConfirmacionCompraViewModel(codigoReserva, codigoOperacion, vueloId, marca, ultimos4, titular, metodoPago);
            return View(model);
        }

        internal ConfirmacionCompraViewModel ConstruirConfirmacionCompraViewModel(string codigoReserva, string codigoOperacion, int vueloId, string marca, string ultimos4, string titular, string metodoPago)
        {
            string codLookup = (codigoReserva ?? "RV000001").Trim().ToUpperInvariant();
            var reservaDb = db.Reservas
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Aeronave))
                .Include(r => r.Compras.Select(c => c.Pagoes))
                .Include(r => r.Compras.Select(c => c.Comprobantes))
                .FirstOrDefault(r => r.CodigoReserva == codLookup);

            var vuelo = reservaDb?.ReservaPasajeroVueloes.FirstOrDefault()?.Vuelo ?? ObtenerVueloConDetalle(vueloId);
            var compra = reservaDb?.Compras.FirstOrDefault();
            var pago = compra?.Pagoes.OrderByDescending(p => p.FechaPago).FirstOrDefault(p => p.EstadoPagoCodigo == "APROBADO");
            var resumen = Session["ReservaActual_Resumen"] as ResumenReservaViewModel;

            var compConfirmacion = compra?.Comprobantes.FirstOrDefault(c => c.TipoComprobanteCodigo == "CONFIRMACION");
            var compFactura = compra?.Comprobantes.FirstOrDefault(c => c.TipoComprobanteCodigo == "FACTURA");

            var model = new ConfirmacionCompraViewModel
            {
                ReservaId = reservaDb?.ReservaId ?? (Session["ReservaActual_ReservaId"] as int?),
                CodigoReserva = codLookup,
                CodigoOperacion = !string.IsNullOrWhiteSpace(codigoOperacion) ? codigoOperacion : (pago?.CodigoOperacion ?? ("SIM-" + codLookup + "-01")),
                NumeroComprobanteConfirmacion = compConfirmacion?.NumeroComprobante ?? ("CNF-" + (compra?.CompraId ?? (reservaDb?.ReservaId ?? 1)).ToString("D6")),
                NumeroComprobanteFactura = compFactura?.NumeroComprobante ?? ("ACAD-" + (compra?.CompraId ?? (reservaDb?.ReservaId ?? 1)).ToString("D6")),
                FechaCompra = compra?.FechaCompra ?? DateTime.Now,
                VueloId = vuelo?.VueloId ?? vueloId,
                CodigoVuelo = "VIP" + (vuelo?.VueloId ?? vueloId).ToString("D5"),
                FechaOperacion = vuelo?.FechaOperacion ?? DateTime.Today.AddDays(2),
                HoraSalida = vuelo?.Frecuencia?.HoraSalida ?? new TimeSpan(7, 30, 0),
                HoraLlegada = vuelo?.Frecuencia?.HoraLlegada ?? new TimeSpan(8, 20, 0),
                OrigenCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA ?? "UIO",
                OrigenCiudad = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.Localidad ?? "Quito",
                DestinoCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA ?? "GYE",
                DestinoCiudad = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.Localidad ?? "Guayaquil",
                AeronaveMatricula = vuelo?.Aeronave?.Matricula ?? "AV-01",
                TarifaSeleccionada = resumen?.TarifaSeleccionada ?? "Classic",
                TotalPagado = compra?.Importe ?? (resumen?.TotalPagar ?? 557.75m),
                MetodoPago = !string.IsNullOrWhiteSpace(metodoPago) ? metodoPago : (pago?.MetodoPagoCodigo ?? "TARJETA_CREDITO"),
                MarcaTarjeta = !string.IsNullOrWhiteSpace(marca) ? marca : (pago?.MarcaTarjetaCodigo ?? "VISA"),
                TarjetaEnmascarada = "**** **** **** " + (!string.IsNullOrWhiteSpace(ultimos4) ? ultimos4 : (pago?.UltimosCuatroDigitos ?? "9010")),
                TitularTarjeta = !string.IsNullOrWhiteSpace(titular) ? titular : (pago?.NombreTitular ?? "CLIENTE VIP"),
                Pasajeros = reservaDb != null
                    ? reservaDb.ReservaPasajeroVueloes.Select((a, idx) => new PasajeroItemViewModel
                    {
                        Indice = idx + 1,
                        Nombres = a.Pasajero?.Nombres ?? "Pasajero",
                        Apellidos = a.Pasajero?.Apellidos ?? "VIP",
                        TipoDocumento = a.Pasajero?.TipoDocumentoCodigo ?? "CEDULA",
                        NumeroDocumento = a.Pasajero?.NumeroDocumento ?? "1710034065",
                        AsientoAsignado = MapearNumeroAsientoACodigo(a.NumeroAsiento)
                    }).ToList()
                    : (resumen?.Pasajeros ?? new List<PasajeroItemViewModel>())
            };

            return model;
        }

        // =========================================================================
        // PANTALLA 13: MIS RESERVAS (PORTAL CLIENTE VIP)
        // =========================================================================
        [HttpGet]
        public ActionResult MisReservas(string tab = "todas")
        {
            VuelosController.LiberarReservasExpiradas(db);

            var model = new MisReservasViewModel
            {
                FiltroActual = string.IsNullOrWhiteSpace(tab) ? "todas" : tab.ToLowerInvariant()
            };

            var todas = new List<ReservaResumenItemViewModel>();
            var checkInsCompletados = Session["CodigosCheckInCompletados"] as HashSet<string>
                                     ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool esPersonalInterno = User.IsInRole("SuperUsuario") || User.IsInRole("Administrador") || User.IsInRole("Counter");
            string currentUserId = ObtenerUsuarioActualId();
            string currentUserName = User?.Identity?.Name ?? "";

            // Cargar reservas desde SQL Server
            var queryDb = db.Reservas
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Aeronave.ModeloAeronave))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.CheckIns))
                .Include(r => r.Compras.Select(c => c.Pagoes))
                .AsQueryable();

            if (!esPersonalInterno)
            {
                // Filtrar por el cliente autenticado (o el ID demo si inició sesión con cliente01)
                bool esClienteDemo = string.Equals(currentUserName, "cliente01", StringComparison.OrdinalIgnoreCase);
                string demoGuid = "7c1e4a20-0001-4b6e-9a01-000000000001";

                queryDb = queryDb.Where(r =>
                    r.UsuarioId == currentUserId ||
                    r.UsuarioId == currentUserName ||
                    (esClienteDemo && r.UsuarioId == demoGuid));
            }

            var reservasDb = queryDb
                .OrderByDescending(r => r.FechaCreacion)
                .ToList();

            foreach (var r in reservasDb)
            {
                var primeraAsignacion = r.ReservaPasajeroVueloes.FirstOrDefault();
                var vuelo = primeraAsignacion?.Vuelo;
                var compra = r.Compras.FirstOrDefault();

                bool tieneCheckIn = checkInsCompletados.Contains(r.CodigoReserva) ||
                                    r.ReservaPasajeroVueloes.Any(a => a.CheckIns != null && a.CheckIns.Any(c => c.EstadoCheckInCodigo == "COMPLETADO"));
                bool esCancelada = string.Equals(r.EstadoReservaCodigo, "CANCELADA", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(r.EstadoReservaCodigo, "EXPIRADA", StringComparison.OrdinalIgnoreCase);
                bool esPendiente = string.Equals(r.EstadoReservaCodigo, "PENDIENTE", StringComparison.OrdinalIgnoreCase);
                DateTime fechaVuelo = vuelo?.FechaOperacion ?? DateTime.Today;

                var item = new ReservaResumenItemViewModel
                {
                    ReservaId = r.ReservaId,
                    CodigoReserva = r.CodigoReserva,
                    UsuarioId = r.UsuarioId,
                    FechaCreacion = r.FechaCreacion,
                    FechaExpiracion = r.FechaExpiracion,
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
                    EstaPendientePago = esPendiente,
                    EsCancelable = !esCancelada && !tieneCheckIn && fechaVuelo >= DateTime.Today,
                    PuedeHacerCheckIn = !esCancelada && !esPendiente && !tieneCheckIn
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

            // Calcular contadores por pestaña
            model.TotalPendientes = todas.Count(r => r.EstadoReservaCodigo == "PENDIENTE");
            model.TotalProximas = todas.Count(r => (r.EstadoReservaCodigo == "CONFIRMADA" || r.EstadoReservaCodigo == "PENDIENTE") && !r.TieneCheckIn && r.FechaOperacion >= DateTime.Today);
            model.TotalCompletadas = todas.Count(r => r.EstadoReservaCodigo == "CONFIRMADA" && (r.TieneCheckIn || r.FechaOperacion < DateTime.Today));
            model.TotalCanceladas = todas.Count(r => r.EstadoReservaCodigo == "CANCELADA" || r.EstadoReservaCodigo == "EXPIRADA");

            // Filtrar según el tab solicitado
            switch (model.FiltroActual)
            {
                case "pendientes":
                    model.Reservas = todas.Where(r => r.EstadoReservaCodigo == "PENDIENTE").ToList();
                    break;
                case "proximas":
                    model.Reservas = todas.Where(r => (r.EstadoReservaCodigo == "CONFIRMADA" || r.EstadoReservaCodigo == "PENDIENTE") && !r.TieneCheckIn && r.FechaOperacion >= DateTime.Today).ToList();
                    break;
                case "completadas":
                    model.Reservas = todas.Where(r => r.EstadoReservaCodigo == "CONFIRMADA" && (r.TieneCheckIn || r.FechaOperacion < DateTime.Today)).ToList();
                    break;
                case "canceladas":
                    model.Reservas = todas.Where(r => r.EstadoReservaCodigo == "CANCELADA" || r.EstadoReservaCodigo == "EXPIRADA").ToList();
                    break;
                default:
                    model.Reservas = todas;
                    break;
            }

            return View(model);
        }

        // =========================================================================
        // PANTALLA 14: GESTIONAR RESERVA (DETALLES Y CANCELACIÓN CON PENALIDAD DP-08)
        // =========================================================================
        [HttpGet]
        public ActionResult Gestionar(string id)
        {
            VuelosController.LiberarReservasExpiradas(db);

            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("MisReservas");
            }

            string codigoLimpio = id.Trim().ToUpperInvariant();

            var reserva = db.Reservas
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Aeronave.ModeloAeronave))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.CheckIns))
                .Include(r => r.Compras.Select(c => c.Pagoes))
                .Include(r => r.Compras.Select(c => c.Comprobantes))
                .FirstOrDefault(r => r.CodigoReserva == codigoLimpio);

            if (reserva == null)
            {
                TempData["MensajeError"] = "No se encontró la reserva " + id;
                return RedirectToAction("MisReservas");
            }

            var primeraAsignacion = reserva.ReservaPasajeroVueloes.FirstOrDefault();
            var vuelo = primeraAsignacion?.Vuelo;
            var compra = reserva.Compras.FirstOrDefault();
            var pago = compra?.Pagoes.OrderByDescending(p => p.FechaPago).FirstOrDefault(p => p.EstadoPagoCodigo == "APROBADO" || p.EstadoPagoCodigo == "REVERSADO")
                       ?? compra?.Pagoes.OrderByDescending(p => p.FechaPago).FirstOrDefault();

            var checkInsCompletados = Session["CodigosCheckInCompletados"] as HashSet<string> ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool tieneCheckIn = checkInsCompletados.Contains(codigoLimpio) ||
                                reserva.ReservaPasajeroVueloes.Any(a => a.CheckIns != null && a.CheckIns.Any(c => c.EstadoCheckInCodigo == "COMPLETADO"));

            string estadoRes = reserva.EstadoReservaCodigo ?? "CONFIRMADA";
            bool esCanceladaOExpirada = estadoRes == "CANCELADA" || estadoRes == "EXPIRADA";
            bool vueloCompletado = string.Equals(vuelo?.EstadoVueloCodigo, "COMPLETADO", StringComparison.OrdinalIgnoreCase);

            DateTime fechaVuelo = vuelo?.FechaOperacion ?? DateTime.Today.AddDays(2);
            TimeSpan horaSalida = vuelo?.Frecuencia?.HoraSalida ?? new TimeSpan(7, 30, 0);
            DateTime fechaHoraSalida = fechaVuelo.Date.Add(horaSalida);
            double horasParaSalida = (fechaHoraSalida - DateTime.Now).TotalHours;

            decimal total = compra?.Importe ?? 557.75m;
            decimal baseTarifa = Math.Round(total / 1.15m, 2);
            decimal iva = total - baseTarifa;

            // Cálculo de penalidad conforme a DP-08:
            // - PENDIENTE: cancelación sin cargo (0%)
            // - CONFIRMADA >= 24h antes: penalidad 20%, reembolso 80%
            // - CONFIRMADA < 24h antes: penalidad 100%, reembolso 0%
            // - Con Check-in COMPLETADO o Vuelo COMPLETADO: No cancelable
            decimal porcentajePenalidad = 0m;
            decimal montoPenalidad = 0m;
            decimal montoReembolso = total;
            bool esCancelable = !esCanceladaOExpirada && !vueloCompletado && !tieneCheckIn && fechaHoraSalida > DateTime.Now;
            string motivoNoCancelable = null;

            if (esCanceladaOExpirada)
            {
                motivoNoCancelable = $"La reserva ya se encuentra en estado {estadoRes}.";
            }
            else if (tieneCheckIn)
            {
                motivoNoCancelable = "No se permite cancelar una reserva que ya cuenta con Check-in COMPLETADO (DP-08).";
            }
            else if (vueloCompletado || fechaHoraSalida <= DateTime.Now)
            {
                motivoNoCancelable = "No se permite cancelar una reserva cuyo vuelo ya ha despegado o se encuentra COMPLETADO (DP-08).";
            }
            else if (estadoRes == "CONFIRMADA")
            {
                if (horasParaSalida >= 24.0)
                {
                    porcentajePenalidad = 20m;
                    montoPenalidad = Math.Round(total * 0.20m, 2);
                    montoReembolso = total - montoPenalidad;
                }
                else
                {
                    porcentajePenalidad = 100m;
                    montoPenalidad = total;
                    montoReembolso = 0m;
                }
            }

            var notaCredito = compra?.Comprobantes.FirstOrDefault(c => c.TipoComprobanteCodigo == "NOTA_CREDITO");

            var model = new GestionarReservaViewModel
            {
                ReservaId = reserva.ReservaId,
                CodigoReserva = reserva.CodigoReserva,
                FechaCreacion = reserva.FechaCreacion,
                FechaExpiracion = reserva.FechaExpiracion,
                EstadoReservaCodigo = estadoRes,
                VueloId = vuelo?.VueloId ?? 1,
                CodigoVuelo = "VIP" + (vuelo?.VueloId ?? 1).ToString("D5"),
                OrigenCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA ?? "UIO",
                OrigenCiudad = vuelo?.Frecuencia?.Ruta?.Aeropuerto?.Localidad ?? "Quito",
                DestinoCodigo = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA ?? "GYE",
                DestinoCiudad = vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.Localidad ?? "Guayaquil",
                FechaOperacion = fechaVuelo,
                HoraSalida = horaSalida,
                HoraLlegada = vuelo?.Frecuencia?.HoraLlegada ?? new TimeSpan(8, 25, 0),
                AeronaveMatricula = vuelo?.Aeronave?.Matricula ?? "AV-01",
                ModeloAeronave = vuelo?.Aeronave?.ModeloAeronave?.Nombre ?? "Phenom 300E",
                Tarifa = "Classic",
                TarifaBase = baseTarifa,
                TasasAeroportuarias = 35.00m,
                Subtotal = baseTarifa,
                Iva = iva,
                TotalPagado = total,
                MarcaTarjeta = pago?.MarcaTarjetaCodigo ?? (estadoRes == "PENDIENTE" ? "PENDIENTE" : "VISA"),
                Ultimos4 = pago?.UltimosCuatroDigitos ?? "----",
                CodigoOperacion = pago?.CodigoOperacion ?? (estadoRes == "PENDIENTE" ? "SIN PAGO AÚN" : "SIM-" + reserva.CodigoReserva + "-01"),
                FechaPago = pago?.FechaPago,
                EsCancelable = esCancelable,
                MotivoNoCancelable = motivoNoCancelable,
                PorcentajePenalidad = porcentajePenalidad,
                MontoPenalidad = montoPenalidad,
                MontoReembolso = montoReembolso,
                NumeroNotaCredito = notaCredito?.NumeroComprobante,
                PoliticaCancelacion = "Política Oficial DP-08: Reservas PENDIENTES se cancelan sin cargo (0%). Reservas CONFIRMADAS canceladas con 24h o más de anticipación tienen penalidad del 20% (reembolso del 80% mediante Nota de Crédito Académica). Con menos de 24h tienen penalidad del 100%. No se admiten cancelaciones tras completar el Check-in.",
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
                    TipoDocumento = a.Pasajero?.TipoDocumentoCodigo ?? "CEDULA",
                    NumeroDocumento = a.Pasajero?.NumeroDocumento ?? "1710034065",
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
        // ACCIÓN: CANCELAR RESERVA (CON PENALIDAD Y NOTA DE CRÉDITO DP-08)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancelar(string codigoReserva, string motivo)
        {
            if (string.IsNullOrWhiteSpace(codigoReserva))
            {
                return RedirectToAction("MisReservas");
            }

            string cod = codigoReserva.Trim().ToUpperInvariant();

            var reservaDb = db.Reservas
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.CheckIns))
                .Include(r => r.Compras.Select(c => c.Pagoes))
                .Include(r => r.Compras.Select(c => c.Comprobantes))
                .FirstOrDefault(r => r.CodigoReserva == cod);

            if (reservaDb == null)
            {
                TempData["MensajeError"] = $"No se encontró la reserva {cod}.";
                return RedirectToAction("MisReservas");
            }

            // Verificar restricciones de cancelación (DP-08)
            bool tieneCheckIn = reservaDb.ReservaPasajeroVueloes.Any(a => a.CheckIns != null && a.CheckIns.Any(c => c.EstadoCheckInCodigo == "COMPLETADO"));
            var vuelo = reservaDb.ReservaPasajeroVueloes.FirstOrDefault()?.Vuelo;
            bool vueloCompletado = string.Equals(vuelo?.EstadoVueloCodigo, "COMPLETADO", StringComparison.OrdinalIgnoreCase);

            if (tieneCheckIn || vueloCompletado)
            {
                TempData["MensajeError"] = $"No es posible cancelar la reserva {cod} porque ya cuenta con Check-in completado o el vuelo ha finalizado (Política DP-08).";
                return RedirectToAction("Gestionar", new { id = cod });
            }

            DateTime ahora = DateTime.Now;
            string estadoAnterior = reservaDb.EstadoReservaCodigo;

            reservaDb.EstadoReservaCodigo = "CANCELADA";
            reservaDb.FechaCancelacion = ahora;
            reservaDb.FechaActualizacion = ahora;
            reservaDb.Observaciones = (reservaDb.Observaciones ?? "") + $" | Cancelada ({motivo ?? "Solicitud del usuario"}).";

            foreach (var a in reservaDb.ReservaPasajeroVueloes)
            {
                a.EstadoAsignacionCodigo = "CANCELADA";
            }

            var cods = reservaDb.ReservaPasajeroVueloes.Select(a => VuelosController.MapearNumeroAsientoACodigo(a.NumeroAsiento)).ToList();
            VuelosController.LiberarAsientos(vuelo?.VueloId ?? 1, cods);

            string detalleReembolsoMsg = "Reserva pendiente cancelada sin cargo.";

            // Si la reserva estaba CONFIRMADA y tenía compra PAGADA, aplicar penalidad DP-08, anular compra, reversar pago y emitir NOTA_CREDITO
            var compraDb = reservaDb.Compras.FirstOrDefault();
            if (estadoAnterior == "CONFIRMADA" && compraDb != null)
            {
                DateTime fechaHoraSalida = (vuelo?.FechaOperacion.Date ?? DateTime.Today.AddDays(1)).Add(vuelo?.Frecuencia?.HoraSalida ?? new TimeSpan(9, 0, 0));
                double horasParaSalida = (fechaHoraSalida - ahora).TotalHours;

                decimal total = compraDb.Importe;
                decimal porcentajePenalidad = horasParaSalida >= 24.0 ? 20m : 100m;
                decimal montoPenalidad = Math.Round(total * (porcentajePenalidad / 100m), 2);
                decimal montoReembolso = total - montoPenalidad;

                compraDb.EstadoCompraCodigo = "ANULADA";
                compraDb.FechaAnulacion = ahora;
                compraDb.Observaciones = $"Anulada por cancelación de reserva (Penalidad {porcentajePenalidad}%: USD {montoPenalidad:N2}, Reembolso: USD {montoReembolso:N2} - DP-08).";

                foreach (var pago in compraDb.Pagoes.Where(p => p.EstadoPagoCodigo == "APROBADO"))
                {
                    pago.EstadoPagoCodigo = "REVERSADO";
                    pago.MensajeResultado = $"Pago reversado por cancelación (Reembolso USD {montoReembolso:N2} - DP-08).";
                }

                // Anular factura previa y emitir NOTA_CREDITO académica (DP-05 / DP-08)
                foreach (var compPrevio in compraDb.Comprobantes.Where(c => c.TipoComprobanteCodigo == "FACTURA"))
                {
                    compPrevio.EstadoComprobanteCodigo = "ANULADO";
                }

                string numNotaCredito = "NC-ACAD-" + compraDb.CompraId.ToString("D6");
                if (!db.Comprobantes.Any(c => c.NumeroComprobante == numNotaCredito))
                {
                    var primerPax = reservaDb.ReservaPasajeroVueloes.Select(a => a.Pasajero).FirstOrDefault();
                    db.Comprobantes.Add(new Comprobante
                    {
                        CompraId = compraDb.CompraId,
                        TipoComprobanteCodigo = "NOTA_CREDITO",
                        EstadoComprobanteCodigo = "EMITIDO",
                        NumeroComprobante = numNotaCredito,
                        FechaEmision = ahora,
                        ReceptorNombre = primerPax != null ? $"{primerPax.Nombres} {primerPax.Apellidos}".Trim() : "Cliente VIP",
                        ReceptorDocumento = primerPax?.NumeroDocumento ?? "1710034065",
                        ReceptorCorreo = primerPax?.Correo ?? "cliente@aerolineavip.com",
                        MontoTotal = montoReembolso,
                        Observaciones = $"Nota de crédito académica por cancelación de reserva {cod}. Penalidad aplicada ({porcentajePenalidad}%): USD {montoPenalidad:N2}. Reembolso neto: USD {montoReembolso:N2} (DP-08)."
                    });
                }

                detalleReembolsoMsg = $"Se aplicó la política DP-08 ({porcentajePenalidad}% penalidad = USD {montoPenalidad:N2}; reembolso = USD {montoReembolso:N2}) y se emitió la Nota de Crédito {numNotaCredito}.";
            }

            db.SaveChanges();

            TempData["MensajeExito"] = $"La reserva {cod} ha sido cancelada y los asientos fueron liberados. {detalleReembolsoMsg}";
            if (User.IsInRole("Counter") && !User.IsInRole("Cliente")) return RedirectToAction("Gestionar", new { id = cod }); return RedirectToAction("MisReservas", new { tab = "canceladas" });
        }

        // =========================================================================
        // PANTALLA 16 (DETALLE INDIVIDUAL): COMPROBANTE ACADÉMICO / FACTURA (DP-05)
        // =========================================================================
        [HttpGet]
        public ActionResult Comprobante(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index", "Comprobantes");
            }

            string cod = id.Trim().ToUpperInvariant();

            var reservaDb = db.Reservas
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Aeronave))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.CheckIns))
                .Include(r => r.Compras.Select(c => c.Pagoes))
                .Include(r => r.Compras.Select(c => c.Comprobantes))
                .FirstOrDefault(r => r.CodigoReserva == cod);

            if (reservaDb == null)
            {
                TempData["MensajeError"] = "No se encontró el comprobante para la reserva " + id;
                return RedirectToAction("MisReservas");
            }

            var datosEmisor = db.DatosEmisors.FirstOrDefault();
            var primerAsig = reservaDb.ReservaPasajeroVueloes.FirstOrDefault();
            var pas = primerAsig?.Pasajero;
            var vuelo = primerAsig?.Vuelo;
            var compra = reservaDb.Compras.FirstOrDefault();
            var pago = compra?.Pagoes.OrderByDescending(p => p.FechaPago).FirstOrDefault();
            var compEmitido = compra?.Comprobantes.OrderByDescending(c => c.FechaEmision).FirstOrDefault(c => c.TipoComprobanteCodigo == "FACTURA")
                              ?? compra?.Comprobantes.OrderByDescending(c => c.FechaEmision).FirstOrDefault();

            var comp = new ComprobanteFiscalViewModel();
            if (datosEmisor != null)
            {
                comp.EmisorRazonSocial = datosEmisor.RazonSocial;
                comp.EmisorRuc = datosEmisor.RucSimulado;
                comp.EmisorDireccion = datosEmisor.DireccionMatriz;
                if (!string.IsNullOrWhiteSpace(datosEmisor.LeyendaComprobante)) comp.LeyendaComprobante = datosEmisor.LeyendaComprobante;
            }

            DateTime fechaEmision = compEmitido?.FechaEmision ?? reservaDb.FechaCreacion;
            comp.FechaEmision = fechaEmision;
            comp.CodigoReserva = cod;
            comp.TipoComprobante = compEmitido != null
                ? $"COMPROBANTE ACADÉMICO ({compEmitido.TipoComprobanteCodigo})"
                : "COMPROBANTE ACADÉMICO DE VENTA (FACTURA SIMULADA)";
            comp.NumeroComprobante = compEmitido?.NumeroComprobante ?? ("ACAD-" + (compra?.CompraId ?? reservaDb.ReservaId).ToString("D6"));
            comp.ClaveAccesoSRI = fechaEmision.ToString("ddMMyyyy") + "0117999999990012001001" + (compra?.CompraId ?? reservaDb.ReservaId).ToString("D9") + "123456781";
            comp.NumeroAutorizacion = comp.ClaveAccesoSRI;

            comp.ClienteNombre = compEmitido?.ReceptorNombre ?? (pas != null ? (pas.Nombres + " " + pas.Apellidos).Trim() : "CLIENTE VIP");
            comp.ClienteIdentificacion = compEmitido?.ReceptorDocumento ?? (pas?.NumeroDocumento ?? "1710034065");
            comp.ClienteCorreo = compEmitido?.ReceptorCorreo ?? (pas?.Correo ?? "cliente@aerolineavip.com");
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
            comp.CodigoAutorizacionBancaria = pago?.CodigoOperacion ?? ("SIM-" + cod + "-01");

            foreach (var a in reservaDb.ReservaPasajeroVueloes)
            {
                string seat = MapearNumeroAsientoACodigo(a.NumeroAsiento);
                string nom = a.Pasajero != null ? $"{a.Pasajero.Nombres} {a.Pasajero.Apellidos}" : "Pasajero VIP";
                decimal unitPrice = Math.Round(subtotal / Math.Max(1, reservaDb.ReservaPasajeroVueloes.Count), 2);
                comp.Items.Add(new ComprobanteItemDetalleViewModel
                {
                    Codigo = "TRN-" + comp.CodigoVuelo,
                    Descripcion = $"Transporte Aéreo Ejecutivo VIP - {vuelo?.Frecuencia?.Ruta?.Aeropuerto?.CodigoIATA}/{vuelo?.Frecuencia?.Ruta?.Aeropuerto1?.CodigoIATA} - Pasajero: {nom} (Asiento {seat})",
                    Cantidad = 1,
                    PrecioUnitario = unitPrice,
                    Total = unitPrice
                });

                comp.PasesAbordar.Add(new PaseAbordarDetalleViewModel
                {
                    CodigoReserva = cod,
                    CodigoVuelo = comp.CodigoVuelo,
                    PasajeroNombre = nom,
                    PasajeroDocumento = a.Pasajero?.NumeroDocumento ?? "1710034065",
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

            return View(comp);
        }

        private string ObtenerUsuarioActualId()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                string uid = User.Identity.GetUserId();
                if (!string.IsNullOrWhiteSpace(uid)) return uid;
                if (!string.IsNullOrWhiteSpace(User.Identity.Name)) return User.Identity.Name;
            }
            return "7c1e4a20-0001-4b6e-9a01-000000000001";
        }

        private static string MapearTipoDocumentoSql(string tipo)
        {
            if (string.IsNullOrWhiteSpace(tipo)) return "CEDULA";
            string upper = tipo.Trim().ToUpperInvariant();
            if (upper == "PAS" || upper == "PASAPORTE") return "PASAPORTE";
            return "CEDULA";
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