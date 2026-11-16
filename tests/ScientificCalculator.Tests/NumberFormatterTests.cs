using ScientificCalculator.Models;

namespace ScientificCalculator.Tests;

public class NumberFormatterTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(-0.0, "0")]
    [InlineData(5, "5")]
    [InlineData(1234567.5, "1,234,567.5")]
    [InlineData(1000, "1,000")]
    public void Format_UsesGrouping(double value, string expected)
    {
        Assert.Equal(expected, NumberFormatter.Format(value));
    }

    [Fact]
    public void Format_HidesFloatingPointNoise()
    {
        Assert.Equal("0.3", NumberFormatter.Format(0.1 + 0.2));
        Assert.Equal("0.333333333333333", NumberFormatter.Format(1.0 / 3.0));
    }

    [Theory]
    [InlineData(1e20, "1E+20")]
    [InlineData(1e-10, "1E-10")]
    [InlineData(1.234e25, "1.234E+25")]
    public void Format_UsesScientificNotationForExtremes(double value, string expected)
    {
        Assert.Equal(expected, NumberFormatter.Format(value));
    }

    [Fact]
    public void ToRawValue_ProducesParseableText()
    {
        string raw = NumberFormatter.ToRawValue(0.1 + 0.2);
        Assert.Equal(0.3, CalculatorEngine.Evaluate(raw), 12);
    }
}
