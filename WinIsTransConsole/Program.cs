

using System.IO;
using WinIsTransLibrary;

List<string> allWindows = WindowManager.GetAllWindowsAndTheirChildren();

const string fileName = "output.txt";

foreach (string window in allWindows)
{
    Console.WriteLine(window);
    File.AppendAllText(fileName, window + Environment.NewLine);
}
