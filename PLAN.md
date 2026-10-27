# PLAN MAESTRO DE IMPLEMENTACIÓN TÉCNICA Y ESTRATEGIA GIT/GITHUB
## Sistema Web Monolítico para Aerolínea VIP (ASP.NET Framework 4.8.1 MVC)
### Universidad de las Fuerzas Armadas ESPE — Grupo 6 (Denilson Celi & Farid Hinojoza)

---

## 1. OBJETIVO DEL PLAN

Establecer la hoja de ruta técnica y metodológica para el desarrollo ágil, colaborativo y profesional del proyecto web monolítico en **Visual Studio con ASP.NET Framework 4.8.1 MVC, Razor, Bootstrap, jQuery, ASP.NET Identity 2.x y SQL Server**. 

El plan define la **estrategia de ramas en GitHub**, la **distribución estricta de tareas entre Denilson y Farid** para evitar colisiones de código, y la **guía paso a paso para cumplir los 12 requisitos del Punto 4 de `Instrucciones_Primera_Entrega.pdf`** e integrar las **21 interfaces Razor basadas en los wireframes oficiales**, gobernadas bajo los **tres (3) roles y usuarios oficiales del sistema: SuperUsuario, Administrador y Cliente**.

---

## 2. ESTRATEGIA PROFESIONAL DE GIT Y GITHUB (GIT-FLOW ADAPTADO)

Para que el repositorio en GitHub refleje el estándar de un equipo de ingeniería de software profesional, se utilizará una arquitectura de ramas estructurada:

```
[main] (Estable / Entrega final v1.0)
   ▲
   │ (Pull Request Final)
[develop] (Integración continua del equipo)
   ▲                     ▲
   │ (Pull Request #1)   │ (Pull Request #2)
[feature/identity-...] [feature/public-home-...]
   (Denilson)               (Farid)
```

### 2.1. Estructura de Ramas

1. **`main`:**
   * Contiene el código 100% estable, probado y listo para entrega.
   * **Regla:** Nadie hace `git push` directo a `main`. Solo recibe fusiones (*Merge*) desde `develop` al finalizar la versión entregable.
2. **`develop`:**
   * Es la rama de integración continua donde convergen las características terminadas.
   * Sirve como base para crear las ramas de trabajo y para validar la compilación conjunta.
3. **Ramas de Funcionalidad (`feature/<nombre-descriptivo>`):**
   * Ramas de vida corta que parten desde `develop` para desarrollar un bloque específico.
   * Se integran a `develop` mediante un **Pull Request (PR)** con revisión del compañero.

### 2.2. Nomenclatura Estándar de Ramas para el Grupo 6

| Integrante | Rama Feature | Propósito Técnico |
| :--- | :--- | :--- |
| **Denilson** | `feature/identity-security-seed` | Creación de los 3 roles y 3 usuarios iniciales por código, y seguridad en registro/login. |
| **Farid** | `feature/layout-avianca-home` | Layout corporativo Avianca (`_Layout.cshtml`, navbar, estilos) y Pantalla 1 (`Home/Index`). |
| **Farid** | `feature/vuelos-search-wireframes` | Pantallas públicas 4, 5, 6 y 7 (recuperación, buscador, resultados y mapa de cabina en solo lectura). |
| **Denilson** | `feature/ef-domain-scaffolding` | Conexión a `AerolineaVIP`, modelo Entity Framework (EDMX) y CRUD con scaffolding (Pantallas 18, 19, 20). |
| **Farid** | `feature/booking-funnel-wireframes` | Embudo de compra: Pantallas 8, 9, 10, 11 (modal pago simulado), 12, 13, 14, 15 y 16. |
| **Denilson** | `feature/admin-menu-reports` | Pantalla 17 (Panel Admin como menú de accesos) y Pantalla 21 (Reportes tabulares). |

### 2.3. Estándar de Mensajes de Commit (Conventional Commits)
Para dar una imagen profesional en el historial de GitHub, se utilizará el estándar:
* `feat:` Nueva funcionalidad o vista (ej: `feat(views): implement wireframe for seat selection screen 8`).
* `fix:` Corrección de bugs o errores (ej: `fix(auth): restrict public registration to Client role`).
* `style:` Ajustes visuales, CSS o layout (ej: `style(navbar): apply Avianca red palette #E1251B`).
* `docs:` Documentación o comentarios (ej: `docs: add database execution guide`).
* `chore:` Tareas de configuración, paquetes NuGet o `.gitignore` (ej: `chore: add Visual Studio gitignore`).

---

## 3. CONFIGURACIÓN INICIAL DEL REPOSITORIO (PASO CERO CONJUNTO)

Este paso debe realizarlo **uno de los dos integrantes** (ej. Denilson) y el otro clonarlo:

```bash
# 1. Crear repositorio local y configurar .gitignore estricto
git init
```

### 3.1. `.gitignore` Obligatorio para ASP.NET Framework
Es **imprescindible** que el `.gitignore` ignore carpetas pesadas para cumplir la restricción oficial de no subir binarios al ZIP ni a GitHub:
```gitignore
## Visual Studio y ASP.NET Framework
.vs/
[Bb]in/
[Oo]bj/
packages/
*.user
*.suo
*.userosscache
*.sln.docstates
```

### 3.2. Creación del Proyecto en Visual Studio 2022
1. Abrir Visual Studio 2022 $\to$ **Crear un nuevo proyecto**.
2. Seleccionar: **Aplicación web ASP.NET (.NET Framework)** en C#.
3. Nombre de la solución: `AerolineaVIP` (Ubicada en la raíz del repositorio).
4. Versión del Framework: **.NET Framework 4.8.1**.
5. Plantilla: **MVC**.
6. **Autenticación:** Hacer clic en "Cambiar autenticación" y elegir **Cuentas de usuario individuales** (*Individual User Accounts*). Esto creará automáticamente la infraestructura de ASP.NET Identity 2.x.
7. Subir a GitHub:
```bash
git add .
git commit -m "chore: initial ASP.NET Framework 4.8.1 MVC project structure"
git branch -M main
git remote add origin https://github.com/TU_USUARIO/AerolineaVIP.git
git push -u origin main

# Crear y publicar rama develop
git checkout -b develop
git push -u origin develop
```

---

## 4. GUÍA DETALLADA: CUMPLIMIENTO PASO A PASO DEL PUNTO 4 (INSTRUCCIONES)

El Punto 4 de `Instrucciones_Primera_Entrega.pdf` enumera 12 requerimientos obligatorios. A continuación se detalla cómo implementarlos técnicamente:

### Requisito 1: Página de inicio Home/Index con diseño propio del grupo
* **Asignado a:** Farid.
* **Controlador:** `Controllers/HomeController.cs` $\to$ Acción `Index()`.
* **Vista:** `Views/Home/Index.cshtml`.
* **Implementación:**
  * Adoptar la barra superior compartida (`_Layout.cshtml`) con los colores corporativos de **Avianca** (Rojo `#E1251B`, Fondo Blanco `#FFFFFF`, Textos Antracita `#53565A`).
  * Hero Section con título de impacto: *"Vuela a tu próximo destino — Viajes privados con atención personalizada"*.
  * Formulario visual rápido "Encuentra tu vuelo" (Origen: Quito por defecto, Destino dependiente, Fecha de salida, Pasajeros de 1 a 7) con botón de acción en Rojo Avianca "Buscar vuelos".
  * Tarjetas informativas de los destinos autorizados (Guayaquil: mar/jue/sáb, Manta: lun/mié/vie, Galápagos: domingos con escala).

---

### Requisitos 2, 3, 4, 5 y 6: Creación de Roles y Usuarios Iniciales mediante Código (3 Roles Oficiales)
* **Asignado a:** Denilson.
* **Ubicación Técnica recomendada:** Clase de inicialización `Models/IdentitySeeder.cs` llamada desde `Global.asax.cs` (`Application_Start`).
* **Regla estricta:** Conforme a `Instrucciones_Primera_Entrega.pdf` y la verificación de `ENUNCIADO PROYECTO.pdf` (apartado 13.7.1), el sistema se estructura sobre **exactamente tres (3) roles y usuarios iniciales**:
  1. **`SuperUsuario`** (Acceso total por código, administración global y asignación de roles).
  2. **`Administrador`** (Acceso restringido a las operaciones CRUD de vuelos, flota y reservas).
  3. **`Cliente`** (Usuario del portal público con acceso a reservas propias, compras y check-in).
* **Implementación del Helper:**
  Crear la clase `Models/IdentitySeeder.cs`:

```csharp
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using AerolineaVIP.Models;
using System;

namespace AerolineaVIP
{
    public static class IdentitySeeder
    {
        public static void SeedRolesAndUsers()
        {
            var context = new ApplicationDbContext();
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

            // 1. Requisito 4: Creación de los 3 roles oficiales por código
            string[] roles = { "SuperUsuario", "Administrador", "Cliente" };
            foreach (var rol in roles)
            {
                if (!roleManager.RoleExists(rol))
                {
                    roleManager.Create(new IdentityRole(rol));
                }
            }

            // 2. Requisito 2: Creación del SuperUsuario por código
            CreateUserIfNotExists(userManager, "superusuario", "super@aerolineavip.com", "SuperAdmin2026*", "SuperUsuario");

            // 3. Requisito 3: Creación del Administrador por código
            CreateUserIfNotExists(userManager, "admin01", "admin@aerolineavip.com", "AdminPass2026*", "Administrador");

            // 4. Requisitos 5 y 6: Creación del Cliente inicial de prueba y asignación de rol
            CreateUserIfNotExists(userManager, "cliente01", "cliente@aerolineavip.com", "ClientePass2026*", "Cliente");
        }

        private static void CreateUserIfNotExists(UserManager<ApplicationUser> userManager, string userName, string email, string password, string role)
        {
            var user = userManager.FindByName(userName);
            if (user == null)
            {
                user = new ApplicationUser { UserName = userName, Email = email };
                var result = userManager.Create(user, password);
                if (result.Succeeded)
                {
                    userManager.AddToRole(user.Id, role);
                }
            }
        }
    }
}
```
* **Llamada en `Global.asax.cs`:**
```csharp
protected void Application_Start()
{
    AreaRegistration.RegisterAllAreas();
    FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
    RouteConfig.RegisterRoutes(RouteTable.Routes);
    BundleConfig.RegisterBundles(BundleTable.Bundles);

    // Inicialización por código de roles y usuarios oficiales
    IdentitySeeder.SeedRolesAndUsers();
}
```

---

### Requisito 7: CRUD con Scaffolding para Tablas Administrables
* **Asignado a:** Denilson.
* **Tablas a Administrar:** `Vuelo`, `Aeronave`, `Pasajero`, `Reserva`.
* **Procedimiento en Visual Studio:**
  1. Agregar Entity Data Model: Click derecho en `Models` $\to$ **Agregar** $\to$ **Nuevo Elemento** $\to$ **Datos** $\to$ **ADO.NET Entity Data Model** (`AerolineaVIPModel.edmx`).
  2. Seleccionar "EF Designer desde base de datos", conectar a la base `AerolineaVIP` y seleccionar las tablas.
  3. Crear Controladores con Scaffolding:
     * Click derecho en `Controllers` $\to$ **Agregar** $\to$ **Controlador...**
     * Elegir: **Controlador de MVC 5 con vistas que usan Entity Framework**.
     * Generar scaffolding para:
       * `VueloController` (administrará la Pantalla 18).
       * `AeronaveController` (administrará la Pantalla 19).
       * `GestionController` (administrará usuarios, pasajeros y reservas para la Pantalla 20).
  4. **Ajuste visual:** Modificar las vistas generadas en `Views/*` para que utilicen el `_Layout.cshtml` general del proyecto con los estilos de Bootstrap y la paleta de Avianca, protegiendo las acciones con `[Authorize(Roles = "Administrador,SuperUsuario")]`.

---

### Requisito 8: Página de Inicio de Sesión
* **Asignado a:** Denilson.
* **Controlador:** `Controllers/AccountController.cs` $\to$ `Login()`.
* **Vista:** `Views/Account/Login.cshtml` (Pantalla 2).
* **Implementación:**
  * Formulario centrado con campos de Usuario/Correo y Contraseña, casilla "Recordarme" y botón de acción en Rojo Avianca.
  * Valida credenciales en servidor con `SignInManager.PasswordSignInAsync`.
  * Redirección inteligente: si el rol es `Administrador` o `SuperUsuario`, redirige a `/Admin/Index` (Pantalla 17); si es `Cliente`, redirige a su área de reservas `/Reservas/MisReservas` (Pantalla 13) o al `ReturnUrl`.
  * Mensaje genérico de error de seguridad ante fallo: *"Usuario o contraseña incorrectos"*.

---

### Requisito 9: Página de Registro y Seguridad Estricta de Roles
* **Asignado a:** Denilson.
* **Controlador:** `Controllers/AccountController.cs` $\to$ `Register()`.
* **Vista:** `Views/Account/Register.cshtml` (Pantalla 3).
* **Regla Crítica de Seguridad del Instructivo:**
  > *"La creación pública de cuentas no puede permitir que un visitante se registre como administrador o superusuario. Los privilegios elevados se asignan solo mediante la inicialización por código o un mecanismo restringido al superusuario, y se validan en el servidor."*
* **Implementación en el Controlador (`AccountController.cs`):**
```csharp
[HttpPost]
[AllowAnonymous]
[ValidateAntiForgeryToken]
public async Task<ActionResult> Register(RegisterViewModel model)
{
    if (ModelState.IsValid)
    {
        var user = new ApplicationUser { UserName = model.UserName, Email = model.Email };
        var result = await UserManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // SEGURIDAD CRÍTICA:
            // Si el usuario actual que registra NO está autenticado como SuperUsuario,
            // SIEMPRE se fuerza el rol "Cliente", ignorando cualquier valor manipulado.
            string assignedRole = "Cliente";
            if (User.Identity.IsAuthenticated && User.IsInRole("SuperUsuario") && !string.IsNullOrEmpty(model.SelectedRole))
            {
                assignedRole = model.SelectedRole;
            }

            await UserManager.AddToRoleAsync(user.Id, assignedRole);

            if (!User.Identity.IsAuthenticated)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("Index", "Admin");
        }
        AddErrors(result);
    }
    return View(model);
}
```

---

### Requisito 10: Implementación de las 21 Interfaces a nivel de Wireframe
* **Distribución:**
  * **Farid:** Pantallas 1, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16.
  * **Denilson:** Pantallas 2, 3, 17, 18, 19, 20, 21.
* **Alcance en Primera Entrega:**
  Conforme a las directrices oficiales, para las pantallas 4 a 21 **no se desarrollará el backend transaccional completo en esta fase**, sino que se construirán las vistas Razor con el layout general compartido, formularios con controles HTML correspondientes, tablas de datos y componentes visuales fieles al documento `Wireframes_21_Interfaces_Aerolinea_VIP.pdf`.

---

### Requisito 11: Implementación del Mapa de Navegación (11.4.2)
* **Asignado a:** Coordinación conjunta (Farid en Navbar, Denilson en controladores/autorización).
* **Implementación según los 3 Roles del Sistema:**
  * La barra superior compartida (`_Layout.cshtml`) contiene enlaces condicionales según el rol autenticado:
    * **Visitante Anónimo (Público):** Inicio, Búsqueda de Vuelos, Iniciar Sesión, Registro.
    * **Cliente:** Inicio, Búsqueda de Vuelos, Mis Reservas (P13), Check-in (P15), Comprobantes (P16).
    * **Administrador / SuperUsuario:** Inicio, Menú de Administración (`/Admin/Index` - P17) con accesos directos a Vuelos (P18), Aeronaves (P19), Gestión de Usuarios (P20) y Reportes (P21).

---

### Requisito 12: Integración de la Base de Datos y Cadenas de Conexión
* **Asignado a:** Denilson (configuración de `Web.config`) y compartido con Farid.
* **Separación de Bases de Datos:**
  * Se deben configurar **dos cadenas de conexión separadas** en `Web.config`:
```xml
<connectionStrings>
  <!-- Base de datos de Identidad (ASP.NET Identity 2.x) -->
  <add name="DefaultConnection" 
       connectionString="Data Source=.;Initial Catalog=AerolineaVIP_Identity;Integrated Security=True" 
       providerName="System.Data.SqlClient" />
       
  <!-- Base de datos de Dominio (Vuelos, Pasajeros, Reservas) -->
  <add name="AerolineaVIPEntities" 
       connectionString="metadata=res://*/Models.AerolineaVIPModel.csdl|res://*/Models.AerolineaVIPModel.ssdl|res://*/Models.AerolineaVIPModel.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=.;initial catalog=AerolineaVIP;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework&quot;" 
       providerName="System.Data.EntityClient" />
</connectionStrings>
```

---

## 5. GUÍA PARA LA IMPLEMENTACIÓN DE LOS 21 WIREFRAMES

Para garantizar una construcción visual armoniosa y sin retrabajos de las 21 pantallas, se establecen las siguientes pautas técnicas:

### 5.1. Componentes Modulares Compartidos (Vistas Parciales)
En lugar de repetir código en cada vista, se crearán vistas parciales reutilizables en `Views/Shared/`:
1. `_Navbar.cshtml`: Barra de navegación superior fija con logotipo corporativo y enlaces dinámicos según `User.IsInRole("Administrador")` o `User.IsInRole("Cliente")`.
2. `_Footer.cshtml`: Pie de página institucional compartido con datos de contacto y leyenda de derechos.
3. `_MapaAsientos.cshtml`: Componente visual que dibuja la cabina del Embraer Phenom 300E con los 7 asientos individuales (`1D` a `4I`) y la leyenda de colores (Verde, Morado, Rojo). Se reutiliza en la Pantalla 7 (Disponibilidad), Pantalla 8 (Selección interactiva) y Pantalla 19 (Consulta de aeronave).
4. `_PasosReserva.cshtml`: Barra horizontal que indica los 4 pasos del embudo de compra (*1 Asientos $\to$ 2 Pasajeros $\to$ 3 Reserva $\to$ 4 Pago*) para las pantallas 8, 9, 10 y 11.
5. `_ModalConfirmarPago.cshtml`: Ventana modal de Bootstrap para confirmar la simulación de pago en la Pantalla 11.

### 5.2. Mapa de Asientos del Phenom 300E (Semántica Visual Inmutable)
El componente Razor del mapa de cabina debe estructurarse utilizando contenedores CSS flexibles o grid:
```html
<div class="avion-cabina">
    <div class="fila fila-1">
        <div class="espacio-puerta">Puerta de acceso</div>
        <button type="button" class="asiento asiento-ocupado" disabled>1D (Ocupado)</button>
    </div>
    <div class="fila fila-2">
        <button type="button" class="asiento asiento-reservado" disabled>2I (Reservado)</button>
        <button type="button" class="asiento asiento-disponible">2D (Disponible)</button>
    </div>
    <!-- Filas 3 y 4 con mesas y baño posterior -->
</div>
<div class="leyenda-asientos mt-3">
    <span class="badge bg-success">Verde: Disponible</span>
    <span class="badge bg-purple" style="background-color: #6F42C1;">Morado: Reservado</span>
    <span class="badge bg-danger">Rojo: Ocupado</span>
</div>
```

---

## 6. PREVENCIÓN DE CONFLICTOS DE CÓDIGO (MERGE CONFLICTS)

Trabajar en paralelo en proyectos de Visual Studio `.NET Framework` suele generar conflictos críticos en archivos compartidos si no se toman precauciones:

### 6.1. El peligro del archivo de proyecto (`.csproj`)
* **Problema:** Cada vez que un integrante agrega una vista, un controlador o una clase en Visual Studio, el IDE modifica el archivo XML del proyecto (`AerolineaVIP.csproj`). Si ambos integrantes agregan archivos al mismo tiempo en ramas diferentes, se produce un conflicto de merge en el `.csproj`.
* **Solución y Regla de Oro:**
  1. Denilson y Farid trabajarán en **carpetas y controladores completamente separados**:
     * Denilson solo crea archivos en `Controllers/Admin/`, `Controllers/Account/`, `Views/Admin/`, `Views/Account/`.
     * Farid solo crea archivos en `Controllers/Vuelos/`, `Controllers/Reservas/`, `Controllers/Compras/`, `Views/Vuelos/`, etc.
  2. Si ocurre un conflicto en el `.csproj` durante un Merge en Git, resolverlo aceptando ambas inclusiones de archivos `<Compile Include="...">` o `<Content Include="...">`.

### 6.2. Protección de Archivos Compartidos (`_Layout.cshtml`, `BundleConfig.cs`, `Web.config`)
* **Regla:** **Farid** será el responsable principal de maquetar `_Layout.cshtml` y los estilos CSS corporativos.
* Denilson no modificará el HTML de `_Layout.cshtml` directamente; simplemente utilizará `@RenderBody()` para incrustar sus vistas administrativas.
* Para los estilos del administrador, Denilson creará un archivo CSS independiente (`Content/admin.css`) que se invocará mediante una sección opcional `@RenderSection("Styles", required: false)`.

---

## 7. PLAN DE ACCIÓN Y DIVISIÓN DE TAREAS POR SPRINT (4 DÍAS)

| Día | Tareas de Denilson (Seguridad, BD y Admin) | Tareas de Farid (UI, Portal Público y Embudo) | Punto de Sincronización Conjunto |
| :---: | :--- | :--- | :--- |
| **Día 1** | • Inicializar solución MVC 4.8.1 y `.gitignore`.<br>• Crear repositorio GitHub y rama `develop`.<br>• Ejecutar `Script_BD_AerolineaVIP.sql` en SQL Server.<br>• Configurar `Web.config` con las dos cadenas de conexión. | • Clonar repositorio desde GitHub.<br>• Diseñar paleta Avianca en `Content/site.css`.<br>• Construir `_Layout.cshtml` con barra superior fija.<br>• Crear parciales `_Navbar.cshtml` y `_Footer.cshtml`. | **Pull Request #1:** Integración de la plantilla base limpia y layout general en `develop`. |
| **Día 2** | • Rama `feature/identity-security-seed`.<br>• Crear `IdentitySeeder.cs` con los 3 roles y 3 usuarios iniciales por código (`superusuario`, `admin01`, `cliente01`).<br>• Modificar `Register` para asegurar que el registro público sea solo `Cliente`.<br>• Construir vista de Login (P2) y Registro (P3). | • Rama `feature/public-home-search`.<br>• Construir Pantalla 1 (`Home/Index`).<br>• Construir wireframe Pantalla 4 (`ForgotPassword`).<br>• Construir wireframe Pantalla 5 (`Vuelos/Buscar`).<br>• Construir wireframe Pantalla 6 (`Vuelos/Resultados`). | **Pull Request #2:** Mergear Login/Registro de Denilson y Home/Búsqueda de Farid en `develop`. |
| **Día 3** | • Rama `feature/ef-domain-scaffolding`.<br>• Generar modelo EF EDMX desde `AerolineaVIP`.<br>• Scaffolding de Vuelos (P18), Aeronaves (P19) y Gestión (P20).<br>• Ajustar vistas CRUD al layout del proyecto y proteger con `[Authorize]`. | • Rama `feature/booking-funnel-seatmap`.<br>• Crear parcial de cabina `_MapaAsientos.cshtml`.<br>• Wireframe Pantalla 7 (Disponibilidad en solo lectura).<br>• Wireframes del embudo: P8 (Asientos), P9 (Pasajeros), P10 (Confirmar reserva).<br>• Wireframe P11 (Pago con modal simulado) y P12 (Confirmación). | **Pull Request #3:** Pruebas cruzadas de scaffolding y embudo de selección de asientos. |
| **Día 4** | • Rama `feature/admin-menu-reports`.<br>• Construir Pantalla 17 (`Admin/Index` menú accesos).<br>• Construir wireframe Pantalla 21 (`Admin/Reportes` tablas).<br>• Probar inicio de sesión con `superusuario` y `admin01`. | • Rama `feature/client-portal-checkin`.<br>• Wireframes P13 (`MisReservas`) y P14 (`Gestionar/Cancelar`).<br>• Wireframe P15 (`CheckIn`) y P16 (`Comprobantes` PDF).<br>• Probar inicio de sesión con `cliente01`. | **Pull Request Final:** Merge de `develop` a `main`, etiquetado `v1.0-entrega1` y generación del `.zip` limpio. |

---

## 8. PREPARACIÓN DEL ENTREGABLE ZIP (PUNTO 1 DE INSTRUCCIONES)

Al finalizar la integración y comprobar que la aplicación compila y se ejecuta en Visual Studio:
1. En Visual Studio, ejecutar: **Compilar $\to$ Limpiar solución**.
2. Cerrar Visual Studio para liberar bloqueos de archivos.
3. Abrir la carpeta del proyecto y verificar que se eliminen manualmente las carpetas temporales:
   * Eliminar todas las carpetas `bin` y `obj`.
   * Eliminar la carpeta `packages` (NuGet las restaura automáticamente).
   * Eliminar la carpeta oculta `.vs`.
4. Incluir dentro de la raíz del proyecto el script SQL: `Script_BD_AerolineaVIP.sql`.
5. Comprimir la solución en un archivo `.zip` cuyo nombre cumpla la estructura oficial:
   `PRIMERA ENTREGA - PROYECTO PRIMER PARCIAL - GRUPO Nro 06.zip`.
6. Verificar que el archivo comprimido se abra y descomprima limpiamente.
