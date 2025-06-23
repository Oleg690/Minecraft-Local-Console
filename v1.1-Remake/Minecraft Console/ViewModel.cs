using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Minecraft_Console
{
    public class ViewModel(string worldNumber) : INotifyPropertyChanged
    {
        public string WorldNumber { get; } = worldNumber;

        private string _upTime = "0h 0m 0s";
        private string _memoryUsage = "0GB / 0GB";
        private string _playersOnline = "0 / 0";
        private string _worldSize = MainWindow.rootWorldsFolder != null && MainWindow.openWorldNumber != null
                  ? ServerStats.GetFolderSize(Path.Combine(MainWindow.rootWorldsFolder, MainWindow.openWorldNumber)) ?? "0MB"
                  : "0MB";
        private string _console = "";
        private bool _isActivePanel;

        public string MemoryUsage
        {
            get => _memoryUsage;
            set => SetProperty(ref _memoryUsage, value);
        }

        public string PlayersOnline
        {
            get => _playersOnline;
            set => SetProperty(ref _playersOnline, value);
        }

        public string WorldSize
        {
            get => _worldSize;
            set => SetProperty(ref _worldSize, value);
        }

        public string UpTime
        {
            get => _upTime;
            set => SetProperty(ref _upTime, value);
        }

        public string Console
        {
            get => _console;
            set => SetProperty(ref _console, value);
        }

        public bool IsActivePanel
        {
            get => _isActivePanel;
            set => SetProperty(ref _isActivePanel, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propName = "")
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            }
        }
    }

    // ------------------------------
    // ViewModelManager - One per worldNumber
    // ------------------------------
    public static class ViewModelManager
    {
        private static readonly Dictionary<string, ViewModel> _viewModels = [];

        /// <summary>
        /// Creates or returns the existing ViewModel for the world.
        /// </summary>
        public static ViewModel Create(string worldNumber)
        {
            if (!_viewModels.ContainsKey(worldNumber))
                _viewModels[worldNumber] = new ViewModel("default");

            _viewModels[worldNumber].IsActivePanel = true;
            return _viewModels[worldNumber];
        }

        /// <summary>
        /// Marks the ViewModel as inactive (used when the panel is closed).
        /// </summary>
        public static void Deactivate(string worldNumber)
        {
            if (_viewModels.TryGetValue(worldNumber, out var vm))
                vm.IsActivePanel = false;
        }

        /// <summary>
        /// Completely removes the ViewModel from memory.
        /// </summary>
        public static void Remove(string worldNumber)
        {
            _viewModels.Remove(worldNumber);
        }

        /// <summary>
        /// Clears all ViewModels — useful on app shutdown.
        /// </summary>
        public static void ClearAll()
        {
            _viewModels.Clear();
        }

        /// <summary>
        /// Gets the ViewModel if already created.
        /// </summary>
        public static ViewModel? Get(string worldNumber)
        {
            return _viewModels.TryGetValue(worldNumber, out var vm) ? vm : null;
        }

        /// <summary>
        /// Checks if a ViewModel exists.
        /// </summary>
        public static bool Exists(string worldNumber)
        {
            return _viewModels.ContainsKey(worldNumber);
        }
    }

    /// <summary>
    /// Manages the monitoring loop for a single Minecraft server.
    /// </summary>
    public class ServerMonitor(ViewModel viewModel, string worldNumber)
    {
        private CancellationTokenSource? _cts;
        private Task? _monitorTask;
        private readonly ViewModel _viewModel = viewModel;
        private readonly string _worldNumber = worldNumber;

        /// <summary>
        /// Starts the monitoring task for this server.
        /// </summary>
        public void StartMonitoring(
            string worldNumber,
            object[] serverData,
            string[] userData,
            string worldFolderPath,
            string ip,
            int jmxPort,
            int serverPort)
        {
            if (_viewModel == null || serverData == null || serverData.Length == 0 || serverData[0] == null)
            {
                CodeLogger.ConsoleLog("Data are null for the server monitoring!");
                return;
            }

            _cts = new CancellationTokenSource();

            if (MainWindow.openWorldNumber == worldNumber)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = (MainWindow)Application.Current.MainWindow;
                    mainWindow.SetConsoleOnline();
                });
            }

            _monitorTask = Task.Run(() => ServerStats.MonitorServer(
                _viewModel,
                worldFolderPath,
                serverData[3].ToString() ?? string.Empty,
                ip,
                jmxPort,
                serverPort,
                serverData[5].ToString() ?? string.Empty,
                userData,
                _cts.Token
            ));
        }

        /// <summary>
        /// It sets the default values to the promps.
        /// </summary>
        public static void VMDefaultValues(string worldNumber, ViewModel viewModel, bool offlineVerificator = true)
        {
            if (MainWindow.openWorldNumber == worldNumber && offlineVerificator)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = (MainWindow)Application.Current.MainWindow;
                    mainWindow.SetConsoleOffline();
                });
            }

            viewModel.UpTime = "0h 0m 0s";
            viewModel.MemoryUsage = "0GB / 0GB";
            viewModel.PlayersOnline = "0 / 0";
            if (MainWindow.rootWorldsFolder != null && MainWindow.openWorldNumber != null)
            {
                viewModel.WorldSize = ServerStats.GetFolderSize(Path.Combine(MainWindow.rootWorldsFolder, MainWindow.openWorldNumber)) ?? "0MB";
            }
            viewModel.Console = "";
        }

        /// <summary>
        /// Cancels the monitoring task.
        /// </summary>
        public async Task StopMonitoringAsync()
        {
            if (_cts == null)
                return;

            try
            {
                _cts.Cancel();

                if (_monitorTask != null)
                    await _monitorTask;  // wait for monitoring loop to exit
            }
            catch (OperationCanceledException)
            {
                CodeLogger.ConsoleLog("\nCancel the _cts nowwwww@!!!!321523512\n");
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error stopping monitoring: {ex}");
            }
            finally
            {
                _cts.Dispose();
                _monitorTask = null;
            }
        }

        /// <summary>
        /// Checks if this monitor is currently active.
        /// </summary>
        public bool IsRunning => _monitorTask != null && !_monitorTask.IsCompleted;

        /// <summary>
        /// Waits for the monitor task to complete (optional).
        /// </summary>
        public async Task WaitAsync()
        {
            if (_monitorTask != null)
            {
                try
                {
                    await _monitorTask;
                }
                catch (TaskCanceledException) { }
            }
        }
    }
}
