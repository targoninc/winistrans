namespace WinIsTransConsole
{
    internal static class Program
    {
        public static void Main()
        {
            var program = new WinIsTransApp();
            program.Run();
            program.Dispose();
        }
    }
}
