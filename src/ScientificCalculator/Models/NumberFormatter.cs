using System;
using System.Globalization;

namespace ScientificCalculator.Models;

/// <summary>Da formato a los resultados siguiendo el estilo de la calculadora de Windows.</summary>
public static class NumberFormatter
{
    /// <summary>Formato de pantalla: hasta 15 decimales, separador de miles y notación científica para extremos.</summary>
    public static string Format(double value)
    {
        if (value == 0 || double.IsNaN(value))
            return "0";

        if (double.IsInfinity(value))
            return value > 0 ? "∞" : "−∞";

        double absolute = Math.Abs(value);

        if (absolute >= 1e15 || absolute < 1e-9)
            return value.ToString("0.##############E+0", CultureInfo.InvariantCulture);

        return value.ToString("#,##0.###############", CultureInfo.InvariantCulture);
    }

    /// <summary>Valor sin formato para reutilizarlo en una nueva expresión.</summary>
    public static string ToRawValue(double value) =>
        value.ToString("G15", CultureInfo.InvariantCulture);
}
