# ✈️ Aerolínea VIP — Sistema Web de Compra y Reserva de Pasajes

Bienvenido al repositorio del proyecto **Aerolínea VIP**, una plataforma web desarrollada en **ASP.NET MVC 5** (.NET Framework 4.8.1) y **Microsoft SQL Server**, diseñada para la reserva, compra simulada, emisión de comprobantes académicos, check-in y gestión de vuelos ejecutivos operados en aeronaves **Embraer Phenom 300E** (configuración VIP de 7 asientos según especificación técnica DP-11 y reglas de negocio del Grupo 6).

El sistema cuenta con autenticación y autorización basada en **ASP.NET Identity**, segregación de permisos en **4 roles oficiales**, mapa interactivo de asientos con 3 estados operativos, validaciones de identidad y bancarias adaptadas al contexto del **Ecuador** (`ValidadorEcuador`), pasarela de pago simulada determinista (DP-06), emisión de comprobantes académicos y notas de crédito (DP-05 / DP-08), check-in con ventana de 24h a 45min (DP-12), emisión de pases de abordar con QR vectorial y las **21 interfaces oficiales** de los wireframes.

---

## 📑 Tabla de Contenidos

1. [Características Principales](#-características-principales)
2. [Usuarios Preconfigurados (Global.asax.cs)](#-usuarios-preconfigurados-globalasaxcs)
   - [Matriz de Roles y Credenciales](#matriz-de-roles-y-credenciales)
   - [Detalle de Permisos por Usuario](#detalle-de-permisos-por-usuario)
3. [Estructura del Proyecto](#-estructura-del-proyecto)
4. [Requisitos Previos](#-requisitos-previos)
5. [Instalación y Puesta en Marcha](#-instalación-y-puesta-en-marcha)
   - [Paso 1: Configurar la Base de Datos](#paso-1-configurar-la-base-de-datos)
   - [Paso 2: Ajustar las Cadenas de Conexión](#paso-2-ajustar-las-cadenas-de-conexión)
   - [Paso 3: Abrir y Compilar en Visual Studio](#paso-3-abrir-y-compilar-en-visual-studio)
6. [Flujos de Navegación y Prueba Rápida](#-flujos-de-navegación-y-prueba-rápida)
7. [Mapa de las 21 Interfaces Oficiales](#-mapa-de-las-21-interfaces-oficiales)

---

## 🌟 Características Principales

- **Flota Exclusiva Phenom 300E (7 Plazas - DP-11)**: Modelado de cabina ejecutiva real con distribución asimétrica:
  - **Fila 1**: Asiento `1D` (lado derecho, ventana junto a cabina) y espacio para `Puerta / Galley` a la izquierda.
  - **Fila 2**: Asientos `2I` (izquierdo) y `2D` (derecho).
  - **Fila 3**: Asientos `3I` y `3D` con mesa ejecutiva de trabajo (*Club Seating*).
  - **Fila 4**: Asientos `4I` y `4D` precediendo al Baño VIP posterior.
- **Códigos de Color Oficiales del Mapa de Asientos (RG-18 / DP-11)**:
  - 🟢 **Verde (`DISPONIBLE`)**: Asiento disponible para selección.
  - 🟣 **Morado (`RESERVADO` - #6F42C1)**: Reserva creada en estado `PENDIENTE` (bloqueo temporal de 20 minutos según DP-07).
  - 🔴 **Rojo (`OCUPADO`)**: Reserva con compra pagada y estado `CONFIRMADA` (o check-in realizado).
- **Rutas Directas y Ruta Dominical con Escala (DP-01, DP-04, DP-09, DP-10)**:
  - Tramos continentales (`UIO ↔ GYE`, `UIO ↔ MEC`): **USD 450.00** por pasajero.
  - Tramo insular directo (`GYE ↔ GPS` - Isla Baltra, Galápagos): **USD 650.00** por pasajero.
  - Ruta dominical en conexión (`UIO ↔ GPS` vía `GYE`, 2 tramos): **USD 1,100.00** por pasajero (asiento seleccionable por cada tramo).
- **Motor de Validaciones y Pago Simulado Determinista (DP-06)**:
  - Validación algorítmica de Cédula de Identidad ecuatoriana (módulo 10), RUC de 13 dígitos y Pasaporte.
  - Simulación de pago determinista: tarjetas terminadas en `0000` producen `RECHAZADO`; cualquier otra numeración válida por marca (Visa, Mastercard, Amex, Diners) produce `APROBADO` con código `SIM-{CodigoReserva}-{Intento}` y emite automáticamente los comprobantes `CONFIRMACION` y `FACTURA`.
- **Política de Cancelación Escalonada (DP-08)**:
  - Cancelación de reserva `PENDIENTE` sin pago: **0% de penalidad**.
  - Cancelación de reserva `CONFIRMADA` con **≥ 24 horas** de anticipación: **10% de penalidad** (90% devolución + emisión automática de `NOTA_CREDITO`).
  - Cancelación con **< 24 horas** y antes de la salida: **25% de penalidad** (75% devolución + `NOTA_CREDITO`).
  - Cancelación posterior a la salida o con check-in realizado: **No permitida**.

---

## 👥 Usuarios Preconfigurados (Global.asax.cs)

La aplicación inicializa automáticamente en el arranque (`Application_Start` en `Global.asax.cs` vía `RolesInicializador.InicializarRolesYUsuarios()`) los **4 roles oficiales** (`SuperUsuario`, `Administrador`, `Counter`, `Cliente`) y las cuentas de demostración:

### Matriz de Roles y Credenciales

| Rol Oficial | Nombre de Usuario | Correo Electrónico | Contraseña | Destino tras Login | Badge en Navbar |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **SuperUsuario** | `superusuario` (o `superadmin`) | `super@aerolineavip.com` | `Admin123*` (`Super123*`) | `/Admin/Index` | Púrpura (`SuperUsuario`) |
| **Administrador** | `admin01` | `admin@aerolineavip.com` | `Admin123*` | `/Admin/Index` | Azul (`Admin`) |
| **Counter** | `counter01` | `counter@aerolineavip.com` | `Counter123*` | `/Counter/Index` | Amarillo (`Counter`) |
| **Cliente** | `cliente01` | `cliente@aerolineavip.com` | `Cliente123*` | `/Home/Index` | Verde (`Cliente VIP`) |

---

### Detalle de Permisos por Usuario

1. **👑 SuperUsuario (`superusuario` / `Admin123*`)**:
   - Acceso total a todos los módulos operativos y administrativos (`MOD-01` a `MOD-10`), incluyendo la **gestión exclusiva de roles de cuentas de usuario** en la pestaña *Usuarios del Sistema* de `/PasajerosAdmin` (`RN-02`).
2. **🛡️ Administrador (`admin01` / `Admin123*`)**:
   - Acceso completo al Panel de Administración (`/Admin/Index`), Vuelos e Itinerarios (`/VuelosAdmin`), Flota Phenom 300E (`/AeronavesAdmin`), Pasajeros y Reservas (`/PasajerosAdmin`) y Consultas/Reportes (`/Admin/Reportes` con exportación CSV).
3. **🎫 Counter (`counter01` / `Counter123*`)**:
   - Acceso al módulo de Atención en Mostrador (`/Counter/Index`), Check-in asistido (`/CheckIn`), reasignación de asientos disponibles por tramo, consulta de reservas y visualización de comprobantes (`/Comprobantes`).
4. **👤 Cliente (`cliente01` / `Cliente123*`)**:
   - Acceso al embudo completo de búsqueda, selección de asientos, registro de pasajeros, confirmación de reserva (`PENDIENTE` por 20 min), pago simulado, portal personal `Mis Reservas` (aislado por titular autenticado), Check-in online y `Mis Comprobantes`.

---

## 📁 Estructura del Proyecto

```text
AerolineaVIP/
├── database/
│   └── Script_BD_AerolineaVIP.sql        # Script SQL completo de Dominio + Identity
├── docs/                                 # Documentación oficial y Wireframes de las 21 interfaces
├── packages/                             # Paquetes NuGet
├── src/
│   ├── AerolineaVIP.slnx                 # Solución Visual Studio
│   └── AerolineaVIP/                     # Proyecto Web ASP.NET MVC 5 (.NET Framework 4.8.1)
│       ├── AerolineaVIP.csproj           # Archivo de proyecto principal
│       ├── App_Start/                    # BundleConfig, FilterConfig, RouteConfig, IdentityConfig
│       ├── Controllers/                  # Controladores MVC (12 controladores)
│       ├── Models/                       # Entidades EF6 (EDMX), ViewModels y Validadores
│       ├── Views/                        # Vistas Razor para las 21 interfaces oficiales
│       ├── Global.asax.cs                # Inicialización en Application_Start
│       └── Web.config                    # Cadenas de conexión con Integrated Security=True
└── README.md                             # Documentación del proyecto
```

---

## 🚀 Instalación y Puesta en Marcha

### Paso 1: Configurar la Base de Datos

1. Abra **SQL Server Management Studio (SSMS)** y conéctese a su instancia local.
2. Abra y ejecute el archivo `database/Script_BD_AerolineaVIP.sql` presionando **F5**.
   - El script crea e inicializa las bases de datos `AerolineaVIP` y `AerolineaVIP_Identity`.

### Paso 2: Ajustar las Cadenas de Conexión

El archivo `src/AerolineaVIP/Web.config` está configurado por defecto con autenticación integrada de Windows (`Integrated Security=True`) apuntando a `localhost`:

```xml
<connectionStrings>
  <add name="DefaultConnection" 
       connectionString="Data Source=localhost;Initial Catalog=AerolineaVIP_Identity;Integrated Security=True;TrustServerCertificate=True" 
       providerName="System.Data.SqlClient" />
  <add name="AerolineaVIPEntities" 
       connectionString="metadata=res://*/Models.AerolineaVIP.csdl|res://*/Models.AerolineaVIP.ssdl|res://*/Models.AerolineaVIP.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=localhost;initial catalog=AerolineaVIP;integrated security=True;trustservercertificate=True;MultipleActiveResultSets=True;App=EntityFramework&quot;" 
       providerName="System.Data.EntityClient" />
</connectionStrings>
```

### Paso 3: Abrir y Compilar en Visual Studio

1. Abra `src/AerolineaVIP.slnx` o `src/AerolineaVIP/AerolineaVIP.csproj` en **Visual Studio 2022**.
2. Compile la solución (`Ctrl + Shift + B`) e inicie con **IIS Express** (`https://localhost:44334/`).

---

## 🗺️ Mapa de las 21 Interfaces Oficiales

| # | Pantalla Wireframe | Controlador / Acción | Ruta URL |
| :--- | :--- | :--- | :--- |
| **01** | **Portada / Home** | `HomeController.Index` | `/` o `/Home/Index` |
| **02** | **Iniciar Sesión** | `AccountController.Login` | `/Account/Login` |
| **03** | **Registro de Usuario** | `AccountController.Register` | `/Account/Register` |
| **04** | **Mi Cuenta / Perfil** | `ManageController.Index` | `/Manage/Index` |
| **05** | **Buscador de Vuelos** | `VuelosController.Buscar` | `/Vuelos/Buscar` |
| **06** | **Resultados de Búsqueda** | `VuelosController.Resultados` | `/Vuelos/Resultados` |
| **07** | **Detalle de Disponibilidad** | `VuelosController.Disponibilidad` | `/Vuelos/Disponibilidad/{id}` |
| **08** | **Selección de Asientos VIP** | `ReservasController.SeleccionarAsientos` | `/Reservas/SeleccionarAsientos` (`/Reservas/SeleccionAsiento`) |
| **09** | **Datos de Pasajeros** | `ReservasController.Pasajeros` | `/Reservas/Pasajeros` |
| **10** | **Confirmar Reserva (Paso 3)** | `ReservasController.Confirmar` | `/Reservas/Confirmar` |
| **11** | **Pago Simulado (Paso 4)** | `ReservasController.Pagar` / `ComprasController.Pagar` | `/Reservas/Pagar` o `/Compras/Pagar` |
| **12** | **Confirmación de Compra y Comprobante** | `ReservasController.Exito` / `ComprasController.Confirmacion` | `/Reservas/Exito` o `/Compras/Confirmacion` |
| **13** | **Mis Reservas** | `ReservasController.MisReservas` | `/Reservas/MisReservas` |
| **14** | **Detalle / Cancelación de Reserva** | `ReservasController.Gestionar` | `/Reservas/Gestionar/{id}` |
| **15** | **Check-in de Pasajeros** | `CheckInController.Index` / `CounterController.Index` | `/CheckIn` y `/Counter` |
| **16** | **Mis Comprobantes + Pase de Abordar** | `ComprobantesController.Index` / `CheckInController.PaseAbordar` | `/Comprobantes` y `/CheckIn/PaseAbordar` |
| **17** | **Panel del Administrador** | `AdminController.Index` | `/Admin` |
| **18** | **Administrar Vuelos e Itinerarios** | `VuelosAdminController.Index` | `/VuelosAdmin` |
| **19** | **Gestionar Aeronaves** | `AeronavesAdminController.Index` | `/AeronavesAdmin` |
| **20** | **Usuarios, Pasajeros y Reservas** | `PasajerosAdminController.Index` | `/PasajerosAdmin` |
| **21** | **Consultas y Reportes** | `AdminController.Reportes` | `/Admin/Reportes` |
