// HighlightingService.cs

using System.Windows;
using System.Windows.Input;
using WhiteboardGUI.ViewModel;
using WhiteboardGUI.Models;
using WhiteboardGUI.Adorners;
using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WhiteboardGUI.Services
{
    public static class HighlightingService
    {
        // Attached Property to enable highlighting
        public static readonly DependencyProperty EnableHighlightingProperty =
            DependencyProperty.RegisterAttached(
                "EnableHighlighting",
                typeof(bool),
                typeof(HighlightingService),
                new PropertyMetadata(false, OnEnableHighlightingChanged));

        // Getter
        public static bool GetEnableHighlighting(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableHighlightingProperty);
        }

        // Setter
        public static void SetEnableHighlighting(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableHighlightingProperty, value);
        }

        // Attached Property to store the hover timer for each element
        private static readonly DependencyProperty HoverTimerProperty =
            DependencyProperty.RegisterAttached(
                "HoverTimer",
                typeof(DispatcherTimer),
                typeof(HighlightingService),
                new PropertyMetadata(null));

        private static DispatcherTimer GetHoverTimer(DependencyObject obj)
        {
            return (DispatcherTimer)obj.GetValue(HoverTimerProperty);
        }

        private static void SetHoverTimer(DependencyObject obj, DispatcherTimer value)
        {
            obj.SetValue(HoverTimerProperty, value);
        }

        // Property Changed Callback
        private static void OnEnableHighlightingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if ((bool)e.NewValue)
                {
                    element.MouseEnter += Element_MouseEnter;
                    element.MouseLeave += Element_MouseLeave;
                }
                else
                {
                    element.MouseEnter -= Element_MouseEnter;
                    element.MouseLeave -= Element_MouseLeave;
                }
            }
        }

        // Mouse Enter Event Handler
        private static void Element_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                DispatcherTimer hoverTimer = new DispatcherTimer();
                hoverTimer.Interval = TimeSpan.FromSeconds(0.4);
                hoverTimer.Tick += (s, args) =>
                {
                    // Stop the timer
                    hoverTimer.Stop();

                    // Remove the timer from the attached property
                    SetHoverTimer(element, null);

                    var viewModel = FindParentViewModel(element);
                    if (viewModel is MainPageViewModel vm)
                    {
                        if (element.DataContext is IShape shape)
                        {

                            Debug.WriteLine("Hi this is" + shape);
                            Point elementPosition = element.TranslatePoint(new Point(0, 0), Application.Current.MainWindow);
                            // Create and add the HoverAdorner
                            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(element);
                            if (adornerLayer != null)
                            {
                                // Remove existing adorner if any
                                RemoveHoverAdorner(adornerLayer, vm);
                                // Update ViewModel properties
                                vm.HoveredShape = shape;
                                vm.IsShapeHovered = true;

                                // Create a new HoverAdorner with the shape details, mouse position, and color preview
                                Color shapeColor = (Color)ColorConverter.ConvertFromString(shape.Color);

                                // Get the ImageSource based on the shape
                                ImageSource imageSource = GetImageSourceForShape(shape);

                                // Create a new HoverAdorner with the shape details and mouse position
                                HoverAdorner hoverAdorner = new HoverAdorner(element, vm.HoveredShapeDetails, elementPosition, imageSource, shapeColor);

                                adornerLayer.Add(hoverAdorner);

                                // Store reference in ViewModel for potential updates/removal
                                vm.CurrentHoverAdorner = hoverAdorner;
                                Debug.WriteLine($"Hovered over shape: {shape.GetType().Name}");
                                Debug.WriteLine($"HoveredShapeDetails: {vm.HoveredShapeDetails}");
                            }
                            else
                            {
                                Debug.WriteLine("AdornerLayer not found.");
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("MainPageViewModel not found in DataContext.");
                    }
                };
                // Store the timer as an attached property
                SetHoverTimer(element, hoverTimer);

                // Start the timer
                hoverTimer.Start();
            }
        }

        private static ImageSource GetImageSourceForShape(IShape shape)
        {
            string imagePath = "";
            imagePath = "../Views/Assets/clienticon.png";

            // Create ImageSource from the image path
            try
            {
                return new BitmapImage(new Uri($"pack://application:,,,/{imagePath}", UriKind.Absolute));
            }
            catch
            {
                // Return null or a default image if the image cannot be found
                return null;
            }
        }
        // Mouse Leave Event Handler
        private static void Element_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                // Retrieve the timer
                DispatcherTimer hoverTimer = GetHoverTimer(element);
                if (hoverTimer != null)
                {
                    // Stop and remove the timer
                    hoverTimer.Stop();
                    SetHoverTimer(element, null);
                }

                // Retrieve the DataContext (ViewModel)
                var viewModel = FindParentViewModel(element);
                if (viewModel is MainPageViewModel vm)
                {
                    if (element.DataContext is IShape shape)
                    {
                        // Remove the HoverAdorner
                        AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(element);
                        if (adornerLayer != null && vm.CurrentHoverAdorner != null)
                        {
                            adornerLayer.Remove(vm.CurrentHoverAdorner);
                            vm.CurrentHoverAdorner = null;
                        }

                        // Update ViewModel properties
                        vm.HoveredShape = null;
                        vm.IsShapeHovered = false;

                        Debug.WriteLine("Mouse left shape.");
                    }
                }
                else
                {
                    Debug.WriteLine("MainPageViewModel not found on MouseLeave.");
                }
            }
        }

        // Helper method to find the ViewModel from the visual tree
        private static MainPageViewModel? FindParentViewModel(FrameworkElement element)
        {
            DependencyObject parent = element;
            while (parent != null)
            {
                parent = VisualTreeHelper.GetParent(parent);
                if (parent is FrameworkElement fe && fe.DataContext is MainPageViewModel)
                {
                    return fe.DataContext as MainPageViewModel;
                }
            }
            return null;
        }

        // Helper method to remove existing HoverAdorner
        private static void RemoveHoverAdorner(AdornerLayer adornerLayer, MainPageViewModel vm)
        {
            if (vm.CurrentHoverAdorner != null)
            {
                adornerLayer.Remove(vm.CurrentHoverAdorner);
                vm.CurrentHoverAdorner = null;
            }
        }

    }
}
