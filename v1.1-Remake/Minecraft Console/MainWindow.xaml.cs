using CreateServerFunc;
using databaseChanger;
using FileExplorer;
using Logger;
using MinecraftServerStats;
using NetworkConfig;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
                if (!IsClickInsidePopupContent(comboBox, e))
                {
                    comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
                    e.Handled = true;
                }
            };
        }

        private static bool IsClickInsidePopupContent(ComboBox comboBox, MouseButtonEventArgs e)
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
        public static CancellationTokenSource cancellationTokenSource = new();

        private Button? _selectedButton;

        private static Dictionary<Button, bool> _previousButtonStates = [];
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
        private static string? CurrentPath;

        private static string? serverDirectoryPath;
        private static string? selectedServer = "";
        private static string? openWorldNumber;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
            CodeLogger.CreateLogFile();
            SetStaticPaths();
            OnLoaded();
            LoadServersPage();
            _ = CheckRunningServersAsync();
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
            rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory))) ?? string.Empty;
            rootWorldsFolder = Path.Combine(rootFolder, "worlds") ?? string.Empty;
            serverVersionsPath = Path.Combine(rootFolder, "versions") ?? string.Empty;
            tempFolderPath = Path.Combine(rootFolder, "temp") ?? string.Empty;
            defaultServerPropertiesPath = Path.Combine(rootFolder, "Preset Files\\server.properties") ?? string.Empty;
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

            var image = new BitmapImage(new Uri(Path.Combine(rootFolder, "assets\\loadingAnimations\\mining0.gif")));
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
        private void LoadFiles(string path)
        {
            if (path[^1] == '\\') path = path[..^1];

            if (CurrentPath == path) return;

            CurrentPath = path;
            List<string[]> Files_Folders = ServerFileExplorer.GetFoldersAndFiles(CurrentPath);
            AddToListView(Files_Folders);

            DisplayPathComponents(pathContainer, CurrentPath);
        }

        public void BackButton_Click(object sender, RoutedEventArgs e)
        {
            string? TEMP_CurrentPath = Path.GetDirectoryName(CurrentPath);
            if (TEMP_CurrentPath == null)
            {
                MessageBox.Show("Current path is null.");
                return;
            }
            string[] pathParts = TEMP_CurrentPath.Split("\\");

            if (pathParts[^1] != "worlds")
            {
                CurrentPath = TEMP_CurrentPath;

                List<string[]> Files_Folders = ServerFileExplorer.GetFoldersAndFiles(CurrentPath);
                AddToListView(Files_Folders);

                DisplayPathComponents(pathContainer, CurrentPath);
            }
        }

        // File Explorer Grid Creator
        public static Grid CreateGrid(int id, string Name, string Type)
        {
            if (string.IsNullOrEmpty(rootFolder))
            {
                MessageBox.Show("Root folder is not set.");
                return new Grid(); // Return an empty grid
            }

            // Create Grid
            Grid grid = new()
            {
                Name = $"Element_{id}",
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = Brushes.Transparent
            };

            // Define Columns
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Create CheckBox
            CheckBox checkBox = new()
            {
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Width = 16,
                Height = 16,
                LayoutTransform = new ScaleTransform(1.5, 1.5),
                RenderTransformOrigin = new Point(0.5, 0.5),
                Background = Brushes.White,
                BorderBrush = Brushes.Transparent,
            };

            Grid.SetColumn(checkBox, 0);
            grid.Children.Add(checkBox);

            // Create Image
            Image image = new()
            {
                Width = 30,
                Height = 30,
                Margin = new Thickness(5, 0, 5, 0),
                Source = new BitmapImage(new Uri(Path.Combine(rootFolder, $"assets\\icons\\folder_file\\{Type}.png")))
            };
            Grid.SetColumn(image, 1);
            grid.Children.Add(image);

            // Create Button (Library)
            TextBlock itemOpenButton = new()
            {
                Padding = new Thickness(5, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.Transparent,
                Text = Name,
                FontSize = 25,
                Foreground = Brushes.White
            };
            Grid.SetColumn(itemOpenButton, 2);
            grid.Children.Add(itemOpenButton);

            // Create Button (...)
            Button optionsButton = new()
            {
                Margin = new Thickness(5, 0, 5, 0),
                Padding = new Thickness(5, 0, 5, 0),
                Width = 40,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                BorderThickness = new Thickness(2),
                BorderBrush = Brushes.White,
            };

            // Create a Viewbox to contain the content
            Viewbox viewBox = new()
            {
                Stretch = Stretch.Uniform, // Maintain aspect ratio
                StretchDirection = StretchDirection.DownOnly, // Only scale down
                VerticalAlignment = VerticalAlignment.Center, // Center vertically
                HorizontalAlignment = HorizontalAlignment.Center, // Center horizontally

                Child = new TextBlock
                {
                    Text = "...",
                    FontSize = 25,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                }
            };

            optionsButton.Content = viewBox; // Set the Viewbox as the button's content

            Grid.SetColumn(optionsButton, 3);
            grid.Children.Add(optionsButton);

            return grid;
        }

        private void AddToListView(List<string[]> Files_Folders)
        {
            Files_Folders_ListView.Items.Clear();

            if (Files_Folders.Count > 0)
            {
                for (int i = 0; i < Files_Folders.Count; i++)
                {
                    string fileType = Files_Folders[i][0];
                    string fileName = Files_Folders[i][1];

                    ListViewItem item = new()
                    {
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        Margin = new Thickness(0, 0, 0, 5),
                    };

                    Border border = new()
                    {
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(10, 10, 10, 10),
                        Child = CreateGrid(i + 1, fileName, fileType),
                        Background = new BrushConverter().ConvertFromString("#262A32") as SolidColorBrush,
                    };

                    item.Content = border;

                    if (fileType == "file")
                    {
                        item.MouseDoubleClick += (sender, e) => OpenFile(fileName);
                    }
                    else if (fileType == "folder")
                    {
                        item.MouseDoubleClick += (sender, e) => OpenFolder(fileName);
                    }

                    Files_Folders_ListView.Items.Add(item);
                }
            }
        }

        public void DisplayPathComponents(StackPanel panel, string fullPath)
        {
            panel.Children.Clear(); // clear previous buttons
            List<string[][]> components = ServerFileExplorer.GetStructuredPathComponents(fullPath);

            for (int i = 0; i < components.Count; i++)
            {
                string fullDirPath = components[i][0][0]; // full path with trailing '\'
                string folderName = components[i][1][0];  // name of the current folder

                var btn = new Button
                {
                    Content = folderName,
                    Tag = fullDirPath,
                    Margin = new Thickness(0),
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Background = Brushes.Transparent,
                    Foreground = Brushes.Gray, // Default text color is gray
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    MinWidth = 0,
                    // Remove Padding from here
                    Template = (ControlTemplate)panel.FindResource("PathButtonTemplate")
                };

                btn.Click += (s, e) =>
                {
                    string? pathToLoad = ((Button)s).Tag?.ToString();
                    if (pathToLoad != null)
                    {
                        LoadFiles(pathToLoad);
                    }
                };

                panel.Children.Add(btn);

                if (i < components.Count - 1)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = "\\",
                        FontSize = 16,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        VerticalAlignment = VerticalAlignment.Center,
                    });
                }
            }
        }

        public static void AddTextEditorItem(ListView listView, string filePath)
        {
            // Clear ListView
            listView.Items.Clear();

            string fileContent = ServerFileExplorer.ReadFromFile(filePath);

            // Outer border styling
            Border border = new()
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222428")),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(10, 10, 10, 10),
                Padding = new Thickness(10),
                VerticalAlignment = VerticalAlignment.Stretch,
                Height = double.NaN
            };

            Grid grid = new();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Extract file name from filePath
            string fileName = Path.GetFileName(filePath);

            // Create TextBlock for file name
            TextBlock fileNameTextBlock = new()
            {
                Text = fileName,
                FontSize = 22, // Adjust as needed
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 10, 10) // Add some margin
            };

            Button saveButton = new()
            {
                Content = "Save",
                FontSize = 14,
                Padding = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(Color.FromRgb(0x3a, 0x8f, 0xf1)),
                Foreground = Brushes.White,
                BorderBrush = Brushes.Transparent,
                Margin = new Thickness(0, 0, 0, 10),
                Cursor = Cursors.Hand
            };

            TextBox textBox = new()
            {
                Text = fileContent,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, // Enable TextBox scrollbars
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                Foreground = Brushes.White,
                BorderBrush = Brushes.Transparent,
                FontSize = 14,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 0)
            };

            saveButton.Click += (s, e) =>
            {
                try
                {
                    ServerFileExplorer.WriteToFile(filePath, textBox.Text);
                    MessageBox.Show("Saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            // Add fileNameTextBlock to the grid
            grid.Children.Add(fileNameTextBlock);
            Grid.SetRow(fileNameTextBlock, 0);
            Grid.SetColumn(fileNameTextBlock, 0);

            // Add saveButton to the grid
            grid.Children.Add(saveButton);
            Grid.SetRow(saveButton, 0);
            Grid.SetColumn(saveButton, 1); // Place it in the second column (adjust if needed)
            Grid.SetColumnSpan(saveButton, 1); // Ensure it only spans one column

            // Add textBox to the grid
            grid.Children.Add(textBox);
            Grid.SetRow(textBox, 1);
            Grid.SetColumn(textBox, 0);
            Grid.SetColumnSpan(textBox, 2); // Make it span both columns

            // Define column definitions to arrange the elements
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Allow the file name to take remaining space
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Allow the button to take its natural size

            border.Child = grid;

            ScrollViewer scrollViewer = new()
            {
                Content = border,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, // Disable ScrollViewer scroll
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0)
            };

            ListViewItem listItem = new()
            {
                Content = scrollViewer,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            Style style = new(typeof(ListViewItem), (Style)listView.FindResource(typeof(ListViewItem)));

            style.Setters.Add(new Setter(BackgroundProperty, Brushes.Transparent));
            style.Setters.Add(new Setter(FocusVisualStyleProperty, null));
            style.Setters.Add(new Setter(TemplateProperty, CreateListViewItemTemplate()));

            scrollViewer.SetValue(VerticalAlignmentProperty, VerticalAlignment.Stretch);
            scrollViewer.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Stretch);

            listItem.Style = style;

            listItem.MaxHeight = listView.ActualHeight;

            listView.Items.Add(listItem);

            listView.VerticalContentAlignment = VerticalAlignment.Stretch;
            listView.VerticalAlignment = VerticalAlignment.Stretch;

            listView.SizeChanged += (s, e) =>
            {
                foreach (ListViewItem item in listView.Items)
                {
                    item.MaxHeight = listView.ActualHeight;
                }
            };
        }

        private static ControlTemplate CreateListViewItemTemplate()
        {
            ControlTemplate template = new(typeof(ListViewItem));
            FrameworkElementFactory borderFactory = new(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            borderFactory.SetValue(Border.BorderBrushProperty, Brushes.Transparent);

            FrameworkElementFactory contentPresenterFactory = new(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Stretch);
            contentPresenterFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Stretch);

            borderFactory.AppendChild(contentPresenterFactory);
            template.VisualTree = borderFactory;
            return template;
        }

        // Funcs for handeling files and folders
        private void OpenFolder(string fileName)
        {
            if (CurrentPath == null)
            {
                MessageBox.Show("Current path is null.");
                return;
            }

            string combinedPath = Path.Combine(CurrentPath, fileName);
            LoadFiles(combinedPath);
        }

        private void OpenFile(string fileName)
        {
            if (CurrentPath == null)
            {
                MessageBox.Show("Current path is null.");
                return;
            }

            string combinedPath = Path.Combine(CurrentPath, fileName);

            // Array of supported extensions
            string[] supportedExtensions = {
                ".txt", ".cs", ".js", ".html", ".css", ".json", ".xml", ".log",
                ".py", ".java", ".cpp", ".c", ".h", ".php", ".rb", ".go", ".swift",
                ".ts", ".jsx", ".tsx", ".vue", ".sh", ".bat", ".ini", ".config",
                ".yaml", ".yml", ".md", ".sql", ".r", ".pl", ".dart", ".kt",
                ".asm", ".pas", ".vb", ".lua", ".groovy", ".fs", ".rs", ".scala",
                ".diff", ".patch", ".cmake", ".dockerfile", ".gitignore", ".env", ".properties",
                // Minecraft server files specific extensions
                ".yml", ".json", ".properties", ".conf", ".txt", ".log", ".toml"
            };

            // Get the file extension
            string fileExtension = Path.GetExtension(combinedPath).ToLower();

            // Check if the extension is supported
            if (supportedExtensions.Contains(fileExtension))
            {
                CurrentPath = combinedPath;
                AddTextEditorItem(Files_Folders_ListView, combinedPath);
                DisplayPathComponents(pathContainer, CurrentPath);
            }
            else
            {
                MessageBox.Show($"File type '{fileExtension}' is not supported.", "Unsupported File", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Check if server is running
        private async Task CheckRunningServersAsync()
        {
            List<object[]> dataDB = dbChanger.SpecificDataFunc($"SELECT worldNumber, Process_ID FROM worlds;");

            for (int i = 0; i < dataDB.Count; i++)
            {
                string worldNumber = dataDB[i][0].ToString() ?? string.Empty;
                string worldProcess = dataDB[i][1].ToString() ?? string.Empty;

                if (dataDB[i][1] != DBNull.Value)
                {
                    if (!IsProcessRunning(Convert.ToInt32(worldProcess)))
                    {
                        dbChanger.SpecificDataFunc($"UPDATE worlds SET serverUser = NULL, serverTempPsw = NULL, Process_ID = NULL WHERE worldNumber = \"{worldNumber}\";");
                        return;
                    }

                    if (rootWorldsFolder == null || string.IsNullOrEmpty(worldNumber))
                    {
                        MessageBox.Show("Required paths are not set.");
                        return;
                    }

                    serverDirectoryPath = Path.Combine(rootWorldsFolder, worldNumber);
                    serverRunning = true;
                    serverStatus = true;

                    List<object[]> serverData = dbChanger.SpecificDataFunc($"SELECT Server_Port, JMX_Port, RCON_Port, RMI_Port FROM worlds WHERE worldNumber = \"{worldNumber}\";");

                    int Server_Port = 0;
                    int JMX_Port = 0;
                    int RCON_Port = 0;
                    int RMI_Port = 0;

                    foreach (object[] data in serverData)
                    {
                        Server_Port = Convert.ToInt32(data[0]);
                        JMX_Port = Convert.ToInt32(data[1]);
                        RCON_Port = Convert.ToInt32(data[2]);
                        RMI_Port = Convert.ToInt32(data[3]);
                    }

                    dbChanger.SpecificDataFunc($"UPDATE worlds SET Process_ID = \"{worldProcess}\" WHERE worldNumber = \"{worldNumber}\";");


                    if (string.IsNullOrEmpty(Server_PublicComputerIP))
                    {
                        Server_PublicComputerIP = await NetworkSetup.GetPublicIP() ?? string.Empty;

                        if (string.IsNullOrEmpty(Server_PublicComputerIP))
                        {
                            MessageBox.Show("Public IP address is not set.");
                            return;
                        }
                    }

                    ServerStatsThread = new(async () =>
                    {
                        while (!cancellationTokenSource.Token.IsCancellationRequested && serverRunning && (ServerOperator.IsPortInUse(JMX_Port) || ServerOperator.IsPortInUse(RCON_Port)))
                        {
                            CodeLogger.ConsoleLog("Correct: " + serverRunning);
                            await ServerStats.GetServerInfo(_viewModel, serverDirectoryPath, worldNumber, Server_PublicComputerIP, JMX_Port, RCON_Port, Server_Port);
                        }
                    });
                    ServerStatsThread.Start();

                    UpdateButtonStates(StartBtn, StopBtn, RestartBtn);

                    break;
                }
            }
        }

        // Load Servers Page Funcs
        private void LoadServersPage()
        {
            SetStatsToEmpty();
            MainContent.Children.Clear();

            ScrollViewer scrollViewer = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            WrapPanel serverPanel = new()
            {
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Thickness buttonMargin = new(25);
            var worldsFromDb = dbChanger.SpecificDataFunc("SELECT worldNumber, name FROM worlds");
            var existingDirs = Directory.Exists(rootWorldsFolder)
                ? [.. Directory.GetDirectories(rootWorldsFolder).Select(Path.GetFileName)]
                : new HashSet<string>();

            double buttonWidth, buttonHeight;
            (buttonWidth, buttonHeight) = GetButtonDimensions(MainContent.ActualWidth, serverPanel.Children.Count + 1); // +1 for CreateServer

            foreach (var worldEntry in worldsFromDb)
            {
                if (worldEntry.Length < 2) continue;

                string worldNumber = worldEntry[0]?.ToString() ?? "";
                string worldName = worldEntry[1]?.ToString() ?? "Unnamed World";

                if (string.IsNullOrWhiteSpace(worldNumber) || !existingDirs.Contains(worldNumber))
                    continue;

                var button = CreateStyledButton(worldName, buttonMargin, () => OpenControlPanel(worldName, worldNumber), buttonWidth, buttonHeight);
                serverPanel.Children.Add(button);
            }

            var createButton = CreateStyledButton("Create Server", buttonMargin, CreateServerButton_Click, buttonWidth, buttonHeight);
            serverPanel.Children.Add(createButton);

            scrollViewer.Content = serverPanel;
            MainContent.Children.Add(scrollViewer);

            HideAllServerInfoGrids(true);
            MainContent.Visibility = Visibility.Visible;
            CreateServerPage.Visibility = Visibility.Collapsed;

            SizeChanged += (s, e) => UpdateButtonSizes(serverPanel); // handle resize
            UpdateButtonSizes(serverPanel); // initial sizing
        }

        private void UpdateButtonSizes(WrapPanel panel)
        {
            double containerWidth = MainContent.ActualWidth;
            double buttonMarginThickness = 2 * 25; // Left and Right margin
            double minButtonWidth = 300; // Increased minimum width for a larger look
            double aspectRatio = 662.5 / 326.0 * 1.5; // Adjusted aspect ratio to make them wider relative to height

            int numberOfButtons = panel.Children.Count;
            if (numberOfButtons == 0) return;

            double availableWidth = containerWidth - (panel.Margin.Left + panel.Margin.Right);

            foreach (Button btn in panel.Children)
            {
                double totalMargin = numberOfButtons * buttonMarginThickness;
                double calculatedButtonWidth = (availableWidth - totalMargin) / numberOfButtons;
                calculatedButtonWidth = Math.Max(minButtonWidth, calculatedButtonWidth);

                btn.MinWidth = minButtonWidth;
                btn.Width = calculatedButtonWidth;
                btn.Height = calculatedButtonWidth / aspectRatio + 50;
                btn.HorizontalAlignment = HorizontalAlignment.Stretch;
            }
        }

        private static Button CreateStyledButton(string content, Thickness margin, System.Action onClick, double width, double height)
        {
            var button = new Button
            {
                Content = content,
                Padding = new Thickness(10),
                Background = (Brush?)new BrushConverter().ConvertFrom("#262A32") ?? Brushes.White,
                Foreground = Brushes.White,
                FontSize = 25,
                Cursor = Cursors.Hand,
                Margin = margin,
                BorderBrush = Brushes.Transparent,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Width = width,
                Height = height,
                MaxWidth = 500,   // Optional - safety limit
                MaxHeight = 250,  // Optional - safety limit
                Template = CreateRoundedButtonTemplate(20)
            };

            button.Click += (s, e) => onClick();
            return button;
        }

        private static (double width, double height) GetButtonDimensions(double containerWidth, int buttonCount)
        {
            double margin = 2 * 25; // Left + Right margin per button
            double minButtonWidth = 300;
            double maxButtonWidth = 600;
            double maxButtonHeight = 350;

            double aspectRatio = 662.5 / 326.0 * 1.5;

            if (buttonCount <= 0) buttonCount = 1;

            double availableWidth = containerWidth - 20; // Account for WrapPanel margin
            double totalMargin = buttonCount * margin;
            double calculatedWidth = (availableWidth - totalMargin) / buttonCount;
            calculatedWidth = Math.Clamp(calculatedWidth, minButtonWidth, maxButtonWidth);

            double calculatedHeight = calculatedWidth / aspectRatio + 50;
            calculatedHeight = Math.Min(calculatedHeight, maxButtonHeight);

            return (calculatedWidth, calculatedHeight);
        }

        private static ControlTemplate CreateRoundedButtonTemplate(int radius)
        {
            var borderFactory = new FrameworkElementFactory(typeof(Border))
            {
                Name = "border"
            };

            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(BorderThicknessProperty));

            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenterFactory.SetValue(MarginProperty, new TemplateBindingExtension(PaddingProperty));
            contentPresenterFactory.SetValue(ContentProperty, new TemplateBindingExtension(ContentProperty));
            contentPresenterFactory.SetValue(ContentPresenter.ContentTemplateProperty, new TemplateBindingExtension(ContentTemplateProperty));

            borderFactory.AppendChild(contentPresenterFactory);

            var template = new ControlTemplate(typeof(Button))
            {
                VisualTree = borderFactory
            };

            var mouseOverTrigger = new Trigger
            {
                Property = IsMouseOverProperty,
                Value = true
            };
            mouseOverTrigger.Setters.Add(new Setter(Border.BackgroundProperty,
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F333D")), "border"));

            template.Triggers.Add(mouseOverTrigger);

            return template;
        }

        // Loading other pages (temp)
        private void LoadPage(string pageName)
        {
            SetStatsToEmpty();

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

        private void OpenControlPanel(string serverName, string worldNumber)
        {
            selectedServer = serverName;
            openWorldNumber = worldNumber;
            SelectedServerLabel.Text = $"Server Name: {serverName}";

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

            // Show control panel, hide server list
            MainContent.Visibility = Visibility.Collapsed;
            ServerDropBox.Visibility = Visibility.Visible;

            LoadFiles(serverDirectoryPath);
            SetStatsToEmpty(openWorldNumber);
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

                // Constants
                int Server_Port = 25565;
                int JMX_Port = 25562;
                int RMI_Port = 25563;
                int RCON_Port = 25575;
                int memoryAlocator = 5000;

                // Utility for checkboxes
                string GetCheckBoxValue(CheckBox cb, bool invert = false) =>
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

                string creationResult = await Task.Run(() =>
                    ServerCreator.CreateServerFunc(
                    rootFolder, rootWorldsFolder, tempFolderPath, defaultServerPropertiesPath,
                    version, worldName, software, totalPlayers,
                    worldSettings, memoryAlocator,
                    localIP, Server_Port, JMX_Port, RCON_Port, RMI_Port
                    )
                );

                SetLoadingBarProgress(100);
                await Task.Delay(500);
                LoadingScreen.Visibility = Visibility.Collapsed;
                UnloadGIF();

                if (string.IsNullOrWhiteSpace(creationResult) || creationResult.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"Failed to create server:\n{creationResult}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                LoadServersPage();
                serverRunning = false;
            }
            catch (Exception ex)
            {
                CodeLogger.ConsoleLog($"Error creating the world. Error: {ex}");
                MessageBox.Show("An unexpected error occurred. Check logs for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CreateServerBTN.IsEnabled = true;
            }
        }

        private async void StartServer(object sender, RoutedEventArgs e)
        {
            UpdateButtonStates(StartBtn, StopBtn, RestartBtn, disableAll: true);

            if (serverRunning)
            {
                MessageBox.Show("An server is already running.");
                return;
            }

            if (rootWorldsFolder == null || openWorldNumber == null)
            {
                MessageBox.Show("Required paths are not set.");
                return;
            }

            serverDirectoryPath = Path.Combine(rootWorldsFolder, openWorldNumber);

            int memoryAlocator = 5000; // in MB

            List<object[]> serverData = dbChanger.SpecificDataFunc($"SELECT Server_Port, JMX_Port, RCON_Port, RMI_Port FROM worlds WHERE worldNumber = \"{openWorldNumber}\";");

            int Server_Port = 0;
            int JMX_Port = 0;
            int RCON_Port = 0;
            int RMI_Port = 0;

            foreach (object[] data in serverData)
            {
                Server_Port = Convert.ToInt32(data[0]);
                JMX_Port = Convert.ToInt32(data[1]);
                RCON_Port = Convert.ToInt32(data[2]);
                RMI_Port = Convert.ToInt32(data[3]);
            }
            if (string.IsNullOrEmpty(Server_PublicComputerIP))
            {
                MessageBox.Show("Public IP address is not set.");
                return;
            }

            Thread StartServerAsyncThread = new(async () => await ServerOperator.Start(openWorldNumber, serverDirectoryPath, memoryAlocator, Server_PublicComputerIP, Server_Port, JMX_Port, RCON_Port, RMI_Port, noGUI: false, viewModel: _viewModel));
            StartServerAsyncThread.Start();

            while (serverRunning == false)
            {
                await Task.Delay(500);
            }

            UpdateButtonStates(StartBtn, StopBtn, RestartBtn, changeAfterEnable: true);
        }

        private void StopServer(object sender, RoutedEventArgs e)
        {
            UpdateButtonStates(StartBtn, StopBtn, RestartBtn);

            serverRunning = false;
            serverStatus = false;
            cancellationTokenSource.Cancel();
            List<object[]> serverData = dbChanger.SpecificDataFunc($"SELECT JMX_Port, RCON_Port FROM worlds WHERE worldNumber = \"{openWorldNumber}\";");

            int JMX_Port = 0;
            int RCON_Port = 0;

            foreach (object[] data in serverData)
            {
                JMX_Port = Convert.ToInt32(data[0]);
                RCON_Port = Convert.ToInt32(data[1]);
            }

            if (string.IsNullOrEmpty(Server_LocalComputerIP))
            {
                MessageBox.Show("Public IP address is not set.");
                return;
            }

            if (string.IsNullOrEmpty(openWorldNumber))
            {
                MessageBox.Show("World number is null.");
                return;
            }

            Thread StopServerAsyncThread = new(async () => await ServerOperator.Stop("stop", openWorldNumber, Server_LocalComputerIP, RCON_Port, JMX_Port, "00:00"));
            StopServerAsyncThread.Start();

            ServerStats.SetServerStatusOffline();
            SetStatsToEmpty(openWorldNumber);
        }

        private async void RestartServer(object sender, RoutedEventArgs e)
        {
            UpdateButtonStates(StartBtn, StopBtn, RestartBtn, disableAll: true);

            if (serverRunning == false)
            {
                MessageBox.Show("No server is running.");
                return;
            }

            if (rootWorldsFolder == null || openWorldNumber == null)
            {
                MessageBox.Show("Required paths are not set.");
                return;
            }

            serverDirectoryPath = Path.Combine(rootWorldsFolder, openWorldNumber);

            int memoryAlocator = 5000; // in MB

            List<object[]> serverData = dbChanger.SpecificDataFunc($"SELECT Server_Port, JMX_Port, RCON_Port, RMI_Port FROM worlds WHERE worldNumber = \"{openWorldNumber}\";");

            int Server_Port = 0;
            int JMX_Port = 0;
            int RCON_Port = 0;
            int RMI_Port = 0;

            foreach (object[] data in serverData)
            {
                Server_Port = Convert.ToInt32(data[0]);
                JMX_Port = Convert.ToInt32(data[1]);
                RCON_Port = Convert.ToInt32(data[2]);
                RMI_Port = Convert.ToInt32(data[3]);
            }

            if (string.IsNullOrEmpty(Server_LocalComputerIP) || string.IsNullOrEmpty(Server_PublicComputerIP))
            {
                MessageBox.Show("Public or Local IP address is not set.");
                return;
            }

            serverRunning = false;
            cancellationTokenSource.Cancel();
            Thread StopServerAsyncThread = new(async () => await ServerOperator.Stop("stop", openWorldNumber, Server_LocalComputerIP, RCON_Port, JMX_Port, "00:00"));
            StopServerAsyncThread.Start();

            SetStatsToEmpty(openWorldNumber);

            Thread StartServerAsyncThread = new(async () => await ServerOperator.Start(openWorldNumber, serverDirectoryPath, memoryAlocator, Server_PublicComputerIP, Server_Port, JMX_Port, RCON_Port, RMI_Port, noGUI: false, viewModel: _viewModel));
            StartServerAsyncThread.Start();

            while (serverRunning == false)
            {
                await Task.Delay(500);
            }

            UpdateButtonStates(StartBtn, StopBtn, RestartBtn, changeAfterEnable: false);
        }

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
                }
            }
        }

        // Send Command to Server
        private async void Send_Command(object sender, RoutedEventArgs e)
        {
            string? command = inputValue.Text;
            if (serverRunning == false)
            {
                MessageBox.Show("Server is not running.");
                return;
            }
            if (string.IsNullOrEmpty(command))
            {
                MessageBox.Show("Please enter a command.");
                return;
            }
            if (command == "stop")
            {
                MessageBox.Show("Stop the server from the button in the manager tab.");
                return;
            }
            if (string.IsNullOrEmpty(openWorldNumber))
            {
                MessageBox.Show("Stop the server from the button in the manager tab.");
                return;
            }

            try
            {
                string query = $"SELECT RCON_Port FROM worlds WHERE worldNumber = {openWorldNumber};";
                string? RCON_Port = dbChanger.SpecificDataFunc(query)[0][0].ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(RCON_Port))
                {
                    MessageBox.Show("Failed to retrieve the RCON port.");
                    return;
                }

                if (string.IsNullOrEmpty(Server_LocalComputerIP))
                {
                    MessageBox.Show("Local IP is not set.");
                    return;
                }

                await ServerOperator.InputForServer(command, openWorldNumber, Convert.ToInt32(RCON_Port), Server_LocalComputerIP);
                inputValue.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        // Loading Screen Progress Bar Setter
        public static void SetLoadingBarProgress(int percentage)
        {
            int width = (int)(400 * (percentage / 100.0));
            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.progresBar.Width = width;
            });
            loadingScreenProcentage = percentage;
        }

        // Set stats to empty
        private static void SetStatsToEmpty(string? worldNumber)
        {
            if (rootWorldsFolder == null || worldNumber == null)
            {
                MessageBox.Show("Required paths are not set.");
                return;
            }

            serverDirectoryPath = Path.Combine(rootWorldsFolder, worldNumber);

            string? maxPlayers = dbChanger.SpecificDataFunc($"SELECT totalPlayers FROM worlds WHERE worldNumber = \"{worldNumber}\";")[0][0].ToString() ?? string.Empty;
            string WorldSize = ServerStats.GetFolderSize(serverDirectoryPath);
            string? ConsoleOutput = ServerStats.GetConsoleOutput(serverDirectoryPath);
            if (_viewModel != null)
            {
                _viewModel.Console = ConsoleOutput;
                _viewModel.MemoryUsage = "0%";
                _viewModel.UpTime = "00:00:00";
                _viewModel.WorldSize = WorldSize;
                _viewModel.PlayersOnline = $"0 / {maxPlayers}";
            }
        }

        private static void SetStatsToEmpty()
        {
            if (_viewModel != null)
            {
                _viewModel.Console = "";
                _viewModel.MemoryUsage = "0%";
                _viewModel.UpTime = "00:00:00";
                _viewModel.WorldSize = "0 MB";
                _viewModel.PlayersOnline = $"0 / 0";
            }
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

        private static bool IsProcessRunning(int pid)
        {
            try
            {
                Process process = Process.GetProcessById(pid);
                return !process.HasExited;
            }
            catch (ArgumentException)
            {
                // Thrown when no process with the specified ID is running
                return false;
            }
        }

        private static void UpdateButtonStates(Button startButton, Button stopButton, Button restartButton, bool disableAll = false, bool changeAfterEnable = false)
        {
            if (disableAll)
            {
                // Save current states
                _previousButtonStates[startButton] = startButton.IsEnabled;
                _previousButtonStates[stopButton] = stopButton.IsEnabled;
                _previousButtonStates[restartButton] = restartButton.IsEnabled;

                // Disable all
                startButton.IsEnabled = false;
                stopButton.IsEnabled = false;
                restartButton.IsEnabled = false;
                return;
            }

            // Restore saved states if available
            if (_previousButtonStates.Count == 3)
            {
                startButton.IsEnabled = _previousButtonStates[startButton];
                stopButton.IsEnabled = _previousButtonStates[stopButton];
                restartButton.IsEnabled = _previousButtonStates[restartButton];
                _previousButtonStates.Clear(); // Optional: Clear after restoring

                if (changeAfterEnable)
                {
                    bool secondIsStartEnabled = startButton.IsEnabled;
                    startButton.IsEnabled = !secondIsStartEnabled;
                    stopButton.IsEnabled = secondIsStartEnabled;
                    restartButton.IsEnabled = secondIsStartEnabled;
                }
                return;
            }

            // Normal logic: Start controls others
            bool isStartEnabled = startButton.IsEnabled;
            startButton.IsEnabled = !isStartEnabled;
            stopButton.IsEnabled = isStartEnabled;
            restartButton.IsEnabled = isStartEnabled;
        }

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
    }
}