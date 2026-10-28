/**
 * Validaciones y Normalizaciones Oficiales para Ecuador
 * Aerolínea VIP — Sistema Web Monolítico
 * - Algoritmo Módulo 10 del Registro Civil del Ecuador (Cédula de Identidad) con exclusión de 1111111111
 * - Validación de RUC SRI (13 dígitos) y Pasaporte internacional
 * - Estándar ARCOTEL para telefonía celular (09XXXXXXXX)
 * - Nombres y Apellidos hispanos (máx 25 caracteres, sin números)
 * - Clasificación de edad (Infante 0-10, Menor 11-17, Adulto 18+)
 * - Algoritmo de Luhn bancario para tarjetas de crédito
 */

var ValidadorEcuadorJS = (function ($) {
    "use strict";

    // -------------------------------------------------------------------------
    // 1. MÉTODOS DE NORMALIZACIÓN
    // -------------------------------------------------------------------------

    function normalizarEspacios(str) {
        if (!str) return "";
        return str.trim().replace(/\s+/g, " ");
    }

    function normalizarNombrePropio(str) {
        var limpio = normalizarEspacios(str);
        if (!limpio) return "";
        return limpio.toLowerCase().replace(/(?:^|\s)\S/g, function (a) {
            return a.toUpperCase();
        });
    }

    function normalizarEmail(str) {
        if (!str) return "";
        return str.trim().toLowerCase().replace(/\s+/g, "");
    }

    function normalizarDocumento(str) {
        if (!str) return "";
        return str.trim().toUpperCase().replace(/[\s\-]/g, "");
    }

    function normalizarTelefono(str) {
        if (!str) return "";
        var soloDigitos = str.trim().replace(/\D/g, "");
        if (soloDigitos.indexOf("593") === 0 && soloDigitos.length === 12) {
            soloDigitos = "0" + soloDigitos.substring(3);
        }
        return soloDigitos;
    }

    // -------------------------------------------------------------------------
    // 2. VALIDACIONES DE IDENTIDAD Y DATOS CIVILES ECUATORIANOS
    // -------------------------------------------------------------------------

    /**
     * Valida cédula ecuatoriana según el Registro Civil (Módulo 10).
     * Excluye expresamente casos repetidos como '1111111111'.
     */
    function validarCedula(cedula) {
        var clean = normalizarDocumento(cedula);

        if (!clean || clean.length !== 10 || !/^\d{10}$/.test(clean)) {
            return { valido: false, mensaje: "La cédula debe contener exactamente 10 dígitos numéricos." };
        }

        // Exclusión explícita de dígitos idénticos repetidos (ej: 1111111111, 0000000000)
        var primerChar = clean.charAt(0);
        var todosIguales = true;
        for (var i = 1; i < clean.length; i++) {
            if (clean.charAt(i) !== primerChar) {
                todosIguales = false;
                break;
            }
        }
        if (todosIguales || clean === "1111111111") {
            return { valido: false, mensaje: "Número de cédula no permitido (secuencia de dígitos repetidos)." };
        }

        // 1. Código de provincia (01 a 24 o 30)
        var provincia = parseInt(clean.substring(0, 2), 10);
        if (!((provincia >= 1 && provincia <= 24) || provincia === 30)) {
            return { valido: false, mensaje: "Código de provincia inválido (los dos primeros dígitos deben estar entre 01 y 24)." };
        }

        // 2. Tercer dígito menor a 6 para personas naturales
        var tercerDigito = parseInt(clean.substring(2, 3), 10);
        if (tercerDigito >= 6) {
            return { valido: false, mensaje: "El tercer dígito de la cédula de persona natural debe ser menor a 6." };
        }

        // 3. Algoritmo Módulo 10 con coeficientes 2,1,2,1,2,1,2,1,2
        var coeficientes = [2, 1, 2, 1, 2, 1, 2, 1, 2];
        var suma = 0;

        for (var k = 0; k < 9; k++) {
            var val = parseInt(clean.charAt(k), 10) * coeficientes[k];
            if (val >= 10) {
                val -= 9;
            }
            suma += val;
        }

        var digitoVerificador = parseInt(clean.charAt(9), 10);
        var residuo = suma % 10;
        var esperado = (residuo === 0) ? 0 : (10 - residuo);

        if (digitoVerificador !== esperado) {
            return { valido: false, mensaje: "El dígito verificador no coincide con el algoritmo del Registro Civil." };
        }

        return { valido: true, mensaje: "Cédula de identidad ecuatoriana válida." };
    }

    function validarRuc(ruc) {
        var clean = normalizarDocumento(ruc);
        if (!clean || clean.length !== 13 || !/^\d{13}$/.test(clean)) {
            return { valido: false, mensaje: "El RUC ecuatoriano debe tener exactamente 13 dígitos numéricos." };
        }

        var establecimiento = clean.substring(10, 13);
        if (establecimiento === "000") {
            return { valido: false, mensaje: "El RUC debe terminar en un código de establecimiento válido (ej: 001)." };
        }

        var tercerDigito = parseInt(clean.substring(2, 3), 10);
        if (tercerDigito < 6) {
            var resCed = validarCedula(clean.substring(0, 10));
            if (!resCed.valido) {
                return { valido: false, mensaje: "Los primeros 10 dígitos del RUC de persona natural deben ser una cédula válida." };
            }
        }

        return { valido: true, mensaje: "RUC ecuatoriano válido." };
    }

    function validarPasaporte(pasaporte) {
        var clean = normalizarDocumento(pasaporte);
        if (!clean || clean.length < 6 || clean.length > 15 || !/^[A-Z0-9]{6,15}$/.test(clean)) {
            return { valido: false, mensaje: "El pasaporte debe ser alfanumérico entre 6 y 15 caracteres sin espacios." };
        }
        return { valido: true, mensaje: "Pasaporte válido." };
    }

    function validarTelefonoEcuador(tel) {
        var clean = normalizarTelefono(tel);
        if (!clean || clean.length !== 10 || !/^09\d{8}$/.test(clean)) {
            return { valido: false, mensaje: "El teléfono debe ser un celular ecuatoriano de 10 dígitos iniciado en 09 (ej: 0991234567)." };
        }
        return { valido: true, mensaje: "Celular ecuatoriano válido." };
    }

    function validarNombreApellido(texto, campoNombre) {
        var clean = normalizarEspacios(texto);
        if (!clean || clean.length < 2) {
            return { valido: false, mensaje: "El campo " + campoNombre + " es obligatorio (mínimo 2 caracteres)." };
        }
        if (clean.length > 25) {
            return { valido: false, mensaje: "El campo " + campoNombre + " no puede exceder los 25 caracteres." };
        }
        // Solo letras hispanas, tildes, eñes, diéresis, apóstrofe y espacios
        if (!/^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\']+$/.test(clean)) {
            return { valido: false, mensaje: "Solo se permiten letras y espacios (sin números ni símbolos especiales)." };
        }
        return { valido: true, mensaje: "" };
    }

    function validarEmail(email) {
        var clean = normalizarEmail(email);
        if (!clean) {
            return { valido: false, mensaje: "El correo electrónico es obligatorio." };
        }
        if (clean.length > 100) {
            return { valido: false, mensaje: "El correo no puede exceder 100 caracteres." };
        }
        if (clean.indexOf(" ") > -1) {
            return { valido: false, mensaje: "El correo no puede contener espacios en blanco." };
        }
        var regex = /^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$/;
        if (!regex.test(clean)) {
            return { valido: false, mensaje: "Ingrese un correo electrónico válido (ej: usuario@ejemplo.com)." };
        }
        return { valido: true, mensaje: "" };
    }

    function validarFechaNacimiento(fechaStr, debeSerMayorEdad) {
        if (!fechaStr) {
            return { valido: false, mensaje: "La fecha de nacimiento es obligatoria." };
        }
        var fecha = new Date(fechaStr + "T00:00:00");
        var hoy = new Date();
        hoy.setHours(0, 0, 0, 0);

        if (fecha > hoy) {
            return { valido: false, mensaje: "La fecha de nacimiento no puede ser futura." };
        }

        var edad = hoy.getFullYear() - fecha.getFullYear();
        var m = hoy.getMonth() - fecha.getMonth();
        if (m < 0 || (m === 0 && hoy.getDate() < fecha.getDate())) {
            edad--;
        }

        if (edad > 110) {
            return { valido: false, mensaje: "La edad no puede ser superior a 110 años." };
        }

        var categoria = "Adulto";
        var badgeColor = "bg-primary";

        if (edad <= 10) {
            categoria = "Infante (0-10 años)";
            badgeColor = "bg-info text-dark";
        } else if (edad >= 11 && edad <= 17) {
            categoria = "Menor (11-17 años)";
            badgeColor = "bg-warning text-dark";
        } else {
            categoria = "Adulto (18+ años)";
            badgeColor = "bg-success text-white";
        }

        return {
            valido: true,
            edad: edad,
            categoria: categoria,
            badgeColor: badgeColor,
            mensaje: "Edad: " + edad + " años — " + categoria
        };
    }

    // -------------------------------------------------------------------------
    // 3. VALIDACIONES BANCARIAS (PAGO CON TARJETA - DÍGITOS MÍNIMOS)
    // -------------------------------------------------------------------------

    /**
     * Valida el número de tarjeta flexibilizado según dígitos mínimos:
     * - Diners: al menos 14 dígitos numéricos.
     * - American Express / Visa / Mastercard y otras: al menos 15 dígitos numéricos.
     * - Máximo general: 19 dígitos numéricos.
     */
    function validarTarjetaMinimos(numero, marca) {
        if (!numero) return { valido: false, mensaje: "El número de tarjeta es obligatorio." };
        var clean = numero.replace(/\D/g, "");
        var minDigitos = (marca === "DINERS") ? 14 : 15;

        if (clean.length < minDigitos) {
            return { valido: false, mensaje: "El número de tarjeta debe contener al menos " + minDigitos + " dígitos numéricos." };
        }
        if (clean.length > 19) {
            return { valido: false, mensaje: "El número de tarjeta no puede superar los 19 dígitos." };
        }
        return { valido: true, mensaje: "Tarjeta válida (" + (marca || "VISA") + ")." };
    }

    function validarLuhn(numero, marca) {
        var res = validarTarjetaMinimos(numero, marca);
        return res.valido;
    }

    function detectarMarcaTarjeta(numero) {
        var clean = (numero || "").replace(/\D/g, "");
        if (clean.indexOf("4") === 0) return "VISA";
        if (/^(5[1-5]|222[1-9]|22[3-9]\d|2[3-6]\d{2}|27[01]\d|2720)/.test(clean)) return "MASTERCARD";
        if (/^3[47]/.test(clean)) return "AMEX";
        if (/^(36|38|30[0-5])/.test(clean)) return "DINERS";
        return null;
    }

    function validarTitular(titular) {
        var clean = normalizarEspacios(titular);
        if (!clean || clean.length < 5 || clean.length > 50) {
            return { valido: false, mensaje: "El nombre del titular debe contener entre 5 y 50 caracteres." };
        }
        if (!/^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\']+$/.test(clean)) {
            return { valido: false, mensaje: "Solo se permiten letras y espacios en el nombre del titular." };
        }
        var palabras = clean.split(" ");
        if (palabras.length < 2) {
            return { valido: false, mensaje: "Ingrese al menos un nombre y un apellido completos como figura en la tarjeta." };
        }
        return { valido: true, mensaje: "" };
    }

    function validarExpiracion(mes, anio) {
        var m = parseInt(mes, 10);
        var y = parseInt(anio, 10);
        if (y < 100) y += 2000;

        var hoy = new Date();
        var anioActual = hoy.getFullYear();
        var mesActual = hoy.getMonth() + 1;

        if (y < anioActual || (y === anioActual && m < mesActual)) {
            return { valido: false, mensaje: "La tarjeta seleccionada se encuentra expirada." };
        }
        return { valido: true, mensaje: "" };
    }

    function validarCvv(cvv, marca) {
        var clean = (cvv || "").trim();
        var esAmex = marca === "AMEX";
        var lenEsperada = esAmex ? 4 : 3;

        if (!clean || clean.length !== lenEsperada || !/^\d+$/.test(clean)) {
            return {
                valido: false,
                mensaje: "El código CVV debe tener exactamente " + lenEsperada + " dígitos numéricos" + (esAmex ? " para American Express." : ".")
            };
        }
        return { valido: true, mensaje: "" };
    }

    // -------------------------------------------------------------------------
    // 4. HELPERS VISUALES DE BOOTSTRAP 5
    // -------------------------------------------------------------------------

    function marcarValido($input, mensajeOk) {
        $input.removeClass("is-invalid").addClass("is-valid");
        var $feedback = $input.siblings(".invalid-feedback");
        if ($feedback.length) {
            $feedback.text("").hide();
        }
        var $okFeedback = $input.siblings(".valid-feedback");
        if ($okFeedback.length && mensajeOk) {
            $okFeedback.text(mensajeOk).show();
        }
    }

    function marcarInvalido($input, mensajeError) {
        $input.removeClass("is-valid").addClass("is-invalid");
        var $feedback = $input.siblings(".invalid-feedback");
        if (!$feedback.length) {
            $feedback = $('<div class="invalid-feedback d-block"></div>').insertAfter($input);
        }
        $feedback.text(mensajeError).show();
        var $okFeedback = $input.siblings(".valid-feedback");
        if ($okFeedback.length) {
            $okFeedback.hide();
        }
    }

    function limpiarEstado($input) {
        $input.removeClass("is-valid is-invalid");
        $input.siblings(".invalid-feedback, .valid-feedback").hide();
    }

    return {
        normalizarEspacios: normalizarEspacios,
        normalizarNombrePropio: normalizarNombrePropio,
        normalizarEmail: normalizarEmail,
        normalizarDocumento: normalizarDocumento,
        normalizarTelefono: normalizarTelefono,
        validarCedula: validarCedula,
        validarRuc: validarRuc,
        validarPasaporte: validarPasaporte,
        validarTelefonoEcuador: validarTelefonoEcuador,
        validarNombreApellido: validarNombreApellido,
        validarEmail: validarEmail,
        validarFechaNacimiento: validarFechaNacimiento,
        esMayorDeEdad: function (fechaStr) {
            if (!fechaStr) return false;
            var res = validarFechaNacimiento(fechaStr, false);
            return res.valido && res.edad >= 18;
        },
        validarLuhn: validarLuhn,
        validarTarjetaMinimos: validarTarjetaMinimos,
        detectarMarcaTarjeta: detectarMarcaTarjeta,
        validarTitular: validarTitular,
        validarExpiracion: validarExpiracion,
        validarCvv: validarCvv,
        marcarValido: marcarValido,
        marcarInvalido: marcarInvalido,
        limpiarEstado: limpiarEstado
    };
})(jQuery);
