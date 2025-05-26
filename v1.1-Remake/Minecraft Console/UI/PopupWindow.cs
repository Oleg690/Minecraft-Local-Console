using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Minecraft_Console.UI
{
    public class PopupWindow : Window
    {
        private static readonly List<Border> ActivePopups = [];
        private const double PopupSpacing = 10;

        public static void CreateStatusPopup(string status, string message, Grid hostPanel)
        {
            Brush headerBackground = status switch
            {
                "Success" => ConvertBrush("#5ACB5A"),
                "Error" => ConvertBrush("#F04A4A"),
                _ => ConvertBrush("#2196F3")
            };

            var mainBorder = new Border
            {
                Name = "popupBorder",
                Margin = new Thickness(0, 25, 25, 0),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = Brushes.Transparent,
                ClipToBounds = true,
                Opacity = 0
            };

            var layoutGrid = new Grid { ClipToBounds = true };
            layoutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var topBorder = new Border
            {
                Background = headerBackground,
                CornerRadius = new CornerRadius(15, 15, 0, 0)
            };
            Grid.SetRow(topBorder, 0);

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Name = "popupStatus",
                Text = status,
                FontSize = 25,
                Foreground = Brushes.White,
                Margin = new Thickness(20, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var closeText = new TextBlock
            {
                Name = "popupCloseButton",
                Text = "×",
                FontSize = 35,
                Foreground = ConvertBrush("#1E1E1E"),
                Margin = new Thickness(0, 0, 20, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = Cursors.Hand,
                RenderTransform = new TranslateTransform { Y = 2 }
            };

            headerGrid.Children.Add(titleText);
            headerGrid.Children.Add(closeText);
            topBorder.Child = headerGrid;

            var bottomBorder = new Border
            {
                Background = ConvertBrush("#21242c"),
                CornerRadius = new CornerRadius(0, 0, 15, 15),
                Padding = new Thickness(20, 10, 20, 10)
            };
            Grid.SetRow(bottomBorder, 1);

            var messageText = new TextBlock
            {
                Name = "popupDescription",
                Text = message,
                FontSize = 25,
                MaxWidth = 400,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top
            };

            bottomBorder.Child = messageText;

            layoutGrid.Children.Add(topBorder);
            layoutGrid.Children.Add(bottomBorder);
            mainBorder.Child = layoutGrid;

            mainBorder.HorizontalAlignment = HorizontalAlignment.Right;
            mainBorder.VerticalAlignment = VerticalAlignment.Top;

            // Temp margin for now; we'll calculate the proper one below
            mainBorder.Margin = new Thickness(10);

            mainBorder.Loaded += (s, e) =>
            {
                SlideInPopup(mainBorder);
                RepositionPopups();
            };

            hostPanel.Children.Add(mainBorder);
            ActivePopups.Add(mainBorder);

            void SlideInPopup(Border popup)
            {
                // Slide-up animation for stacking
                var translateY = new DoubleAnimation(-100, 0, TimeSpan.FromMilliseconds(400))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                popup.RenderTransform = new TranslateTransform();
                popup.RenderTransformOrigin = new Point(0.5, 0.5); // Origin at center for sliding
                popup.RenderTransform.BeginAnimation(TranslateTransform.YProperty, translateY);

                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                popup.BeginAnimation(OpacityProperty, fadeIn);
            }

            void SlideOutAndRemove(Border popup)
            {
                // Slide-out to the right (without changing opacity)
                var translateX = new DoubleAnimation(0, 500, TimeSpan.FromMilliseconds(400))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };

                var transform = new TranslateTransform();
                popup.RenderTransform = transform;

                var storyboard = new Storyboard();
                storyboard.Children.Add(translateX);

                Storyboard.SetTarget(translateX, popup);
                Storyboard.SetTargetProperty(translateX, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

                storyboard.Completed += (s, e) =>
                {
                    hostPanel.Children.Remove(popup);
                    ActivePopups.Remove(popup);
                    RepositionPopups();
                };

                storyboard.Begin();
            }

            closeText.MouseLeftButtonUp += (s, e) => SlideOutAndRemove(mainBorder);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                SlideOutAndRemove(mainBorder);
            };
            timer.Start();

            CodeLogger.ConsoleLog(message);
        }

        private static void RepositionPopups()
        {
            double offsetY = 10;
            foreach (var popup in ActivePopups)
            {
                var translateY = new DoubleAnimation
                {
                    From = popup.Margin.Top,
                    To = offsetY,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                popup.BeginAnimation(MarginProperty, new ThicknessAnimation
                {
                    From = popup.Margin,
                    To = new Thickness(10, offsetY, 10, 0),
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                });

                offsetY += popup.ActualHeight + PopupSpacing;
            }
        }

        private static Brush ConvertBrush(string colorCode)
        {
            var brush = (Brush?)new BrushConverter().ConvertFromString(colorCode);
            return brush ?? throw new ArgumentNullException(nameof(colorCode), $"Invalid color code: {colorCode}");
        }
    }
}