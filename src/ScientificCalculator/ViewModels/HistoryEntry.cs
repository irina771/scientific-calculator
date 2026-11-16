namespace ScientificCalculator.ViewModels;

/// <summary>Una operación guardada en el historial.</summary>
public sealed record HistoryEntry(string Expression, string Result, double Value);
