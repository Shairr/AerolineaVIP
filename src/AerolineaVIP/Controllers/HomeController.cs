using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    public class HomeController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        public ActionResult Index()
        {
            // Aeropuertos activos de la BD para el buscador rápido
            ViewBag.Aeropuertos = db.Aeropuertoes
                .Where(a => a.Activo)
                .OrderBy(a => a.Localidad)
                .ToList();

            // Rutas autorizadas reales con su tarifa referencial y frecuencias
            var rutas = db.Rutas
                .Include(r => r.Aeropuerto)
                .Include(r => r.Aeropuerto1)
                .Include(r => r.Frecuencias)
                .Where(r => r.Activa && r.Aeropuerto.CodigoIATA == "UIO")
                .ToList();

            // Modelo de aeronave ejecutiva de la BD
            ViewBag.ModeloAeronave = db.ModeloAeronaves
                .FirstOrDefault(m => m.Nombre.Contains("Phenom"));

            // Fecha del próximo vuelo disponible en la BD
            var proximaFecha = db.Vueloes
                .Where(v => v.EstadoVueloCodigo != "CANCELADO" && v.FechaOperacion >= DateTime.Today)
                .OrderBy(v => v.FechaOperacion)
                .Select(v => v.FechaOperacion)
                .FirstOrDefault();

            ViewBag.FechaSugerida = proximaFecha != default(DateTime) ? proximaFecha.ToString("yyyy-MM-dd") : DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");

            return View(rutas);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Aerolínea VIP — Transporte aéreo ejecutivo privado en Ecuador.";
            ViewBag.Flota = db.Aeronaves.Include(a => a.ModeloAeronave).ToList();
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Centro de atención y reservas personalizadas Aerolínea VIP.";
            ViewBag.Emisor = db.DatosEmisors.FirstOrDefault();
            return View();
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
