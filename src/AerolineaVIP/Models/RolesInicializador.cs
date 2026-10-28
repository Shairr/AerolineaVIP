using System;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using AerolineaVIP.Models;

namespace AerolineaVIP.Models
{
    public static class RolesInicializador
    {
        public static void InicializarRolesYUsuarios()
        {
            var contexto = new ApplicationDbContext();
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

            // 1. Requisito 4: Creación de los 3 roles oficiales por código
            string[] roles = { "SuperUsuario", "Administrador", "Cliente" };
            foreach (var rol in roles)
            {
                if (!gestorRoles.RoleExists(rol))
                {
                    gestorRoles.Create(new IdentityRole(rol));
                }
            }

            // 2. Requisito 2: Creación del SuperUsuario por código
            CrearUsuarioSiNoExiste(gestorUsuarios, "superusuario", "super@aerolineavip.com", "Admin123*", "SuperUsuario");

            // 3. Requisito 3: Creación del Administrador por código
            CrearUsuarioSiNoExiste(gestorUsuarios, "admin01", "admin@aerolineavip.com", "Admin123*", "Administrador");

            // 4. Requisitos 5 y 6: Creación del Cliente inicial y asignación de su rol
            CrearUsuarioSiNoExiste(gestorUsuarios, "cliente01", "cliente@aerolineavip.com", "Cliente123*", "Cliente");
        }

        private static void CrearUsuarioSiNoExiste(UserManager<ApplicationUser> gestorUsuarios, string nombreUsuario, string correo, string contrasena, string rol)
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
        }
    }
}