using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Windows.Automation;
using Avalonia.Input;

namespace WinIsTransConsole;

public class WinIsTransApp : IDisposable
{
    private const int TransparencyStep = 10;
    private const int MaxTransparency = 255;
    private const int MinTransparency = 0;
    private const int MaxRetries = 2;

    public string OutText = string.Empty;
    
    private int _transparency = MaxTransparency;
    private int _selectedWindowIndex;
    private bool _helpActive;

    private ConcurrentDictionary<AutomationElement, bool> _windows = new();
    private Dictionary<ConsoleKey, Func<Task>> _keyHandlers = new();
    private Dictionary<Key, ConsoleKey> _avaloniaToConsoleKeyMap = new();
    private readonly object _textLock = new();
    
    private void InitializeAvaloniaToConsoleKeyMap()
    {
        _avaloniaToConsoleKeyMap = new Dictionary<Key, ConsoleKey>
        {
            {Key.W, ConsoleKey.W},
            {Key.Up, ConsoleKey.UpArrow},
            {Key.S, ConsoleKey.S},
            {Key.Down, ConsoleKey.DownArrow},
            {Key.OemPlus, ConsoleKey.OemPlus},
            {Key.Add, ConsoleKey.OemPlus},
            {Key.OemMinus, ConsoleKey.OemMinus},
            {Key.Subtract, ConsoleKey.OemMinus},
            {Key.Enter, ConsoleKey.Enter},
            {Key.R, ConsoleKey.R},
            {Key.T, ConsoleKey.T},
            {Key.C, ConsoleKey.C},
            {Key.V, ConsoleKey.V},
            {Key.Space, ConsoleKey.Spacebar},
            {Key.F1, ConsoleKey.F1},
            {Key.Q, ConsoleKey.Q},
            {Key.Escape, ConsoleKey.Escape}
        };
    }
    
    public WinIsTransApp() {
        InitializeKeyHandlers();
        InitializeAvaloniaToConsoleKeyMap();
    }
    
    private void InitializeKeyHandlers()
    {
        _keyHandlers = new Dictionary<ConsoleKey, Func<Task>>
        {
            {ConsoleKey.W, SelectWindowUp},
            {ConsoleKey.UpArrow, SelectWindowUp},
            {ConsoleKey.S, SelectWindowDown},
            {ConsoleKey.DownArrow, SelectWindowDown},
            {ConsoleKey.OemPlus, IncreaseTransparency},
            {ConsoleKey.OemMinus, DecreaseTransparency},
            {ConsoleKey.Spacebar, ToggleCurrentSelectedWindow},
            {ConsoleKey.R, RemoveTransparency},
            {ConsoleKey.T, ResetTransparency},
            {ConsoleKey.C, UnselectAll},
            {ConsoleKey.V, SelectAll},
            {ConsoleKey.F1, UpdateWindows},
            {ConsoleKey.Enter, ToggleHelp},
            {ConsoleKey.Q, ExitApplication},
            {ConsoleKey.Escape, ExitApplication}
        };
    }

    private Task ExitApplication()
    {
        Environment.Exit(0);
        return Task.CompletedTask;
    }

    public async Task HandleKey(ConsoleKeyInfo keyInfo)
    {
        if (!_keyHandlers.TryGetValue(keyInfo.Key, out Func<Task>? handler))
        {
            Console.WriteLine($"Unsupported key ({keyInfo.Key})");
            await UpdateTransparency();
            return;
        }

        await handler();
    }

    public async Task HandleAvaloniaKey(KeyEventArgs key)
    {
        if (!_avaloniaToConsoleKeyMap.TryGetValue(key.Key, out ConsoleKey consoleKey))
        {
            Console.WriteLine($"Unsupported key ({key.Key})");
            return;
        }

        if (!_keyHandlers.TryGetValue(consoleKey, out Func<Task>? handler))
        {
            Console.WriteLine($"Unsupported key ({key.Key})");
            return;
        }

        await handler();
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
            try
            {
                string name = window.Current.Name;
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = $"{window.Current.ClassName} ({window.Current.NativeWindowHandle.ToString()})";
                }
                sb.AppendLine($"{activeIndicator} {name} {selectedIndicator}");
            } catch (ElementNotAvailableException) // Window has been closed
            {
                toRemove.Add(window);
            }
        }
        toRemove.ForEach(w => _windows.Remove(w, out _));
        OutText = sb.ToString();
    }
    
    private Task UpdateText()
    {
        string transparencyPercentage = Math.Round((double) _transparency / 255 * 100, 0).ToString(CultureInfo.InvariantCulture) + "%";
    
        lock(_textLock)
        {
            OutText = "";
            OutText += $"Transparency: {transparencyPercentage}\n";
            ListWindows();
        }
        
        return Task.CompletedTask;
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

    public async Task UpdateWindows()
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


    private async Task SelectWindowUp()
    {
        if (_windows.IsEmpty)
        {
            return;
        }
        _selectedWindowIndex--;
        _selectedWindowIndex = _selectedWindowIndex < 0 ? _windows.Count - 1 : _selectedWindowIndex;
        await UpdateText();
    }

    private async Task SelectWindowDown()
    {
        if (_windows.IsEmpty)
        {
            return;
        }
        _selectedWindowIndex++;
        _selectedWindowIndex = _selectedWindowIndex >= _windows.Count ? 0 : _selectedWindowIndex;
        await UpdateText();
    }

    private async Task IncreaseTransparency()
    {
        if (_transparency == MinTransparency)
        {
            return;
        }
        _transparency -= TransparencyStep;
        _transparency = Math.Max(MinTransparency, _transparency);
        await UpdateTransparency();
    }

    private async Task DecreaseTransparency()
    {
        if (_transparency == MaxTransparency)
        {
            return;
        }
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
        ConcurrentDictionary<AutomationElement, bool> windows = _windows;
        WindowManager.ApplyTransparencyToWindows(windows, _transparency);
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
            OutText = string.Empty;
            await UpdateText();
        }
        
        else
        {
            _helpActive = true;
            OutText = "Help:\n" +
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
        }
    }

    public Task RemoveTransparency()
    {
        WindowManager.ResetTransparencyOnWindows(_windows.Keys.ToList());
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Console.WriteLine("Removing transparency...");
        RemoveTransparency();
    }
}