using System.Runtime.InteropServices;

namespace WinIsTransLibrary;

public static class NativeMethods
{
    public const int GwlExstyle = -20;
    public const int WsExLayered = 0x80000;
    public const int LwaAlpha = 0x2;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
}