using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ScientificCalculator.ViewModels;

namespace ScientificCalculator.Views;

public partial class MainWindow : Window
{
    private readonly Dictionary<Button, DispatcherTimer> _pressedTimers = new();
    private readonly Dictionary<string, Button> _buttonNameMap = new(StringComparer.OrdinalIgnoreCase);

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(InputElement.PointerPressedEvent, Window_PointerPressedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        _buttonNameMap.Clear();
        foreach (var button in this.GetVisualDescendants().OfType<Button>())
        {
            var name = AutomationProperties.GetName(button);
            if (!string.IsNullOrEmpty(name))
            {
                _buttonNameMap[name] = button;
            }
        }
    }

    private void Window_PointerPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        if (e.Source is Visual visual)
        {
            var button = visual.FindAncestorOfType<Button>(includeSelf: true);
            if (button != null && (button.Classes.Contains("key") || button.Classes.Contains("pixel") || button.Classes.Contains("memo")))
            {
                TriggerButtonPress(button);
            }
        }
    }

    /// <summary>
    /// Muestra el estado "pressed" del botón durante 0.3 segundos, volviendo luego al estado "normal".
    /// </summary>
    public void TriggerButtonPress(Button button)
    {
        if (_pressedTimers.TryGetValue(button, out var activeTimer))
        {
            activeTimer.Stop();
            _pressedTimers.Remove(button);
        }

        // Teclas estándar con imágenes normal y pressed
        if (button.Content is Grid grid)
        {
            var normalImg = grid.Children.OfType<Image>().FirstOrDefault(img => img.Classes.Contains("normal"));
            var pressedImg = grid.Children.OfType<Image>().FirstOrDefault(img => img.Classes.Contains("pressed"));

            if (normalImg != null && pressedImg != null)
            {
                normalImg.Opacity = 0;
                pressedImg.Opacity = 1;

                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    normalImg.Opacity = 1;
                    pressedImg.Opacity = 0;
                    _pressedTimers.Remove(button);
                };

                _pressedTimers[button] = timer;
                timer.Start();
                return;
            }
        }

        // Teclas científicas o de memoria
        button.Classes.Add("is-pressed");
        var pixelTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        pixelTimer.Tick += (_, _) =>
        {
            pixelTimer.Stop();
            button.Classes.Remove("is-pressed");
            _pressedTimers.Remove(button);
        };

        _pressedTimers[button] = pixelTimer;
        pixelTimer.Start();
    }

    private void TriggerButtonPressByName(string name)
    {
        if (_buttonNameMap.TryGetValue(name, out var button))
        {
            TriggerButtonPress(button);
        }
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

        // No se permite cambiar de tamaño ni maximizar con doble clic
        BeginMoveDrag(e);
    }

    private void Minimize_Click(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

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
                    TriggerButtonPressByName(character.ToString());
                    break;
                case '.':
                case ',':
                    viewModel.InputDecimal();
                    TriggerButtonPressByName("coma");
                    break;
                case '+':
                    viewModel.InputOperator("+");
                    TriggerButtonPressByName("mas");
                    break;
                case '-':
                    viewModel.InputOperator("-");
                    TriggerButtonPressByName("menos");
                    break;
                case '*':
                    viewModel.InputOperator("*");
                    TriggerButtonPressByName("multiplicar");
                    break;
                case '/':
                    viewModel.InputOperator("/");
                    TriggerButtonPressByName("division");
                    break;
                case '^':
                    viewModel.InputOperator("^");
                    TriggerButtonPressByName("potencia");
                    break;
                case '(':
                    viewModel.InputOpenParen();
                    TriggerButtonPressByName("parentesis_abre");
                    break;
                case ')':
                    viewModel.InputCloseParen();
                    TriggerButtonPressByName("parentesis_cierra");
                    break;
                case '%':
                    viewModel.InputPercent();
                    TriggerButtonPressByName("mod");
                    break;
                case '!':
                    viewModel.InputFactorial();
                    TriggerButtonPressByName("factorial");
                    break;
                case '=':
                    viewModel.CalculateEquals();
                    TriggerButtonPressByName("igual");
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
                TriggerButtonPressByName("igual");
                e.Handled = true;
                break;
            case Key.Back:
                viewModel.Backspace();
                TriggerButtonPressByName("borrar");
                e.Handled = true;
                break;
            case Key.Escape:
                viewModel.ClearAll();
                TriggerButtonPressByName("CE");
                e.Handled = true;
                break;
            case Key.Delete:
                viewModel.ClearEntry();
                TriggerButtonPressByName("CE");
                e.Handled = true;
                break;
            case Key.F9:
                viewModel.ToggleSign();
                TriggerButtonPressByName("cambiar_signo");
                e.Handled = true;
                break;
        }
    }
}
