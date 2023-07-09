using System.Windows.Automation;
using WinIsTransLibrary;

List<AutomationElement> allWindows = WindowManager.GetAllWindowsAndTheirChildren();

WindowManager.ApplyTransparencyToWindows(allWindows, 128);
await Task.Delay(2000);
WindowManager.ResetTransparencyOnWindows(allWindows);
