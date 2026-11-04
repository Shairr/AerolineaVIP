using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using AerolineaVIP.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace AerolineaVIP
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Creación directa de roles, usuarios oficiales y datos semilla por código en Global.asax
            InicializarSeguridadYDatos();
        }

        private void InicializarSeguridadYDatos()
        {
            using (var contexto = new ApplicationDbContext())
            {
                var gestorRoles = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(contexto));
                var gestorUsuarios = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(contexto));

                // Configuración de validación de contraseñas para entorno académico
                gestorUsuarios.PasswordValidator = new PasswordValidator
                {
                    RequiredLength = 6,
                    RequireNonLetterOrDigit = false,
                    RequireDigit = false,
                    RequireLowercase = false,
                    RequireUppercase = false
                };

                // 1. Creación de los 4 roles oficiales por código (DP-13 / MOD-01)
                string[] roles = { "SuperUsuario", "Administrador", "Counter", "Cliente" };
                foreach (var rol in roles)
                {
                    if (!gestorRoles.RoleExists(rol))
                    {
                        gestorRoles.Create(new IdentityRole(rol));
                    }
                }

                // 2. Creación del SuperUsuario por código (soporta superusuario y superadmin)
                //CrearUsuarioSiNoExiste(gestorUsuarios, "superusuario", "super@aerolineavip.com", "Admin123*", "SuperUsuario");
                CrearUsuarioSiNoExiste(gestorUsuarios, "superadmin", "superadmin@aerolineavip.com", "Super123*", "SuperUsuario");

                // 3. Creación del Administrador por código
                CrearUsuarioSiNoExiste(gestorUsuarios, "admin01", "admin@aerolineavip.com", "Admin123*", "Administrador");

                // 4. Creación del Counter inicial por código (MOD-09 / DP-13)
                CrearUsuarioSiNoExiste(gestorUsuarios, "counter01", "counter@aerolineavip.com", "Counter123*", "Counter");

                // 5. Creación del Cliente inicial y asignación de su rol
                var cliente = CrearUsuarioSiNoExiste(gestorUsuarios, "cliente01", "cliente@aerolineavip.com", "Cliente123*", "Cliente");

                // 6. Sincronización idempotente de datos semilla de dominio (DP-01, DP-05 y reservas demo de cliente01)
                SincronizarDatosDominio(cliente?.Id);
            }
        }

        private ApplicationUser CrearUsuarioSiNoExiste(UserManager<ApplicationUser> gestorUsuarios, string nombreUsuario, string correo, string contrasena, string rol)
        {
            var usuario = gestorUsuarios.FindByName(nombreUsuario);
            if (usuario == null)
            {
                usuario = new ApplicationUser
                {
                    UserName = nombreUsuario,
                    Email = correo,
                    EmailConfirmed = true
                };

                var resultado = gestorUsuarios.Create(usuario, contrasena);
                if (resultado.Succeeded)
                {
                    gestorUsuarios.AddToRole(usuario.Id, rol);
                }
            }
            else if (!gestorUsuarios.IsInRole(usuario.Id, rol))
            {
                gestorUsuarios.AddToRole(usuario.Id, rol);
            }

            return usuario;
        }

        private void SincronizarDatosDominio(string clienteId)
        {
            try
            {
                using (var db = new AerolineaVIPEntities())
                {
                    // DP-05: Datos oficiales del emisor académico (Grupo 6)
                    var emisor = db.DatosEmisors.FirstOrDefault(e => e.DatosEmisorId == 1);
                    if (emisor != null && (emisor.Identificacion == null || emisor.Identificacion.Contains("PENDIENTE")))
                    {
                        emisor.NombreComercial = "AEROLINEA VIP S.A. - PROYECTO ACADEMICO DIT";
                        emisor.Identificacion = "1799999999001";
                        emisor.Direccion = "Aeropuerto Internacional Mariscal Sucre, Tababela - Quito, Ecuador";
                        emisor.Telefono = "(02) 399-0000";
                        emisor.Correo = "facturacion@aerolineavip.ec";
                        emisor.LeyendaComprobante = "Comprobante académico de una operación simulada. No constituye factura tributaria ni documento autorizado.";
                        emisor.FechaActualizacion = DateTime.Now;
                    }

                    // DP-01: Aeropuerto de Galápagos (Isla Baltra - GPS)
                    var aeroGps = db.Aeropuertoes.FirstOrDefault(a => a.CodigoIATA == "GPS");
                    if (aeroGps != null && (aeroGps.Nombre.Contains("supuesto") || aeroGps.Localidad.Contains("supuesto")))
                    {
                        aeroGps.Nombre = "Aeropuerto Ecológico Galápagos Seymour";
                        aeroGps.Localidad = "Isla Baltra - Galápagos";
                    }

                    // Vincular las reservas de demostración de la semilla SQL al cliente01 real
                    if (!string.IsNullOrWhiteSpace(clienteId))
                    {
                        var reservasDemo = db.Reservas
                            .Where(r => r.UsuarioId == "7c1e4a20-0001-4b6e-9a01-000000000001" || r.UsuarioId == "cliente01")
                            .ToList();

                        foreach (var res in reservasDemo)
                        {
                            res.UsuarioId = clienteId;
                        }
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Aviso al sincronizar datos de dominio: " + ex.Message);
            }
        }
    }
}
