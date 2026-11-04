using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace AerolineaVIP
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Pantalla 7: Consulta de disponibilidad por vuelo
            routes.MapRoute(
                name: "VuelosDisponibilidad",
                url: "Vuelos/Disponibilidad/{id}",
                defaults: new { controller = "Vuelos", action = "Disponibilidad", id = UrlParameter.Optional }
            );

            // Pantalla 8: Selección de vuelo y asiento (soporta nombre singular oficial y plural)
            routes.MapRoute(
                name: "ReservasSeleccionAsiento",
                url: "Reservas/SeleccionAsiento/{vueloId}",
                defaults: new { controller = "Reservas", action = "SeleccionarAsientos", vueloId = UrlParameter.Optional }
            );

            // Pantallas 11 y 12: Módulo de Compras (ComprasController dedicado)
            routes.MapRoute(
                name: "ComprasPagar",
                url: "Compras/Pagar/{reservaId}",
                defaults: new { controller = "Compras", action = "Pagar", reservaId = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "ComprasConfirmacion",
                url: "Compras/Confirmacion/{codigoReserva}",
                defaults: new { controller = "Compras", action = "Confirmacion", codigoReserva = UrlParameter.Optional }
            );

            // Pantalla 14: Gestión y cancelaciones de reserva con identificador
            routes.MapRoute(
                name: "ReservasGestionar",
                url: "Reservas/Gestionar/{id}",
                defaults: new { controller = "Reservas", action = "Gestionar", id = UrlParameter.Optional }
            );

            // Pantalla 16: Consulta y emisión de comprobantes académicos (ComprobantesController dedicado)
            routes.MapRoute(
                name: "ComprobantesIndex",
                url: "Comprobantes/Index/{id}",
                defaults: new { controller = "Comprobantes", action = "Index", id = UrlParameter.Optional }
            );

            // Pantallas 18, 19 y 20: Rutas amigables del Área Administrativa (/Admin/...)
            routes.MapRoute(
                name: "AdminVuelos",
                url: "Admin/Vuelos/{action}/{id}",
                defaults: new { controller = "VuelosAdmin", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "AdminAeronaves",
                url: "Admin/Aeronaves/{action}/{id}",
                defaults: new { controller = "AeronavesAdmin", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "AdminGestion",
                url: "Admin/Gestion/{action}/{id}",
                defaults: new { controller = "PasajerosAdmin", action = "Index", id = UrlParameter.Optional }
            );

            // Ruta predeterminada
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
