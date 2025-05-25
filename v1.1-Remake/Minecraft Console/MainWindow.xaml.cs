using CreateServerFunc;
using Logger;
using Minecraft_Console.ServerControl;
using Minecraft_Console.UI;
using NetworkConfig;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Updater;
using WpfAnimatedGif;

namespace Minecraft_Console
{
    public static class ComboBoxExtensions
    {
        public static void ToggleDropDownOnClick(this ComboBox comboBox)
        {
            comboBox.PreviewMouseLeftButtonDown += (sender, e) =>
            {
                if (!IsClickInsidePopupContent(e))
                {
                    comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
                    e.Handled = true;
                }
            };
        }

        private static bool IsClickInsidePopupContent(MouseButtonEventArgs e)
        {
            var clickedElement = e.OriginalSource as DependencyObject;

            while (clickedElement != null)
            {
                // Allow ComboBoxItem or ScrollBar clicks
                if (clickedElement is ComboBoxItem || clickedElement is ScrollBar)
                    return true;

                // Bonus: if you have checkboxes, add:
                if (clickedElement is CheckBox)
                    return true;

                clickedElement = VisualTreeHelper.GetParent(clickedElement);
            }

            return false;
        }
    }

    public static class ButtonExtensions
    {
        public static readonly DependencyProperty IsSidebarSelectedProperty =
            DependencyProperty.RegisterAttached(
                "IsSidebarSelected",
                typeof(bool),
                typeof(ButtonExtensions),
                new FrameworkPropertyMetadata(false));

        public static bool GetIsSidebarSelected(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsSidebarSelectedProperty);
        }

        public static void SetIsSidebarSelected(DependencyObject obj, bool value)
        {
            obj.SetValue(IsSidebarSelectedProperty, value);
        }
    }

    public partial class MainWindow : Window
    {
        private static readonly ServerInfoViewModel _viewModel = new();
        private Button? _selectedButton;
        private ServerManager _serverManager;

        public bool ExplorerPopupStatus = false;

        private static readonly string? currentDirectory = Directory.GetCurrentDirectory();

        private static string? Server_PublicComputerIP;
        private static string? Server_LocalComputerIP;

        public static Thread? ServerStatsThread = null;

        public static bool serverStatus = false; // false -> Offline; true -> Online
        public static bool serverRunning = false;
        public static int loadingScreenProcentage = 0;

        private static string? rootFolder;
        private static string? rootWorldsFolder;
        private static string? serverVersionsPath;
        private static string? tempFolderPath;
        private static string? defaultServerPropertiesPath;
        private static string? userData;
        public static string? CurrentPath;

        private static string? serverDirectoryPath;
        private static string? selectedServer = "";
        private static string? openWorldNumber;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
            _serverManager = new ServerManager(_viewModel);
            CodeLogger.CreateLogFile(5);
            SetStaticPaths();
            LoadDataJSONFile();
            OnLoaded();
            LoadServersPage();
            //_ = CheckRunningServersAsync();
        }

        private static void LoadDataJSONFile()
        {
            if (string.IsNullOrEmpty(userData))
            {
                throw new InvalidOperationException("The 'userData' path is not set.");
            }

            JsonHelper.CreateJsonIfNotExists(userData, new Dictionary<string, object>
            {
                { "runningServerMemory", "5000" },
                { "archivePath", $"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")}" }
            });
        }

        private void NavigateToServers(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(sender as Button);
            LoadServersPage();
        }
        private void NavigateToSettings(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(sender as Button);
            LoadPage("Settings Page");
        }
        private void NavigateToProfile(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(sender as Button);
            LoadPage("Profile Page");
        }
        private void NavigateToSupport(object sender, RoutedEventArgs e)
        {
            SetSelectedButton(sender as Button);
            LoadPage("Support Page");
        }

        private void LoadPage(string pageName)
        {
            MainContent.Children.Clear();
            MainContent.VerticalAlignment = VerticalAlignment.Stretch;
            MainContent.Children.Add(new Label
            {
                Content = pageName,
                Foreground = Brushes.White,
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center
            });

            MainContent.Visibility = Visibility.Visible;
            HideAllServerInfoGrids(true);
            CreateServerPage.Visibility = Visibility.Collapsed;
        }

        private void SetSelectedButton(Button? button)
        {
            if (_selectedButton != null)
            {
                ButtonExtensions.SetIsSidebarSelected(_selectedButton, false);
            }
            _selectedButton = button;
            if (_selectedButton != null)
            {
                ButtonExtensions.SetIsSidebarSelected(_selectedButton, true);
            }
        }

        private void OnLoaded()
        {
            GamemodeComboBox.ToggleDropDownOnClick();
            DifficultyComboBox.ToggleDropDownOnClick();
            SoftwareChangeBox.ToggleDropDownOnClick();
            VersionsChangeBox.ToggleDropDownOnClick();
            ServerDropBox.ToggleDropDownOnClick();

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(sidebarStackPanel); i++)
            {
                if (VisualTreeHelper.GetChild(sidebarStackPanel, i) is Button firstButton)
                {
                    SetSelectedButton(firstButton);
                    break;
                }
            }
        }

        // Paths setter func
        private static async void SetStaticPaths()
        {
            rootFolder = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(currentDirectory))) ?? string.Empty;
            rootWorldsFolder = System.IO.Path.Combine(rootFolder, "worlds") ?? string.Empty;
            serverVersionsPath = System.IO.Path.Combine(rootFolder, "versions") ?? string.Empty;
            tempFolderPath = System.IO.Path.Combine(rootFolder, "temp") ?? string.Empty;
            defaultServerPropertiesPath = System.IO.Path.Combine(rootFolder, "Preset Files\\server.properties") ?? string.Empty;
            userData = System.IO.Path.Combine(rootFolder, "data\\userData.json") ?? string.Empty;
            Server_PublicComputerIP = await NetworkSetup.GetPublicIP() ?? string.Empty;
            Server_LocalComputerIP = NetworkSetup.GetLocalIP() ?? string.Empty;
        }

        // GIF Loader/Unloader
        private void LoadGIF()
        {
            if (rootFolder == null)
            {
                MessageBox.Show("Root folder is not set.");
                return;
            }

            var image = new BitmapImage(new Uri(System.IO.Path.Combine(rootFolder, "assets\\loadingAnimations\\mining0.gif")));
            ImageBehavior.SetAnimatedSource(gifImage, image);
        }
        private void UnloadGIF()
        {
            // Remove the animated source
            ImageBehavior.SetAnimatedSource(gifImage, null);

            // Optionally force garbage collection to release the image
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        // File Explorer funcs
        public void ChangeAllCheckBoxes(object sender, RoutedEventArgs e)
        {
            var container = FindVisualParent<Grid>(ExplorerParent);
            if (container == null) return;

            bool shouldCheck = SelectAllCheckBox.IsChecked == true;

            foreach (var checkBox in FindVisualChildren<CheckBox>(container))
            {
                if (checkBox != SelectAllCheckBox) // avoid checking the controller itself
                {
                    checkBox.IsChecked = shouldCheck;
                }
            }
        }

        // Load Servers Page Funcs
        private void LoadServersPage()
        {
            MainContent.Children.Clear();
            MainContent.Opacity = 0;

            ScrollViewer scrollViewer = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            WrapPanel serverPanel = new()
            {
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                Orientation = Orientation.Horizontal
            };

            int cornerRadius = 20;
            Thickness buttonMargin = new(25);
            var worldsFromDb = dbChanger.SpecificDataFunc("SELECT worldNumber, name, version, totalPlayers, Process_ID FROM worlds");
            var existingDirs = Directory.Exists(rootWorldsFolder)
                ? [.. Directory.GetDirectories(rootWorldsFolder).Select(Path.GetFileName)]
                : new HashSet<string>();

            foreach (var worldEntry in worldsFromDb)
            {
                if (worldEntry.Length < 2) continue;

                string worldNumber = worldEntry[0]?.ToString() ?? "";
                string worldName = worldEntry[1]?.ToString() ?? "Unnamed World";
                string worldVersion = worldEntry[2]?.ToString() ?? "";
                string worldTotalPlayers = worldEntry[3]?.ToString() ?? "";
                string processID = worldEntry[4]?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(worldNumber) || !existingDirs.Contains(worldNumber))
                    continue;

                var button = ServerCard.CreateStyledButton(worldNumber, cornerRadius, worldName, worldVersion, worldTotalPlayers, processID, buttonMargin, () => OpenControlPanel(worldName, worldNumber));

                serverPanel.Children.Add(button);
            }

            var createButton = ServerCard.CreateStyledCreateButton(buttonMargin, cornerRadius, CreateServerButton_Click);
            serverPanel.Children.Add(createButton);

            scrollViewer.Content = serverPanel;
            MainContent.Children.Add(scrollViewer);

            HideAllServerInfoGrids(true);
            CreateServerPage.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;

            // Handle dynamic resizing
            SizeChanged += (s, e) => ServerCard.UpdateButtonSizes(serverPanel, MainContent.ActualWidth);

            // Delay initial sizing to ensure layout is updated
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ServerCard.UpdateButtonSizes(serverPanel, MainContent.ActualWidth);
            }), DispatcherPriority.Loaded);

            ServerCard.AnimateFadeIn(MainContent);
        }

        // Open Control Panel Funcs
        private void OpenControlPanel(string serverName, string worldNumber)
        {
            selectedServer = serverName;
            openWorldNumber = worldNumber;
            //SelectedServerLabel.Text = $"Server Name: {serverName}";

            if (string.IsNullOrEmpty(openWorldNumber))
            {
                MessageBox.Show("No world selected.");
                return;
            }
            if (rootWorldsFolder == null)
            {
                MessageBox.Show("Root worlds folder is not set.");
                return;
            }

            serverDirectoryPath = Path.Combine(rootWorldsFolder, openWorldNumber);
            CurrentPath = serverDirectoryPath;

            // Show control panel, hide server list
            MainContent.Visibility = Visibility.Collapsed;
            ServerDropBox.SelectedIndex = 0;
            ServerDropBox.Visibility = Visibility.Visible;

            if (ServerDropBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string? selectedName = selectedItem.Content.ToString();

                if (FindName(selectedName) is Grid selectedGrid)
                {
                    selectedGrid.Visibility = Visibility.Visible;
                }
            }
        }

        // Server Handeling funcs
        private async void CreateServer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(userData))
            {
                throw new InvalidOperationException("The 'userData' path is not set.");
            }

            try
            {
                CreateServerBTN.IsEnabled = false;

                // Gather inputs
                string software = ((ComboBoxItem)SoftwareChangeBox.SelectedItem)?.Content.ToString() ?? "";
                string version = VersionsChangeBox.Text;
                string worldName = ServerNameInput.Text;
                if (!int.TryParse(SlotsInput.Text, out int totalPlayers))
                {
                    MessageBox.Show("Invalid number of slots.");
                    return;
                }

                string localIP = NetworkSetup.GetLocalIP();
                string publicIP = await NetworkSetup.GetPublicIP();

                // Server ports
                int Server_Port = 25565;
                int JMX_Port = 25562;
                int RMI_Port = 25563;
                int RCON_Port = 25575;
                // Server Memory Allocator
                int memoryAllocator = Convert.ToInt32(JsonHelper.GetOrSetValue(userData, "runningServerMemory")?.ToString());

                // Utility for checkboxes
                static string GetCheckBoxValue(CheckBox cb, bool invert = false) =>
                    (invert ? !(cb.IsChecked ?? false) : cb.IsChecked ?? false).ToString().ToLowerInvariant();

                object[,] worldSettings =
                {
                    { "max-players", totalPlayers.ToString() },
                    { "gamemode", (GamemodeComboBox.SelectedItem as string ?? "").ToLowerInvariant() },
                    { "difficulty", (DifficultyComboBox.SelectedItem as string ?? "").ToLowerInvariant() },
                    { "white-list", GetCheckBoxValue(WhitelistCheckBox) },
                    { "online-mode", GetCheckBoxValue(CrackedCheckBox, invert: true) },
                    { "pvp", GetCheckBoxValue(PVPCheckBox) },
                    { "enable-command-block", GetCheckBoxValue(CommandblocksCheckBox) },
                    { "allow-flight", GetCheckBoxValue(FlyCheckBox) },
                    { "spawn-animals", GetCheckBoxValue(AnimalsCheckBox) },
                    { "spawn-monsters", GetCheckBoxValue(MonsterCheckBox) },
                    { "spawn-npcs", GetCheckBoxValue(VillagersCheckBox) },
                    { "allow-nether", GetCheckBoxValue(NetherCheckBox) },
                    { "force-gamemode", GetCheckBoxValue(ForceGamemodeCheckBox) },
                    { "spawn-protection", (SpawnProtectionInput.Text ?? "").ToLowerInvariant() }
                };

                if (rootFolder == null || rootWorldsFolder == null || tempFolderPath == null || defaultServerPropertiesPath == null)
                {
                    MessageBox.Show("One or more required paths are not set.");
                    return;
                }

                SetLoadingBarProgress(0);
                LoadGIF();
                LoadingScreen.Visibility = Visibility.Visible;

                string[] creationStatus = await Task.Run(() =>
                    ServerCreator.CreateServerFunc(
                    rootFolder, rootWorldsFolder, tempFolderPath, defaultServerPropertiesPath,
                    version, worldName, software, totalPlayers,
                    worldSettings, memoryAllocator,
                    localIP, Server_Port, JMX_Port, RCON_Port, RMI_Port
                    )
                );

                SetLoadingBarProgress(100);
                await Task.Delay(500);
                LoadingScreen.Visibility = Visibility.Collapsed;
                UnloadGIF();

                LoadServersPage();
                serverRunning = false;

                if (creationStatus[0] == "Error" && creationStatus.Length == 3)
                {
                    File.Delete(Path.Combine(rootWorldsFolder, creationStatus[2]));
                }

                PopupWindow.CreateStatusPopup(creationStatus[0], creationStatus[1], PopupHost); // TO DO
            }
            catch (Exception ex)
            {
                if (rootFolder == null)
                {
                    MessageBox.Show("Root folder is not set.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                CodeLogger.ConsoleLog($"Error creating the world. Error: {ex}");
                MessageBox.Show($"An unexpected error occurred. Check logs for details. Path: {Path.Combine(rootFolder, "logs\\latest.log")}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CreateServerBTN.IsEnabled = true;
            }
        }

        //private async void StartServer()
        //{
        //    Dispatcher.Invoke(() => DisableAllButtons(openWorldNumber));

        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(openWorldNumber) ||
        //            string.IsNullOrWhiteSpace(rootWorldsFolder) ||
        //            string.IsNullOrWhiteSpace(Server_PublicComputerIP) ||
        //            string.IsNullOrWhiteSpace(serverDirectoryPath) ||
        //            _serverManager == null)
        //        {
        //            MessageBox.Show("Some required fields are not set. Cannot start the server.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            return;
        //        }

        //        bool success = await _serverManager.StartServer(
        //            openWorldNumber,
        //            rootWorldsFolder,
        //            Server_PublicComputerIP,
        //            () => serverRunning,
        //            () => serverRunning = true,
        //            serverDirectoryPath,
        //            onServerRunning: (_) =>
        //            {
        //                Dispatcher.Invoke(() =>
        //                {
        //                    ToggleButtonStates(openWorldNumber, true);
        //                });
        //            }
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"An error occurred while starting the server:\n{ex.Message}", "Start Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        Dispatcher.Invoke(() => SetDefaultButtonStates(openWorldNumber, true));
        //    }
        //}

        //private async void StopServer()
        //{
        //    Dispatcher.Invoke(() => DisableAllButtons(openWorldNumber));

        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(openWorldNumber) ||
        //            string.IsNullOrWhiteSpace(Server_LocalComputerIP) ||
        //            _serverManager == null)
        //        {
        //            MessageBox.Show("Some required fields are not set. Cannot stop the server.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            return;
        //        }

        //        bool success = await ServerManager.StopServer(
        //            openWorldNumber, Server_LocalComputerIP,
        //            () => { serverRunning = false; serverStatus = false; }
        //        );

        //        Dispatcher.Invoke(() => ToggleButtonStates(openWorldNumber, !success));
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"An error occurred while stopping the server:\n{ex.Message}", "Stop Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        Dispatcher.Invoke(() => SetDefaultButtonStates(openWorldNumber, true));
        //    }
        //}

        //private async void RestartServer()
        //{
        //    Dispatcher.Invoke(() => DisableAllButtons(openWorldNumber));

        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(openWorldNumber) ||
        //            string.IsNullOrWhiteSpace(rootWorldsFolder) ||
        //            string.IsNullOrWhiteSpace(Server_LocalComputerIP) ||
        //            string.IsNullOrWhiteSpace(Server_PublicComputerIP) ||
        //            _serverManager == null)
        //        {
        //            MessageBox.Show("Some required fields are not set. Cannot restart the server.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            return;
        //        }

        //        bool success = await _serverManager.RestartServer(
        //            openWorldNumber, rootWorldsFolder, Server_LocalComputerIP, Server_PublicComputerIP,
        //            () => serverRunning,
        //            onServerRunning: (_) =>
        //            {
        //                Dispatcher.Invoke(() =>
        //                {
        //                    SetDefaultButtonStates(openWorldNumber, false);
        //                });
        //            }
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"An error occurred while restarting the server:\n{ex.Message}", "Restart Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        Dispatcher.Invoke(() => SetDefaultButtonStates(openWorldNumber, true));
        //    }
        //}

        // Create Server Page Show func

        private void CreateServerButton_Click()
        {
            ResetCreateServerButtons();

            // Switch to the create server page
            MainContent.Visibility = Visibility.Collapsed;
            CreateServerPage.Visibility = Visibility.Visible;
            ServerDropBox.Visibility = Visibility.Collapsed;

            List<string> supportedSoftwares = VersionsUpdater.GetSupportedSoftwares();

            SoftwareChangeBox.Items.Clear();
            foreach (string software in supportedSoftwares)
            {
                ComboBoxItem item = new()
                {
                    Content = software,
                    Tag = software
                };
                SoftwareChangeBox.Items.Add(item);
            }
            SoftwareChangeBox.SelectedIndex = 0; // Set default selection to the first item
        }

        private void ChangeSupportedVersions(object sender, SelectionChangedEventArgs e)
        {
            if (SoftwareChangeBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string? selectedName = selectedItem.Content.ToString();

                if (string.IsNullOrEmpty(selectedName))
                {
                    MessageBox.Show("Selected software is null.");
                    return;
                }

                // Get versions for the selected software
                List<string> versions = VersionsUpdater.GetSupportedVersions(selectedName);
                VersionsChangeBox.Items.Clear();
                foreach (string version in versions)
                {
                    ComboBoxItem item = new()
                    {
                        Content = version,
                        Tag = version
                    };
                    VersionsChangeBox.Items.Add(item);
                }

                VersionsChangeBox.SelectedIndex = 0; // Set default selection to the first item
            }
        }

        // Control Panel DropBox Handler
        private void ServerControlPanelDropBoxChanged(object sender, RoutedEventArgs e)
        {
            HideAllServerInfoGrids();
            if (ServerDropBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string? selectedName = selectedItem.Content.ToString();

                // Show the selected grid
                if (FindName(selectedName) is Grid selectedGrid)
                {
                    selectedGrid.Visibility = Visibility.Visible;

                    if (selectedName == "Files")
                    {
                        List<List<string>> Files_Folders = ServerFileExplorer.GetFoldersAndFiles(CurrentPath);
                        FileExplorerCards.CreateExplorerItems(ExplorerParent, Files_Folders, pathContainer);
                    }
                }
            }
        }

        // Send Command to Server
        //private async void Send_Command(object sender, RoutedEventArgs e)
        //{
        //    string? command = inputValue.Text;
        //    if (serverRunning == false)
        //    {
        //        MessageBox.Show("Server is not running.");
        //        return;
        //    }
        //    if (string.IsNullOrEmpty(command))
        //    {
        //        MessageBox.Show("Please enter a command.");
        //        return;
        //    }
        //    if (command == "stop")
        //    {
        //        MessageBox.Show("Stop the server from the button in the manager tab.");
        //        return;
        //    }
        //    if (string.IsNullOrEmpty(openWorldNumber))
        //    {
        //        MessageBox.Show("Stop the server from the button in the manager tab.");
        //        return;
        //    }

        //    try
        //    {
        //        string query = $"SELECT RCON_Port FROM worlds WHERE worldNumber = {openWorldNumber};";
        //        string? RCON_Port = dbChanger.SpecificDataFunc(query)[0][0].ToString() ?? string.Empty;

        //        if (string.IsNullOrEmpty(RCON_Port))
        //        {
        //            MessageBox.Show("Failed to retrieve the RCON port.");
        //            return;
        //        }

        //        if (string.IsNullOrEmpty(Server_LocalComputerIP))
        //        {
        //            MessageBox.Show("Local IP is not set.");
        //            return;
        //        }

        //        await ServerOperator.InputForServer(command, openWorldNumber, Convert.ToInt32(RCON_Port), Server_LocalComputerIP);
        //        //inputValue.Text = "";
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"An error occurred: {ex.Message}");
        //    }
        //}

        // Loading Screen Progress Bar Setter

        public static void SetLoadingBarProgress(int percentage)
        {
            int width = (int)(400 * (percentage / 100.0));
            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.progresBar.Width = width;
                loadingScreenProcentage = percentage;
            });
        }

        private void HideAllServerInfoGrids(bool verificator = false)
        {
            if (Manage != null) Manage.Visibility = Visibility.Collapsed;
            if (Console != null) Console.Visibility = Visibility.Collapsed;
            if (Files != null) Files.Visibility = Visibility.Collapsed;
            if (Stats != null) Stats.Visibility = Visibility.Collapsed;
            if (Settings != null) Settings.Visibility = Visibility.Collapsed;
            if (ServerDropBox != null && verificator == true) ServerDropBox.Visibility = Visibility.Collapsed;
        }

        //private static bool IsProcessRunning(int pid)
        //{
        //    try
        //    {
        //        if (pid <= 0) return false;
        //        Process process = Process.GetProcessById(pid);
        //        return !process.HasExited;
        //    }
        //    catch (ArgumentException)
        //    {
        //        // Thrown when no process with the specified ID is running
        //        return false;
        //    }
        //}

        private void ResetCreateServerButtons()
        {
            ServerNameInput.Text = "";

            SlotsInput.Text = "20";
            GamemodeComboBox.SelectedIndex = 0;
            DifficultyComboBox.SelectedIndex = 2;
            WhitelistCheckBox.IsChecked = false;
            CrackedCheckBox.IsChecked = false;
            PVPCheckBox.IsChecked = true;
            CommandblocksCheckBox.IsChecked = true;
            FlyCheckBox.IsChecked = true;
            AnimalsCheckBox.IsChecked = true;
            MonsterCheckBox.IsChecked = true;
            VillagersCheckBox.IsChecked = true;
            NetherCheckBox.IsChecked = true;
            ForceGamemodeCheckBox.IsChecked = false;
            SpawnProtectionInput.Text = "0";
        }

        private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;

                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T t)
                    yield return t;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }

        // To do: Implement the following methods for file operations
        private async void ExplorerArhiveBtn(object sender, RoutedEventArgs e)
        {
            try
            {
                ArhiveBtn.IsEnabled = false;
                DeleteBtn.IsEnabled = false;

                if (string.IsNullOrEmpty(userData))
                {
                    MessageBox.Show("The 'userData' path is not set.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                object[] selectedItems = FileExplorerCards.ShowSelectedItems(ExplorerParent);
                List<string> itemPaths = selectedItems[2] as List<string> ?? [];
                string downloadsPath = JsonHelper.GetOrSetValue(userData, "archivePath")?.ToString() ?? string.Empty;

                await Task.Run(() => AchiveExplorerItems(itemPaths, downloadsPath));

                CodeLogger.ConsoleLog($"Archived successfully to: {downloadsPath}");
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error archiving items: {ex.Message}");
                MessageBox.Show($"An error occurred while archiving items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                List<List<string>> Files_Folders = ServerFileExplorer.GetFoldersAndFiles(CurrentPath);
                FileExplorerCards.CreateExplorerItems(ExplorerParent, Files_Folders, pathContainer);

                await Task.Delay(300);

                ArhiveBtn.IsEnabled = true;
                DeleteBtn.IsEnabled = true;
            }
        }

        private async void ExplorerDeleteBtn(object sender, RoutedEventArgs e)
        {
            try
            {
                ArhiveBtn.IsEnabled = false;
                DeleteBtn.IsEnabled = false;

                object[] selectedItems = FileExplorerCards.ShowSelectedItems(ExplorerParent);
                List<string> itemPaths = selectedItems[2] as List<string> ?? [];

                foreach (var item in itemPaths)
                {
                    await Task.Run(() => DeleteExplorerItems(item));
                }

                CodeLogger.ConsoleLog($"Items deleted succeasfully!");
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error deleting items: {ex.Message}");
                MessageBox.Show($"An error occurred while deleting items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                List<List<string>> Files_Folders = ServerFileExplorer.GetFoldersAndFiles(CurrentPath);
                FileExplorerCards.CreateExplorerItems(ExplorerParent, Files_Folders, pathContainer);

                await Task.Delay(300);

                ArhiveBtn.IsEnabled = true;
                DeleteBtn.IsEnabled = true;
            }
        }

        private static void DeleteExplorerItems(string item)
        {
            if (Directory.Exists(item))
            {
                Directory.Delete(item, true); // Delete directory and its contents
            }
            else if (File.Exists(item))
            {
                File.Delete(item); // Delete file
            }
            else
            {
                CodeLogger.ConsoleLog($"Item not found: {item}");
            }
        }

        public static void AchiveExplorerItems(List<string> paths, string destinationFolder)
        {
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            // Generate random file name that doesn't exist
            string baseName = "archive";
            string zipPath = Path.Combine(destinationFolder, baseName + ".zip");
            int counter = 1;

            while (File.Exists(zipPath))
            {
                zipPath = Path.Combine(destinationFolder, $"{baseName} ({counter}).zip");
                counter++;
            }

            // Temp folder to stage all files/folders
            string tempRoot = Path.Combine(Path.GetTempPath(), "ArchiveTemp_" + Guid.NewGuid());
            Directory.CreateDirectory(tempRoot);

            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    string fileName = Path.GetFileName(path);
                    File.Copy(path, Path.Combine(tempRoot, fileName), true);
                }
                else if (Directory.Exists(path))
                {
                    string folderName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar));
                    string destFolder = Path.Combine(tempRoot, folderName);
                    DirectoryCopy(path, destFolder, true);
                }
            }

            // Create zip from temp folder
            ZipFile.CreateFromDirectory(tempRoot, zipPath);

            // Clean up temp
            Directory.Delete(tempRoot, true);
        }

        private static void DirectoryCopy(string sourceDir, string destDir, bool copySubDirs)
        {
            DirectoryInfo dir = new(sourceDir);
            if (!dir.Exists)
                throw new DirectoryNotFoundException("Source directory not found: " + sourceDir);

            Directory.CreateDirectory(destDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dir.GetDirectories())
                {
                    string newDestDir = Path.Combine(destDir, subdir.Name);
                    DirectoryCopy(subdir.FullName, newDestDir, copySubDirs);
                }
            }
        }
    }
}