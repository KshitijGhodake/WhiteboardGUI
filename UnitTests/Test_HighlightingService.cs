// File: UnitTests/Test_HighlightingService.cs

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Reflection;
using System.Threading;
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

namespace WhiteboardGUI.UnitTests
{
    [TestClass]
    public class HighlightingServiceTests
    {
        /// <summary>
        /// Helper method to execute test actions on the STA thread.
        /// WPF requires UI components to be accessed on an STA thread.
        /// </summary>
        /// <param name="action">The test action to execute.</param>
        private void RunOnUIThread(Action action)
        {
            Exception capturedException = null;
            var done = new ManualResetEvent(false);
            var thread = new Thread(() =>
            {
                try
                {
                    // Initialize a DispatcherFrame to keep the STA thread alive
                    var frame = new DispatcherFrame();

                    // Execute the test action
                    action();

                    // Exit the DispatcherFrame after the action is executed
                    frame.Continue = false;
                    Dispatcher.PushFrame(frame);
                }
                catch (Exception ex)
                {
                    capturedException = ex;
                }
                finally
                {
                    done.Set();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            // Wait for the action to complete or timeout after 5 seconds
            if (!done.WaitOne(TimeSpan.FromSeconds(5)))
            {
                Assert.Fail("Test execution timed out.");
            }

            if (capturedException != null)
            {
                throw new Exception("Exception in UI thread.", capturedException);
            }
        }

        /// <summary>
        /// Tests the GetEnableHighlighting and SetEnableHighlighting methods.
        /// Ensures that the attached property is correctly set and retrieved.
        /// </summary>
        [TestMethod]
        public void EnableHighlightingProperty_GetSet()
        {
            RunOnUIThread(() =>
            {
                // Arrange
                var element = new Button();

                // Act
                HighlightingService.SetEnableHighlighting(element, true);
                var result = HighlightingService.GetEnableHighlighting(element);

                // Assert
                Assert.IsTrue(result, "EnableHighlighting should be true after setting to true.");

                // Act
                HighlightingService.SetEnableHighlighting(element, false);
                result = HighlightingService.GetEnableHighlighting(element);

                // Assert
                Assert.IsFalse(result, "EnableHighlighting should be false after setting to false.");
            });
        }

        /// <summary>
        /// Tests that enabling highlighting attaches the necessary event handlers
        /// and that the HoverAdorner is created and added after the hover timer ticks.
        /// </summary>
        [TestMethod]
        public void OnEnableHighlightingChanged_Enable_EventsAttached()
        {
            RunOnUIThread(() =>
            {
                // Arrange
                var window = new Window();
                var adornerDecorator = new AdornerDecorator();
                window.Content = adornerDecorator;

                var grid = new Grid();
                adornerDecorator.Child = grid;

                var element = new Button();
                grid.Children.Add(element);

                var viewModel = new MainPageViewModel(); // Use real instance
                window.DataContext = viewModel;

                // Show the window to build the visual tree
                window.Show();

                // Act
                HighlightingService.SetEnableHighlighting(element, true);

                // Simulate setting DataContext for the shape
                var mockShape = new Mock<IShape>();
                mockShape.Setup(s => s.ShapeId).Returns(Guid.NewGuid());
                mockShape.Setup(s => s.ShapeType).Returns("Circle");
                mockShape.Setup(s => s.Color).Returns("#FF0000");
                mockShape.Setup(s => s.StrokeThickness).Returns(2.0);
                mockShape.Setup(s => s.UserID).Returns(123.45);
                mockShape.Setup(s => s.LastModifierID).Returns(678.90);
                mockShape.Setup(s => s.ZIndex).Returns(1);
                mockShape.Setup(s => s.IsSelected).Returns(false);
                mockShape.Setup(s => s.GetBounds()).Returns(new Rect(0, 0, 100, 100));
                mockShape.Setup(s => s.Clone()).Returns(mockShape.Object);

                element.DataContext = mockShape.Object;

                // Raise MouseEnter event
                var mouseEnterEvent = new MouseEventArgs(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.MouseEnterEvent
                };
                element.RaiseEvent(mouseEnterEvent);

                // Wait for the DispatcherTimer to tick (0.5 seconds)
                var frame = new DispatcherFrame();
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(0.5)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    frame.Continue = false;
                };
                timer.Start();
                Dispatcher.PushFrame(frame);

                // Assert
                Assert.IsTrue(viewModel.IsShapeHovered, "IsShapeHovered should be true after timer tick.");
                Assert.IsNotNull(viewModel.HoveredShape, "HoveredShape should be set after timer tick.");
                Assert.IsNotNull(viewModel.CurrentHoverAdorner, "CurrentHoverAdorner should be set after timer tick.");

                // Optionally, verify that the HoverAdorner is added to the AdornerLayer
                var adornerLayer = AdornerLayer.GetAdornerLayer(element);
                var adorners = adornerLayer.GetAdorners(element);
                Assert.IsNotNull(adorners, "Adorners should not be null.");
                bool hoverAdornerAdded = false;
                if (adorners != null)
                {
                    foreach (var adorner in adorners)
                    {
                        if (adorner is HoverAdorner)
                        {
                            hoverAdornerAdded = true;
                            break;
                        }
                    }
                }
                Assert.IsTrue(hoverAdornerAdded, "HoverAdorner should be added to the AdornerLayer.");

                // Clean up
                window.Close();
            });
        }

        /// <summary>
        /// Tests that disabling highlighting detaches the event handlers
        /// and prevents further hover-related actions.
        /// </summary>
        [TestMethod]
        public void OnEnableHighlightingChanged_Disable_EventsDetached()
        {
            RunOnUIThread(() =>
            {
                // Arrange
                var window = new Window();
                var adornerDecorator = new AdornerDecorator();
                window.Content = adornerDecorator;

                var grid = new Grid();
                adornerDecorator.Child = grid;

                var element = new Button();
                grid.Children.Add(element);

                var viewModel = new MainPageViewModel(); // Use real instance
                window.DataContext = viewModel;

                window.Show();

                // Enable highlighting first
                HighlightingService.SetEnableHighlighting(element, true);

                // Now disable highlighting
                HighlightingService.SetEnableHighlighting(element, false);

                // Simulate MouseEnter event
                var mouseEnterEvent = new MouseEventArgs(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.MouseEnterEvent
                };
                element.RaiseEvent(mouseEnterEvent);

                // Wait to ensure that no timer is started
                var frame = new DispatcherFrame();
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(0.5)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    frame.Continue = false;
                };
                timer.Start();
                Dispatcher.PushFrame(frame);

                // Assert
                Assert.IsFalse(viewModel.IsShapeHovered, "IsShapeHovered should remain false after disabling highlighting.");
                Assert.IsNull(viewModel.HoveredShape, "HoveredShape should remain null after disabling highlighting.");
                Assert.IsNull(viewModel.CurrentHoverAdorner, "CurrentHoverAdorner should remain null after disabling highlighting.");

                // Clean up
                window.Close();
            });
        }

        /// <summary>
        /// Tests that the HoverTimer starts upon MouseEnter event.
        /// </summary>
        [TestMethod]
        public void MouseEnter_StartsHoverTimer()
        {
            RunOnUIThread(() =>
            {
                // Arrange
                var window = new Window();
                var adornerDecorator = new AdornerDecorator();
                window.Content = adornerDecorator;

                var grid = new Grid();
                adornerDecorator.Child = grid;

                var element = new Button();
                grid.Children.Add(element);

                var viewModel = new MainPageViewModel(); // Use real instance
                window.DataContext = viewModel;

                window.Show();

                HighlightingService.SetEnableHighlighting(element, true);

                var mockShape = new Mock<IShape>();
                mockShape.Setup(s => s.ShapeId).Returns(Guid.NewGuid());
                mockShape.Setup(s => s.ShapeType).Returns("Circle");
                mockShape.Setup(s => s.Color).Returns("#FF0000");
                mockShape.Setup(s => s.StrokeThickness).Returns(2.0);
                mockShape.Setup(s => s.UserID).Returns(123.45);
                mockShape.Setup(s => s.LastModifierID).Returns(678.90);
                mockShape.Setup(s => s.ZIndex).Returns(1);
                mockShape.Setup(s => s.IsSelected).Returns(false);
                mockShape.Setup(s => s.GetBounds()).Returns(new Rect(0, 0, 100, 100));
                mockShape.Setup(s => s.Clone()).Returns(mockShape.Object);

                element.DataContext = mockShape.Object;

                // Act
                var mouseEnterEvent = new MouseEventArgs(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.MouseEnterEvent
                };
                element.RaiseEvent(mouseEnterEvent);

                // Access the private HoverTimer using reflection
                var hoverTimer = GetPrivateHoverTimer(element);
                Assert.IsNotNull(hoverTimer, "HoverTimer should be initialized on MouseEnter.");
                Assert.IsTrue(hoverTimer.IsEnabled, "HoverTimer should be started on MouseEnter.");

                // Clean up
                hoverTimer.Stop();
                window.Close();
            });
        }

        /// <summary>
        /// Tests that the HoverTimer stops and the HoverAdorner is removed upon MouseLeave event.
        /// </summary>
        [TestMethod]
        public void MouseLeave_StopsHoverTimerAndRemovesAdorner()
        {
            RunOnUIThread(() =>
            {
                // Arrange
                var window = new Window();
                var adornerDecorator = new AdornerDecorator();
                window.Content = adornerDecorator;

                var grid = new Grid();
                adornerDecorator.Child = grid;

                var element = new Button();
                grid.Children.Add(element);

                var viewModel = new MainPageViewModel(); // Use real instance
                window.DataContext = viewModel;

                window.Show();

                HighlightingService.SetEnableHighlighting(element, true);

                var mockShape = new Mock<IShape>();
                mockShape.Setup(s => s.ShapeId).Returns(Guid.NewGuid());
                mockShape.Setup(s => s.ShapeType).Returns("Circle");
                mockShape.Setup(s => s.Color).Returns("#FF0000");
                mockShape.Setup(s => s.StrokeThickness).Returns(2.0);
                mockShape.Setup(s => s.UserID).Returns(123.45);
                mockShape.Setup(s => s.LastModifierID).Returns(678.90);
                mockShape.Setup(s => s.ZIndex).Returns(1);
                mockShape.Setup(s => s.IsSelected).Returns(false);
                mockShape.Setup(s => s.GetBounds()).Returns(new Rect(0, 0, 100, 100));
                mockShape.Setup(s => s.Clone()).Returns(mockShape.Object);

                element.DataContext = mockShape.Object;

                // Simulate MouseEnter to start the timer
                var mouseEnterEvent = new MouseEventArgs(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.MouseEnterEvent
                };
                element.RaiseEvent(mouseEnterEvent);

                // Access the private HoverTimer using reflection
                var hoverTimer = GetPrivateHoverTimer(element);
                Assert.IsNotNull(hoverTimer, "HoverTimer should be initialized on MouseEnter.");

                // Act
                var mouseLeaveEvent = new MouseEventArgs(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.MouseLeaveEvent
                };
                element.RaiseEvent(mouseLeaveEvent);

                // Assert
                Assert.IsFalse(hoverTimer.IsEnabled, "HoverTimer should be stopped on MouseLeave.");
                Assert.IsNull(viewModel.CurrentHoverAdorner, "CurrentHoverAdorner should be null after MouseLeave.");
                Assert.IsFalse(viewModel.IsShapeHovered, "IsShapeHovered should be false after MouseLeave.");
                Assert.IsNull(viewModel.HoveredShape, "HoveredShape should be null after MouseLeave.");

                // Clean up
                window.Close();
            });
        }

        /// <summary>
        /// Tests that the HoverAdorner is created and added to the AdornerLayer when the HoverTimer ticks.
        /// </summary>
        [TestMethod]
        public void HoverTimer_Tick_CreatesHoverAdorner()
        {
            RunOnUIThread(() =>
            {
                // Arrange
                var window = new Window();
                var adornerDecorator = new AdornerDecorator();
                window.Content = adornerDecorator;

                var grid = new Grid();
                adornerDecorator.Child = grid;

                var element = new Button();
                grid.Children.Add(element);

                var viewModel = new MainPageViewModel(); // Use real instance
                window.DataContext = viewModel;

                window.Show();

                HighlightingService.SetEnableHighlighting(element, true);

                var mockShape = new Mock<IShape>();
                mockShape.Setup(s => s.ShapeId).Returns(Guid.NewGuid());
                mockShape.Setup(s => s.ShapeType).Returns("Circle");
                mockShape.Setup(s => s.Color).Returns("#FF0000");
                mockShape.Setup(s => s.StrokeThickness).Returns(2.0);
                mockShape.Setup(s => s.UserID).Returns(123.45);
                mockShape.Setup(s => s.LastModifierID).Returns(678.90);
                mockShape.Setup(s => s.ZIndex).Returns(1);
                mockShape.Setup(s => s.IsSelected).Returns(false);
                mockShape.Setup(s => s.GetBounds()).Returns(new Rect(0, 0, 100, 100));
                mockShape.Setup(s => s.Clone()).Returns(mockShape.Object);

                element.DataContext = mockShape.Object;

                // Act
                var mouseEnterEvent = new MouseEventArgs(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.MouseEnterEvent
                };
                element.RaiseEvent(mouseEnterEvent);

                // Wait for the DispatcherTimer to tick (0.5 seconds)
                var frame = new DispatcherFrame();
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(0.5)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    frame.Continue = false;
                };
                timer.Start();
                Dispatcher.PushFrame(frame);

                // Assert
                Assert.IsTrue(viewModel.IsShapeHovered, "IsShapeHovered should be true after timer tick.");
                Assert.IsNotNull(viewModel.HoveredShape, "HoveredShape should be set after timer tick.");
                Assert.IsNotNull(viewModel.CurrentHoverAdorner, "CurrentHoverAdorner should be set after timer tick.");

                // Verify that the HoverAdorner is added to the AdornerLayer
                var adornerLayer = AdornerLayer.GetAdornerLayer(element);
                var adorners = adornerLayer.GetAdorners(element);
                Assert.IsNotNull(adorners, "Adorners should not be null.");
                bool hoverAdornerAdded = false;
                if (adorners != null)
                {
                    foreach (var adorner in adorners)
                    {
                        if (adorner is HoverAdorner)
                        {
                            hoverAdornerAdded = true;
                            break;
                        }
                    }
                }
                Assert.IsTrue(hoverAdornerAdded, "HoverAdorner should be added to the AdornerLayer.");

                // Clean up
                window.Close();
            });
        }

        /// <summary>
        /// Tests that no exception is thrown when MouseEnter is raised without a ViewModel present.
        /// </summary>
        [TestMethod]
        public void MouseEnter_NoViewModel_DoesNotThrow()
        {
            RunOnUIThread(() =>
            {
                // Arrange
                var window = new Window();
                var adornerDecorator = new AdornerDecorator();
                window.Content = adornerDecorator;

                var grid = new Grid();
                adornerDecorator.Child = grid;

                var element = new Button();
                grid.Children.Add(element);

                window.Show();

                HighlightingService.SetEnableHighlighting(element, true);

                // Ensure DataContext is not set
                element.DataContext = null;

                // Act & Assert
                try
                {
                    var mouseEnterEvent = new MouseEventArgs(Mouse.PrimaryDevice, 0)
                    {
                        RoutedEvent = UIElement.MouseEnterEvent
                    };
                    element.RaiseEvent(mouseEnterEvent);

                    // Wait for the DispatcherTimer to tick
                    var frame = new DispatcherFrame();
                    var timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(0.5)
                    };
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        frame.Continue = false;
                    };
                    timer.Start();
                    Dispatcher.PushFrame(frame);

                    // If no exception is thrown, pass the test
                    Assert.IsTrue(true);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Exception was thrown when ViewModel was not present: {ex.Message}");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>
        /// Tests that no exception is thrown when the HoverTimer ticks without an AdornerLayer present.
        /// </summary>
        [TestMethod]
        public void HoverTimer_Tick_NoAdornerLayer_DoesNotThrow()
        {
            RunOnUIThread(() =>
            {
                // Arrange
                // Create an element without adding it to a visual tree with AdornerLayer
                var element = new Button();
                HighlightingService.SetEnableHighlighting(element, true);

                var viewModel = new MainPageViewModel(); // Use real instance
                element.DataContext = viewModel;

                // Act & Assert
                try
                {
                    var mouseEnterEvent = new MouseEventArgs(Mouse.PrimaryDevice, 0)
                    {
                        RoutedEvent = UIElement.MouseEnterEvent
                    };
                    element.RaiseEvent(mouseEnterEvent);

                    // Wait for the DispatcherTimer to tick
                    var frame = new DispatcherFrame();
                    var timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(0.5)
                    };
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        frame.Continue = false;
                    };
                    timer.Start();
                    Dispatcher.PushFrame(frame);

                    // If no exception is thrown, pass the test
                    Assert.IsTrue(true);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Exception was thrown when AdornerLayer was not found: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Tests that no exception is thrown when MouseLeave is raised without an active HoverTimer.
        /// </summary>
        [TestMethod]
        public void MouseLeave_NoHoverTimer_DoesNotThrow()
        {
            RunOnUIThread(() =>
            {
                // Arrange
                var window = new Window();
                var adornerDecorator = new AdornerDecorator();
                window.Content = adornerDecorator;

                var grid = new Grid();
                adornerDecorator.Child = grid;

                var element = new Button();
                grid.Children.Add(element);

                var viewModel = new MainPageViewModel(); // Use real instance
                window.DataContext = viewModel;

                window.Show();

                HighlightingService.SetEnableHighlighting(element, true);

                // Simulate MouseLeave without a running HoverTimer
                var mouseLeaveEvent = new MouseEventArgs(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.MouseLeaveEvent
                };
                element.RaiseEvent(mouseLeaveEvent);

                // Assert that no exceptions are thrown and properties remain unset
                Assert.IsFalse(viewModel.IsShapeHovered, "IsShapeHovered should remain false.");
                Assert.IsNull(viewModel.HoveredShape, "HoveredShape should remain null.");
                Assert.IsNull(viewModel.CurrentHoverAdorner, "CurrentHoverAdorner should remain null.");

                // Clean up
                window.Close();
            });
        }

        /// <summary>
        /// Tests that multiple hover events correctly remove the previous HoverAdorner and add a new one.
        /// </summary>
        [TestMethod]
        public void MultipleHovers_RemoveAndAddHoverAdorner()
        {
            RunOnUIThread(() =>
            {
                // Arrange
                var window = new Window();
                var adornerDecorator = new AdornerDecorator();
                window.Content = adornerDecorator;

                var grid = new Grid();
                adornerDecorator.Child = grid;

                var element = new Button();
                grid.Children.Add(element);

                var viewModel = new MainPageViewModel();
                window.DataContext = viewModel;

                // Set Application.Current.MainWindow explicitly
                Application.Current.MainWindow = window;

                window.Show();

                HighlightingService.SetEnableHighlighting(element, true);

                var mockShape1 = new Mock<IShape>();
                mockShape1.Setup(s => s.ShapeId).Returns(Guid.NewGuid());
                mockShape1.Setup(s => s.ShapeType).Returns("Circle");
                mockShape1.Setup(s => s.Color).Returns("#FF0000");
                mockShape1.Setup(s => s.StrokeThickness).Returns(2.0);
                mockShape1.Setup(s => s.UserID).Returns(123.45);
                mockShape1.Setup(s => s.LastModifierID).Returns(678.90);
                mockShape1.Setup(s => s.ZIndex).Returns(1);
                mockShape1.Setup(s => s.IsSelected).Returns(false);
                mockShape1.Setup(s => s.GetBounds()).Returns(new Rect(0, 0, 100, 100));
                mockShape1.Setup(s => s.Clone()).Returns(mockShape1.Object);

                var mockShape2 = new Mock<IShape>();
                mockShape2.Setup(s => s.ShapeId).Returns(Guid.NewGuid());
                mockShape2.Setup(s => s.ShapeType).Returns("Rectangle");
                mockShape2.Setup(s => s.Color).Returns("#00FF00");
                mockShape2.Setup(s => s.StrokeThickness).Returns(3.0);
                mockShape2.Setup(s => s.UserID).Returns(543.21);
                mockShape2.Setup(s => s.LastModifierID).Returns(987.65);
                mockShape2.Setup(s => s.ZIndex).Returns(2);
                mockShape2.Setup(s => s.IsSelected).Returns(true);
                mockShape2.Setup(s => s.GetBounds()).Returns(new Rect(10, 10, 150, 150));
                mockShape2.Setup(s => s.Clone()).Returns(mockShape2.Object);

                // First hover
                element.DataContext = mockShape1.Object;
                var mouseEnterEvent1 = new MouseEventArgs(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.MouseEnterEvent
                };
                element.RaiseEvent(mouseEnterEvent1);

                // Wait for the DispatcherTimer to tick
                WaitForDispatcherTimer();

                // Assert first hover
                Assert.IsTrue(viewModel.IsShapeHovered, "IsShapeHovered should be true after first hover.");
                Assert.IsNotNull(viewModel.HoveredShape, "HoveredShape should be set after first hover.");
                Assert.IsNotNull(viewModel.CurrentHoverAdorner, "CurrentHoverAdorner should be set after first hover.");

                var adornerLayer = AdornerLayer.GetAdornerLayer(element);
                Assert.IsNotNull(adornerLayer, "AdornerLayer should not be null.");

                var adorners1 = adornerLayer.GetAdorners(element);
                Assert.IsNotNull(adorners1, "Adorners should not be null after first hover.");
                Assert.AreEqual(1, adorners1.Count(a => a is HoverAdorner), "There should be exactly one HoverAdorner after first hover.");

                // Second hover with a different shape
                element.DataContext = mockShape2.Object;
                var mouseEnterEvent2 = new MouseEventArgs(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.MouseEnterEvent
                };
                element.RaiseEvent(mouseEnterEvent2);

                // Wait for the DispatcherTimer to tick
                WaitForDispatcherTimer();

                // Assert second hover
                Assert.IsTrue(viewModel.IsShapeHovered, "IsShapeHovered should be true after second hover.");
                Assert.IsNotNull(viewModel.HoveredShape, "HoveredShape should be set after second hover.");
                Assert.IsNotNull(viewModel.CurrentHoverAdorner, "CurrentHoverAdorner should be set after second hover.");
                Assert.AreEqual(mockShape2.Object, viewModel.HoveredShape, "HoveredShape should be updated to the second shape.");

                var adorners2 = adornerLayer.GetAdorners(element);
                Assert.IsNotNull(adorners2, "Adorners should not be null after second hover.");
                Assert.AreEqual(1, adorners2.Count(a => a is HoverAdorner), "There should be exactly one HoverAdorner after second hover.");

                // Clean up
                window.Close();
            });
        }

        // Helper method to wait for the DispatcherTimer
        private void WaitForDispatcherTimer()
        {
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.5)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                frame.Continue = false;
            };
            timer.Start();
            Dispatcher.PushFrame(frame);
        }



        /// <summary>
        /// Helper method to access the private HoverTimer attached property using reflection.
        /// </summary>
        /// <param name="obj">The UI element from which to retrieve the HoverTimer.</param>
        /// <returns>The DispatcherTimer instance if found; otherwise, null.</returns>
        private DispatcherTimer GetPrivateHoverTimer(DependencyObject obj)
        {
            var hoverTimerField = typeof(HighlightingService).GetField("HoverTimerProperty", BindingFlags.NonPublic | BindingFlags.Static);
            if (hoverTimerField == null)
                return null;

            var hoverTimerDependencyProperty = hoverTimerField.GetValue(null) as DependencyProperty;
            if (hoverTimerDependencyProperty == null)
                return null;

            return obj.GetValue(hoverTimerDependencyProperty) as DispatcherTimer;
        }
    }
}
