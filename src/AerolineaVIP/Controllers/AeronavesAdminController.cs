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
    public class AeronavesAdminController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: AeronavesAdmin (Pantalla 19: Gestión de aeronaves)
        public ActionResult Index()
        {
            var aeronaves = db.Aeronaves
                .Include(a => a.EstadoAeronave)
                .Include(a => a.ModeloAeronave)
                .Include(a => a.Vueloes)
                .OrderBy(a => a.Matricula)
                .ToList();
            return View(aeronaves);
        }

        // POST: AeronavesAdmin/AlternarDisponibilidad/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AlternarDisponibilidad(int id)
        {
            var aeronave = db.Aeronaves.Include(a => a.Vueloes).FirstOrDefault(a => a.AeronaveId == id);
            if (aeronave == null) return RedirectToAction("Index");

            string nuevoEstado = aeronave.EstadoAeronaveCodigo == "DISPONIBLE" ? "NO_DISPONIBLE" : "DISPONIBLE";
            aeronave.EstadoAeronaveCodigo = nuevoEstado;
            db.SaveChanges();

            int vuelosProgramados = aeronave.Vueloes.Count(v => v.EstadoVueloCodigo == "PROGRAMADO");
            if (nuevoEstado == "NO_DISPONIBLE" && vuelosProgramados > 0)
            {
                TempData["MensajeExito"] = $"La aeronave {aeronave.Matricula} pasó a estado NO_DISPONIBLE (RNP-08). Nota: posee {vuelosProgramados} vuelo(s) previamente programado(s); no podrá asignarse a nuevos vuelos mientras permanezca no disponible.";
            }
            else
            {
                TempData["MensajeExito"] = $"Estado operativo de la aeronave {aeronave.Matricula} actualizado a {nuevoEstado}.";
            }

            return RedirectToAction("Index");
        }

        // GET: AeronavesAdmin/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeronave aeronave = db.Aeronaves
                .Include(a => a.EstadoAeronave)
                .Include(a => a.ModeloAeronave)
                .Include(a => a.Vueloes.Select(v => v.Frecuencia.Ruta.Aeropuerto))
                .Include(a => a.Vueloes.Select(v => v.Frecuencia.Ruta.Aeropuerto1))
                .FirstOrDefault(a => a.AeronaveId == id.Value);
            if (aeronave == null)
            {
                return HttpNotFound();
            }
            return View(aeronave);
        }

        // GET: AeronavesAdmin/Create
        public ActionResult Create()
        {
            CargarCombosAeronave(null);
            return View(new Aeronave { EstadoAeronaveCodigo = "DISPONIBLE" });
        }

        // POST: AeronavesAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "AeronaveId,Matricula,ModeloAeronaveId,EstadoAeronaveCodigo")] Aeronave aeronave)
        {
            aeronave.Matricula = (aeronave.Matricula ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(aeronave.Matricula))
            {
                ModelState.AddModelError("Matricula", "La matrícula de la aeronave es obligatoria (ej: AV-04 o HC-VIP4).");
            }
            else if (db.Aeronaves.Any(a => a.Matricula == aeronave.Matricula))
            {
                ModelState.AddModelError("Matricula", $"Ya existe una aeronave registrada con la matrícula {aeronave.Matricula}.");
            }

            if (ModelState.IsValid)
            {
                aeronave.FechaRegistro = DateTime.Now;
                db.Aeronaves.Add(aeronave);
                db.SaveChanges();
                TempData["MensajeExito"] = $"Aeronave {aeronave.Matricula} registrada exitosamente en la flota ejecutiva.";
                return RedirectToAction("Index");
            }

            CargarCombosAeronave(aeronave);
            return View(aeronave);
        }

        // GET: AeronavesAdmin/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeronave aeronave = db.Aeronaves.Find(id);
            if (aeronave == null)
            {
                return HttpNotFound();
            }
            CargarCombosAeronave(aeronave);
            return View(aeronave);
        }

        // POST: AeronavesAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "AeronaveId,Matricula,ModeloAeronaveId,EstadoAeronaveCodigo")] Aeronave aeronave)
        {
            aeronave.Matricula = (aeronave.Matricula ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(aeronave.Matricula))
            {
                ModelState.AddModelError("Matricula", "La matrícula de la aeronave es obligatoria.");
            }
            else if (db.Aeronaves.Any(a => a.Matricula == aeronave.Matricula && a.AeronaveId != aeronave.AeronaveId))
            {
                ModelState.AddModelError("Matricula", $"Ya existe otra aeronave con la matrícula {aeronave.Matricula}.");
            }

            if (ModelState.IsValid)
            {
                var aeronaveDb = db.Aeronaves.Find(aeronave.AeronaveId);
                if (aeronaveDb == null) return HttpNotFound();

                aeronaveDb.Matricula = aeronave.Matricula;
                aeronaveDb.ModeloAeronaveId = aeronave.ModeloAeronaveId;
                aeronaveDb.EstadoAeronaveCodigo = aeronave.EstadoAeronaveCodigo;

                db.SaveChanges();
                TempData["MensajeExito"] = $"Aeronave {aeronaveDb.Matricula} actualizada correctamente.";
                return RedirectToAction("Index");
            }
            CargarCombosAeronave(aeronave);
            return View(aeronave);
        }

        // GET: AeronavesAdmin/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeronave aeronave = db.Aeronaves
                .Include(a => a.EstadoAeronave)
                .Include(a => a.ModeloAeronave)
                .Include(a => a.Vueloes)
                .FirstOrDefault(a => a.AeronaveId == id.Value);
            if (aeronave == null)
            {
                return HttpNotFound();
            }
            return View(aeronave);
        }

        // POST: AeronavesAdmin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Aeronave aeronave = db.Aeronaves.Include(a => a.Vueloes).FirstOrDefault(a => a.AeronaveId == id);
            if (aeronave == null) return RedirectToAction("Index");

            if (aeronave.Vueloes.Any())
            {
                aeronave.EstadoAeronaveCodigo = "NO_DISPONIBLE";
                db.SaveChanges();
                TempData["MensajeExito"] = $"La aeronave {aeronave.Matricula} tiene {aeronave.Vueloes.Count} vuelos asociados en el historial, por lo que fue marcada como NO_DISPONIBLE (baja lógica RNP-08).";
                return RedirectToAction("Index");
            }

            db.Aeronaves.Remove(aeronave);
            db.SaveChanges();
            TempData["MensajeExito"] = $"Aeronave {aeronave.Matricula} eliminada de la base de datos.";
            return RedirectToAction("Index");
        }

        private void CargarCombosAeronave(Aeronave actual)
        {
            var modelos = db.ModeloAeronaves.ToList().Select(m => new SelectListItem
            {
                Value = m.ModeloAeronaveId.ToString(),
                Text = $"{m.Fabricante} {m.Nombre} ({m.CapacidadAsientos} asientos ejecutivos)",
                Selected = actual != null && actual.ModeloAeronaveId == m.ModeloAeronaveId
            }).ToList();

            ViewBag.ModeloAeronaveId = modelos;
            ViewBag.EstadoAeronaveCodigo = new SelectList(db.EstadoAeronaves, "Codigo", "Descripcion", actual?.EstadoAeronaveCodigo ?? "DISPONIBLE");
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
