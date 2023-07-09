using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using static WinIsTransLibrary.NativeMethods;

namespace WinIsTransLibrary;

public static class WindowManager
{
    private static IEnumerable<AutomationElement> GetWindowsAndChildren(AutomationElement element, int level)
    {
        List<AutomationElement> output = new();
        if (element.Current.NativeWindowHandle == 0 || level == 1)
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
            Console.WriteLine($"{indentation}{i}/{children.Count}");
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
            Console.WriteLine($"{i}/{allWindows.Count}");
            output.AddRange(GetWindowsAndChildren(window, 0));
        }

        return output;
    }
    
    public static void ApplyTransparencyToWindow(AutomationElement window, int transparency)
    {
        // Get the window's handle
        IntPtr windowHandle = new(window.Current.NativeWindowHandle);

        // Get the extended window style
        int extendedStyle = GetWindowLong(windowHandle, GwlExstyle);

        // Add the WS_EX_LAYERED extended style
        extendedStyle |= WsExLayered;

        // Set the new extended window style
        int result = SetWindowLong(windowHandle, GwlExstyle, extendedStyle);
        if (result < 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        // Set the transparency
        SetLayeredWindowAttributes(windowHandle, 0, (byte)transparency, LwaAlpha);
    }
    
    public static void ApplyTransparencyToWindows(IEnumerable<AutomationElement> windows, int transparency)
    {
        foreach (AutomationElement window in windows)
        {
            ApplyTransparencyToWindow(window, transparency);
        }
    }
    
    public static void ResetTransparencyOnWindow(AutomationElement window)
    {
        // Get the window's handle
        IntPtr windowHandle = new(window.Current.NativeWindowHandle);

        // Get the extended window style
        int extendedStyle = GetWindowLong(windowHandle, GwlExstyle);

        // Remove the WS_EX_LAYERED extended style
        extendedStyle &= ~WsExLayered;

        // Set the new extended window style
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
            ResetTransparencyOnWindow(window);
        }
    }
}