using System.Globalization;
using System.Windows.Automation;
using WinIsTransLibrary;

namespace WinIsTransConsole;

public class WinIsTransApp : IDisposable
{
    private int _transparency = 255;
    private const int TransparencyStep = 10;
    private int _selectedWindowIndex;
    private Dictionary<AutomationElement, bool> _windows = new();

    public void Run()
    {
        Console.Title = "WinIsTrans";
        GetWindows();
        RemoveTransparency();

        while (true)
        {
            UpdateConsole();

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            Console.WriteLine();

            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                    SelectWindowUp();
                    break;
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
                case ConsoleKey.Escape:
                    Console.WriteLine("Exiting...");
                    Environment.Exit(0);
                    return;
                default:
                    Console.WriteLine("Unsupported key. Try again.");
                    break;
            }
        }
    }

    private void ListWindows()
    {
        for (int i = 0; i < _windows.Count; i++)
        {
            AutomationElement window = _windows.Keys.ElementAt(i);
            bool isSelected = i == _selectedWindowIndex;
            bool isTransparent = _windows[window];
            Console.ForegroundColor = isTransparent ? ConsoleColor.Yellow : ConsoleColor.White; 
            Console.ForegroundColor = isSelected ? ConsoleColor.Green : Console.ForegroundColor;
            Console.WriteLine($"{i + 1}. {window.Current.Name}");
        }
        Console.ForegroundColor = ConsoleColor.White;
    }

    private void UpdateConsole()
    {
        Console.Clear();
        string transparencyPercentage = Math.Round((double) _transparency / 255 * 100, 0).ToString(CultureInfo.InvariantCulture) + "%";
        Console.WriteLine($"WinIsTrans | Transparency: {transparencyPercentage}");
        Console.WriteLine("----------");
        Console.WriteLine();
        ListWindows();
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