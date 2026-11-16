using System.Globalization;

namespace ScientificCalculator.Models;

/// <summary>
/// Motor de cálculo de la calculadora.
///
/// Acepta expresiones completas con paréntesis y prioridad de operaciones:
///   + - * / ^  ( ), factorial (!), porcentaje (%), constantes (π, e),
///   notación científica (1.5e3) y funciones trigonométricas, logarítmicas
///   y de raíz. La multiplicación implícita está permitida: 2π, 3(4+1).
/// </summary>
public static class CalculatorEngine
{
    private const double MaxFactorialInput = 170; // 170! es el mayor factorial finito en double

    /// <summary>Evalúa una expresión y devuelve el resultado.</summary>
    /// <exception cref="CalculatorException">Si la expresión no es válida o el resultado es indeterminado.</exception>
    public static double Evaluate(string expression, AngleMode angleMode = AngleMode.Degrees)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new CalculatorException("Escribe una operación");

        var tokens = Tokenize(expression);
        var root = new Parser(tokens).Parse();
        double result = EvaluateNode(root, angleMode);

        if (double.IsNaN(result))
            throw new CalculatorException("Resultado indefinido");

        if (double.IsInfinity(result))
            throw new CalculatorException("Número demasiado grande");

        return result;
    }

    /// <summary>Comprueba si una expresión se puede evaluar sin lanzar excepción.</summary>
    public static bool TryEvaluate(string expression, AngleMode angleMode, out double result)
    {
        try
        {
            result = Evaluate(expression, angleMode);
            return true;
        }
        catch (CalculatorException)
        {
            result = 0;
            return false;
        }
    }

    // ---------------------------------------------------------------- lexer

    private enum TokenType
    {
        Number,
        Constant,
        Function,
        Plus,
        Minus,
        Star,
        Slash,
        Power,
        OpenParen,
        CloseParen,
        Factorial,
        Percent,
        End
    }

    private readonly record struct Token(TokenType Type, double Number, string Text);

    private static List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        int i = 0;

        while (i < input.Length)
        {
            char c = input[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (char.IsDigit(c) || c == '.')
            {
                int start = i;
                bool hasDot = false;

                while (i < input.Length && (char.IsDigit(input[i]) || input[i] == '.'))
                {
                    if (input[i] == '.')
                    {
                        if (hasDot)
                            throw new CalculatorException("Número no válido");
                        hasDot = true;
                    }
                    i++;
                }

                // Exponente científico: 1.5e3, 2E-4
                if (i < input.Length && (input[i] == 'e' || input[i] == 'E'))
                {
                    int exponentStart = i;
                    int j = i + 1;

                    if (j < input.Length && (input[j] == '+' || input[j] == '-'))
                        j++;

                    if (j < input.Length && char.IsDigit(input[j]))
                    {
                        i = j;
                        while (i < input.Length && char.IsDigit(input[i]))
                            i++;
                    }
                    else
                    {
                        i = exponentStart; // la 'e' es la constante de Euler
                    }
                }

                string text = input[start..i];
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    throw new CalculatorException("Número no válido");

                tokens.Add(new Token(TokenType.Number, value, text));
                continue;
            }

            if (char.IsLetter(c) || c == 'π' || c == 'Π')
            {
                if (c == 'π' || c == 'Π')
                {
                    tokens.Add(new Token(TokenType.Constant, Math.PI, "π"));
                    i++;
                    continue;
                }

                int start = i;
                while (i < input.Length && char.IsLetter(input[i]))
                    i++;

                string name = input[start..i];
                switch (name.ToLowerInvariant())
                {
                    case "pi":
                        tokens.Add(new Token(TokenType.Constant, Math.PI, "π"));
                        break;
                    case "e":
                        tokens.Add(new Token(TokenType.Constant, Math.E, "e"));
                        break;
                    case "sin":
                    case "cos":
                    case "tan":
                    case "asin":
                    case "acos":
                    case "atan":
                    case "arcsin":
                    case "arccos":
                    case "arctan":
                    case "log":
                    case "ln":
                    case "sqrt":
                    case "cbrt":
                    case "abs":
                    case "exp":
                        tokens.Add(new Token(TokenType.Function, 0, CanonicalFunction(name)));
                        break;
                    default:
                        throw new CalculatorException($"Función desconocida: {name}");
                }
                continue;
            }

            switch (c)
            {
                case '+':
                    tokens.Add(new Token(TokenType.Plus, 0, "+"));
                    i++;
                    break;
                case '-' or '−':
                    tokens.Add(new Token(TokenType.Minus, 0, "-"));
                    i++;
                    break;
                case '*':
                case '×':
                case '·':
                    tokens.Add(new Token(TokenType.Star, 0, "*"));
                    i++;
                    break;
                case '/':
                case '÷':
                    tokens.Add(new Token(TokenType.Slash, 0, "/"));
                    i++;
                    break;
                case '^':
                    tokens.Add(new Token(TokenType.Power, 0, "^"));
                    i++;
                    break;
                case '(':
                    tokens.Add(new Token(TokenType.OpenParen, 0, "("));
                    i++;
                    break;
                case ')':
                    tokens.Add(new Token(TokenType.CloseParen, 0, ")"));
                    i++;
                    break;
                case '!':
                    tokens.Add(new Token(TokenType.Factorial, 0, "!"));
                    i++;
                    break;
                case '%':
                    tokens.Add(new Token(TokenType.Percent, 0, "%"));
                    i++;
                    break;
                case '√':
                    tokens.Add(new Token(TokenType.Function, 0, "sqrt"));
                    i++;
                    break;
                case '∛':
                    tokens.Add(new Token(TokenType.Function, 0, "cbrt"));
                    i++;
                    break;
                default:
                    throw new CalculatorException($"Carácter no válido: {c}");
            }
        }

        tokens.Add(new Token(TokenType.End, 0, ""));
        return tokens;
    }

    private static string CanonicalFunction(string name) => name.ToLowerInvariant() switch
    {
        "arcsin" => "asin",
        "arccos" => "acos",
        "arctan" => "atan",
        var other => other
    };

    // --------------------------------------------------------------- parser
    // Gramática:
    //   expresión  := término (("+" | "-") término)*
    //   término    := unario (("*" | "/") unario | unario implícito)*
    //   unario     := ("+" | "-") unario | potencia
    //   potencia   := postfijo ("^" unario)?          (asociativa a la derecha)
    //   postfijo   := primario ("!" | "%")*
    //   primario   := número | constante | función "(" expresión ")" | "(" expresión ")"

    private abstract record Node;

    private sealed record NumberNode(double Value) : Node;

    private sealed record UnaryNode(Node Operand) : Node;

    private sealed record BinaryNode(TokenType Operator, Node Left, Node Right) : Node;

    private sealed record FunctionNode(string Name, Node Argument) : Node;

    private sealed record PostfixNode(TokenType Operator, Node Operand) : Node;

    private sealed class Parser(List<Token> tokens)
    {
        private int _position;

        private Token Current => tokens[_position];

        public Node Parse()
        {
            if (Current.Type == TokenType.End)
                throw new CalculatorException("Escribe una operación");

            var node = ParseAdditive();

            if (Current.Type != TokenType.End)
            {
                throw Current.Type == TokenType.CloseParen
                    ? new CalculatorException("Paréntesis sin abrir")
                    : new CalculatorException("Expresión incompleta");
            }

            return node;
        }

        private Node ParseAdditive()
        {
            var left = ParseMultiplicative();

            while (Current.Type is TokenType.Plus or TokenType.Minus)
            {
                var op = Current.Type;
                _position++;
                var right = ParseMultiplicative();
                left = new BinaryNode(op, left, right);
            }

            return left;
        }

        private Node ParseMultiplicative()
        {
            var left = ParseUnary();

            while (true)
            {
                if (Current.Type is TokenType.Star or TokenType.Slash)
                {
                    var op = Current.Type;
                    _position++;
                    left = new BinaryNode(op, left, ParseUnary());
                    continue;
                }

                // Multiplicación implícita: 2π, 3(4+1), 2sin(30)
                if (StartsPrimary(Current.Type))
                {
                    left = new BinaryNode(TokenType.Star, left, ParseUnary());
                    continue;
                }

                break;
            }

            return left;
        }

        private Node ParseUnary()
        {
            if (Current.Type == TokenType.Plus)
            {
                _position++;
                return ParseUnary();
            }

            if (Current.Type == TokenType.Minus)
            {
                _position++;
                return new UnaryNode(ParseUnary());
            }

            return ParsePower();
        }

        private Node ParsePower()
        {
            var left = ParsePostfix();

            if (Current.Type == TokenType.Power)
            {
                _position++;
                // Asociativa a la derecha y admite exponente negativo: 2^-3, 2^3^2.
                return new BinaryNode(TokenType.Power, left, ParseUnary());
            }

            return left;
        }

        private Node ParsePostfix()
        {
            var operand = ParsePrimary();

            while (Current.Type is TokenType.Factorial or TokenType.Percent)
            {
                var op = Current.Type;
                _position++;
                operand = new PostfixNode(op, operand);
            }

            return operand;
        }

        private Node ParsePrimary()
        {
            var token = Current;

            switch (token.Type)
            {
                case TokenType.Number:
                    _position++;
                    return new NumberNode(token.Number);

                case TokenType.Constant:
                    _position++;
                    return new NumberNode(token.Number);

                case TokenType.OpenParen:
                {
                    _position++;
                    var inner = ParseAdditive();
                    if (Current.Type != TokenType.CloseParen)
                        throw new CalculatorException("Falta cerrar un paréntesis");
                    _position++;
                    return inner;
                }

                case TokenType.Function:
                {
                    _position++;
                    if (Current.Type != TokenType.OpenParen)
                        throw new CalculatorException($"La función {token.Text} necesita paréntesis");
                    _position++;
                    var argument = ParseAdditive();
                    if (Current.Type != TokenType.CloseParen)
                        throw new CalculatorException("Falta cerrar un paréntesis");
                    _position++;
                    return new FunctionNode(token.Text, argument);
                }

                case TokenType.End:
                    throw new CalculatorException("Expresión incompleta");

                default:
                    throw new CalculatorException("Operador sin operando");
            }
        }

        private static bool StartsPrimary(TokenType type) =>
            type is TokenType.Number or TokenType.Constant or TokenType.Function or TokenType.OpenParen;
    }

    // ------------------------------------------------------------ evaluator

    private static double EvaluateNode(Node node, AngleMode mode) => node switch
    {
        NumberNode number => number.Value,
        UnaryNode unary => -EvaluateNode(unary.Operand, mode),
        FunctionNode function => EvaluateFunction(function.Name, EvaluateNode(function.Argument, mode), mode),
        PostfixNode postfix => EvaluatePostfix(postfix, mode),
        BinaryNode binary => EvaluateBinary(binary, mode),
        _ => throw new CalculatorException("Expresión no válida")
    };

    private static double EvaluatePostfix(PostfixNode postfix, AngleMode mode)
    {
        double value = EvaluateNode(postfix.Operand, mode);

        if (postfix.Operator == TokenType.Percent)
            return value / 100.0;

        return Factorial(value);
    }

    private static double EvaluateBinary(BinaryNode binary, AngleMode mode)
    {
        double left = EvaluateNode(binary.Left, mode);

        // Porcentaje relativo, como en la calculadora de Windows:
        //   200 + 10%  ->  200 + 20
        //   200 - 10%  ->  200 - 20
        if (binary.Operator is TokenType.Plus or TokenType.Minus &&
            binary.Right is PostfixNode { Operator: TokenType.Percent } percent)
        {
            double percentage = EvaluateNode(percent.Operand, mode);
            double delta = left * percentage / 100.0;
            return binary.Operator == TokenType.Plus ? left + delta : left - delta;
        }

        double right = EvaluateNode(binary.Right, mode);

        switch (binary.Operator)
        {
            case TokenType.Plus:
                return left + right;
            case TokenType.Minus:
                return left - right;
            case TokenType.Star:
                return left * right;
            case TokenType.Slash:
                if (right == 0)
                    throw new CalculatorException("No se puede dividir entre cero");
                return left / right;
            case TokenType.Power:
                return Power(left, right);
            default:
                throw new CalculatorException("Operador no válido");
        }
    }

    private static double Power(double baseValue, double exponent)
    {
        if (baseValue == 0 && exponent < 0)
            throw new CalculatorException("No se puede dividir entre cero");

        double result = Math.Pow(baseValue, exponent);

        if (double.IsNaN(result))
            throw new CalculatorException("Resultado indefinido");

        return result;
    }

    private static double EvaluateFunction(string name, double argument, AngleMode mode)
    {
        switch (name)
        {
            case "sin":
                return RoundTrig(Math.Sin(ToRadians(argument, mode)));
            case "cos":
                return RoundTrig(Math.Cos(ToRadians(argument, mode)));
            case "tan":
                return Tangent(argument, mode);
            case "asin":
                if (argument < -1 || argument > 1)
                    throw new CalculatorException("Dominio inválido para sin⁻¹");
                return FromRadians(Math.Asin(argument), mode);
            case "acos":
                if (argument < -1 || argument > 1)
                    throw new CalculatorException("Dominio inválido para cos⁻¹");
                return FromRadians(Math.Acos(argument), mode);
            case "atan":
                return FromRadians(Math.Atan(argument), mode);
            case "log":
                if (argument <= 0)
                    throw new CalculatorException("El logaritmo requiere un número positivo");
                return Math.Log10(argument);
            case "ln":
                if (argument <= 0)
                    throw new CalculatorException("El logaritmo requiere un número positivo");
                return Math.Log(argument);
            case "sqrt":
                if (argument < 0)
                    throw new CalculatorException("No existe la raíz cuadrada de un número negativo");
                return Math.Sqrt(argument);
            case "cbrt":
                return Math.Cbrt(argument);
            case "abs":
                return Math.Abs(argument);
            case "exp":
                return Math.Exp(argument);
            default:
                throw new CalculatorException($"Función desconocida: {name}");
        }
    }

    private static double Tangent(double angle, AngleMode mode)
    {
        if (mode == AngleMode.Degrees)
        {
            double normalized = angle % 180.0;
            if (Math.Abs(Math.Abs(normalized) - 90.0) < 1e-9)
                throw new CalculatorException("La tangente no está definida en 90° + 180°k");
        }
        else
        {
            double cosine = Math.Cos(angle);
            if (Math.Abs(cosine) < 1e-12)
                throw new CalculatorException("La tangente no está definida aquí");
        }

        double result = Math.Tan(ToRadians(angle, mode));

        if (double.IsNaN(result) || double.IsInfinity(result) || Math.Abs(result) > 1e15)
            throw new CalculatorException("La tangente no está definida aquí");

        return RoundTrig(result);
    }

    private static double ToRadians(double angle, AngleMode mode) =>
        mode == AngleMode.Degrees ? angle * Math.PI / 180.0 : angle;

    private static double FromRadians(double angle, AngleMode mode) =>
        mode == AngleMode.Degrees ? angle * 180.0 / Math.PI : angle;

    /// <summary>Elimina el ruido de coma flotante (sin 30° = 0.49999999999999994).</summary>
    private static double RoundTrig(double value)
    {
        if (Math.Abs(value) < 1e-12)
            return 0;

        return Math.Round(value, 12);
    }

    private static double Factorial(double value)
    {
        if (value < 0 || Math.Abs(value - Math.Round(value)) > 1e-9)
            throw new CalculatorException("El factorial requiere un entero no negativo");

        if (value > MaxFactorialInput)
            throw new CalculatorException("El factorial es demasiado grande");

        int n = (int)Math.Round(value);
        double result = 1;

        for (int i = 2; i <= n; i++)
            result *= i;

        return result;
    }

    /// <summary>Texto listo para mostrar en pantalla (símbolos matemáticos).</summary>
    public static string Prettify(string expression)
    {
        if (string.IsNullOrEmpty(expression))
            return string.Empty;

        return expression
            .Replace("asin(", "sin⁻¹(")
            .Replace("acos(", "cos⁻¹(")
            .Replace("atan(", "tan⁻¹(")
            .Replace("sqrt(", "√(")
            .Replace("cbrt(", "∛(")
            .Replace("exp(", "e^(")
            .Replace("*", "×")
            .Replace("/", "÷")
            .Replace("-", "−");
    }
}
