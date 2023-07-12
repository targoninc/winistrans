using System.Globalization;
using System.Windows.Automation;
using Avalonia.Input;
using WinIsTransLibrary;

namespace WinIsTransConsole;

public class WinIsTransApp : IDisposable
{
    public void AttachTextHandler(Func<string, bool> onTextChanged)
    {
        _onTextChanged = onTextChanged;
    }
    
    private int _transparency = 255;
    private const int TransparencyStep = 10;
    private int _selectedWindowIndex;
    private Dictionary<AutomationElement, bool> _windows = new();
    private Func<string, bool> _onTextChanged = text => true;
    private string _outText = string.Empty;

    public void Initialize()
    {
        GetWindows();
        RemoveTransparency();
    }

    public void Run()
    {
        UpdateText();
        
        while (true)
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey();
            Console.WriteLine();

            HandleKey(keyInfo);
            UpdateText();
        }
    }

    public void HandleKey(ConsoleKeyInfo keyInfo)
    {
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
            case ConsoleKey.Enter:
                ToggleCurrentSelectedWindow();
                break;
            case ConsoleKey.R:
                RemoveTransparency();
                break;
            case ConsoleKey.U:
                ResetTransparency();
                break;
            case ConsoleKey.Q:
            case ConsoleKey.Escape:
                Console.WriteLine("Exiting...");
                Environment.Exit(0);
                return;
            default:
                Console.WriteLine("Unsupported key. Try again.");
                break;
        }
    }
    
    public void HandleAvaloniaKey(KeyEventArgs key)
    {
        ConsoleKeyInfo keyInfo = new((char) key.Key, (ConsoleKey) key.Key, false, false, false);
    }

    private void ListWindows()
    {
        for (int i = 0; i < _windows.Count; i++)
        {
            AutomationElement window = _windows.Keys.ElementAt(i);
            bool isSelected = i == _selectedWindowIndex;
            bool isTransparent = _windows[window];
            string selectedIndicator = isSelected ? "<" : " ";
            string activeIndicator = isTransparent ? ">" : " ";
            _outText += $"{activeIndicator} {window.Current.Name} {selectedIndicator}\n";
        }
    }

    private void UpdateText()
    {
        string transparencyPercentage = Math.Round((double) _transparency / 255 * 100, 0).ToString(CultureInfo.InvariantCulture) + "%";
        _outText += $"Transparency: {transparencyPercentage}\n";
        ListWindows();
        _onTextChanged(_outText);
    }
    
    private void ToggleCurrentSelectedWindow()
    {
        GetWindows();
        AutomationElement window = _windows.Keys.ElementAt(_selectedWindowIndex);
        _windows[window] = !_windows[window];
        UpdateTransparency();
    }

    private void GetWindows()
    {
        List<AutomationElement> windows = WindowManager.GetAllWindowsAndTheirChildren();
        Dictionary<AutomationElement, bool> windowsCache = _windows;
        _windows = windows.ToDictionary(window => window, window => false);
        foreach (AutomationElement window in windowsCache.Keys.Where(window => _windows.ContainsKey(window)))
        {
            _windows[window] = windowsCache[window];
        }
    }

    private void SelectWindowUp()
    {
        GetWindows();
        _selectedWindowIndex--;
        _selectedWindowIndex = Math.Max(0, _selectedWindowIndex);
    }

    private void SelectWindowDown()
    {
        GetWindows();
        _selectedWindowIndex++;
        _selectedWindowIndex = Math.Min(_windows.Count - 1, _selectedWindowIndex);
    }

    private void IncreaseTransparency()
    {
        _transparency -= TransparencyStep;
        _transparency = Math.Max(0, _transparency);
        UpdateTransparency();
    }

    private void DecreaseTransparency()
    {
        _transparency += TransparencyStep;
        _transparency = Math.Min(255, _transparency);
        UpdateTransparency();
    }

    private void ResetTransparency()
    {
        _transparency = 255;
        UpdateTransparency();
    }

    private void UpdateTransparency()
    {
        GetWindows();
        WindowManager.ApplyTransparencyToWindows(_windows, _transparency);
    }

    private void RemoveTransparency()
    {
        GetWindows();
        WindowManager.ResetTransparencyOnWindows(_windows.Keys.ToList());
    }

    public void Dispose()
    {
        Console.WriteLine("Removing transparency...");
        RemoveTransparency();
    }
}