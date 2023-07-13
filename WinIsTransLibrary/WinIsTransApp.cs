using System.Collections.Concurrent;
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
    }
    
    private const int TransparencyStep = 10;
    private const int GetWindowsIntervalSeconds = 60;
    private const int MaxTransparency = 255;
    private const int MinTransparency = 0;
    private const int MaxRetries = 2;
    private int _transparency = 255;
    private int _selectedWindowIndex;
    private string _outText = string.Empty;
    private Timer? _timer;
    private bool _helpActive;
    private ConcurrentDictionary<AutomationElement, bool> _windows = new();
    private Func<string, bool> _onTextChanged = _ => true;

    public async Task HandleKey(ConsoleKeyInfo keyInfo)
    {
        _outText = string.Empty;
        switch (keyInfo.Key)
        {
            case ConsoleKey.W:
            case ConsoleKey.UpArrow:
                await SelectWindowUp();
                break;
            case ConsoleKey.S:
            case ConsoleKey.DownArrow:
                await SelectWindowDown();
                break;
            case ConsoleKey.OemPlus:
                await IncreaseTransparency();
                break;
            case ConsoleKey.OemMinus:
                await DecreaseTransparency();
                break;
            case ConsoleKey.Spacebar:
                await ToggleCurrentSelectedWindow();
                break;
            case ConsoleKey.R:
                RemoveTransparency();
                break;
            case ConsoleKey.T:
                await ResetTransparency();
                break;
            case ConsoleKey.C:
                await UnselectAll();
                break;
            case ConsoleKey.V:
                await SelectAll();
                break;
            case ConsoleKey.F1:
                await Task.Run(() => UpdateWindows(null));
                break;
            case ConsoleKey.Enter:
                await ToggleHelp();
                break;
            case ConsoleKey.Q:
            case ConsoleKey.Escape:
                Console.WriteLine("Exiting...");
                RemoveTransparency();
                Environment.Exit(0);
                return;
            default:
                Console.WriteLine($"Unsupported key ({keyInfo.Key})");
                await UpdateTransparency();
                break;
        }
    }
    
    public async Task HandleAvaloniaKey(KeyEventArgs key)
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
            case Key.F1:
                newKey = ConsoleKey.F1;
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
        await HandleKey(keyInfo);
    }

    private void ListWindows()
    {
        StringBuilder sb = new();
        sb.AppendLine($"Transparency: {_transparency}");
        sb.AppendLine("Show Help (Enter)");
        if (_windows.IsEmpty)
        {
            sb.AppendLine("Press any key if there are no windows...");
        }

        List<AutomationElement> toRemove = new();
        for (int i = 0; i < _windows.Count; i++)
        {
            AutomationElement window = _windows.Keys.ElementAt(i);
            bool isSelected = i == _selectedWindowIndex;
            bool isTransparent = _windows[window];
            string selectedIndicator = isSelected ? "<" : " ";
            string activeIndicator = isTransparent ? ">" : " ";
            string name = window.Current.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"{window.Current.ClassName} ({window.Current.NativeWindowHandle.ToString()})";
            }
            try
            {
                sb.AppendLine($"{activeIndicator} {name} {selectedIndicator}");
            } catch (ElementNotAvailableException) // Window has been closed
            {
                toRemove.Add(window);
            }
        }
        toRemove.ForEach(w => _windows.Remove(w, out _));
        _outText = sb.ToString();
    }


    private async Task UpdateText()
    {
        string transparencyPercentage = Math.Round((double) _transparency / 255 * 100, 0).ToString(CultureInfo.InvariantCulture) + "%";
        _outText += $"Transparency: {transparencyPercentage}\n";
        ListWindows();
        await UpdateTextInternalAsync(_outText);
    }

    private async Task UpdateTextInternalAsync(string text, int maxRetries = MaxRetries, CancellationToken cancellationToken = default)
    {
        int retries = 0;
    
        while (retries <= maxRetries)
        {
            try
            {
                bool success = _onTextChanged(text);
    
                if (success)
                {
                    return;
                }
    
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
    
                await Task.Delay(1000, cancellationToken);
                retries++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating text: {ex.Message}");
                return;
            }
        }
    }

    
    private async Task ToggleCurrentSelectedWindow()
    {
        if (_windows.Count == 0) 
        {
            Console.WriteLine($"There are no windows to select.");
            return;
        }
        if (_selectedWindowIndex < 0 || _selectedWindowIndex >= _windows.Count)
        {
            Console.WriteLine($"Invalid window index: {_selectedWindowIndex}");
            return;
        }
        AutomationElement window = _windows.Keys.ElementAt(_selectedWindowIndex);
        _windows[window] = !_windows[window]; 
        await UpdateTransparency();
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
            List<AutomationElement> windows = await Task.Run(WindowManager.GetAllWindowsAndTheirChildren).ConfigureAwait(false);
            ConcurrentDictionary<AutomationElement, bool> newWindows = new(windows.ToDictionary(window => window, _ => false));
    
            ConcurrentDictionary<AutomationElement, bool> windowsCache = _windows;
            foreach (AutomationElement window in windowsCache.Keys.Where(window => newWindows.ContainsKey(window)))
            {
                newWindows[window] = windowsCache[window];
            }
            _windows = newWindows;
            await UpdateText();
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                default:
                    Console.WriteLine($"Unhandled exception occurred: {ex.Message}\n{ex.StackTrace}");
                    throw;
            }
        }
    }

    private async Task SelectWindowUp()
    {
        _selectedWindowIndex--;
        _selectedWindowIndex = _selectedWindowIndex < 0 ? _windows.Count - 1 : _selectedWindowIndex;
        await UpdateText();
    }

    private async Task SelectWindowDown()
    {
        _selectedWindowIndex++;
        _selectedWindowIndex = _selectedWindowIndex >= _windows.Count ? 0 : _selectedWindowIndex;
        await UpdateText();
    }

    private async Task IncreaseTransparency()
    {
        _transparency -= TransparencyStep;
        _transparency = Math.Max(MinTransparency, _transparency);
        await UpdateTransparency();
    }

    private async Task DecreaseTransparency()
    {
        _transparency += TransparencyStep;
        _transparency = Math.Min(MaxTransparency, _transparency);
        await UpdateTransparency();
    }

    private async Task ResetTransparency()
    {
        _transparency = 255;
        await UpdateTransparency();
    }

    private async Task UpdateTransparency()
    {
        WindowManager.ApplyTransparencyToWindows(_windows, _transparency);
        await UpdateText();
    }

    private async Task SelectAll()
    {
        foreach (AutomationElement window in _windows.Keys)
        {
            _windows[window] = true;
        }
        await UpdateTransparency();
    }

    private async Task UnselectAll()
    {
        foreach (AutomationElement window in _windows.Keys)
        {
            _windows[window] = false;
        }
        await UpdateTransparency();
    }

    private async Task ToggleHelp()
    {
        if (_helpActive)
        {
            _helpActive = false;
            _outText = string.Empty;
            await UpdateText();
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
                       "F1: Refresh windows\n" +
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
        try
        {
            _timer?.Dispose();
            _timer = null;
        }
        finally
        {
            Console.WriteLine("Removing transparency...");
            RemoveTransparency();
        }
    }
}