using FileExplorer;
using Logger;
using Minecraft_Console;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FileExplorerCardsCreator
{
    public class FileExplorerCards : Window
    {
        private static readonly string? currentDirectory = Directory.GetCurrentDirectory();
        private static readonly string? rootFolder = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(currentDirectory)));

        public static void LoadFiles(string path, ListView Files_Folders_ListView, StackPanel pathContainer)
        {
            if (path[^1] == '\\') path = path[..^1];

            if (MainWindow.CurrentPath == path) return;

            MainWindow.CurrentPath = path;
            List<string[]> Files_Folders = ServerFileExplorer.GetFoldersAndFiles(MainWindow.CurrentPath);
            AddToListView(Files_Folders, Files_Folders_ListView, pathContainer);

            DisplayPathComponents(MainWindow.CurrentPath, Files_Folders_ListView, pathContainer);
        }

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
                Source = new BitmapImage(new Uri(System.IO.Path.Combine(rootFolder, $"assets\\icons\\folder_file\\{Type}.png")))
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

        public static void AddToListView(List<string[]> Files_Folders, ListView Files_Folders_ListView, StackPanel pathContainer)
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
                        item.MouseDoubleClick += (sender, e) => OpenFile(fileName, Files_Folders_ListView, pathContainer);
                    }
                    else if (fileType == "folder")
                    {
                        item.MouseDoubleClick += (sender, e) => OpenFolder(fileName, Files_Folders_ListView, pathContainer);
                    }

                    Files_Folders_ListView.Items.Add(item);
                }
            }
        }

        public static void DisplayPathComponents(string fullPath, ListView Files_Folders_ListView, StackPanel panel)
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
                    Foreground = Brushes.Gray,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    MinWidth = 0,
                    Template = (ControlTemplate)panel.FindResource("PathButtonTemplate")
                };

                btn.Click += (s, e) =>
                {
                    string? pathToLoad = ((Button)s).Tag?.ToString();
                    if (pathToLoad != null)
                    {
                        LoadFiles(pathToLoad, Files_Folders_ListView, panel);
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

        public static void AddTextEditorItem(ListView listView, string filePath, string fileContent)
        {
            // Clear ListView
            listView.Items.Clear();

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
            string fileName = System.IO.Path.GetFileName(filePath);

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
        private static void OpenFolder(string fileName, ListView Files_Folders_ListView, StackPanel pathContainer)
        {
            if (string.IsNullOrEmpty(MainWindow.CurrentPath))
            {
                MessageBox.Show("Current path is null.");
                return;
            }

            string combinedPath = System.IO.Path.Combine(MainWindow.CurrentPath, fileName);
            LoadFiles(combinedPath, Files_Folders_ListView, pathContainer);
        }

        private static void OpenFile(string fileName, ListView Files_Folders_ListView, StackPanel pathContainer)
        {
            if (string.IsNullOrEmpty(MainWindow.CurrentPath))
            {
                MessageBox.Show("Current path is null.");
                return;
            }

            string combinedPath = System.IO.Path.Combine(MainWindow.CurrentPath, fileName);

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
            string fileExtension = System.IO.Path.GetExtension(combinedPath).ToLower();

            CodeLogger.ConsoleLog($"fileExtension: {fileExtension}");
            string? fileContent = ServerFileExplorer.ReadFromFile(combinedPath);
            if (fileContent == null)
            {
                return;
            }

            // Check if the extension is supported
            if (supportedExtensions.Contains(fileExtension))
            {
                MainWindow.CurrentPath = combinedPath;
                AddTextEditorItem(Files_Folders_ListView, combinedPath, fileContent);
                DisplayPathComponents(MainWindow.CurrentPath, Files_Folders_ListView, pathContainer);
            }
            else
            {
                MessageBox.Show($"File type '{fileExtension}' is not supported.", "Unsupported File", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
    }
}