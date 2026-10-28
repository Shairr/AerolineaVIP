using System.Web.Mvc;

namespace AerolineaVIP.Controllers

// Controlador para la gestión de funcionalidades administrativas para el wireframe 17 y 21. 

{
    // Solo protección de acceso a nivel de roles
    [Authorize(Roles = "Administrador,SuperUsuario")]
    public class AdminController : Controller
    {
        // GET: /Admin/Index (Pantalla 17 - Menú de Accesos)
        public ActionResult Index()
        {
            return View();
        }

        // GET: /Admin/Reportes (Pantalla 21 - Wireframe de Reportes)
        public ActionResult Reportes()
        {
            return View();
        }
    }
}