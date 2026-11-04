using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    [Authorize(Roles = "Administrador,SuperUsuario")]
    public class VuelosAdminController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: VuelosAdmin (Pantalla 18: Administración de vuelos e itinerarios)
        public ActionResult Index(string ruta = "TODAS", string aeronave = "TODAS", string estado = "TODOS", DateTime? fechaDesde = null, DateTime? fechaHasta = null)
        {
            var query = db.Vueloes
                .Include(v => v.Aeronave.ModeloAeronave)
                .Include(v => v.EstadoVuelo)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Include(v => v.ReservaPasajeroVueloes)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(ruta) && ruta != "TODAS")
            {
                var partes = ruta.Split('-');
                if (partes.Length == 2)
                {
                    string orig = partes[0].Trim();
                    string dest = partes[1].Trim();
                    query = query.Where(v => v.Frecuencia.Ruta.Aeropuerto.CodigoIATA == orig && v.Frecuencia.Ruta.Aeropuerto1.CodigoIATA == dest);
                }
            }

            if (!string.IsNullOrWhiteSpace(aeronave) && aeronave != "TODAS")
            {
                query = query.Where(v => v.Aeronave.Matricula == aeronave);
            }

            if (!string.IsNullOrWhiteSpace(estado) && estado != "TODOS")
            {
                query = query.Where(v => v.EstadoVueloCodigo == estado);
            }

            if (fechaDesde.HasValue)
            {
                DateTime d = fechaDesde.Value.Date;
                query = query.Where(v => DbFunctions.TruncateTime(v.FechaOperacion) >= d);
            }

            if (fechaHasta.HasValue)
            {
                DateTime h = fechaHasta.Value.Date;
                query = query.Where(v => DbFunctions.TruncateTime(v.FechaOperacion) <= h);
            }

            var vuelos = query
                .OrderBy(v => v.FechaOperacion)
                .ThenBy(v => v.Frecuencia.HoraSalida)
                .ToList();

            ViewBag.RutaFiltro = ruta;
            ViewBag.AeronaveFiltro = aeronave;
            ViewBag.EstadoFiltro = estado;
            ViewBag.FechaDesde = fechaDesde?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.FechaHasta = fechaHasta?.ToString("yyyy-MM-dd") ?? "";

            ViewBag.AeronavesList = db.Aeronaves.OrderBy(a => a.Matricula).ToList();
            ViewBag.Itinerarios = db.Itinerarios
                .Include(i => i.TramoItinerarios.Select(t => t.Vuelo.Frecuencia.Ruta.Aeropuerto))
                .Include(i => i.TramoItinerarios.Select(t => t.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                .Include(i => i.TramoItinerarios.Select(t => t.Vuelo.Aeronave))
                .OrderBy(i => i.ItinerarioId)
                .ToList();

            return View(vuelos);
        }

        // POST: VuelosAdmin/GenerarVuelosMasivo (Invoca usp_GenerarVuelos de la BD)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerarVuelosMasivo(DateTime? fechaInicio, DateTime? fechaFin, int? aeronaveId)
        {
            if (!fechaInicio.HasValue || !fechaFin.HasValue || !aeronaveId.HasValue)
            {
                TempData["MensajeError"] = "Por favor especifique el rango de fechas y la aeronave para generar vuelos masivamente.";
                return RedirectToAction("Index");
            }

            try
            {
                db.Database.ExecuteSqlCommand("EXEC dbo.usp_GenerarVuelos @p0, @p1", fechaInicio.Value.Date, fechaFin.Value.Date);
                if (aeronaveId.HasValue) {
                    var sinAeronave = db.Vueloes.Where(v => v.FechaOperacion >= fechaInicio.Value.Date && v.FechaOperacion <= fechaFin.Value.Date && v.AeronaveId == null).ToList();
                    foreach (var v in sinAeronave) { v.AeronaveId = aeronaveId.Value; }
                    db.SaveChanges();
                }
                TempData["MensajeExito"] = $"Se ejecutó la programación automática de vuelos (usp_GenerarVuelos) entre {fechaInicio.Value:dd/MM/yyyy} y {fechaFin.Value:dd/MM/yyyy}.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "No se pudo completar la generación masiva: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToAction("Index");
        }

        // GET: VuelosAdmin/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vuelo vuelo = db.Vueloes
                .Include(v => v.Aeronave.ModeloAeronave)
                .Include(v => v.EstadoVuelo)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Include(v => v.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                .Include(v => v.ReservaPasajeroVueloes.Select(a => a.Reserva))
                .FirstOrDefault(v => v.VueloId == id.Value);
            if (vuelo == null)
            {
                return HttpNotFound();
            }
            return View(vuelo);
        }

        // GET: VuelosAdmin/Create
        public ActionResult Create()
        {
            CargarCombosVuelo(null);
            var model = new Vuelo
            {
                FechaOperacion = DateTime.Today.AddDays(7),
                EstadoVueloCodigo = "PROGRAMADO"
            };
            return View(model);
        }

        // POST: VuelosAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "VueloId,FrecuenciaId,FechaOperacion,AeronaveId,EstadoVueloCodigo")] Vuelo vuelo)
        {
            ValidarReglasOperativasVuelo(vuelo, esEdicion: false);

            if (ModelState.IsValid)
            {
                try
                {
                    vuelo.FechaCreacion = DateTime.Now;
                    db.Vueloes.Add(vuelo);
                    db.SaveChanges();
                    TempData["MensajeExito"] = $"Vuelo VIP{vuelo.VueloId:D5} programado exitosamente para el {vuelo.FechaOperacion:dd/MM/yyyy}.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar el vuelo en la base de datos: " + (ex.InnerException?.InnerException?.Message ?? ex.Message));
                }
            }

            CargarCombosVuelo(vuelo);
            return View(vuelo);
        }

        // GET: VuelosAdmin/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vuelo vuelo = db.Vueloes.Find(id);
            if (vuelo == null)
            {
                return HttpNotFound();
            }
            CargarCombosVuelo(vuelo);
            return View(vuelo);
        }

        // POST: VuelosAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "VueloId,FrecuenciaId,FechaOperacion,AeronaveId,EstadoVueloCodigo")] Vuelo vuelo)
        {
            ValidarReglasOperativasVuelo(vuelo, esEdicion: true);

            if (ModelState.IsValid)
            {
                try
                {
                    var vueloDb = db.Vueloes.Find(vuelo.VueloId);
                    if (vueloDb == null) return HttpNotFound();

                    vueloDb.FrecuenciaId = vuelo.FrecuenciaId;
                    vueloDb.FechaOperacion = vuelo.FechaOperacion.Date;
                    vueloDb.AeronaveId = vuelo.AeronaveId;
                    vueloDb.EstadoVueloCodigo = vuelo.EstadoVueloCodigo;

                    db.SaveChanges();
                    TempData["MensajeExito"] = $"Vuelo VIP{vuelo.VueloId:D5} actualizado correctamente.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al actualizar el vuelo: " + (ex.InnerException?.InnerException?.Message ?? ex.Message));
                }
            }

            CargarCombosVuelo(vuelo);
            return View(vuelo);
        }

        // GET: VuelosAdmin/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vuelo vuelo = db.Vueloes
                .Include(v => v.Aeronave.ModeloAeronave)
                .Include(v => v.EstadoVuelo)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Include(v => v.ReservaPasajeroVueloes)
                .FirstOrDefault(v => v.VueloId == id.Value);
            if (vuelo == null)
            {
                return HttpNotFound();
            }
            return View(vuelo);
        }

        // POST: VuelosAdmin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Vuelo vuelo = db.Vueloes
                .Include(v => v.ReservaPasajeroVueloes)
                .Include(v => v.TramoItinerarios)
                .FirstOrDefault(v => v.VueloId == id);

            if (vuelo == null) return RedirectToAction("Index");

            if (vuelo.ReservaPasajeroVueloes.Any() || vuelo.TramoItinerarios.Any())
            {
                vuelo.EstadoVueloCodigo = "CANCELADO";
                db.SaveChanges();
                TempData["MensajeExito"] = $"El vuelo VIP{id:D5} posee reservas o tramos de itinerario asociados, por lo que su estado fue cambiado a CANCELADO preservando la trazabilidad histórica.";
                return RedirectToAction("Index");
            }

            db.Vueloes.Remove(vuelo);
            db.SaveChanges();
            TempData["MensajeExito"] = $"El vuelo VIP{id:D5} fue eliminado exitosamente.";
            return RedirectToAction("Index");
        }

        private void ValidarReglasOperativasVuelo(Vuelo vuelo, bool esEdicion)
        {
            var frecuencia = db.Frecuencias
                .Include(f => f.Ruta.Aeropuerto)
                .Include(f => f.Ruta.Aeropuerto1)
                .FirstOrDefault(f => f.FrecuenciaId == vuelo.FrecuenciaId);

            var aeronave = db.Aeronaves.Find(vuelo.AeronaveId);

            if (frecuencia == null)
            {
                ModelState.AddModelError("FrecuenciaId", "Seleccione una frecuencia operativa válida.");
                return;
            }

            if (aeronave == null)
            {
                ModelState.AddModelError("AeronaveId", "Seleccione una aeronave válida.");
                return;
            }

            // 1. Validar que la aeronave esté DISPONIBLE (trg_Vuelo_ValidarAeronave / RNP-08)
            if (aeronave.EstadoAeronaveCodigo != "DISPONIBLE")
            {
                ModelState.AddModelError("AeronaveId", $"La aeronave {aeronave.Matricula} tiene estado '{aeronave.EstadoAeronaveCodigo}'. Solo se pueden asignar aeronaves en estado DISPONIBLE (RNP-08).");
            }

            // 2. Validar coincidencia entre FechaOperacion y DiaSemana de la frecuencia (trg_Vuelo_ValidarFrecuencia)
            int diaSemanaIso = ((int)vuelo.FechaOperacion.DayOfWeek == 0) ? 7 : (int)vuelo.FechaOperacion.DayOfWeek;
            if (diaSemanaIso != frecuencia.DiaSemana)
            {
                string nombreRequerido = ObtenerNombreDia(frecuencia.DiaSemana);
                string nombreSeleccionado = ObtenerNombreDia((byte)diaSemanaIso);
                ModelState.AddModelError("FechaOperacion", $"La frecuencia seleccionada ({frecuencia.Ruta.Aeropuerto.CodigoIATA} → {frecuencia.Ruta.Aeropuerto1.CodigoIATA} {frecuencia.HoraSalida:hh\\:mm}) opera únicamente los días {nombreRequerido}, pero la fecha {vuelo.FechaOperacion:dd/MM/yyyy} es {nombreSeleccionado}.");
            }

            // 3. Validar duplicado de Frecuencia + FechaOperacion (UQ_Vuelo_Frecuencia_Fecha)
            DateTime fechaTrunc = vuelo.FechaOperacion.Date;
            bool existeDuplicado = db.Vueloes.Any(v =>
                v.FrecuenciaId == vuelo.FrecuenciaId &&
                DbFunctions.TruncateTime(v.FechaOperacion) == fechaTrunc &&
                (!esEdicion || v.VueloId != vuelo.VueloId));

            if (existeDuplicado)
            {
                ModelState.AddModelError("FechaOperacion", $"Ya existe un vuelo programado para esta frecuencia en la fecha {vuelo.FechaOperacion:dd/MM/yyyy}.");
            }

            // 4. Validar no solapamiento y turnaround mínimo de 60 minutos en la misma aeronave (DP-03 / RG-07)
            var otrosVuelosAeronave = db.Vueloes
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Where(v => v.AeronaveId == vuelo.AeronaveId
                         && v.EstadoVueloCodigo != "CANCELADO"
                         && DbFunctions.TruncateTime(v.FechaOperacion) == fechaTrunc
                         && (!esEdicion || v.VueloId != vuelo.VueloId))
                .ToList();

            TimeSpan nuevaSalida = frecuencia.HoraSalida;
            TimeSpan nuevaLlegada = frecuencia.HoraLlegada;

            foreach (var otro in otrosVuelosAeronave)
            {
                TimeSpan otraSalida = otro.Frecuencia.HoraSalida;
                TimeSpan otraLlegada = otro.Frecuencia.HoraLlegada;

                // Se requiere al menos 60 minutos entre la llegada de un tramo y la salida del siguiente (DP-03)
                bool solapa = (nuevaSalida < otraLlegada.Add(TimeSpan.FromMinutes(60))) &&
                              (otraSalida < nuevaLlegada.Add(TimeSpan.FromMinutes(60)));

                if (solapa)
                {
                    ModelState.AddModelError("AeronaveId", $"Conflicto de programación (Política DP-03): La aeronave {aeronave.Matricula} ya opera el vuelo VIP{otro.VueloId:D5} ({otro.Frecuencia.Ruta.Aeropuerto.CodigoIATA}→{otro.Frecuencia.Ruta.Aeropuerto1.CodigoIATA} de {otraSalida:hh\\:mm} a {otraLlegada:hh\\:mm}) y requiere al menos 60 minutos de turnaround.");
                    break;
                }
            }
        }

        private void CargarCombosVuelo(Vuelo vueloActual)
        {
            var frecuencias = db.Frecuencias
                .Include(f => f.Ruta.Aeropuerto)
                .Include(f => f.Ruta.Aeropuerto1)
                .Where(f => f.Activa)
                .OrderBy(f => f.DiaSemana)
                .ThenBy(f => f.HoraSalida)
                .ToList();

            var itemsFrecuencia = frecuencias.Select(f => new SelectListItem
            {
                Value = f.FrecuenciaId.ToString(),
                Text = $"#{f.FrecuenciaId} · {f.Ruta.Aeropuerto.CodigoIATA} → {f.Ruta.Aeropuerto1.CodigoIATA} · {ObtenerNombreDia(f.DiaSemana)} · {f.HoraSalida:hh\\:mm} - {f.HoraLlegada:hh\\:mm} (USD {f.Ruta.TarifaReferencial:N0})",
                Selected = (vueloActual != null && vueloActual.FrecuenciaId == f.FrecuenciaId)
            }).ToList();

            var aeronaves = db.Aeronaves
                .Include(a => a.ModeloAeronave)
                .OrderBy(a => a.Matricula)
                .ToList();

            var itemsAeronave = aeronaves.Select(a => new SelectListItem
            {
                Value = a.AeronaveId.ToString(),
                Text = $"{a.Matricula} · {a.ModeloAeronave?.Fabricante} {a.ModeloAeronave?.Nombre} ({a.EstadoAeronaveCodigo})",
                Selected = (vueloActual != null && vueloActual.AeronaveId == a.AeronaveId)
            }).ToList();

            ViewBag.FrecuenciaId = itemsFrecuencia;
            ViewBag.AeronaveId = itemsAeronave;
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVueloes, "Codigo", "Descripcion", vueloActual?.EstadoVueloCodigo ?? "PROGRAMADO");
        }

        public static string ObtenerNombreDia(byte dia)
        {
            switch (dia)
            {
                case 1: return "Lunes";
                case 2: return "Martes";
                case 3: return "Miércoles";
                case 4: return "Jueves";
                case 5: return "Viernes";
                case 6: return "Sábado";
                case 7: return "Domingo";
                default: return "Día " + dia;
            }
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
