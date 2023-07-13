using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using static WinIsTransConsole.NativeMethods;

namespace WinIsTransConsole;

public static class WindowManager
{
    private static IEnumerable<AutomationElement> GetWindowsAndChildren(AutomationElement element, int level)
    {
        List<AutomationElement> output = new();
        if (element.Current.NativeWindowHandle == 0 || level == 1)
        {
            return output;
        }

        if (element.Current.IsOffscreen)
        {
            return output;
        }

        // Add the current element's name to the list
        string indentation = new(' ', level * 4);
        output.Add(element);

        // Find all children
        Condition condition = new PropertyCondition(AutomationElement.IsEnabledProperty, true);
        AutomationElementCollection children = element.FindAll(TreeScope.Children, condition);

        int i = 0;
        foreach (AutomationElement child in children)
        {
            i++;
            output.AddRange(GetWindowsAndChildren(child, level + 1));
        }

        return output;
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

        int i = 0;
        foreach (AutomationElement window in allWindows)
        {
            i++;
            output.AddRange(GetWindowsAndChildren(window, 0));
        }

        return output;
    }
    
    public static void ApplyTransparencyToWindow(AutomationElement window, int transparency)
    {
        IntPtr windowHandle = new(window.Current.NativeWindowHandle);

        int extendedStyle = GetWindowLong(windowHandle, GwlExstyle);

        // Add the WS_EX_LAYERED extended style
        extendedStyle |= WsExLayered;

        // Set the new extended window style
        int result = SetWindowLong(windowHandle, GwlExstyle, extendedStyle);
        if (result < 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        SetLayeredWindowAttributes(windowHandle, 0, (byte)transparency, LwaAlpha);
    }
    
    public static void ApplyTransparencyToWindows(ConcurrentDictionary<AutomationElement, bool> windows, int transparency)
    {
        foreach (AutomationElement window in windows.Keys.Where(window => windows[window]))
        {
            ApplyTransparencyToWindow(window, transparency);
        }
        foreach (AutomationElement window in windows.Keys.Where(window => !windows[window]))
        {
            ResetTransparencyOnWindow(window);
        }
    }
    
    public static void ResetTransparencyOnWindow(AutomationElement window)
    {
        IntPtr windowHandle = new(window.Current.NativeWindowHandle);

        // Get the extended window style
        int extendedStyle = GetWindowLong(windowHandle, GwlExstyle);

        // Remove the WS_EX_LAYERED extended style
        extendedStyle &= ~WsExLayered;

        int result = SetWindowLong(windowHandle, GwlExstyle, extendedStyle);
        if (result < 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
    
    public static void ResetTransparencyOnWindows(IEnumerable<AutomationElement> windows)
    {
        foreach (AutomationElement window in windows)
        {
            try
            {
                ResetTransparencyOnWindow(window);
            } catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}