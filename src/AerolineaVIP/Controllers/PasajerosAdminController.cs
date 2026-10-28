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
    public class PasajerosAdminController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: PasajerosAdmin
        public ActionResult Index()
        {
            var pasajeroes = db.Pasajeroes.Include(p => p.TipoDocumentoIdentidad);
            return View(pasajeroes.ToList());
        }

        // GET: PasajerosAdmin/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pasajero pasajero = db.Pasajeroes.Find(id);
            if (pasajero == null)
            {
                return HttpNotFound();
            }
            return View(pasajero);
        }

        // GET: PasajerosAdmin/Create
        public ActionResult Create()
        {
            ViewBag.TipoDocumentoCodigo = new SelectList(db.TipoDocumentoIdentidads, "Codigo", "Descripcion");
            return View();
        }

        // POST: PasajerosAdmin/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "PasajeroId,TipoDocumentoCodigo,NumeroDocumento,Nombres,Apellidos,FechaNacimiento,Correo,Telefono,FechaRegistro")] Pasajero pasajero)
        {
            if (ModelState.IsValid)
            {
                db.Pasajeroes.Add(pasajero);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.TipoDocumentoCodigo = new SelectList(db.TipoDocumentoIdentidads, "Codigo", "Descripcion", pasajero.TipoDocumentoCodigo);
            return View(pasajero);
        }

        // GET: PasajerosAdmin/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pasajero pasajero = db.Pasajeroes.Find(id);
            if (pasajero == null)
            {
                return HttpNotFound();
            }
            ViewBag.TipoDocumentoCodigo = new SelectList(db.TipoDocumentoIdentidads, "Codigo", "Descripcion", pasajero.TipoDocumentoCodigo);
            return View(pasajero);
        }

        // POST: PasajerosAdmin/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "PasajeroId,TipoDocumentoCodigo,NumeroDocumento,Nombres,Apellidos,FechaNacimiento,Correo,Telefono,FechaRegistro")] Pasajero pasajero)
        {
            if (ModelState.IsValid)
            {
                db.Entry(pasajero).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.TipoDocumentoCodigo = new SelectList(db.TipoDocumentoIdentidads, "Codigo", "Descripcion", pasajero.TipoDocumentoCodigo);
            return View(pasajero);
        }

        // GET: PasajerosAdmin/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pasajero pasajero = db.Pasajeroes.Find(id);
            if (pasajero == null)
            {
                return HttpNotFound();
            }
            return View(pasajero);
        }

        // POST: PasajerosAdmin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Pasajero pasajero = db.Pasajeroes.Find(id);
            db.Pasajeroes.Remove(pasajero);
            db.SaveChanges();
            return RedirectToAction("Index");
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
