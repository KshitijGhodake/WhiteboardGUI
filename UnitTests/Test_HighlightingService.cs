//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Moq;
//using System;
//using System.Threading;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Threading;
//using WhiteboardGUI.Adorners;
//using WhiteboardGUI.Models;
//using WhiteboardGUI.Services;
//using WhiteboardGUI.ViewModel;

//namespace UnitTests
//{
//    [TestClass]
//    public class Test_HighlightingService
//    {
//        private FrameworkElement _frameworkElement;
//        private MainPageViewModel _viewModel;
//        private DispatcherTimer _mockTimer;
//        private Window _testWindow;
//        private Canvas _canvas;
//        private Thread _uiThread;

//        [TestInitialize]
//        public void Setup()
//        {
//            _uiThread = new Thread(() =>
//            {
//                if (Application.Current == null)
//                {
//                    new Application();
//                }

//                _viewModel = new MainPageViewModel();
//                _mockTimer = new DispatcherTimer();

//                _testWindow = new Window();
//                _canvas = new Canvas();
//                _frameworkElement = new FrameworkElement
//                {
//                    DataContext = _viewModel
//                };
//                _canvas.Children.Add(_frameworkElement);
//                _testWindow.Content = _canvas;
//                _testWindow.Show();

//                Dispatcher.Run();
//            });

//            _uiThread.SetApartmentState(ApartmentState.STA);
//            _uiThread.Start();
//        }

//        [TestCleanup]
//        public void Cleanup()
//        {
//            if (_testWindow != null)
//            {
//                _testWindow.Dispatcher.Invoke(() =>
//                {
//                    _testWindow.Close();
//                    _testWindow = null;
//                    _frameworkElement = null;
//                    _canvas = null;
//                });

//                _testWindow.Dispatcher.InvokeShutdown();
//                _uiThread.Join();
//            }
//        }

//        private void InvokeOnUIThread(Action action)
//        {
//            _testWindow.Dispatcher.Invoke(action);
//        }

//        [TestMethod]
//        public void GetEnableHighlighting_ShouldReturnDefaultValue()
//        {
//            InvokeOnUIThread(() =>
//            {
//                bool result = HighlightingService.GetEnableHighlighting(_frameworkElement);
//                Assert.IsFalse(result);
//            });
//        }

//        [TestMethod]
//        public void SetEnableHighlighting_ShouldEnableHighlighting()
//        {
//            InvokeOnUIThread(() =>
//            {
//                HighlightingService.SetEnableHighlighting(_frameworkElement, true);
//                bool result = HighlightingService.GetEnableHighlighting(_frameworkElement);
//                Assert.IsTrue(result);
//            });
//        }

//        [TestMethod]
//        public void FindParentViewModel_ShouldReturnCorrectViewModel()
//        {
//            InvokeOnUIThread(() =>
//            {
//                var result = HighlightingService.FindParentViewModel(_frameworkElement);
//                Assert.IsNotNull(result);
//                Assert.AreEqual(_viewModel, result);
//            });
//        }

//        [TestMethod]
//        public void FindParentViewModel_ShouldReturnNull_WhenNoViewModelFound()
//        {
//            InvokeOnUIThread(() =>
//            {
//                _frameworkElement.DataContext = null;
//                var result = HighlightingService.FindParentViewModel(_frameworkElement);
//                Assert.IsNull(result);
//            });
//        }

//        [TestMethod]
//        public void Element_MouseEnter_ShouldStartHoverTimer()
//        {
//            InvokeOnUIThread(() =>
//            {
//                HighlightingService.SetEnableHighlighting(_frameworkElement, true);
//                HighlightingService.SetHoverTimer(_frameworkElement, _mockTimer);

//                HighlightingService.Element_MouseEnter(_frameworkElement, null);

//                Assert.IsNotNull(_mockTimer);
//                Assert.IsTrue(_mockTimer.IsEnabled);
//            });
//        }

//        [TestMethod]
//        public void Element_MouseLeave_ShouldStopHoverTimer()
//        {
//            InvokeOnUIThread(() =>
//            {
//                _mockTimer.Start();
//                HighlightingService.SetHoverTimer(_frameworkElement, _mockTimer);

//                HighlightingService.Element_MouseLeave(_frameworkElement, null);

//                Assert.IsNotNull(_mockTimer);
//                Assert.IsFalse(_mockTimer.IsEnabled);
//            });
//        }

//        [TestMethod]
//        public void RemoveHoverAdorner_ShouldClearCurrentHoverAdorner()
//        {
//            InvokeOnUIThread(() =>
//            {
//                var hoverAdorner = new HoverAdorner(_frameworkElement, "", new Point(), null, Colors.Green);
//                _viewModel.CurrentHoverAdorner = hoverAdorner;

//                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(_frameworkElement);
//                if (adornerLayer != null)
//                {
//                    HighlightingService.RemoveHoverAdorner(adornerLayer, _viewModel);
//                    adornerLayer.Remove(hoverAdorner);
//                }

//                Assert.IsNull(_viewModel.CurrentHoverAdorner);
//            });
//        }

//        [TestMethod]
//        public void Element_MouseEnter_ShouldCreateHoverAdorner()
//        {
//            InvokeOnUIThread(() =>
//            {
//                _viewModel.HoveredShape = new Mock<IShape>().Object;
//                _viewModel.IsShapeHovered = true;

//                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(_frameworkElement);
//                Assert.IsNull(adornerLayer); // Typically null in a unit test environment.

//                HighlightingService.Element_MouseEnter(_frameworkElement, null);
//            });
//        }

//        [TestMethod]
//        public void Element_MouseLeave_ShouldRemoveHoverAdorner()
//        {
//            InvokeOnUIThread(() =>
//            {
//                var hoverAdorner = new HoverAdorner(_frameworkElement, "", new Point(), null, Colors.Green);
//                _viewModel.CurrentHoverAdorner = hoverAdorner;

//                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(_frameworkElement);
//                if (adornerLayer != null)
//                {
//                    HighlightingService.Element_MouseLeave(_frameworkElement, null);
//                    adornerLayer.Remove(hoverAdorner);
//                }

//                Assert.IsNull(_viewModel.CurrentHoverAdorner);
//            });
//        }
//    }
//}
