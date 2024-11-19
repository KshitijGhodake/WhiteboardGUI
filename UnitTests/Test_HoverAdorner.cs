using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiteboardGUI.Adorners;

namespace WhiteboardGUI.Tests.Adorners
{
    [TestClass]
    public class Test_HoverAdorner
    {
        private static Thread _uiThread;
        private static AutoResetEvent _uiThreadReady = new AutoResetEvent(false);
        private static Dispatcher _uiDispatcher;
        private static Exception _uiException;

        private HoverAdorner _hoverAdorner;
        private FrameworkElement _adornedElement;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _uiThread = new Thread(() =>
            {
                _uiDispatcher = Dispatcher.CurrentDispatcher;
                _uiThreadReady.Set();
                try
                {
                    Dispatcher.Run(); // Start the Dispatcher loop
                }
                catch (Exception ex)
                {
                    _uiException = ex;
                }
            });

            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();
            _uiThreadReady.WaitOne(); // Wait until the Dispatcher is ready
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (_uiDispatcher != null)
            {
                _uiDispatcher.InvokeShutdown(); // Shut down the Dispatcher
            }

            if (_uiThread != null)
            {
                _uiThread.Join(); // Ensure the thread has exited
            }
        }

        [TestInitialize]
        public void Setup()
        {
            RunOnUIThread(() =>
            {
                // Create a simple adorned element
                _adornedElement = new Canvas
                {
                    Width = 500,
                    Height = 500
                };

                // Create a HoverAdorner instance
                _hoverAdorner = new HoverAdorner(
                    _adornedElement,
                    text: "Sample Text",
                    mousePosition: new Point(50, 50),
                    imageSource: null,
                    shapeColor: Colors.Blue);
            });
        }

        [TestMethod]
        public void HoverAdorner_VisualChildrenCount_ReturnsExpectedValue()
        {
            RunOnUIThread(() =>
            {
                PropertyInfo visualChildrenCountProperty = typeof(HoverAdorner).GetProperty("VisualChildrenCount", BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.IsNotNull(visualChildrenCountProperty);

                int visualChildrenCount = (int)visualChildrenCountProperty.GetValue(_hoverAdorner);

                Assert.AreEqual(1, visualChildrenCount);
            });
        }

        [TestMethod]
        public void HoverAdorner_GetVisualChild_ReturnsCorrectVisual()
        {
            RunOnUIThread(() =>
            {
                MethodInfo getVisualChildMethod = typeof(HoverAdorner).GetMethod("GetVisualChild", BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.IsNotNull(getVisualChildMethod);

                var visualChild = getVisualChildMethod.Invoke(_hoverAdorner, new object[] { 0 });

                Assert.IsInstanceOfType(visualChild, typeof(Border));
            });
        }

        [TestMethod]
        public void HoverAdorner_ArrangeOverride_AdjustsWithinBounds()
        {
            RunOnUIThread(() =>
            {
                MethodInfo arrangeOverrideMethod = typeof(HoverAdorner).GetMethod("ArrangeOverride", BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.IsNotNull(arrangeOverrideMethod);

                var arrangeBounds = new Size(300, 300);

                var finalSize = (Size)arrangeOverrideMethod.Invoke(_hoverAdorner, new object[] { arrangeBounds });

                Assert.AreEqual(arrangeBounds, finalSize);
            });
        }

        [TestMethod]
        public void HoverAdorner_MeasureOverride_ReturnsDesiredSize()
        {
            RunOnUIThread(() =>
            {
                MethodInfo measureOverrideMethod = typeof(HoverAdorner).GetMethod("MeasureOverride", BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.IsNotNull(measureOverrideMethod);

                var availableSize = new Size(300, 300);

                var desiredSize = (Size)measureOverrideMethod.Invoke(_hoverAdorner, new object[] { availableSize });

                Assert.IsTrue(desiredSize.Width > 0);
                Assert.IsTrue(desiredSize.Height > 0);
            });
        }

        private void RunOnUIThread(Action action)
        {
            if (_uiDispatcher == null)
                throw new InvalidOperationException("Dispatcher is not initialized.");

            _uiDispatcher.Invoke(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    _uiException = ex;
                }
            });

            if (_uiException != null)
            {
                var exception = _uiException;
                _uiException = null;
                throw exception;
            }
        }
    }
}
