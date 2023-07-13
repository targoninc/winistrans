using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using WinIsTrans.ViewModels;
using WinIsTransConsole;

namespace WinIsTrans.Views;

public partial class MainWindow : Window
{
    private readonly WinIsTransApp _program;
    
    public MainWindow()
    {
        InitializeComponent();
        
        _program = new WinIsTransApp();
        _program.AttachTextHandler(OnTextChanged);
        
        KeyDown += OnKeyDown;
    }
    
    private MainWindowViewModel? GetMainWindowViewModel()
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            return viewModel;
        }
        Console.WriteLine("DataContext is not MainWindowViewModel");
        return null;

    }
    
    private bool OnTextChanged(string text)
    {
        MainWindowViewModel? viewModel = GetMainWindowViewModel();
        if (viewModel is null)
        {
            return false;
        }
        
        viewModel.MainText = text;
        return true;
    }
    
    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        Console.WriteLine($"OnKeyDown: {e.Key}");
    
        MainWindowViewModel? viewModel = GetMainWindowViewModel();
        if (viewModel is null)
        {
            return;
        }
        
        await _program.HandleAvaloniaKey(e);
    }
}