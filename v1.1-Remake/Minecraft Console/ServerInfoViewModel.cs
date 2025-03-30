using System.ComponentModel;

public class ServerInfoViewModel : INotifyPropertyChanged
{
    private string? _memoryUsage = "0%";
    private string? _worldSize = "0 MB";
    private string? _playersOnline = "0 / 0";
    private string? _upTime = "00:00:00";
    private string? _console = "Offline";

    public string MemoryUsage
    {
        get => _memoryUsage ?? string.Empty;
        set
        {
            _memoryUsage = value;
            OnPropertyChanged(nameof(MemoryUsage));
        }
    }

    public string WorldSize
    {
        get => _worldSize ?? string.Empty;
        set
        {
            _worldSize = value;
            OnPropertyChanged(nameof(WorldSize));
        }
    }

    public string PlayersOnline
    {
        get => _playersOnline ?? string.Empty;
        set
        {
            _playersOnline = value;
            OnPropertyChanged(nameof(PlayersOnline));
        }
    }

    public string UpTime
    {
        get => _upTime ?? string.Empty;
        set
        {
            _upTime = value;
            OnPropertyChanged(nameof(UpTime));
        }
    }

    public string Console
    {
        get => _console ?? string.Empty;
        set
        {
            _console = value;
            OnPropertyChanged(nameof(Console));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
