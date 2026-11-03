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

            // Integración de las rutas para cada vista y controlador según el flujo de la aplicación, incluyendo las rutas amigables para el área administrativa.

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


            // Pantallas 11 y 12: Módulo de Compras (alias hacia el flujo de pago y confirmación)
            routes.MapRoute(
                name: "ComprasPagar",
                url: "Compras/Pagar/{reservaId}",
                defaults: new { controller = "Reservas", action = "Pagar", reservaId = UrlParameter.Optional }
            );


            routes.MapRoute(
           name: "ComprasConfirmacion",
           url: "Compras/Confirmacion/{reservaId}",
           defaults: new { controller = "Reservas", action = "Exito", reservaId = UrlParameter.Optional }
            );

            // Pantalla 14: Gestión y cancelaciones de reserva con identificador
            routes.MapRoute(
                name: "ReservasGestionar",
                url: "Reservas/Gestionar/{reservaId}",
                defaults: new { controller = "Reservas", action = "Gestionar", reservaId = UrlParameter.Optional }
            );

            // Pantalla 16: Consulta / Visualización de comprobante fiscal
            routes.MapRoute(
                name: "ComprobantesIndex",
                url: "Comprobantes/Index/{id}",
                defaults: new { controller = "Reservas", action = "Comprobante", id = UrlParameter.Optional }
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


            //ruta predeterminada

            routes.MapRoute(
              name: "Default",
              url: "{controller}/{action}/{id}",
              defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
          );


        }
    }
}
