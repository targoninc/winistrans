using System.Globalization;
using System.Text;
using System.Windows.Automation;
using Avalonia.Input;

namespace WinIsTransConsole;

public class WinIsTransApp : IDisposable
{
    public void AttachTextHandler(Func<string, bool> onTextChanged)
    {
        _onTextChanged = onTextChanged;
        GetWindows();
        RemoveTransparency();
        UpdateText();
    }
    
    private const int TransparencyStep = 10;
    private const int GetWindowsIntervalSeconds = 300;
    private const int MaxTransparency = 255;
    private const int MinTransparency = 0;
    private const int MaxRetries = 2;
    private int _transparency = 255;
    private int _selectedWindowIndex;
    private string _outText = string.Empty;
    private Timer _timer;
    private bool _helpActive;
    private Dictionary<AutomationElement, bool> _windows = new();
    private Func<string, bool> _onTextChanged = _ => true;
    private readonly object _windowsLock = new object();

    public void HandleKey(ConsoleKeyInfo keyInfo)
    {
        _outText = string.Empty;
        switch (keyInfo.Key)
        {
            case ConsoleKey.W:
            case ConsoleKey.UpArrow:
                SelectWindowUp();
                break;
            case ConsoleKey.S:
            case ConsoleKey.DownArrow:
                SelectWindowDown();
                break;
            case ConsoleKey.OemPlus:
                IncreaseTransparency();
                break;
            case ConsoleKey.OemMinus:
                DecreaseTransparency();
                break;
            case ConsoleKey.Spacebar:
                ToggleCurrentSelectedWindow();
                break;
            case ConsoleKey.R:
                RemoveTransparency();
                break;
            case ConsoleKey.T:
                ResetTransparency();
                break;
            case ConsoleKey.C:
                UnselectAll();
                break;
            case ConsoleKey.V:
                SelectAll();
                break;
            case ConsoleKey.Enter:
                ToggleHelp();
                break;
            case ConsoleKey.Q:
            case ConsoleKey.Escape:
                Console.WriteLine("Exiting...");
                RemoveTransparency();
                Environment.Exit(0);
                return;
            default:
                Console.WriteLine($"Unsupported key ({keyInfo.Key})");
                UpdateText();
                break;
        }
    }
    
    public void HandleAvaloniaKey(KeyEventArgs key)
    {
        ConsoleKey newKey;
        switch (key.Key)
        {
            case Key.W:
            case Key.Up:
                newKey = ConsoleKey.UpArrow;
                break;
            case Key.S:
            case Key.Down:
                newKey = ConsoleKey.DownArrow;
                break;
            case Key.OemPlus:
            case Key.Add:
                newKey = ConsoleKey.OemPlus;
                break;
            case Key.OemMinus:
            case Key.Subtract:
                newKey = ConsoleKey.OemMinus;
                break;
            case Key.Enter:
                newKey = ConsoleKey.Enter;
                break;
            case Key.R:
                newKey = ConsoleKey.R;
                break;
            case Key.T:
                newKey = ConsoleKey.T;
                break;
            case Key.C:
                newKey = ConsoleKey.C;
                break;
            case Key.V:
                newKey = ConsoleKey.V;
                break;
            case Key.Space:
                newKey = ConsoleKey.Spacebar;
                break;
            case Key.Q:
            case Key.Escape:
                newKey = ConsoleKey.Escape;
                break;
            default:
                Console.WriteLine($"Unsupported key ({key.Key})");
                return;
        }

        ConsoleKeyInfo keyInfo = new((char) newKey, newKey, false, false, false);
        HandleKey(keyInfo);
    }

    private void ListWindows()
    {
        StringBuilder sb = new();
        sb.AppendLine($"Transparency: {_transparency}");
        sb.AppendLine("Windows:");
        lock(_windowsLock) {
            if (_windows.Count == 0)
            {
                sb.AppendLine("Press any key if there are no windows...");
            }
            for (int i = 0; i < _windows.Count; i++)
            {
                AutomationElement window = _windows.Keys.ElementAt(i);
                bool isSelected = i == _selectedWindowIndex;
                bool isTransparent = _windows[window];
                string selectedIndicator = isSelected ? "<" : " ";
                string activeIndicator = isTransparent ? ">" : " ";
                sb.AppendLine($"{activeIndicator} {window.Current.Name} {selectedIndicator}");
            }
        }
        _outText = sb.ToString();
    }


    private void UpdateText()
    {
        string transparencyPercentage = Math.Round((double) _transparency / 255 * 100, 0).ToString(CultureInfo.InvariantCulture) + "%";
        _outText += $"Transparency: {transparencyPercentage}\n";
        ListWindows();
        UpdateTextInternalAsync(_outText);
    }

    private async void UpdateTextInternalAsync(string text, int retries = 0, CancellationToken cancellationToken = default)
    {
        try
        {
            if (retries > MaxRetries)
            {
                return;
            }
            bool success = _onTextChanged(text);
            if (success)
            {
                return;
            }
            await Task.Delay(1000, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            UpdateTextInternalAsync(text, retries + 1, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating text: {ex.Message}");
        }
    }

    
    private void ToggleCurrentSelectedWindow()
    {
        AutomationElement window = _windows.Keys.ElementAt(_selectedWindowIndex);
        _windows[window] = !_windows[window];
        UpdateTransparency();
    }

    private void GetWindows()
    {
        const int timerInterval = GetWindowsIntervalSeconds * 1000;
        _timer = new Timer(UpdateWindows, null, 0, timerInterval);
    }

    private async void UpdateWindows(object? state)
    {
        try
        {
            List<AutomationElement> windows = await Task.Run(WindowManager.GetAllWindowsAndTheirChildren);
            Dictionary<AutomationElement, bool> newWindows = windows.ToDictionary(window => window, window => false);

            lock (_windowsLock)
            {
                Dictionary<AutomationElement, bool> windowsCache = _windows;
                foreach (AutomationElement window in windowsCache.Keys.Where(window => newWindows.ContainsKey(window)))
                {
                    newWindows[window] = windowsCache[window];
                }
                _windows = newWindows;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting windows: {ex.Message}");
        }
    }

    private void SelectWindowUp()
    {
        _selectedWindowIndex--;
        _selectedWindowIndex = _selectedWindowIndex < 0 ? _windows.Count - 1 : _selectedWindowIndex;
        UpdateText();
    }

    private void SelectWindowDown()
    {
        _selectedWindowIndex++;
        _selectedWindowIndex = _selectedWindowIndex >= _windows.Count ? 0 : _selectedWindowIndex;
        UpdateText();
    }

    private void IncreaseTransparency()
    {
        _transparency -= TransparencyStep;
        _transparency = Math.Max(MinTransparency, _transparency);
        UpdateTransparency();
    }

    private void DecreaseTransparency()
    {
        _transparency += TransparencyStep;
        _transparency = Math.Min(MaxTransparency, _transparency);
        UpdateTransparency();
    }

    private void ResetTransparency()
    {
        _transparency = 255;
        UpdateTransparency();
    }

    private void UpdateTransparency()
    {
        WindowManager.ApplyTransparencyToWindows(_windows, _transparency);
        UpdateText();
    }

    private void SelectAll()
    {
        lock (_windowsLock)
        {
            foreach (AutomationElement window in _windows.Keys)
            {
                _windows[window] = true;
            }
        }
        UpdateTransparency();
    }

    private void UnselectAll()
    {
        lock (_windowsLock)
        {
            foreach (AutomationElement window in _windows.Keys)
            {
                _windows[window] = false;
            }
        }
        UpdateTransparency();
    }

    private void ToggleHelp()
    {
        if (_helpActive)
        {
            _helpActive = false;
            _outText = string.Empty;
            UpdateText();
        }
        
        else
        {
            _helpActive = true;
            _outText = "Help:\n" +
                       "W/Up Arrow: Select window above\n" +
                       "S/Down Arrow: Select window below\n" +
                       "+: Increase transparency\n" +
                       "-: Decrease transparency\n" +
                       "Space: Toggle transparency on selected window\n" +
                       "R: Remove transparency from all windows\n" +
                       "T: Reset transparency to 100%\n" +
                       "C: Unselect all windows\n" +
                       "V: Select all windows\n" +
                       "Enter: Toggle help\n" +
                       "Q/Esc: Exit\n";
            _onTextChanged(_outText);
        }
    }

    private void RemoveTransparency()
    {
        WindowManager.ResetTransparencyOnWindows(_windows.Keys.ToList());
    }

    public void Dispose()
    {
        Console.WriteLine("Removing transparency...");
        RemoveTransparency();
        _timer?.Dispose();
    }
}