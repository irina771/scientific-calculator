using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ScientificCalculator.ViewModels;

namespace ScientificCalculator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void History_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: double value } && DataContext is MainViewModel viewModel)
            viewModel.UseHistoryValue(value);
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

            return;
        }

        BeginMoveDrag(e);
    }

    private void Minimize_Click(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Maximize_Click(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (e.Handled || DataContext is not MainViewModel viewModel || string.IsNullOrEmpty(e.Text))
            return;

        foreach (char character in e.Text)
        {
            switch (character)
            {
                case >= '0' and <= '9':
                    viewModel.InputDigit(character.ToString());
                    break;
                case '.':
                case ',':
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
                case '!':
                    viewModel.InputFactorial();
                    break;
                case '=':
                    viewModel.CalculateEquals();
                    break;
                default:
                    return;
            }
        }

        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled || DataContext is not MainViewModel viewModel)
            return;

        switch (e.Key)
        {
            case Key.Enter:
                viewModel.CalculateEquals();
                e.Handled = true;
                break;
            case Key.Back:
                viewModel.Backspace();
                e.Handled = true;
                break;
            case Key.Escape:
                viewModel.ClearAll();
                e.Handled = true;
                break;
            case Key.Delete:
                viewModel.ClearEntry();
                e.Handled = true;
                break;
            case Key.F9:
                viewModel.ToggleSign();
                e.Handled = true;
                break;
        }
    }
}
