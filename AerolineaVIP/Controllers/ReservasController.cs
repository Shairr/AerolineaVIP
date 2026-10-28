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
            if (!ModelState.IsValid)
            {
                return View("Pagar", model);
            }

            var random = new Random();
            string codigoReserva = "VIP-" + random.Next(10000, 99999);
            string codigoOperacion = "SIM-" + random.Next(100000, 999999);

            return RedirectToAction("Exito", new
            {
                codigoReserva = codigoReserva,
                codigoOperacion = codigoOperacion,
                vueloId = model.VueloId,
                marca = model.MarcaTarjeta,
                ultimos4 = model.NumeroTarjeta?.Length >= 4 ? model.NumeroTarjeta.Substring(model.NumeroTarjeta.Length - 4) : "9010",
                titular = model.TitularTarjeta
            });
        }

        // =========================================================================
        // PANTALLA 12: CONFIRMACIÓN DE COMPRA EXITOSA Y PASES DE ABORDAR
        // =========================================================================
        [HttpGet]
        public ActionResult Exito(string codigoReserva = "VIP-78492", string codigoOperacion = "SIM-582910", int vueloId = 1, string marca = "VISA", string ultimos4 = "9010", string titular = "CARLOS ROBERTO MENDOZA")
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
