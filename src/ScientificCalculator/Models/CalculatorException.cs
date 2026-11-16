using System;

namespace ScientificCalculator.Models;

/// <summary>Error matemático o de sintaxis con un mensaje apto para el usuario.</summary>
public sealed class CalculatorException : Exception
{
    public CalculatorException(string message) : base(message)
    {
    }
}
