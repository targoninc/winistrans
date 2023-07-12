
namespace WinIsTransConsole
{
    internal static class Program
    {
        public static void Main()
        {
            WinIsTransApp program = new();
            program.Run();
            program.Dispose();
        }
    }
}
