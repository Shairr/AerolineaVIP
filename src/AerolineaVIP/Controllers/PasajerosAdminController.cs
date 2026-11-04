using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using AerolineaVIP.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace AerolineaVIP.Controllers
{
    [Authorize(Roles = "Administrador,SuperUsuario")]
    public class PasajerosAdminController : Controller
    {
        private readonly AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: PasajerosAdmin (Pantalla 20: Gestión de usuarios, pasajeros y reservas)
        public ActionResult Index(string tab = "usuarios", string busqueda = null, string estadoReserva = "TODOS")
        {
            VuelosController.LiberarReservasExpiradas(db);

            bool esSuper = User.IsInRole("SuperUsuario");
            var model = new AdminGestionIntegralViewModel
            {
                TabActiva = string.IsNullOrWhiteSpace(tab) ? "usuarios" : tab.ToLowerInvariant(),
                Busqueda = busqueda,
                FiltroEstadoReserva = string.IsNullOrWhiteSpace(estadoReserva) ? "TODOS" : estadoReserva.ToUpperInvariant(),
                EsSuperUsuario = esSuper
            };

            // 1. Cargar Usuarios y Roles desde Identity (ApplicationDbContext)
            using (var identityCtx = new ApplicationDbContext())
            {
                var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityCtx));
                var users = identityCtx.Users.OrderBy(u => u.UserName).ToList();

                foreach (var u in users)
                {
                    if (!string.IsNullOrWhiteSpace(busqueda) && model.TabActiva == "usuarios")
                    {
                        string b = busqueda.Trim().ToLowerInvariant();
                        if (!(u.UserName.ToLowerInvariant().Contains(b) || (u.Email ?? "").ToLowerInvariant().Contains(b)))
                            continue;
                    }

                    var roles = userManager.GetRoles(u.Id);
                    bool bloqueado = u.LockoutEnabled && u.LockoutEndDateUtc.HasValue && u.LockoutEndDateUtc.Value > DateTime.UtcNow;

                    model.Usuarios.Add(new UsuarioRolItemViewModel
                    {
                        UsuarioId = u.Id,
                        UserName = u.UserName,
                        Email = u.Email,
                        RolActual = roles.FirstOrDefault() ?? "Cliente",
                        EstaActivo = !bloqueado
                    });
                }
            }

            // 2. Cargar Pasajeros desde dbo.Pasajero
            var queryPasajeros = db.Pasajeroes.Include(p => p.TipoDocumentoIdentidad).AsQueryable();
            if (!string.IsNullOrWhiteSpace(busqueda) && model.TabActiva == "pasajeros")
            {
                string b = busqueda.Trim().ToUpperInvariant();
                queryPasajeros = queryPasajeros.Where(p =>
                    p.NumeroDocumento.Contains(b) ||
                    p.Nombres.ToUpper().Contains(b) ||
                    p.Apellidos.ToUpper().Contains(b) ||
                    p.Correo.ToUpper().Contains(b));
            }
            model.Pasajeros = queryPasajeros.OrderByDescending(p => p.PasajeroId).ToList();

            // 3. Cargar Reservas desde dbo.Reserva
            var queryReservas = db.Reservas
                .Include(r => r.EstadoReserva)
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Pasajero))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                .Include(r => r.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                .Include(r => r.Compras)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda) && model.TabActiva == "reservas")
            {
                string b = busqueda.Trim().ToUpperInvariant();
                queryReservas = queryReservas.Where(r =>
                    r.CodigoReserva.Contains(b) ||
                    r.ReservaPasajeroVueloes.Any(a => a.Pasajero.NumeroDocumento.Contains(b) || a.Pasajero.Apellidos.ToUpper().Contains(b)));
            }

            if (model.FiltroEstadoReserva != "TODOS")
            {
                queryReservas = queryReservas.Where(r => r.EstadoReservaCodigo == model.FiltroEstadoReserva);
            }

            model.Reservas = queryReservas.OrderByDescending(r => r.FechaCreacion).Take(50).ToList();

            return View(model);
        }

        // POST: PasajerosAdmin/CambiarRolUsuario (Exclusivo SuperUsuario - RN-02 / RG-01)
        [HttpPost]
        [Authorize(Roles = "SuperUsuario")]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarRolUsuario(string usuarioId, string nuevoRol)
        {
            if (string.IsNullOrWhiteSpace(usuarioId) || string.IsNullOrWhiteSpace(nuevoRol))
            {
                TempData["MensajeError"] = "Datos de usuario o rol inválidos.";
                return RedirectToAction("Index", new { tab = "usuarios" });
            }

            using (var identityCtx = new ApplicationDbContext())
            {
                var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityCtx));
                var user = userManager.FindById(usuarioId);
                if (user == null)
                {
                    TempData["MensajeError"] = "No se encontró el usuario especificado.";
                    return RedirectToAction("Index", new { tab = "usuarios" });
                }

                var rolesActuales = userManager.GetRoles(user.Id).ToArray();
                if (rolesActuales.Any())
                {
                    userManager.RemoveFromRoles(user.Id, rolesActuales);
                }
                userManager.AddToRole(user.Id, nuevoRol);

                TempData["MensajeExito"] = $"El rol del usuario '{user.UserName}' fue actualizado a '{nuevoRol}' (Política RN-02).";
            }

            return RedirectToAction("Index", new { tab = "usuarios" });
        }

        // POST: PasajerosAdmin/AlternarEstadoUsuario (Exclusivo SuperUsuario - RN-02)
        [HttpPost]
        [Authorize(Roles = "SuperUsuario")]
        [ValidateAntiForgeryToken]
        public ActionResult AlternarEstadoUsuario(string usuarioId)
        {
            using (var identityCtx = new ApplicationDbContext())
            {
                var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityCtx));
                var user = userManager.FindById(usuarioId);
                if (user != null)
                {
                    bool estabaBloqueado = user.LockoutEnabled && user.LockoutEndDateUtc.HasValue && user.LockoutEndDateUtc.Value > DateTime.UtcNow;
                    user.LockoutEnabled = true;
                    user.LockoutEndDateUtc = estabaBloqueado ? (DateTime?)null : DateTime.UtcNow.AddYears(10);
                    identityCtx.SaveChanges();

                    TempData["MensajeExito"] = estabaBloqueado
                        ? $"Cuenta '{user.UserName}' reactivada exitosamente."
                        : $"Cuenta '{user.UserName}' desactivada temporalmente.";
                }
            }

            return RedirectToAction("Index", new { tab = "usuarios" });
        }

        // POST: PasajerosAdmin/CrearUsuarioInterno (Exclusivo SuperUsuario - RN-02)
        [HttpPost]
        [Authorize(Roles = "SuperUsuario")]
        [ValidateAntiForgeryToken]
        public ActionResult CrearUsuarioInterno(string userName, string email, string password, string rol)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["MensajeError"] = "Complete todos los campos para registrar al nuevo usuario.";
                return RedirectToAction("Index", new { tab = "usuarios" });
            }

            using (var identityCtx = new ApplicationDbContext())
            {
                var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityCtx));
                userManager.UserValidator = new UserValidator<ApplicationUser>(userManager)
                {
                    AllowOnlyAlphanumericUserNames = false,
                    RequireUniqueEmail = true
                };

                var nuevoUsuario = new ApplicationUser
                {
                    UserName = userName.Trim(),
                    Email = email.Trim().ToLowerInvariant(),
                    EmailConfirmed = true
                };

                var res = userManager.Create(nuevoUsuario, password);
                if (res.Succeeded)
                {
                    string rolAsignar = string.IsNullOrWhiteSpace(rol) ? "Counter" : rol;
                    userManager.AddToRole(nuevoUsuario.Id, rolAsignar);
                    TempData["MensajeExito"] = $"Usuario '{nuevoUsuario.UserName}' creado exitosamente con el rol '{rolAsignar}'.";
                }
                else
                {
                    TempData["MensajeError"] = "No se pudo crear el usuario: " + string.Join("; ", res.Errors);
                }
            }

            return RedirectToAction("Index", new { tab = "usuarios" });
        }

        // POST: PasajerosAdmin/CancelarReservaAdmin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelarReservaAdmin(string codigoReserva)
        {
            using (var reservasCtrl = new ReservasController())
            {
                reservasCtrl.ControllerContext = this.ControllerContext;
                reservasCtrl.Cancelar(codigoReserva, "Cancelación administrativa (Pantalla 20)");
            }
            return RedirectToAction("Index", new { tab = "reservas" });
        }

        // GET: PasajerosAdmin/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pasajero pasajero = db.Pasajeroes
                .Include(p => p.TipoDocumentoIdentidad)
                .Include(p => p.ReservaPasajeroVueloes.Select(a => a.Reserva))
                .Include(p => p.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto))
                .Include(p => p.ReservaPasajeroVueloes.Select(a => a.Vuelo.Frecuencia.Ruta.Aeropuerto1))
                .FirstOrDefault(p => p.PasajeroId == id.Value);
            if (pasajero == null)
            {
                return HttpNotFound();
            }
            return View(pasajero);
        }

        // GET: PasajerosAdmin/Create
        public ActionResult Create()
        {
            ViewBag.TipoDocumentoCodigo = new SelectList(db.TipoDocumentoIdentidads, "Codigo", "Descripcion", "CEDULA");
            return View(new Pasajero { FechaNacimiento = new DateTime(1990, 1, 1) });
        }

        // POST: PasajerosAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "PasajeroId,TipoDocumentoCodigo,NumeroDocumento,Nombres,Apellidos,FechaNacimiento,Correo,Telefono")] Pasajero pasajero)
        {
            ValidarPasajero(pasajero, esEdicion: false);

            if (ModelState.IsValid)
            {
                pasajero.FechaRegistro = DateTime.Now;
                db.Pasajeroes.Add(pasajero);
                db.SaveChanges();
                TempData["MensajeExito"] = $"Pasajero {pasajero.Nombres} {pasajero.Apellidos} registrado exitosamente.";
                return RedirectToAction("Index", new { tab = "pasajeros" });
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "PasajeroId,TipoDocumentoCodigo,NumeroDocumento,Nombres,Apellidos,FechaNacimiento,Correo,Telefono,FechaRegistro")] Pasajero pasajero)
        {
            ValidarPasajero(pasajero, esEdicion: true);

            if (ModelState.IsValid)
            {
                var pasDb = db.Pasajeroes.Find(pasajero.PasajeroId);
                if (pasDb == null) return HttpNotFound();

                pasDb.TipoDocumentoCodigo = pasajero.TipoDocumentoCodigo;
                pasDb.NumeroDocumento = pasajero.NumeroDocumento;
                pasDb.Nombres = pasajero.Nombres;
                pasDb.Apellidos = pasajero.Apellidos;
                pasDb.FechaNacimiento = pasajero.FechaNacimiento;
                pasDb.Correo = pasajero.Correo;
                pasDb.Telefono = pasajero.Telefono;

                db.SaveChanges();
                TempData["MensajeExito"] = $"Datos del pasajero {pasDb.Nombres} {pasDb.Apellidos} actualizados correctamente.";
                return RedirectToAction("Index", new { tab = "pasajeros" });
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
            Pasajero pasajero = db.Pasajeroes
                .Include(p => p.TipoDocumentoIdentidad)
                .Include(p => p.ReservaPasajeroVueloes)
                .FirstOrDefault(p => p.PasajeroId == id.Value);
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
            Pasajero pasajero = db.Pasajeroes.Include(p => p.ReservaPasajeroVueloes).FirstOrDefault(p => p.PasajeroId == id);
            if (pasajero == null) return RedirectToAction("Index", new { tab = "pasajeros" });

            if (pasajero.ReservaPasajeroVueloes.Any())
            {
                TempData["MensajeError"] = $"No se puede eliminar al pasajero {pasajero.Nombres} {pasajero.Apellidos} porque tiene {pasajero.ReservaPasajeroVueloes.Count} reserva(s) asociada(s) en el historial.";
                return RedirectToAction("Index", new { tab = "pasajeros" });
            }

            db.Pasajeroes.Remove(pasajero);
            db.SaveChanges();
            TempData["MensajeExito"] = "Registro de pasajero eliminado correctamente.";
            return RedirectToAction("Index", new { tab = "pasajeros" });
        }

        private void ValidarPasajero(Pasajero p, bool esEdicion)
        {
            p.Nombres = ValidadorEcuador.NormalizarNombrePropio(p.Nombres);
            p.Apellidos = ValidadorEcuador.NormalizarNombrePropio(p.Apellidos);
            p.NumeroDocumento = ValidadorEcuador.NormalizarDocumento(p.NumeroDocumento);
            p.Correo = ValidadorEcuador.NormalizarEmail(p.Correo);
            p.Telefono = ValidadorEcuador.NormalizarTelefono(p.Telefono);

            if (!ValidadorEcuador.ValidarNombreApellido(p.Nombres, 25))
            {
                ModelState.AddModelError("Nombres", "Los nombres deben contener solo letras y máximo 25 caracteres.");
            }
            if (!ValidadorEcuador.ValidarNombreApellido(p.Apellidos, 25))
            {
                ModelState.AddModelError("Apellidos", "Los apellidos deben contener solo letras y máximo 25 caracteres.");
            }
            if (p.TipoDocumentoCodigo == "CEDULA")
            {
                if (!ValidadorEcuador.ValidarCedula(p.NumeroDocumento))
                {
                    ModelState.AddModelError("NumeroDocumento", "La cédula ecuatoriana ingresada no supera la validación Módulo 10.");
                }
            }
            else
            {
                if (!ValidadorEcuador.ValidarPasaporte(p.NumeroDocumento))
                {
                    ModelState.AddModelError("NumeroDocumento", "El pasaporte debe tener entre 6 y 15 caracteres alfanuméricos.");
                }
            }

            bool existeDoc = db.Pasajeroes.Any(x =>
                x.TipoDocumentoCodigo == p.TipoDocumentoCodigo &&
                x.NumeroDocumento == p.NumeroDocumento &&
                (!esEdicion || x.PasajeroId != p.PasajeroId));
            if (existeDoc)
            {
                ModelState.AddModelError("NumeroDocumento", $"Ya existe un pasajero registrado con el documento {p.TipoDocumentoCodigo} {p.NumeroDocumento}.");
            }

            if (!ValidadorEcuador.ValidarEmail(p.Correo))
            {
                ModelState.AddModelError("Correo", "Ingrese un correo electrónico válido.");
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
