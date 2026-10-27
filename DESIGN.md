# DESIGN.md — Sistema de diseño inspirado en Avianca

> Aproximación no oficial. El rojo de marca (`#FC0404`) proviene de fuentes públicas de logos; el resto de valores (tipografía, espaciado, radios, sombras) son decisiones propias que buscan acercarse al estilo de una aerolínea moderna: limpio, blanco, con rojo como acento fuerte. No usar el logo ni la tipografía propietaria de Avianca sin autorización.

---

## 1. Principios

1. **Claridad primero:** reservar vuelos es una tarea; la interfaz no compite con ella.
2. **Rojo con intención:** el rojo es para acciones principales y énfasis, no para decorar.
3. **Mucho blanco:** fondos claros, tarjetas limpias, jerarquía por tamaño y peso.
4. **Accesible:** contraste AA mínimo, foco visible, objetivos táctiles de 44 px.
5. **Móvil primero:** la mayoría de las compras de vuelos empiezan en el teléfono.

---

## 2. Color

### 2.1 Marca

| Token | Hex | Uso |
|---|---|---|
| `--color-brand-500` | `#FC0404` | Color de marca, ilustraciones, acentos grandes |
| `--color-brand-600` | `#D90000` | **Botón primario**, enlaces sobre blanco (AA) |
| `--color-brand-700` | `#B00000` | Hover / pressed |
| `--color-brand-800` | `#8A0000` | Texto sobre fondos rojos claros |
| `--color-brand-100` | `#FFE5E5` | Fondos de resaltado, chips |
| `--color-brand-50` | `#FFF4F4` | Fondos sutiles, filas seleccionadas |

### 2.2 Neutros

| Token | Hex | Uso |
|---|---|---|
| `--color-ink-900` | `#1A1A1A` | Texto principal, títulos |
| `--color-ink-700` | `#444444` | Texto secundario |
| `--color-ink-500` | `#767676` | Texto terciario, placeholders (AA sobre blanco) |
| `--color-line-300` | `#D9D9D9` | Bordes de inputs, divisores |
| `--color-line-200` | `#E8E8E8` | Bordes de tarjetas |
| `--color-surface-100` | `#F5F5F5` | Fondo de página alterno |
| `--color-surface-0` | `#FFFFFF` | Fondo base y tarjetas |

### 2.3 Semánticos

| Token | Hex | Uso |
|---|---|---|
| `--color-success-600` | `#1E7B3A` | Confirmaciones, check-in completado |
| `--color-success-50` | `#E8F5EC` | Fondo de alerta de éxito |
| `--color-warning-600` | `#B26A00` | Advertencias, cambios de itinerario |
| `--color-warning-50` | `#FFF4E0` | Fondo de alerta de advertencia |
| `--color-info-600` | `#0B5CAD` | Información, enlaces secundarios |
| `--color-info-50` | `#E6F1FB` | Fondo de alerta informativa |
| `--color-error-600` | `#C62828` | Errores de formulario |
| `--color-error-50` | `#FDECEC` | Fondo de error |

### 2.4 Reglas de contraste

- Texto normal sobre blanco: usar `ink-900` o `ink-700`.
- `#FC0404` sobre blanco **no** cumple AA para texto pequeño; usar `brand-600` o más oscuro.
- Texto blanco sobre `brand-600` cumple AA.
- Nunca comunicar estado solo con color: añadir icono o texto.

---

## 3. Tipografía

### 3.1 Familias

- **Títulos:** `Montserrat` (alternativa libre cercana a una sans geométrica gruesa).
- **Cuerpo / UI:** `Nunito Sans`.
- **Fallback:** `system-ui, -apple-system, "Segoe UI", Roboto, Arial, sans-serif`.

```css
--font-heading: "Montserrat", system-ui, -apple-system, "Segoe UI", Roboto, Arial, sans-serif;
--font-body: "Nunito Sans", system-ui, -apple-system, "Segoe UI", Roboto, Arial, sans-serif;
```

### 3.2 Escala

| Token | Tamaño | Interlineado | Peso | Uso |
|---|---|---|---|---|
| `display` | 48 / 56 px | 1.1 | 800 | Hero de campañas |
| `h1` | 36 / 40 px | 1.2 | 700 | Título de página |
| `h2` | 28 / 32 px | 1.25 | 700 | Secciones |
| `h3` | 22 / 24 px | 1.3 | 700 | Tarjetas, módulos |
| `h4` | 18 px | 1.4 | 700 | Subtítulos |
| `body-lg` | 18 px | 1.6 | 400 | Texto destacado |
| `body` | 16 px | 1.5 | 400 | Texto general |
| `body-sm` | 14 px | 1.5 | 400 | Ayudas, metadatos |
| `caption` | 12 px | 1.4 | 600 | Etiquetas, legales |
| `button` | 16 px | 1 | 700 | Botones |
| `price` | 24–32 px | 1.1 | 800 | Precios |

(En móvil: reducir `display` a 32 px, `h1` a 28 px, `h2` a 22 px.)

### 3.3 Reglas

- Títulos en mayúscula inicial (no todo en mayúsculas).
- Máximo 70 caracteres por línea en texto corrido.
- Números de precio con `font-variant-numeric: tabular-nums`.

---

## 4. Espaciado y layout

### 4.1 Escala (base 4 px)

`4, 8, 12, 16, 24, 32, 48, 64, 96`

```css
--space-1: 4px;  --space-2: 8px;  --space-3: 12px; --space-4: 16px;
--space-6: 24px; --space-8: 32px; --space-12: 48px; --space-16: 64px; --space-24: 96px;
```

### 4.2 Grid y contenedor

| Breakpoint | Ancho | Columnas | Gutter | Margen |
|---|---|---|---|---|
| `sm` | < 640 px | 4 | 16 px | 16 px |
| `md` | 640–1023 px | 8 | 24 px | 32 px |
| `lg` | 1024–1279 px | 12 | 24 px | 48 px |
| `xl` | ≥ 1280 px | 12 | 32 px | auto (máx. 1200 px) |

---

## 5. Forma, bordes y elevación

### 5.1 Radios

| Token | Valor | Uso |
|---|---|---|
| `--radius-sm` | 4 px | Chips pequeños, tooltips |
| `--radius-md` | 8 px | Inputs, tarjetas |
| `--radius-lg` | 16 px | Módulos grandes, buscador |
| `--radius-pill` | 999 px | Botones, tabs, chips |

### 5.2 Sombras

```css
--shadow-1: 0 1px 2px rgba(0,0,0,.08);
--shadow-2: 0 4px 12px rgba(0,0,0,.10);
--shadow-3: 0 12px 32px rgba(0,0,0,.14);
```

- Tarjetas en reposo: `shadow-1` + borde `line-200`.
- Buscador de vuelos y menús flotantes: `shadow-2` o `shadow-3`.

### 5.3 Movimiento

```css
--ease: cubic-bezier(.2, 0, 0, 1);
--dur-fast: 120ms; --dur-base: 200ms; --dur-slow: 320ms;
```

Respetar `prefers-reduced-motion`: desactivar transiciones no esenciales.

---

## 6. Componentes

### 6.1 Botones

| Variante | Fondo | Texto | Borde | Hover |
|---|---|---|---|---|
| Primario | `brand-600` | Blanco | — | `brand-700` |
| Secundario | Transparente | `brand-600` | 2 px `brand-600` | `brand-50` |
| Terciario (texto) | Transparente | `brand-600` | — | Subrayado |
| Deshabilitado | `#EDEDED` | `#9A9A9A` | — | — |

- Altura: 48 px (móvil y escritorio); compacto 36 px.
- Padding horizontal: 24 px. Radio: `pill`.
- Foco: anillo de 3 px `rgba(217,0,0,.35)` con 2 px de separación.

```css
.btn-primary {
  background: var(--color-brand-600);
  color: #fff;
  height: 48px;
  padding: 0 24px;
  border-radius: var(--radius-pill);
  font: 700 16px/1 var(--font-heading);
  transition: background var(--dur-base) var(--ease);
}
.btn-primary:hover { background: var(--color-brand-700); }
.btn-primary:focus-visible { outline: 3px solid rgba(217,0,0,.35); outline-offset: 2px; }
```

### 6.2 Campos de formulario

- Altura 56 px, borde 1 px `line-300`, radio `md`, fondo blanco.
- Etiqueta flotante de 12 px `ink-700`; valor de 16 px `ink-900`.
- Foco: borde 2 px `brand-600`.
- Error: borde `error-600`, mensaje de 14 px debajo con icono.
- Placeholder: `ink-500`.

### 6.3 Buscador de vuelos (componente principal)

- Tarjeta blanca, radio `lg`, `shadow-3`, padding 24 px.
- Tabs superiores tipo pill: *Ida y vuelta · Solo ida · Multidestino*. Activo: fondo `brand-600`, texto blanco.
- Campos en fila (escritorio): Origen · Destino · Fechas · Pasajeros · botón **Buscar vuelos**.
- En móvil: campos apilados, botón primario de ancho completo.
- Botón de intercambio origen/destino: círculo de 40 px, borde `line-300`.

### 6.4 Tarjetas

- Fondo blanco, borde 1 px `line-200`, radio `md`, `shadow-1`.
- Hover (si es clicable): `shadow-2` y elevación de 2 px.
- Tarjeta de oferta de destino: imagen superior 16:9, destino en `h3`, precio "Desde" en `price` con color `brand-600`.

### 6.5 Tarjeta de resultado de vuelo

- Fila: hora salida — duración/escalas — hora llegada — precio.
- Horas en `h3`; códigos IATA en `caption` mayúsculas, `ink-700`.
- Línea de trayecto de 2 px `line-300` con punto en cada extremo.
- Precio a la derecha, botón **Seleccionar** secundario.
- Etiqueta de tarifa (Basic / Classic / Flex): chip `brand-100` con texto `brand-800`.

### 6.6 Navegación

- Barra superior blanca, 72 px (escritorio) / 56 px (móvil), borde inferior `line-200`.
- Logo a la izquierda; enlaces en `body` 600; selector de idioma/país y botón **Iniciar sesión** a la derecha.
- Enlace activo: subrayado de 3 px `brand-600`.
- Menú móvil: panel a pantalla completa con enlaces de 56 px de alto.

### 6.7 Chips y etiquetas

- Altura 32 px, radio `pill`, padding 0 12 px, 14 px / 600.
- Neutro: fondo `surface-100`; seleccionado: fondo `brand-100`, borde `brand-600`.

### 6.8 Alertas

- Radio `md`, padding 16 px, borde izquierdo de 4 px del color semántico, icono a la izquierda.
- Usar los fondos `*-50` y texto `ink-900`.

### 6.9 Modales

- Ancho máx. 560 px, radio `lg`, `shadow-3`, overlay `rgba(0,0,0,.55)`.
- Cierre con botón X (44 px) y tecla Esc; foco atrapado dentro.

### 6.10 Stepper de reserva

- Pasos: *Vuelos · Pasajeros · Extras · Pago · Confirmación*.
- Completado: círculo `success-600` con check; actual: círculo `brand-600`; pendiente: borde `line-300`.
- Resumen del viaje fijo a la derecha (escritorio) o colapsable abajo (móvil).

### 6.11 Pie de página

- Fondo `ink-900`, texto blanco / `#CFCFCF`, enlaces con subrayado al hover.
- Columnas: Reserva · Tu viaje · Ayuda · Compañía. Redes sociales y legales al final.

---

## 7. Iconografía e imágenes

- Iconos de línea, trazo 1.5–2 px, esquinas redondeadas, 24 px base (16 / 20 / 32 también).
- Color por defecto `ink-900`; acento `brand-600`.
- Fotografía de destinos: luminosa, personas reales, colores cálidos; relación 16:9 o 4:3; esquinas `md`.
- Texto sobre imagen: degradado oscuro inferior (`rgba(0,0,0,.6)` → transparente) para asegurar contraste.

---

## 8. Voz y contenido

- Tono: cercano, claro, profesional. Español neutro latinoamericano.
- Botones con verbos: **Buscar vuelos**, **Continuar**, **Pagar ahora**.
- Mensajes de error: qué pasó + cómo resolverlo.
- Fechas: `12 mar 2027`; horas en formato 24 h; moneda con código (`COP 450.000`, `USD 120`).

---

## 9. Accesibilidad

- Contraste AA (4.5:1 texto normal, 3:1 texto grande e iconos).
- Foco siempre visible; orden de tabulación lógico.
- Objetivos táctiles ≥ 44 × 44 px.
- Etiquetas `<label>` asociadas; errores con `aria-live="polite"`.
- Textos alternativos descriptivos en imágenes informativas.
- Soportar zoom hasta 200 % sin pérdida de funcionalidad.

---

## 10. Tokens CSS completos

```css
:root {
  /* Color */
  --color-brand-50: #FFF4F4;
  --color-brand-100: #FFE5E5;
  --color-brand-500: #FC0404;
  --color-brand-600: #D90000;
  --color-brand-700: #B00000;
  --color-brand-800: #8A0000;

  --color-ink-900: #1A1A1A;
  --color-ink-700: #444444;
  --color-ink-500: #767676;
  --color-line-300: #D9D9D9;
  --color-line-200: #E8E8E8;
  --color-surface-100: #F5F5F5;
  --color-surface-0: #FFFFFF;

  --color-success-600: #1E7B3A;  --color-success-50: #E8F5EC;
  --color-warning-600: #B26A00;  --color-warning-50: #FFF4E0;
  --color-info-600: #0B5CAD;     --color-info-50: #E6F1FB;
  --color-error-600: #C62828;    --color-error-50: #FDECEC;

  /* Tipografía */
  --font-heading: "Montserrat", system-ui, -apple-system, "Segoe UI", Roboto, Arial, sans-serif;
  --font-body: "Nunito Sans", system-ui, -apple-system, "Segoe UI", Roboto, Arial, sans-serif;

  /* Espaciado */
  --space-1: 4px;  --space-2: 8px;  --space-3: 12px; --space-4: 16px;
  --space-6: 24px; --space-8: 32px; --space-12: 48px; --space-16: 64px; --space-24: 96px;

  /* Forma */
  --radius-sm: 4px; --radius-md: 8px; --radius-lg: 16px; --radius-pill: 999px;

  /* Elevación */
  --shadow-1: 0 1px 2px rgba(0,0,0,.08);
  --shadow-2: 0 4px 12px rgba(0,0,0,.10);
  --shadow-3: 0 12px 32px rgba(0,0,0,.14);

  /* Movimiento */
  --ease: cubic-bezier(.2, 0, 0, 1);
  --dur-fast: 120ms; --dur-base: 200ms; --dur-slow: 320ms;
}

body {
  margin: 0;
  background: var(--color-surface-0);
  color: var(--color-ink-900);
  font: 400 16px/1.5 var(--font-body);
}

h1, h2, h3, h4 { font-family: var(--font-heading); font-weight: 700; margin: 0 0 var(--space-4); }
h1 { font-size: clamp(28px, 4vw, 36px); line-height: 1.2; }
h2 { font-size: clamp(22px, 3vw, 28px); line-height: 1.25; }
h3 { font-size: 22px; line-height: 1.3; }

a { color: var(--color-brand-600); text-underline-offset: 3px; }
a:hover { color: var(--color-brand-700); }

:focus-visible { outline: 3px solid rgba(217,0,0,.35); outline-offset: 2px; }

@media (prefers-reduced-motion: reduce) {
  * { transition: none !important; animation: none !important; }
}
```

---

## 11. Modo oscuro (opcional)

| Token | Valor oscuro |
|---|---|
| `--color-surface-0` | `#121212` |
| `--color-surface-100` | `#1E1E1E` |
| `--color-ink-900` | `#F2F2F2` |
| `--color-ink-700` | `#C8C8C8` |
| `--color-line-200` | `#2E2E2E` |
| `--color-brand-600` (botones) | `#E53935` |

Mantener el rojo como único acento y verificar contraste en cada par.

---

## 12. Checklist de implementación

- [ ] Cargar Montserrat y Nunito Sans (pesos 400, 600, 700, 800) con `font-display: swap`.
- [ ] Definir los tokens en `:root` antes de cualquier componente.
- [ ] Un solo botón primario por vista.
- [ ] Verificar contraste de todos los pares texto/fondo.
- [ ] Probar a 320 px, 768 px y 1280 px.
- [ ] Probar navegación completa con teclado.
