using ScientificCalculator.Models;

namespace ScientificCalculator.Tests;

public class CalculatorEngineTests
{
    private static double Evaluate(string expression, AngleMode mode = AngleMode.Degrees) =>
        CalculatorEngine.Evaluate(expression, mode);

    [Theory]
    [InlineData("2+3", 5)]
    [InlineData("7-10", -3)]
    [InlineData("6*7", 42)]
    [InlineData("9/4", 2.25)]
    [InlineData("2+3*4", 14)]
    [InlineData("(2+3)*4", 20)]
    [InlineData("100/5/2", 10)]
    [InlineData("10-2-3", 5)]
    public void BasicOperations_RespectPrecedence(string expression, double expected)
    {
        Assert.Equal(expected, Evaluate(expression), 10);
    }

    [Theory]
    [InlineData("2^10", 1024)]
    [InlineData("2^3^2", 512)]
    [InlineData("9^0.5", 3)]
    [InlineData("5^-1", 0.2)]
    [InlineData("-2^2", -4)]
    [InlineData("(-2)^2", 4)]
    public void Powers_AreRightAssociativeAndSupportNegatives(string expression, double expected)
    {
        Assert.Equal(expected, Evaluate(expression), 10);
    }

    [Fact]
    public void ImplicitMultiplication_IsSupported()
    {
        Assert.Equal(2 * Math.PI, Evaluate("2π"), 10);
        Assert.Equal(15, Evaluate("3(2+3)"), 10);
        Assert.Equal(2 * Math.E, Evaluate("2e"), 10);
    }

    [Theory]
    [InlineData("sin(30)", 0.5)]
    [InlineData("cos(60)", 0.5)]
    [InlineData("tan(45)", 1)]
    [InlineData("sin(90)", 1)]
    [InlineData("cos(180)", -1)]
    public void Trigonometry_UsesDegreesByDefault(string expression, double expected)
    {
        Assert.Equal(expected, Evaluate(expression), 9);
    }

    [Fact]
    public void Trigonometry_UsesRadiansWhenSelected()
    {
        Assert.Equal(1, Evaluate("sin(π/2)", AngleMode.Radians), 10);
        Assert.Equal(0.5, Evaluate("sin(π/6)", AngleMode.Radians), 10);
    }

    [Theory]
    [InlineData("asin(1)", 90)]
    [InlineData("acos(0)", 90)]
    [InlineData("atan(1)", 45)]
    [InlineData("arcsin(0.5)", 30)]
    public void InverseTrigonometry_ReturnsDegrees(string expression, double expected)
    {
        Assert.Equal(expected, Evaluate(expression), 9);
    }

    [Theory]
    [InlineData("log(1000)", 3)]
    [InlineData("log(1)", 0)]
    [InlineData("ln(e)", 1)]
    [InlineData("sqrt(144)", 12)]
    [InlineData("cbrt(27)", 3)]
    [InlineData("abs(-7)", 7)]
    [InlineData("exp(0)", 1)]
    public void ScientificFunctions_Work(string expression, double expected)
    {
        Assert.Equal(expected, Evaluate(expression), 9);
    }

    [Theory]
    [InlineData("5!", 120)]
    [InlineData("0!", 1)]
    [InlineData("10!", 3628800)]
    [InlineData("3!!", 720)]
    public void Factorial_Works(string expression, double expected)
    {
        Assert.Equal(expected, Evaluate(expression), 0);
    }

    [Fact]
    public void Factorial_Of170_IsFinite()
    {
        Assert.True(double.IsFinite(Evaluate("170!")));
    }

    [Fact]
    public void Percent_IsRelativeInAdditionAndSubtraction()
    {
        Assert.Equal(220, Evaluate("200+10%"), 10);
        Assert.Equal(180, Evaluate("200-10%"), 10);
        Assert.Equal(20, Evaluate("200*10%"), 10);
        Assert.Equal(2000, Evaluate("200/10%"), 10);
    }

    [Fact]
    public void Constants_AreAvailable()
    {
        Assert.Equal(Math.PI, Evaluate("π"), 12);
        Assert.Equal(Math.E, Evaluate("e"), 12);
    }

    [Theory]
    [InlineData("1.5e3+2", 1502)]
    [InlineData("2E-3*1000", 2)]
    [InlineData("1e2", 100)]
    public void ScientificNotation_IsSupported(string expression, double expected)
    {
        Assert.Equal(expected, Evaluate(expression), 10);
    }

    [Theory]
    [InlineData("1/0")]
    [InlineData("sqrt(-1)")]
    [InlineData("log(0)")]
    [InlineData("ln(-5)")]
    [InlineData("asin(2)")]
    [InlineData("acos(-1.5)")]
    [InlineData("tan(90)")]
    [InlineData("tan(270)")]
    [InlineData("3.5!")]
    [InlineData("(-2)!")]
    [InlineData("171!")]
    [InlineData("(2+3")]
    [InlineData("2+")]
    [InlineData("foo(2)")]
    public void InvalidExpressions_ThrowFriendlyErrors(string expression)
    {
        var exception = Assert.Throws<CalculatorException>(() => Evaluate(expression));
        Assert.False(string.IsNullOrWhiteSpace(exception.Message));
    }

    [Fact]
    public void Tangent_IsDefinedForEquivalentAngles()
    {
        Assert.Equal(0, Evaluate("tan(180)"), 9);
        Assert.Equal(1, Evaluate("tan(225)"), 9);
    }

    [Fact]
    public void TryEvaluate_ReturnsFalseOnErrors()
    {
        Assert.False(CalculatorEngine.TryEvaluate("2+", AngleMode.Degrees, out _));
        Assert.True(CalculatorEngine.TryEvaluate("2+2", AngleMode.Degrees, out double result));
        Assert.Equal(4, result, 10);
    }

    [Fact]
    public void Prettify_UsesMathSymbols()
    {
        Assert.Equal("2×3÷4", CalculatorEngine.Prettify("2*3/4"));
        Assert.Equal("sin⁻¹(0.5)", CalculatorEngine.Prettify("asin(0.5)"));
    }
}
