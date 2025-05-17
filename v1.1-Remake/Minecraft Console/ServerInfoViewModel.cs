using System.ComponentModel;

namespace Minecraft_Console;

public class ServerInfoViewModel : INotifyPropertyChanged
{
    private string _memoryUsage = "0%";
    private string _worldSize = "0 MB";
    private string _playersOnline = "0 / 0";
    private string _upTime = "00:00:00";
    private string _console = "Offline";

    public string MemoryUsage
    {
        get => _memoryUsage;
        set
        {
            if (_memoryUsage != value)
            {
                _memoryUsage = value;
                OnPropertyChanged(nameof(MemoryUsage));
            }
        }
    }

    public string WorldSize
    {
        get => _worldSize;
        set
        {
            if (_worldSize != value)
            {
                _worldSize = value;
                OnPropertyChanged(nameof(WorldSize));
            }
        }
    }

    public string PlayersOnline
    {
        get => _playersOnline;
        set
        {
            if (_playersOnline != value)
            {
                _playersOnline = value;
                OnPropertyChanged(nameof(PlayersOnline));
            }
        }
    }

    public string UpTime
    {
        get => _upTime;
        set
        {
            if (_upTime != value)
            {
                _upTime = value;
                OnPropertyChanged(nameof(UpTime));
            }
        }
    }

    public string Console
    {
        get => _console;
        set
        {
            if (_console != value)
            {
                _console = value;
                OnPropertyChanged(nameof(Console));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
