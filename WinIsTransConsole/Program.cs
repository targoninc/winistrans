

namespace WinIsTransConsole
{
    internal class Program
    {
        public static void Main()
        {
            WinIsTransApp program = new();
            program.AttachTextHandler(TextChangedEventHandler);
            program.Run();
            program.Dispose();
        }

        private static bool TextChangedEventHandler(string text)
        {
            Console.WriteLine(text);
            return true;
        }
    }
}
