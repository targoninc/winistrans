using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using static WinIsTransConsole.NativeMethods;

namespace WinIsTransConsole;

public static class WindowManager
{
    private static void AddWindowsAndChildren(AutomationElement element, int level, List<AutomationElement> output)
    {
        if (element.Current.NativeWindowHandle == 0 || level == 1 || element.Current.IsOffscreen)
        {
            return;
        }
        output.Add(element);

        Condition condition = new PropertyCondition(AutomationElement.IsEnabledProperty, true);
        AutomationElementCollection children = element.FindAll(TreeScope.Children, condition);

        foreach (AutomationElement child in children)
        {
            AddWindowsAndChildren(child, level + 1, output);
        }
    }

    public static List<AutomationElement> GetAllWindowsAndTheirChildren()
    {
        List<AutomationElement> output = new();
        AutomationElement desktop = AutomationElement.RootElement;

        if (desktop == null)
        {
            return output;
        }

        Condition condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
        AutomationElementCollection allWindows = desktop.FindAll(TreeScope.Children, condition);

        foreach (AutomationElement window in allWindows)
        {
            AddWindowsAndChildren(window, 0, output);
        }

        return output;
    }

    private static void ChangeWindowTransparency(AutomationElement window, int flagWithValue, int value)
    {
        IntPtr windowHandle = new(window.Current.NativeWindowHandle);

        int extendedStyle = GetWindowLong(windowHandle, GwlExstyle);
        extendedStyle |= flagWithValue;

        int result = SetWindowLong(windowHandle, GwlExstyle, extendedStyle);
        if (result < 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (value == -1)
        {
            value = 255;
        }
        SetLayeredWindowAttributes(windowHandle, 0, (byte) value, LwaAlpha);
    }

    public static void ApplyTransparencyToWindow(AutomationElement window, int transparency)
    {
        ChangeWindowTransparency(window, WsExLayered, transparency);
    }

    public static void ApplyTransparencyToWindows(ConcurrentDictionary<AutomationElement, bool> windows,
        int transparency)
    {
        foreach (AutomationElement window in windows.Keys)
        {
            if (windows[window])
            {
                ApplyTransparencyToWindow(window, transparency);
            }
            else
            {
                ResetTransparencyOnWindow(window);
            }
        }
    }

    public static void ResetTransparencyOnWindow(AutomationElement window)
    {
        ChangeWindowTransparency(window, ~WsExLayered, -1);
    }

    public static void ResetTransparencyOnWindows(IEnumerable<AutomationElement> windows)
    {
        foreach (AutomationElement window in windows)
        {
            try
            {
                ResetTransparencyOnWindow(window);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}