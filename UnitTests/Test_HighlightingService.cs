using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Reflection;
using System.Threading; // Added to resolve ApartmentAttribute and ApartmentState
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WhiteboardGUI.Services;
using WhiteboardGUI.ViewModel;
using WhiteboardGUI.Models;
using WhiteboardGUI.Adorners;

namespace UnitTests
{
    [TestClass]
    public class Test_HighlightingService
    {
        /// <summary>
        /// Executes the given action on an STA thread and waits for its completion.
        /// Captures any exceptions thrown during the execution and rethrows them on the main thread.
        /// </summary>
        /// <param name="action">The action to execute on the STA thread.</param>
        private void RunInSta(Action action)
        {
            Exception exception = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (exception != null)
            {
                // Rethrow the exception on the main thread to fail the test
                throw new AssertFailedException("Test failed on STA thread.", exception);
            }
        }

        [TestMethod]
        public void Test_EnableHighlighting_AttachesEventHandlers()
        {
            RunInSta(() =>
            {
                // Arrange
                var testElement = new Border();

                var mockViewModel = new Mock<MainPageViewModel>();
                var mockShape = new Mock<IShape>();
                mockShape.Setup(s => s.Color).Returns("#FF0000"); // Example color

                // Create a parent container
                var parentContainer = new Grid
                {
                    DataContext = mockViewModel.Object
                };
                parentContainer.Children.Add(testElement);

                // Assign DataContext to testElement
                testElement.DataContext = mockShape.Object;

                // Create AdornerDecorator and add the container
                var adornerDecorator = new AdornerDecorator();
                adornerDecorator.Child = parentContainer;

                // Create a Window and set its Content to the adornerDecorator
                var window = new Window
                {
                    Content = adornerDecorator,
                    Width = 800,
                    Height = 600,
                    WindowStyle = WindowStyle.None, // Hide window decorations
                    ShowInTaskbar = false,
                    Visibility = Visibility.Hidden // Keep the window hidden during tests
                };

                // Show the window to initialize the visual tree
                window.Show();

                // Get the AdornerLayer via the actual visual tree
                var adornerLayer = AdornerLayer.GetAdornerLayer(testElement);
                Assert.IsNotNull(adornerLayer, "AdornerLayer was not found.");

                // Act
                HighlightingService.SetEnableHighlighting(testElement, true);

                // Simulate MouseEnter
                SimulateMouseEnter(testElement);

                // Access the private HoverTimer via reflection
                var hoverTimer = GetHoverTimer(testElement);
                Assert.IsNotNull(hoverTimer, "HoverTimer was not set on MouseEnter.");
                Assert.AreEqual(TimeSpan.FromSeconds(0.4), hoverTimer.Interval, "HoverTimer interval is incorrect.");
                Assert.IsTrue(hoverTimer.IsEnabled, "HoverTimer was not started.");

                // Cleanup
                window.Close();
            });
        }

        [TestMethod]
        public void Test_DisableHighlighting_DetachesEventHandlers()
        {
            RunInSta(() =>
            {
                // Arrange
                var testElement = new Border();

                var mockViewModel = new Mock<MainPageViewModel>();
                var mockShape = new Mock<IShape>();
                mockShape.Setup(s => s.Color).Returns("#FF0000"); // Example color

                // Create a parent container
                var parentContainer = new Grid
                {
                    DataContext = mockViewModel.Object
                };
                parentContainer.Children.Add(testElement);

                // Assign DataContext to testElement
                testElement.DataContext = mockShape.Object;

                // Create AdornerDecorator and add the container
                var adornerDecorator = new AdornerDecorator();
                adornerDecorator.Child = parentContainer;

                // Create a Window and set its Content to the adornerDecorator
                var window = new Window
                {
                    Content = adornerDecorator,
                    Width = 800,
                    Height = 600,
                    WindowStyle = WindowStyle.None, // Hide window decorations
                    ShowInTaskbar = false,
                    Visibility = Visibility.Hidden // Keep the window hidden during tests
                };

                // Show the window to initialize the visual tree
                window.Show();

                // Get the AdornerLayer via the actual visual tree
                var adornerLayer = AdornerLayer.GetAdornerLayer(testElement);
                Assert.IsNotNull(adornerLayer, "AdornerLayer was not found.");

                // Enable highlighting
                HighlightingService.SetEnableHighlighting(testElement, true);

                // Disable highlighting
                HighlightingService.SetEnableHighlighting(testElement, false);

                // Simulate MouseEnter and MouseLeave to verify that event handlers are detached
                SimulateMouseEnter(testElement);
                SimulateMouseLeave(testElement);

                // Access the private HoverTimer via reflection
                var hoverTimer = GetHoverTimer(testElement);
                Assert.IsNull(hoverTimer, "HoverTimer was not removed when highlighting was disabled.");

                // Cleanup
                window.Close();
            });
        }

        [TestMethod]
        public void Test_MouseEnter_StartsHoverTimer()
        {
            RunInSta(() =>
            {
                // Arrange
                var testElement = new Border();

                var mockViewModel = new Mock<MainPageViewModel>();
                var mockShape = new Mock<IShape>();
                mockShape.Setup(s => s.Color).Returns("#FF0000"); // Example color

                // Create a parent container
                var parentContainer = new Grid
                {
                    DataContext = mockViewModel.Object
                };
                parentContainer.Children.Add(testElement);

                // Assign DataContext to testElement
                testElement.DataContext = mockShape.Object;

                // Create AdornerDecorator and add the container
                var adornerDecorator = new AdornerDecorator();
                adornerDecorator.Child = parentContainer;

                // Create a Window and set its Content to the adornerDecorator
                var window = new Window
                {
                    Content = adornerDecorator,
                    Width = 800,
                    Height = 600,
                    WindowStyle = WindowStyle.None, // Hide window decorations
                    ShowInTaskbar = false,
                    Visibility = Visibility.Hidden // Keep the window hidden during tests
                };

                // Show the window to initialize the visual tree
                window.Show();

                // Get the AdornerLayer via the actual visual tree
                var adornerLayer = AdornerLayer.GetAdornerLayer(testElement);
                Assert.IsNotNull(adornerLayer, "AdornerLayer was not found.");

                // Act
                HighlightingService.SetEnableHighlighting(testElement, true);

                // Simulate MouseEnter
                SimulateMouseEnter(testElement);

                // Assert
                var hoverTimer = GetHoverTimer(testElement);
                Assert.IsNotNull(hoverTimer, "HoverTimer was not set on MouseEnter.");
                Assert.AreEqual(TimeSpan.FromSeconds(0.4), hoverTimer.Interval, "HoverTimer interval is incorrect.");
                Assert.IsTrue(hoverTimer.IsEnabled, "HoverTimer was not started.");

                // Cleanup
                window.Close();
            });
        }

        [TestMethod]
        public void Test_MouseEnter_Tick_AddsHoverAdorner()
        {
            RunInSta(() =>
            {
                // Arrange
                var testElement = new Border();

                var mockViewModel = new Mock<MainPageViewModel>();
                var mockShape = new Mock<IShape>();
                mockShape.Setup(s => s.Color).Returns("#FF0000"); // Example color

                // Create a parent container
                var parentContainer = new Grid
                {
                    DataContext = mockViewModel.Object
                };
                parentContainer.Children.Add(testElement);

                // Assign DataContext to testElement
                testElement.DataContext = mockShape.Object;

                // Create AdornerDecorator and add the container
                var adornerDecorator = new AdornerDecorator();
                adornerDecorator.Child = parentContainer;

                // Create a Window and set its Content to the adornerDecorator
                var window = new Window
                {
                    Content = adornerDecorator,
                    Width = 800,
                    Height = 600,
                    WindowStyle = WindowStyle.None, // Hide window decorations
                    ShowInTaskbar = false,
                    Visibility = Visibility.Hidden // Keep the window hidden during tests
                };

                // Show the window to initialize the visual tree
                window.Show();

                // Get the AdornerLayer via the actual visual tree
                var adornerLayer = AdornerLayer.GetAdornerLayer(testElement);
                Assert.IsNotNull(adornerLayer, "AdornerLayer was not found.");

                // Act
                HighlightingService.SetEnableHighlighting(testElement, true);

                // Simulate MouseEnter
                SimulateMouseEnter(testElement);

                // Access the private HoverTimer via reflection
                var hoverTimer = GetHoverTimer(testElement);
                Assert.IsNotNull(hoverTimer, "HoverTimer was not set on MouseEnter.");

                // Raise the Tick event
                RaiseDispatcherTimerTick(hoverTimer);

                // Assert
                mockViewModel.VerifySet(vm => vm.HoveredShape = mockShape.Object, Times.Once);
                mockViewModel.VerifySet(vm => vm.IsShapeHovered = true, Times.Once);

                // Verify that HoverAdorner was added
                var adorners = adornerLayer.GetAdorners(testElement);
                Assert.IsNotNull(adorners, "HoverAdorner was not added.");
                bool hoverAdornerFound = false;
                foreach (var adorner in adorners)
                {
                    if (adorner is HoverAdorner)
                    {
                        hoverAdornerFound = true;
                        break;
                    }
                }
                Assert.IsTrue(hoverAdornerFound, "HoverAdorner was not added to the AdornerLayer.");
                mockViewModel.VerifySet(vm => vm.CurrentHoverAdorner = It.IsAny<HoverAdorner>(), Times.Once);

                // Cleanup
                window.Close();
            });
        }

        [TestMethod]
        public void Test_MouseLeave_StopsAndRemovesHoverTimer()
        {
            RunInSta(() =>
            {
                // Arrange
                var testElement = new Border();

                var mockViewModel = new Mock<MainPageViewModel>();
                var mockShape = new Mock<IShape>();
                mockShape.Setup(s => s.Color).Returns("#FF0000"); // Example color

                // Create a parent container
                var parentContainer = new Grid
                {
                    DataContext = mockViewModel.Object
                };
                parentContainer.Children.Add(testElement);

                // Assign DataContext to testElement
                testElement.DataContext = mockShape.Object;

                // Create AdornerDecorator and add the container
                var adornerDecorator = new AdornerDecorator();
                adornerDecorator.Child = parentContainer;

                // Create a Window and set its Content to the adornerDecorator
                var window = new Window
                {
                    Content = adornerDecorator,
                    Width = 800,
                    Height = 600,
                    WindowStyle = WindowStyle.None, // Hide window decorations
                    ShowInTaskbar = false,
                    Visibility = Visibility.Hidden // Keep the window hidden during tests
                };

                // Show the window to initialize the visual tree
                window.Show();

                // Get the AdornerLayer via the actual visual tree
                var adornerLayer = AdornerLayer.GetAdornerLayer(testElement);
                Assert.IsNotNull(adornerLayer, "AdornerLayer was not found.");

                // Enable highlighting
                HighlightingService.SetEnableHighlighting(testElement, true);

                // Simulate MouseEnter
                SimulateMouseEnter(testElement);

                // Simulate MouseLeave
                SimulateMouseLeave(testElement);

                // Access the private HoverTimer via reflection
                var hoverTimer = GetHoverTimer(testElement);
                Assert.IsNull(hoverTimer, "HoverTimer was not removed on MouseLeave.");

                // Verify that the HoverAdorner was removed
                var adorners = adornerLayer.GetAdorners(testElement);
                if (adorners != null)
                {
                    bool hoverAdornerFound = false;
                    foreach (var adorner in adorners)
                    {
                        if (adorner is HoverAdorner)
                        {
                            hoverAdornerFound = true;
                            break;
                        }
                    }
                    Assert.IsFalse(hoverAdornerFound, "HoverAdorner was not removed from the AdornerLayer.");
                }

                // Verify ViewModel properties reset
                mockViewModel.VerifySet(vm => vm.HoveredShape = null, Times.Once);
                mockViewModel.VerifySet(vm => vm.IsShapeHovered = false, Times.Once);
                mockViewModel.VerifySet(vm => vm.CurrentHoverAdorner = null, Times.Once);

                // Cleanup
                window.Close();
            });
        }

        [TestMethod]
        public void Test_OnEnableHighlightingChanged_AttachAndDetach()
        {
            RunInSta(() =>
            {
                // Arrange
                var testElement = new Border();

                var mockViewModel = new Mock<MainPageViewModel>();
                var mockShape = new Mock<IShape>();
                mockShape.Setup(s => s.Color).Returns("#FF0000"); // Example color

                // Create a parent container
                var parentContainer = new Grid
                {
                    DataContext = mockViewModel.Object
                };
                parentContainer.Children.Add(testElement);

                // Assign DataContext to testElement
                testElement.DataContext = mockShape.Object;

                // Create AdornerDecorator and add the container
                var adornerDecorator = new AdornerDecorator();
                adornerDecorator.Child = parentContainer;

                // Create a Window and set its Content to the adornerDecorator
                var window = new Window
                {
                    Content = adornerDecorator,
                    Width = 800,
                    Height = 600,
                    WindowStyle = WindowStyle.None, // Hide window decorations
                    ShowInTaskbar = false,
                    Visibility = Visibility.Hidden // Keep the window hidden during tests
                };

                // Show the window to initialize the visual tree
                window.Show();

                // Get the AdornerLayer via the actual visual tree
                var adornerLayer = AdornerLayer.GetAdornerLayer(testElement);
                Assert.IsNotNull(adornerLayer, "AdornerLayer was not found.");

                // Act - Enable highlighting
                HighlightingService.SetEnableHighlighting(testElement, true);
                SimulateMouseEnter(testElement);
                var hoverTimer = GetHoverTimer(testElement);
                Assert.IsNotNull(hoverTimer, "HoverTimer was not set when highlighting was enabled.");

                // Act - Disable highlighting
                HighlightingService.SetEnableHighlighting(testElement, false);
                SimulateMouseLeave(testElement);
                hoverTimer = GetHoverTimer(testElement);
                Assert.IsNull(hoverTimer, "HoverTimer was not removed when highlighting was disabled.");

                // Cleanup
                window.Close();
            });
        }

        [TestMethod]
        public void Test_ElementMouseEnter_WithInvalidViewModel()
        {
            RunInSta(() =>
            {
                // Arrange
                var testElement = new Border();

                var mockViewModel = new Mock<MainPageViewModel>();
                var mockShape = new Mock<IShape>();
                mockShape.Setup(s => s.Color).Returns("#FF0000"); // Example color

                // Create a parent container with null DataContext
                var parentContainer = new Grid
                {
                    DataContext = null // Invalid ViewModel
                };
                parentContainer.Children.Add(testElement);

                // Assign DataContext to testElement
                testElement.DataContext = mockShape.Object;

                // Create AdornerDecorator and add the container
                var adornerDecorator = new AdornerDecorator();
                adornerDecorator.Child = parentContainer;

                // Create a Window and set its Content to the adornerDecorator
                var window = new Window
                {
                    Content = adornerDecorator,
                    Width = 800,
                    Height = 600,
                    WindowStyle = WindowStyle.None, // Hide window decorations
                    ShowInTaskbar = false,
                    Visibility = Visibility.Hidden // Keep the window hidden during tests
                };

                // Show the window to initialize the visual tree
                window.Show();

                // Get the AdornerLayer via the actual visual tree
                var adornerLayer = AdornerLayer.GetAdornerLayer(testElement);
                Assert.IsNotNull(adornerLayer, "AdornerLayer was not found.");

                // Act
                HighlightingService.SetEnableHighlighting(testElement, true);

                // Simulate MouseEnter
                SimulateMouseEnter(testElement);

                // Access the private HoverTimer via reflection
                var hoverTimer = GetHoverTimer(testElement);
                Assert.IsNotNull(hoverTimer, "HoverTimer was not set on MouseEnter.");

                // Raise the Tick event
                RaiseDispatcherTimerTick(hoverTimer);

                // Assert
                mockViewModel.VerifySet(vm => vm.HoveredShape = It.IsAny<IShape>(), Times.Never);
                mockViewModel.VerifySet(vm => vm.IsShapeHovered = true, Times.Never);
                mockViewModel.VerifySet(vm => vm.CurrentHoverAdorner = It.IsAny<HoverAdorner>(), Times.Never);

                // Verify that no HoverAdorner was added
                var adorners = adornerLayer.GetAdorners(testElement);
                bool hoverAdornerFound = false;
                if (adorners != null)
                {
                    foreach (var adorner in adorners)
                    {
                        if (adorner is HoverAdorner)
                        {
                            hoverAdornerFound = true;
                            break;
                        }
                    }
                }
                Assert.IsFalse(hoverAdornerFound, "HoverAdorner should not be added when ViewModel is invalid.");

                // Cleanup
                window.Close();
            });
        }

        [TestMethod]
        public void Test_ElementMouseEnter_WithAdornerLayerNotFound()
        {
            RunInSta(() =>
            {
                // Arrange
                var testElement = new Border();

                var mockViewModel = new Mock<MainPageViewModel>();
                var mockShape = new Mock<IShape>();
                mockShape.Setup(s => s.Color).Returns("#FF0000"); // Example color

                // Create a parent container
                var parentContainer = new Grid
                {
                    DataContext = mockViewModel.Object
                };
                parentContainer.Children.Add(testElement);

                // Assign DataContext to testElement
                testElement.DataContext = mockShape.Object;

                // Create AdornerDecorator without adding it to a Window
                var adornerDecorator = new AdornerDecorator();
                adornerDecorator.Child = parentContainer;

                // Intentionally NOT creating a Window or adding to the visual tree

                // Get the AdornerLayer via the actual visual tree
                var adornerLayer = AdornerLayer.GetAdornerLayer(testElement);
                Assert.IsNull(adornerLayer, "AdornerLayer should not be found as it's not part of the visual tree.");

                // Act
                HighlightingService.SetEnableHighlighting(testElement, true);

                // Simulate MouseEnter
                SimulateMouseEnter(testElement);

                // Access the private HoverTimer via reflection
                var hoverTimer = GetHoverTimer(testElement);
                Assert.IsNotNull(hoverTimer, "HoverTimer was not set on MouseEnter.");

                // Raise the Tick event
                RaiseDispatcherTimerTick(hoverTimer);

                // Assert
                mockViewModel.VerifySet(vm => vm.HoveredShape = It.IsAny<IShape>(), Times.Never);
                mockViewModel.VerifySet(vm => vm.IsShapeHovered = true, Times.Never);
                mockViewModel.VerifySet(vm => vm.CurrentHoverAdorner = It.IsAny<HoverAdorner>(), Times.Never);

                // Verify that no HoverAdorner was added
                var adornersAfterTick = AdornerLayer.GetAdornerLayer(testElement)?.GetAdorners(testElement);
                Assert.IsNull(adornersAfterTick, "HoverAdorner should not be added when AdornerLayer is not found.");
            });
        }

        [TestMethod]
        public void Test_GetImageSourceForShape_ReturnsImage()
        {
            RunInSta(() =>
            {
                // Arrange
                var method = typeof(HighlightingService).GetMethod("GetImageSourceForShape", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.IsNotNull(method, "GetImageSourceForShape method not found.");

                var mockShape = new Mock<IShape>();
                mockShape.Setup(s => s.Color).Returns("#FF0000");

                // Act
                var imageSource = method.Invoke(null, new object[] { mockShape.Object }) as ImageSource;

                // Assert
                Assert.IsNotNull(imageSource, "ImageSource should not be null.");
            });
        }

        #region Helper Methods

        /// <summary>
        /// Simulates the MouseEnter event on the given FrameworkElement.
        /// </summary>
        /// <param name="element">The FrameworkElement to raise the MouseEnter event on.</param>
        private void SimulateMouseEnter(FrameworkElement element)
        {
            var mouseEnterEvent = new MouseEventArgs(Mouse.PrimaryDevice, 0)
            {
                RoutedEvent = UIElement.MouseEnterEvent
            };
            element.RaiseEvent(mouseEnterEvent);
        }

        /// <summary>
        /// Simulates the MouseLeave event on the given FrameworkElement.
        /// </summary>
        /// <param name="element">The FrameworkElement to raise the MouseLeave event on.</param>
        private void SimulateMouseLeave(FrameworkElement element)
        {
            var mouseLeaveEvent = new MouseEventArgs(Mouse.PrimaryDevice, 0)
            {
                RoutedEvent = UIElement.MouseLeaveEvent
            };
            element.RaiseEvent(mouseLeaveEvent);
        }

        /// <summary>
        /// Uses reflection to access the private static method GetHoverTimer from HighlightingService.
        /// </summary>
        /// <param name="element">The DependencyObject to retrieve the HoverTimer for.</param>
        /// <returns>The DispatcherTimer associated with the element, or null if not found.</returns>
        private DispatcherTimer GetHoverTimer(FrameworkElement element)
        {
            var method = typeof(HighlightingService).GetMethod("GetHoverTimer", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                return null;

            return method.Invoke(null, new object[] { element }) as DispatcherTimer;
        }

        /// <summary>
        /// Uses reflection to invoke the private method OnTick on the DispatcherTimer.
        /// </summary>
        /// <param name="timer">The DispatcherTimer to raise the Tick event on.</param>
        private void RaiseDispatcherTimerTick(DispatcherTimer timer)
        {
            if (timer == null)
                return;

            var method = typeof(DispatcherTimer).GetMethod("OnTick", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(timer, null);
        }

        #endregion
    }

    // Extension method to raise Tick event on DispatcherTimer
    public static class DispatcherTimerExtensions
    {
        /// <summary>
        /// Raises the Tick event on the given DispatcherTimer by invoking the non-public OnTick method.
        /// </summary>
        /// <param name="timer">The DispatcherTimer to raise the Tick event on.</param>
        public static void RaiseTick(this DispatcherTimer timer)
        {
            var method = typeof(DispatcherTimer).GetMethod("OnTick", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(timer, null);
        }
    }
}