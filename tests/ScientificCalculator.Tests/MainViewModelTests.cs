using ScientificCalculator.Models;
using ScientificCalculator.ViewModels;

namespace ScientificCalculator.Tests;

public class MainViewModelTests
{
    private static MainViewModel Type(string expression)
    {
        var viewModel = new MainViewModel();

        foreach (char character in expression)
        {
            switch (character)
            {
                case >= '0' and <= '9':
                    viewModel.InputDigit(character.ToString());
                    break;
                case '.':
                    viewModel.InputDecimal();
                    break;
                case '+':
                case '-':
                case '*':
                case '/':
                case '^':
                    viewModel.InputOperator(character.ToString());
                    break;
                case '(':
                    viewModel.InputOpenParen();
                    break;
                case ')':
                    viewModel.InputCloseParen();
                    break;
                case '%':
                    viewModel.InputPercent();
                    break;
                default:
                    throw new ArgumentException($"Carácter no soportado en la prueba: {character}");
            }
        }

        return viewModel;
    }

    [Fact]
    public void Equals_ComputesResultAndStoresHistory()
    {
        var viewModel = Type("2*(3+4)");
        viewModel.CalculateEquals();

        Assert.Equal("14", viewModel.DisplayText);
        Assert.Equal("2×(3+4)", viewModel.SecondaryText);
        Assert.Single(viewModel.History);
        Assert.Equal(14, viewModel.History[0].Value, 10);
    }

    [Fact]
    public void Typing_ShowsLivePreview()
    {
        var viewModel = Type("2+3*4");

        Assert.Equal("2+3×4", viewModel.DisplayText);
        Assert.Equal("= 14", viewModel.SecondaryText);
    }

    [Fact]
    public void Percent_UsesWindowsSemantics()
    {
        var viewModel = Type("200+10%");
        viewModel.CalculateEquals();

        Assert.Equal("220", viewModel.DisplayText);
    }

    [Fact]
    public void ScientificFunctions_UseSelectedAngleMode()
    {
        var degrees = new MainViewModel();
        degrees.InputFunction("sin");
        degrees.InputDigit("3");
        degrees.InputDigit("0");
        degrees.InputCloseParen();
        degrees.CalculateEquals();
        Assert.Equal("0.5", degrees.DisplayText);

        var radians = new MainViewModel();
        radians.SetRadians();
        radians.InputFunction("sin");
        radians.InputConstant("π");
        radians.InputOperator("/");
        radians.InputDigit("2");
        radians.CalculateEquals();

        Assert.Equal("1", radians.DisplayText);
    }

    [Fact]
    public void Factorial_AndConstants_WorkFromTheUi()
    {
        var viewModel = Type("5");
        viewModel.InputFactorial();
        viewModel.CalculateEquals();

        Assert.Equal("120", viewModel.DisplayText);
    }

    [Fact]
    public void DivisionByZero_ShowsFriendlyErrorAndRecovers()
    {
        var viewModel = Type("5/0");
        viewModel.CalculateEquals();

        Assert.Equal("No se puede dividir entre cero", viewModel.DisplayText);

        viewModel.InputDigit("7");
        Assert.Equal("7", viewModel.DisplayText);
    }

    [Fact]
    public void MemoryButtons_StoreRecallAndClear()
    {
        var viewModel = Type("12.5");
        viewModel.CalculateEquals();
        viewModel.MemoryAdd();

        Assert.True(viewModel.HasMemory);

        viewModel.ClearAll();
        viewModel.MemoryRecall();
        Assert.Equal("12.5", viewModel.DisplayText);

        viewModel.MemoryStore();
        viewModel.MemoryClear();
        Assert.False(viewModel.HasMemory);
    }

    [Fact]
    public void MemoryRecall_DoesNothingWhenMemoryIsEmpty()
    {
        var viewModel = Type("9");
        viewModel.MemoryRecall();

        Assert.Equal("9", viewModel.DisplayText);
    }

    [Fact]
    public void Backspace_RemovesWholeFunction()
    {
        var viewModel = new MainViewModel();
        viewModel.InputFunction("sin");
        Assert.Equal("sin(", viewModel.DisplayText);

        viewModel.Backspace();
        Assert.Equal("0", viewModel.DisplayText);
    }

    [Fact]
    public void Function_AfterNumber_WrapsCurrentOperand()
    {
        var viewModel = Type("30");
        viewModel.InputFunction("sin");

        Assert.Equal("sin(30)", viewModel.DisplayText);

        viewModel.CalculateEquals();
        Assert.Equal("0.5", viewModel.DisplayText);
    }

    [Fact]
    public void Function_AfterOperator_OpensPrefixInsteadOfWrapping()
    {
        var viewModel = Type("2+");
        viewModel.InputFunction("sin");

        viewModel.InputDigit("9");
        viewModel.InputDigit("0");
        viewModel.InputCloseParen();
        viewModel.CalculateEquals();

        Assert.Equal("3", viewModel.DisplayText);
    }

    [Fact]
    public void SecondToggle_ChangesLabelsAndInsertsInverseFunction()
    {
        var viewModel = new MainViewModel();
        Assert.Equal("sin", viewModel.SinLabel);

        viewModel.ToggleSecond();
        Assert.Equal("sin⁻¹", viewModel.SinLabel);

        viewModel.Science("sin");
        Assert.Equal("sin⁻¹(", viewModel.DisplayText);

        viewModel.InputDigit("1");
        viewModel.InputCloseParen();
        viewModel.CalculateEquals();

        Assert.Equal("90", viewModel.DisplayText);
    }

    [Fact]
    public void SignToggle_NegatesLastOperand()
    {
        var viewModel = Type("2+3");
        viewModel.ToggleSign();

        Assert.Equal("2+(−3)", viewModel.DisplayText);

        viewModel.CalculateEquals();
        Assert.Equal("-1", viewModel.DisplayText);
    }

    [Fact]
    public void OperatorAfterResult_ContinuesFromAnswer()
    {
        var viewModel = Type("2+3");
        viewModel.CalculateEquals();
        viewModel.InputOperator("+");
        viewModel.InputDigit("4");
        viewModel.CalculateEquals();

        Assert.Equal("9", viewModel.DisplayText);
    }

    [Fact]
    public void DigitAfterResult_StartsFreshInput()
    {
        var viewModel = Type("2+3");
        viewModel.CalculateEquals();
        viewModel.InputDigit("8");

        Assert.Equal("8", viewModel.DisplayText);
    }

    [Fact]
    public void Decimal_IsAcceptedOnlyOncePerNumber()
    {
        var viewModel = Type("1.5");
        viewModel.InputDecimal();
        Assert.Equal("1.5", viewModel.DisplayText);

        viewModel.InputDecimal();
        viewModel.InputDigit("2");
        Assert.Equal("1.52", viewModel.DisplayText);
    }

    [Fact]
    public void ExponentialButton_BuildsScientificNotation()
    {
        var viewModel = Type("1.5");
        viewModel.InputExp();
        viewModel.InputDigit("3");
        viewModel.CalculateEquals();

        Assert.Equal("1,500", viewModel.DisplayText);
    }

    [Fact]
    public void HistoryEntry_CanBeReused()
    {
        var viewModel = Type("2+2");
        viewModel.CalculateEquals();
        var entry = viewModel.History[0];

        viewModel.ClearAll();
        viewModel.UseHistoryValue(entry.Value);

        Assert.Equal("4", viewModel.DisplayText);
    }

    [Fact]
    public void ClearHistory_EmptiesTheList()
    {
        var viewModel = Type("2+2");
        viewModel.CalculateEquals();
        viewModel.ClearHistory();

        Assert.Empty(viewModel.History);
    }

    [Fact]
    public void SquareAndReciprocalButtons_Work()
    {
        var viewModel = Type("9");
        viewModel.Science("square");
        viewModel.CalculateEquals();
        Assert.Equal("81", viewModel.DisplayText);

        var reciprocal = Type("4");
        reciprocal.InputReciprocal();
        reciprocal.CalculateEquals();
        Assert.Equal("0.25", reciprocal.DisplayText);
    }
}
