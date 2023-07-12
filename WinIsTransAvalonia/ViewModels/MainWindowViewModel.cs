using ReactiveUI;

namespace WinIsTransAvalonia.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private string _mainText = "Waiting...";

    public string MainText
    {
        get => _mainText;
        set
        {
            if (value == _mainText)
            {
                return;
            }
            _mainText = value;
            ((IReactiveObject) this).RaisePropertyChanged();
        }
    }
}
