using OpenTK.Windowing.Desktop;
using WinIsTransConsole.GlDisplay;

namespace WinIsTransConsole
{
    internal static class Program
    {
        public static void Main()
        {
            GameWindowSettings gameWindowSettings = new()
            {
                UpdateFrequency = 120.0,
                RenderFrequency = 120.0
            };
            NativeWindowSettings nativeWindowSettings = new()
            {
                Location = new OpenTK.Mathematics.Vector2i(40, 40),
                Size = new OpenTK.Mathematics.Vector2i(800, 600),
                Title = "WinIsTrans"
            };
            using WinIsTransGlWindow window = new(gameWindowSettings, nativeWindowSettings);
            window.Run();

            WinIsTransApp program = new();
            program.Run(window);
            program.Dispose();
        }
    }
}
