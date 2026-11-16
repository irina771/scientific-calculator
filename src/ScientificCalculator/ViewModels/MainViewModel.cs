using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScientificCalculator.Models;

namespace ScientificCalculator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const int MaxHistoryEntries = 200;

    private string _input = string.Empty;
    private string? _displayOverride;
    private string _lastExpression = string.Empty;
    private double? _answer;
    private bool _hasError;
    private double _memoryValue;

    [ObservableProperty]
    private string displayText = "0";

    [ObservableProperty]
    private string secondaryText = string.Empty;

    [ObservableProperty]
    private double displayFontSize = 44;

    [ObservableProperty]
    private bool hasMemory;

    [ObservableProperty]
    private bool isHistoryOpen;

    [ObservableProperty]
    private bool isSecondActive;

    [ObservableProperty]
    private AngleMode currentAngleMode = AngleMode.Degrees;

    public MainViewModel()
    {
        Refresh();
    }

    public ObservableCollection<HistoryEntry> History { get; } = new();

    public bool IsDegrees => CurrentAngleMode == AngleMode.Degrees;

    public bool IsRadians => CurrentAngleMode == AngleMode.Radians;

    // Etiquetas que cambian con el botón "2nd" (estilo calculadora de Windows).
    public string SinLabel => IsSecondActive ? "sin⁻¹" : "sin";
    public string CosLabel => IsSecondActive ? "cos⁻¹" : "cos";
    public string TanLabel => IsSecondActive ? "tan⁻¹" : "tan";
    public string LogLabel => IsSecondActive ? "10ˣ" : "log";
    public string LnLabel => IsSecondActive ? "eˣ" : "ln";
    public string SqrtLabel => IsSecondActive ? "∛" : "√";
    public string SquareLabel => IsSecondActive ? "x³" : "x²";

    partial void OnCurrentAngleModeChanged(AngleMode value)
    {
        OnPropertyChanged(nameof(IsDegrees));
        OnPropertyChanged(nameof(IsRadians));
        Refresh();
    }

    partial void OnIsSecondActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(SinLabel));
        OnPropertyChanged(nameof(CosLabel));
        OnPropertyChanged(nameof(TanLabel));
        OnPropertyChanged(nameof(LogLabel));
        OnPropertyChanged(nameof(LnLabel));
        OnPropertyChanged(nameof(SqrtLabel));
        OnPropertyChanged(nameof(SquareLabel));
    }

    // --------------------------------------------------------------- entrada

    [RelayCommand]
    public void InputDigit(string? digit)
    {
        if (string.IsNullOrEmpty(digit))
            return;

        BeginNewInput(continueFromAnswer: false);

        if (CurrentNumberIsBareZero())
            _input = _input[..^1];

        _input += digit;
        Refresh();
    }

    [RelayCommand]
    public void InputDecimal()
    {
        BeginNewInput(continueFromAnswer: false);

        if (_input.Length > 0 && _input[^1] is 'e' or 'E')
            return;

        if (_input.Length > 0 && (char.IsDigit(_input[^1]) || _input[^1] == '.'))
        {
            int start = _input.Length - 1;
            while (start >= 0 && (char.IsDigit(_input[start]) || _input[start] == '.'))
                start--;

            string number = _input[(start + 1)..];

            if (number.Contains('.'))
                return;

            _input += ".";
        }
        else
        {
            _input += "0.";
        }

        Refresh();
    }

    [RelayCommand]
    public void InputOperator(string? op)
    {
        if (string.IsNullOrEmpty(op))
            return;

        BeginNewInput(continueFromAnswer: true);

        if (_input.Length == 0)
        {
            if (op == "-")
            {
                _input = "-";
                Refresh();
                return;
            }

            _input = "0";
        }

        char last = _input[^1];

        if (last is '+' or '-' or '*' or '/' or '^')
        {
            // Permite escribir un exponente negativo: 2^-3
            if (op == "-" && last == '^')
            {
                _input += "-";
                Refresh();
                return;
            }

            _input = _input[..^1] + op;
        }
        else if (last == '(')
        {
            _input += op == "-" ? "-" : "0" + op;
        }
        else
        {
            _input += op;
        }

        Refresh();
    }

    [RelayCommand]
    public void InputFunction(string? function)
    {
        if (string.IsNullOrEmpty(function))
            return;

        BeginNewInput(continueFromAnswer: false);

        if (NeedsImplicitMultiplication())
            _input += "×";

        _input += function + "(";
        Refresh();
    }

    [RelayCommand]
    public void InputConstant(string? constant)
    {
        if (string.IsNullOrEmpty(constant))
            return;

        BeginNewInput(continueFromAnswer: false);

        if (NeedsImplicitMultiplication())
            _input += "×";

        _input += constant;
        Refresh();
    }

    [RelayCommand]
    public void InputOpenParen()
    {
        BeginNewInput(continueFromAnswer: false);

        if (NeedsImplicitMultiplication())
            _input += "×";

        _input += "(";
        Refresh();
    }

    [RelayCommand]
    public void InputCloseParen()
    {
        BeginNewInput(continueFromAnswer: false);

        if (_input.Length == 0)
            return;

        if (_input[^1] is '(')
            return;

        _input += ")";
        Refresh();
    }

    [RelayCommand]
    public void InputFactorial()
    {
        BeginNewInput(continueFromAnswer: true);

        if (_input.Length == 0 || _input[^1] is '+')
            return;

        _input += "!";
        Refresh();
    }

    [RelayCommand]
    public void InputPercent()
    {
        BeginNewInput(continueFromAnswer: true);

        if (_input.Length == 0)
            return;

        _input += "%";
        Refresh();
    }

    [RelayCommand]
    public void InputPower()
    {
        InputOperator("^");
    }

    /// <summary>Botón EXP: añade una potencia de diez al número actual (1,5 EXP 3 = 1,5e3).</summary>
    [RelayCommand]
    public void InputExp()
    {
        BeginNewInput(continueFromAnswer: false);

        if (_input.Length == 0)
        {
            _input = "1e";
        }
        else if (char.IsDigit(_input[^1]) || _input[^1] == '.')
        {
            _input += "e";
        }

        Refresh();
    }

    [RelayCommand]
    public void InputReciprocal()
    {
        BeginNewInput(continueFromAnswer: true);

        if (_input.Length == 0)
            return;

        char last = _input[^1];
        if (char.IsDigit(last) || last is ')' or 'π' or '!' or '%')
            _input += "^-1";

        Refresh();
    }

    [RelayCommand]
    public void InputAbs() => InputFunction("abs");

    [RelayCommand]
    public void InputAns()
    {
        BeginNewInput(continueFromAnswer: false);

        if (NeedsImplicitMultiplication())
            _input += "×";

        _input += _answer.HasValue ? NumberFormatter.ToRawValue(_answer.Value) : "0";
        Refresh();
    }

    [RelayCommand]
    public void ToggleSign()
    {
        if (_hasError)
            ResetState();

        if (_displayOverride is not null)
        {
            if (_answer.HasValue)
            {
                _input = NumberFormatter.ToRawValue(-_answer.Value);
                _displayOverride = null;
                _lastExpression = string.Empty;
            }
            else
            {
                ResetState();
            }

            Refresh();
            return;
        }

        if (_input.Length == 0)
        {
            _input = "-";
            Refresh();
            return;
        }

        // Niega el último operando: 2+3 -> 2+(-3)
        int start = LastOperandStart();

        if (start < 0)
        {
            _input = "-(" + _input + ")";
        }
        else if (_input[start] == '-')
        {
            _input = _input.Remove(start, 1);
        }
        else
        {
            _input = _input.Insert(start, "(-") + ")";
        }

        Refresh();
    }

    [RelayCommand]
    public void Backspace()
    {
        if (_hasError || _displayOverride is not null)
        {
            ResetState();
            Refresh();
            return;
        }

        if (_input.Length == 0)
            return;

        // Borra la función completa: "sin(" -> ""
        if (_input.EndsWith('('))
        {
            int index = _input.Length - 2;
            int end = index;

            while (index >= 0 && char.IsLetter(_input[index]))
                index--;

            if (end > index)
            {
                _input = _input[..(index + 1)];
                Refresh();
                return;
            }
        }

        _input = _input[..^1];
        Refresh();
    }

    [RelayCommand]
    public void ClearAll()
    {
        ResetState();
        Refresh();
    }

    [RelayCommand]
    public void ClearEntry()
    {
        _input = string.Empty;
        _displayOverride = null;
        _lastExpression = string.Empty;
        _hasError = false;
        Refresh();
    }

    [RelayCommand]
    public void CalculateEquals()
    {
        if (_hasError)
        {
            ResetState();
            Refresh();
            return;
        }

        if (_input.Length == 0)
            return;

        string expression = CloseOpenParentheses(_input);

        try
        {
            double value = CalculatorEngine.Evaluate(expression, CurrentAngleMode);
            string prettyExpression = CalculatorEngine.Prettify(expression);
            string formatted = NumberFormatter.Format(value);

            _answer = value;
            _lastExpression = prettyExpression;
            _displayOverride = formatted;
            _input = string.Empty;

            History.Insert(0, new HistoryEntry(prettyExpression, formatted, value));

            while (History.Count > MaxHistoryEntries)
                History.RemoveAt(History.Count - 1);
        }
        catch (CalculatorException exception)
        {
            _hasError = true;
            _lastExpression = CalculatorEngine.Prettify(expression);
            _displayOverride = exception.Message;
            _input = string.Empty;
        }

        Refresh();
    }

    // ------------------------------------------------------------ científico

    [RelayCommand]
    public void Science(string? key)
    {
        switch (key)
        {
            case "sin":
                InputFunction(IsSecondActive ? "asin" : "sin");
                break;
            case "cos":
                InputFunction(IsSecondActive ? "acos" : "cos");
                break;
            case "tan":
                InputFunction(IsSecondActive ? "atan" : "tan");
                break;
            case "log":
                if (IsSecondActive)
                    AppendRawToken("10^(");
                else
                    InputFunction("log");
                break;
            case "ln":
                InputFunction(IsSecondActive ? "exp" : "ln");
                break;
            case "sqrt":
                InputFunction(IsSecondActive ? "cbrt" : "sqrt");
                break;
            case "square":
                AppendRawToken(IsSecondActive ? "^3" : "^2", requireOperand: true);
                break;
            case "abs":
                InputFunction("abs");
                break;
        }
    }

    private void AppendRawToken(string token, bool requireOperand = false)
    {
        BeginNewInput(continueFromAnswer: !requireOperand);

        if (requireOperand && _input.Length == 0)
            return;

        if (!token.StartsWith('^') && NeedsImplicitMultiplication())
            _input += "×";

        _input += token;
        Refresh();
    }

    // --------------------------------------------------------------- memoria

    [RelayCommand]
    public void MemoryClear()
    {
        _memoryValue = 0;
        HasMemory = false;
    }

    [RelayCommand]
    public void MemoryRecall()
    {
        if (!HasMemory)
            return;

        BeginNewInput(continueFromAnswer: false);

        if (NeedsImplicitMultiplication())
            _input += "×";

        _input += NumberFormatter.ToRawValue(_memoryValue);
        Refresh();
    }

    [RelayCommand]
    public void MemoryAdd()
    {
        _memoryValue += CurrentValue();
        HasMemory = true;
    }

    [RelayCommand]
    public void MemorySubtract()
    {
        _memoryValue -= CurrentValue();
        HasMemory = true;
    }

    [RelayCommand]
    public void MemoryStore()
    {
        _memoryValue = CurrentValue();
        HasMemory = true;
    }

    // -------------------------------------------------------------- historial

    [RelayCommand]
    public void ToggleHistory()
    {
        IsHistoryOpen = !IsHistoryOpen;
    }

    [RelayCommand]
    public void ClearHistory()
    {
        History.Clear();
    }

    public void UseHistoryValue(double value)
    {
        ResetState();
        _input = NumberFormatter.ToRawValue(value);
        Refresh();
    }

    // ----------------------------------------------------------------- modos

    [RelayCommand]
    public void SetDegrees() => CurrentAngleMode = AngleMode.Degrees;

    [RelayCommand]
    public void SetRadians() => CurrentAngleMode = AngleMode.Radians;

    [RelayCommand]
    public void ToggleAngleMode() =>
        CurrentAngleMode = CurrentAngleMode == AngleMode.Degrees ? AngleMode.Radians : AngleMode.Degrees;

    [RelayCommand]
    public void ToggleSecond()
    {
        IsSecondActive = !IsSecondActive;
    }

    // --------------------------------------------------------------- interno

    private void BeginNewInput(bool continueFromAnswer)
    {
        if (_hasError)
            ResetState();

        if (_displayOverride is null)
            return;

        _input = continueFromAnswer && _answer.HasValue
            ? NumberFormatter.ToRawValue(_answer.Value)
            : string.Empty;

        _displayOverride = null;
        _lastExpression = string.Empty;
    }

    private void ResetState()
    {
        _input = string.Empty;
        _displayOverride = null;
        _lastExpression = string.Empty;
        _answer = null;
        _hasError = false;
    }

    private void Refresh()
    {
        if (_hasError)
        {
            DisplayText = _displayOverride ?? "Error";
            SecondaryText = _lastExpression;
            DisplayFontSize = 22;
            return;
        }

        if (_displayOverride is not null)
        {
            DisplayText = _displayOverride;
            SecondaryText = _lastExpression;
            DisplayFontSize = FitFontSize(_displayOverride);
            return;
        }

        if (_input.Length == 0)
        {
            DisplayText = "0";
            SecondaryText = string.Empty;
            DisplayFontSize = 44;
            return;
        }

        DisplayText = CalculatorEngine.Prettify(_input);

        SecondaryText = CalculatorEngine.TryEvaluate(_input, CurrentAngleMode, out double preview)
            ? "= " + NumberFormatter.Format(preview)
            : string.Empty;

        DisplayFontSize = FitFontSize(DisplayText);
    }

    private static double FitFontSize(string text) => text.Length switch
    {
        > 24 => 22,
        > 16 => 27,
        _ => 44
    };

    private double CurrentValue()
    {
        if (_displayOverride is not null && _answer.HasValue)
            return _answer.Value;

        if (_input.Length > 0 && CalculatorEngine.TryEvaluate(_input, CurrentAngleMode, out double value))
            return value;

        return 0;
    }

    private bool CurrentNumberIsBareZero()
    {
        if (_input.Length == 0 || _input[^1] != '0')
            return false;

        return _input.Length == 1 || (!char.IsDigit(_input[^2]) && _input[^2] != '.');
    }

    private bool NeedsImplicitMultiplication()
    {
        if (_input.Length == 0)
            return false;

        char last = _input[^1];
        return char.IsDigit(last) || last is ')' or '!' or '%' or 'π';
    }

    /// <summary>Cierra los paréntesis que falten al pulsar "=", como la calculadora de Windows.</summary>
    private static string CloseOpenParentheses(string expression)
    {
        int balance = 0;

        foreach (char character in expression)
        {
            if (character == '(')
                balance++;
            else if (character == ')')
                balance--;
        }

        return balance > 0 ? expression + new string(')', balance) : expression;
    }

    /// <summary>Índice donde empieza el último operando de la expresión.</summary>
    private int LastOperandStart()
    {
        int depth = 0;

        for (int i = _input.Length - 1; i >= 0; i--)
        {
            char c = _input[i];

            if (c == ')')
            {
                depth++;
            }
            else if (c == '(')
            {
                if (depth == 0)
                    return i + 1;
                depth--;
            }
            else if (depth == 0 && c is '+' or '-' or '*' or '/' or '^')
            {
                return i + 1;
            }
        }

        return 0;
    }
}
