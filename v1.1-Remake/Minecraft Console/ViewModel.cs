using System.ComponentModel;

namespace Minecraft_Console;

public class ViewModel : INotifyPropertyChanged
{
    private string _smth = "smth";

    public string Smth
    {
        get => _smth;
        set
        {
            if (_smth != value)
            {
                _smth = value;
                OnPropertyChanged(nameof(Smth));
            }
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
