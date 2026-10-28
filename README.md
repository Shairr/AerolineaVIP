# ✈️ Aerolínea VIP — Sistema Web de Compra y Reserva de Pasajes

Bienvenido al repositorio del proyecto **Aerolínea VIP**, una plataforma web desarrollada en **ASP.NET MVC 5** (.NET Framework 4.8.1) y **Microsoft SQL Server**, diseñada para la reserva y gestión de vuelos ejecutivos de alta gama operados en aeronaves **Embraer Phenom 300E** (configuración VIP de 7 asientos según especificación técnica DP-11 y regla de negocio RN-03).

El sistema cuenta con autenticación y autorización robusta basada en **ASP.NET Identity**, segregación de permisos por roles, mapa interactivo de asientos por cuadrantes (Fila/Lado I-D), validaciones de identidad y bancarias adaptadas al contexto del **Ecuador** (`ValidadorEcuador`), pasarela de pago simulada, emisión de pases de abordar con QR vectorial y un panel de administración alineado estrictamente a los wireframes de la primera entrega académica.

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
   - [Flujo 1: Experiencia del Cliente VIP](#flujo-1-experiencia-del-cliente-vip)
   - [Flujo 2: Experiencia Administrativa](#flujo-2-experiencia-administrativa)
7. [Mapa de Rutas y Wireframes Oficiales](#-mapa-de-rutas-y-wireframes-oficiales)
8. [Solución de Problemas Frecuentes](#-solución-de-problemas-frecuentes)

---

## 🌟 Características Principales

- **Flota Exclusiva Phenom 300E (7 Plazas)**: Modelado de cabina ejecutiva real con distribución asimétrica:
  - **Fila 1**: Asiento `1D` (lado derecho, ventana junto a cabina) y espacio para `Puerta / Galley` a la izquierda.
  - **Fila 2**: Asientos `2I` (izquierdo) y `2D` (derecho).
  - **Fila 3**: Asientos `3I` y `3D` con mesa ejecutiva de trabajo (*Club Seating*).
  - **Fila 4**: Asientos `4I` y `4D` precediendo al Baño VIP y guardarropa posterior.
- **Códigos de Color Oficiales del Mapa de Asientos**:
  - 🟢 **Verde**: Asiento disponible para selección.
  - 🟣 **Morado (#6F42C1)**: Reservado (reserva creada, check-in pendiente).
  - 🔴 **Rojo**: Ocupado (check-in completado y confirmado).
  - 🔴 **Rojo Marca / Dorado**: Asiento seleccionado en la sesión activa.
- **Motor de Validaciones Nacionales (`ValidadorEcuador`)**:
  - Validación algorítmica de Cédula de Identidad ecuatoriana (módulo 10 del Registro Civil).
  - Validación de números celulares ecuatorianos (formato `09xxxxxxxx` / `+593`).
  - Validación de tarjetas de crédito/débito por marca (Visa, Mastercard, Diners, American Express) con control de titular (nombre y apellido real) y fecha de vencimiento.
- **Flujo Completo de Reserva**: Búsqueda por rutas nacionales (Quito UIO, Guayaquil GYE, Manta MEC, Galápagos GPS), selección de tarifas (Classic, Plus, Premier) y pasarela de pago simulada.
- **Autoservicio del Pasajero**: Portal "Mis Reservas" con pestañas de estado (*Todas, Confirmadas, Pendientes, Canceladas*), cancelación motivada de reservas, comprobante digital/factura y Check-in online con emisión de Pase de Abordar (*Boarding Pass*) con código QR integrado en SVG.
- **Panel Administrativo (Wireframe 17)**: Menú de accesos en cuadrícula 2x2 para Vuelos (Pantalla 18), Aeronaves (Pantalla 19), Pasajeros (Pantalla 20) y Consultas/Reportes con filtros y exportación tabular (Pantalla 21). *Nota: Por especificación de alcance, el dashboard analítico de gráficos está excluido de esta entrega.*

---

## 👥 Usuarios Preconfigurados (Global.asax.cs)

La aplicación inicializa automáticamente en el arranque (`Application_Start` en `Global.asax.cs` vía `InicializarSeguridadPorCodigo()` / `RolesInicializador.cs`) los 3 roles oficiales y las 3 cuentas académicas predeterminadas con sus respectivas credenciales hasheadas en ASP.NET Identity:

### Matriz de Roles y Credenciales

| Rol Oficial | Nombre de Usuario | Correo Electrónico | Contraseña | Destino tras Login | Badge en Navbar |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **SuperUsuario** | `superusuario` | `super@aerolineavip.com` | `Admin123*` | `/Admin/Index` | Púrpura (`SuperUsuario`) |
| **Administrador** | `admin01` | `admin@aerolineavip.com` | `Admin123*` | `/Admin/Index` | Azul (`Admin`) |
| **Cliente** | `cliente01` | `cliente@aerolineavip.com` | `Cliente123*` | `/Home/Index` | Verde (`Cliente VIP`) |

> 💡 **Política de Contraseñas Académica**: En `Global.asax.cs` se ha flexibilizado la validación a longitud mínima de 6 caracteres sin exigir obligatoriamente caracteres especiales para facilitar las pruebas universitarias, aunque las cuentas sembradas utilizan contraseñas robustas (`Admin123*` y `Cliente123*`).

---

### Detalle de Permisos por Usuario

#### 1. 👑 SuperUsuario (`superusuario`)
Representa el rol técnico de máxima jerarquía del sistema:
- **Redirección automática**: Tras autenticarse, aterriza directamente en el **Menú de Administración** (`/Admin/Index` - Pantalla 17).
- **Acceso Administrativo Completo**:
  - **Administrar Vuelos e Itinerarios (`/VuelosAdmin` - Pantalla 18)**: Crear nuevos vuelos, editar frecuencias, asociar aeronaves de la flota y eliminar vuelos.
  - **Gestionar Aeronaves (`/AeronavesAdmin` - Pantalla 19)**: Administrar la flota Phenom 300E (matrículas AV-01, AV-02, AV-03) y modificar su estado operativo (*DISPONIBLE, EN_VUELO, MANTENIMIENTO, RETIRADA*).
  - **Usuarios, Pasajeros y Reservas (`/PasajerosAdmin` - Pantalla 20)**: Gestionar el catálogo civil de pasajeros (cédula, pasaporte, nombres, teléfonos y correos).
  - **Consultas y Reportes (`/Admin/Reportes` - Pantalla 21)**: Filtrar reportes operativos por tipo de consulta (*Ocupación por vuelo, Reservas por estado, Compras y pagos simulados*), rangos de fechas, rutas y aeronaves.
- **Acceso Comercial Total**: Como usuario comodín, puede ingresar a "Mis Reservas" y "Check-in" para auditar y probar flujos de pasajeros.
- **Distintivo Visual**: Badge exclusivo color púrpura `SuperUsuario` en la esquina superior derecha del Navbar.

#### 2. 🛡️ Administrador (`admin01`)
Representa al operador administrativo y logístico de la aerolínea:
- **Redirección automática**: Tras el login, es dirigido inmediatamente al **Menú de Administración** (`/Admin/Index`).
- **Acceso Operativo de Gestión**:
  - Programación y mantenimiento de la grilla de vuelos (`/VuelosAdmin`).
  - Monitoreo del estado físico de las aeronaves Phenom 300E (`/AeronavesAdmin`).
  - Consulta y mantenimiento de fichas de pasajeros (`/PasajerosAdmin`).
  - Generación de reportes de ocupación y ventas simuladas (`/Admin/Reportes`).
  - Verificación de reservas de clientes y soporte en check-in.
- **Distintivo Visual**: Badge azul `Admin` en la barra de navegación.

#### 3. 👤 Cliente (`cliente01`)
Representa al cliente VIP de vuelos ejecutivos:
- **Redirección automática**: Tras autenticarse, se le envía a la página principal (`/Home/Index`) o a la vista de reserva que requirió autenticación.
- **Portal de Autoservicio del Pasajero**:
  - **Búsqueda y Cotización (`/Vuelos/Buscar` y `/Vuelos/Resultados`)**: Consulta de vuelos disponibles en Ecuador.
  - **Selección de Asientos VIP (`/Reservas/SeleccionarAsientos`)**: Interacción en tiempo real con la cabina del Phenom 300E (asientos `1D`, `2I`, `2D`, `3I`, `3D`, `4I`, `4D`).
  - **Registro de Pasajeros y Pago Simulado (`/Reservas/Pasajeros`, `/Reservas/Confirmar`, `/Reservas/Pagar`)**: Registro con validación de cédula ecuatoriana y pasarela de pago con comprobante digital.
  - **Mis Reservas (`/Reservas/MisReservas` - Pantalla 13)**: Bandeja personal organizada en pestañas (*Todas, Confirmadas, Pendientes, Canceladas*).
  - **Gestión de Reserva (`/Reservas/Gestionar/{id}` - Pantalla 14)**: Detalle del itinerario y opción de cancelación motivada.
  - **Comprobante Electrónico (`/Reservas/Comprobante/{id}`)**: Visualización con desglose tributario y código de autorización.
  - **Check-in Online (`/CheckIn` - Pantalla 15)**: Confirmación de pasajero y asignación de asiento.
  - **Pase de Abordar (`/CheckIn/PaseAbordar` - Pantalla 16)**: Tarjeta de embarque VIP con código QR vectorial en SVG para impresión o móvil.
- **Control de Seguridad**: Bloqueado ante cualquier intento de ingresar a `/Admin`, `/VuelosAdmin`, `/AeronavesAdmin` o `/PasajerosAdmin`.
- **Distintivo Visual**: Badge verde `Cliente VIP` en el Navbar.

#### 4. 🌐 Visitante / Registro Público
Cualquier persona no autenticada puede consultar la portada corporativa y buscar vuelos. Si decide registrarse a través de `/Account/Register`, el controlador le asigna **de forma estricta y automática el rol de `Cliente`**.

---

## 📁 Estructura del Proyecto

```text
AerolineaVIP/
├── database/
│   └── Script_BD_AerolineaVIP.sql        # Script SQL completo de Dominio + Identity
├── docs/
│   └── Primera_Entrega_Documentacion_Grupo6.pdf # Documentación oficial de entrega
├── packages/                             # Paquetes NuGet
├── src/
│   ├── AerolineaVIP.slnx                 # Solución en formato moderno (VS 2022 v17.10+)
│   └── AerolineaVIP/                     # Proyecto Web ASP.NET MVC 5
│       ├── AerolineaVIP.csproj           # Archivo de proyecto principal
│       ├── App_Start/                    # BundleConfig, FilterConfig, RouteConfig, IdentityConfig
│       ├── Controllers/                  # Controladores MVC
│       │   ├── AccountController.cs      # Login, registro y redirección según rol
│       │   ├── AdminController.cs        # Pantalla 17 (Menú) y Pantalla 21 (Reportes)
│       │   ├── AeronavesAdminController.cs # Pantalla 19 (CRUD Flota Phenom 300E)
│       │   ├── CheckInController.cs      # Pantalla 15 (Check-in) y 16 (Pase de Abordar)
│       │   ├── HomeController.cs          # Portada e información corporativa
│       │   ├── ManageController.cs       # Gestión de credenciales del usuario
│       │   ├── PasajerosAdminController.cs # Pantalla 20 (CRUD Pasajeros)
│       │   ├── ReservasController.cs     # Asientos, pasajeros, pago, mis reservas y gestión
│       │   ├── VuelosAdminController.cs  # Pantalla 18 (CRUD Vuelos programados)
│       │   └── VuelosController.cs       # Búsqueda y disponibilidad pública
│       ├── Models/                       # Entidades EF6 (EDMX), ViewModels y Validadores
│       │   ├── AerolineaVIP.edmx         # Modelo entidad-relación de dominio
│       │   ├── IdentityModels.cs         # ApplicationUser y ApplicationDbContext
│       │   ├── RolesInicializador.cs     # Lógica de siembra de usuarios y roles
│       │   └── ValidadorEcuador.cs       # Validaciones de cédula, celular y tarjetas
│       ├── Views/                        # Vistas Razor con diseño responsive VIP
│       │   ├── Admin/                    # Vistas Index y Reportes
│       │   ├── AeronavesAdmin/           # Vistas CRUD de aeronaves
│       │   ├── CheckIn/                  # Vistas Index y PaseAbordar
│       │   ├── PasajerosAdmin/           # Vistas CRUD de pasajeros
│       │   ├── Reservas/                 # Mapa de asientos, pago, mis reservas y voucher
│       │   ├── Shared/                   # Layout, Navbar, Footer, MapaAsientos y Modales
│       │   ├── Vuelos/                   # Búsqueda y resultados
│       │   └── VuelosAdmin/              # Vistas CRUD de vuelos
│       ├── Global.asax.cs                # Inicialización en Application_Start
│       └── Web.config                    # Cadenas de conexión (DefaultConnection y AerolineaVIPEntities)
└── README.md                             # Documentación del proyecto
```

---

## 💻 Requisitos Previos

- **Sistema Operativo**: Windows 10 o Windows 11.
- **Entorno de Desarrollo**: [Visual Studio 2019 o 2022](https://visualstudio.microsoft.com/) con la carga de trabajo **"Desarrollo de ASP.NET y web"**.
- **Framework**: [.NET Framework 4.8.1](https://dotnet.microsoft.com/download/dotnet-framework/net481).
- **Motor de Base de Datos**: [Microsoft SQL Server 2016](https://www.microsoft.com/sql-server) o superior (instancia local, SQL Server Express o Developer).
- **Herramienta de Administración SQL**: [SQL Server Management Studio (SSMS)](https://learn.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms) o Azure Data Studio.

---

## 🚀 Instalación y Puesta en Marcha

### Paso 1: Configurar la Base de Datos

1. Abra **SQL Server Management Studio (SSMS)** y conéctese a su servidor SQL Server local.
2. Abra el archivo:
   ```text
   database/Script_BD_AerolineaVIP.sql
   ```
3. Ejecute el script completo presionando **F5**.
   - El script es idempotente y crea en orden las dos bases de datos:
     1. **`AerolineaVIP`**: Tablas de dominio (aeronaves, asientos, rutas, frecuencias, vuelos, itinerarios, reservas, pasajeros, compras, pagos simulados y comprobantes).
     2. **`AerolineaVIP_Identity`**: Tablas para la gestión de usuarios, roles, claims y logins de ASP.NET Identity.
4. Confirme que en la ventana de resultados de SSMS todas las verificaciones terminen con **`OK`**.

### Paso 2: Ajustar las Cadenas de Conexión

Revise el archivo `src/AerolineaVIP/Web.config` y verifique los parámetros según su configuración local:

```xml
<connectionStrings>
  <!-- Base de Datos para ASP.NET Identity -->
  <add name="DefaultConnection" 
       connectionString="Data Source=localhost;Initial Catalog=AerolineaVIP_Identity;Persist Security Info=True;User ID=sa;Password=123;TrustServerCertificate=True" 
       providerName="System.Data.SqlClient" />

  <!-- Base de Datos de Dominio (Entity Framework 6) -->
  <add name="AerolineaVIPEntities" 
       connectionString="metadata=res://*/Models.AerolineaVIP.csdl|res://*/Models.AerolineaVIP.ssdl|res://*/Models.AerolineaVIP.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=localhost;initial catalog=AerolineaVIP;persist security info=True;user id=sa;password=123;trustservercertificate=True;MultipleActiveResultSets=True;App=EntityFramework&quot;" 
       providerName="System.Data.EntityClient" />
</connectionStrings>
```

### Paso 3: Abrir y Compilar en Visual Studio

1. Abra la solución `src/AerolineaVIP.slnx` o abra directamente el archivo de proyecto `src/AerolineaVIP/AerolineaVIP.csproj` en **Visual Studio 2022 / 2019**.
2. Restaure los paquetes NuGet haciendo clic derecho sobre la solución -> **Restaurar paquetes NuGet**.
3. Compile la solución.
4. Inicie la ejecución con **IIS Express**.
   - El navegador abrirá automáticamente la URL local: `https://localhost:44334/`.
   - En la primera solicitud, `Global.asax.cs` registrará automáticamente los roles y las 3 cuentas académicas.

---

## 🧭 Flujos de Navegación y Prueba Rápida

### Flujo 1: Experiencia del Cliente VIP

1. **Iniciar Sesión**:
   - Inicie sesión con el usuario `cliente01` y contraseña `Cliente123*`.
2. **Buscar Vuelo**:
   - Ingrese a **Buscar Vuelos** en el menú superior.
   - Seleccione origen **Quito (UIO)** y destino **Guayaquil (GYE)**. Presione *Buscar Vuelos*.
3. **Selección de Asientos VIP**:
   - En el vuelo deseado, haga clic en *Seleccionar Asientos*.
   - Verifique el mapa de cabina del **Phenom 300E** y haga clic en un asiento disponible (en verde, por ejemplo `1D` o `2I`).
4. **Registro de Pasajeros y Pago Simulado**:
   - Ingrese los datos del pasajero (ejemplo: Cédula válida ecuatoriana como `1710000001` o número de pasaporte, nombres completos y teléfono celular).
   - En la pasarela de pago, ingrese un titular ficticio (ej. `CARLOS ROBERTO MENDOZA`), número de tarjeta (ej. `4532 1111 2222 9010`), vencimiento futuro y CVV `123`.
5. **Comprobante y Gestión**:
   - Una vez procesado el pago simulado, el sistema mostrará el código de confirmación.
   - Ingrese a **Mis Reservas** para consultar la reserva en la pestaña *Confirmadas* y abrir su **Comprobante Digital**.
6. **Check-in Online y Boarding Pass**:
   - Vaya a **Check-in** en la barra superior.
   - Seleccione al pasajero confirmado y presione *Completar Check-in*.
   - Se abrirá la **Pantalla 16 (Pase de Abordar VIP)** con código QR en formato SVG.

---

### Flujo 2: Experiencia Administrativa

1. **Cerrar Sesión** del usuario anterior.
2. **Iniciar Sesión**:
   - Ingrese con el usuario `admin01` (o `superusuario`) y la contraseña `Admin123*`.
   - El sistema lo dirigirá automáticamente a la **Pantalla 17: Panel del Administrador** (`/Admin/Index`).
3. **Prueba de los Módulos del Wireframe**:
   - **Administrar vuelos e itinerarios (`/VuelosAdmin` - Pantalla 18)**: Verifique la lista de vuelos, cree un nuevo vuelo asignando fecha, frecuencia y aeronave.
   - **Gestionar aeronaves (`/AeronavesAdmin` - Pantalla 19)**: Examine las aeronaves Phenom 300E (`AV-01`, `AV-02`, `AV-03`) y modifique su estado operativo.
   - **Usuarios, pasajeros y reservas (`/PasajerosAdmin` - Pantalla 20)**: Revise el padrón de pasajeros registrados y edite sus datos.
   - **Consultas y reportes (`/Admin/Reportes` - Pantalla 21)**: Utilice los filtros para consultar *Ocupación por vuelo* o *Compras y pagos simulados*.

---

## 🗺️ Mapa de Rutas y Wireframes Oficiales

| Pantalla Wireframe | Controlador / Acción | Ruta URL | Roles Permitidos |
| :--- | :--- | :--- | :--- |
| **Inicio / Portada** | `HomeController.Index` | `/` o `/Home/Index` | Público / Todos |
| **Búsqueda de Vuelos** | `VuelosController.Buscar` | `/Vuelos/Buscar` | Público / Cliente |
| **Resultados de Vuelos** | `VuelosController.Resultados` | `/Vuelos/Resultados` | Público / Cliente |
| **Selección de Asientos** | `ReservasController.SeleccionarAsientos` | `/Reservas/SeleccionarAsientos` | Público / Cliente |
| **Registro de Pasajeros** | `ReservasController.Pasajeros` | `/Reservas/Pasajeros` | Público / Cliente |
| **Confirmación de Reserva** | `ReservasController.Confirmar` | `/Reservas/Confirmar` | Público / Cliente |
| **Pasarela de Pago** | `ReservasController.Pagar` | `/Reservas/Pagar` | Público / Cliente |
| **Confirmación de Compra** | `ReservasController.Exito` | `/Reservas/Exito` | Público / Cliente |
| **Pantalla 13: Mis Reservas** | `ReservasController.MisReservas` | `/Reservas/MisReservas` | Cliente, SuperUsuario, Administrador |
| **Pantalla 14: Gestionar Reserva** | `ReservasController.Gestionar` | `/Reservas/Gestionar/{id}` | Cliente, SuperUsuario, Administrador |
| **Comprobante Digital** | `ReservasController.Comprobante` | `/Reservas/Comprobante/{id}` | Cliente, SuperUsuario, Administrador |
| **Pantalla 15: Check-In Online** | `CheckInController.Index` | `/CheckIn` | Cliente, SuperUsuario, Administrador |
| **Pantalla 16: Pase de Abordar** | `CheckInController.PaseAbordar` | `/CheckIn/PaseAbordar` | Cliente, SuperUsuario, Administrador |
| **Pantalla 17: Panel Admin** | `AdminController.Index` | `/Admin` | Administrador, SuperUsuario |
| **Pantalla 18: Adm. Vuelos** | `VuelosAdminController.Index` | `/VuelosAdmin` | Administrador, SuperUsuario |
| **Pantalla 19: Adm. Aeronaves** | `AeronavesAdminController.Index` | `/AeronavesAdmin` | Administrador, SuperUsuario |
| **Pantalla 20: Adm. Pasajeros** | `PasajerosAdminController.Index` | `/PasajerosAdmin` | Administrador, SuperUsuario |
| **Pantalla 21: Reportes** | `AdminController.Reportes` | `/Admin/Reportes` | Administrador, SuperUsuario |
| **Iniciar Sesión** | `AccountController.Login` | `/Account/Login` | Público |
| **Registro de Usuario** | `AccountController.Register` | `/Account/Register` | Público (Crea rol `Cliente`) |

---
