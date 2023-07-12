using Avalonia.Controls;
using Avalonia.Input;
using WinIsTransAvalonia.ViewModels;
using WinIsTransConsole;

namespace WinIsTransAvalonia.Views;

public partial class MainWindow : Window
{
    private readonly WinIsTransApp _program;
    
    public MainWindow()
    {
        InitializeComponent();
        
        _program = new WinIsTransApp();
        _program.Initialize();
        _program.AttachTextHandler(OnTextChanged);
        
        KeyDown += OnKeyDown;
    }
    
    private bool OnTextChanged(string text)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return false;
        }
        viewModel.MainText = text;
        return true;
    }
    
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }
        _program.HandleAvaloniaKey(e);
    }
}