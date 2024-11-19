using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiteboardGUI.Converters;

namespace WhiteboardGUI.Tests.Converters
{
    [TestClass]
    public class Test_NullToBooleanConverter
    {
        private NullToBooleanConverter _converter;

        [TestInitialize]
        public void Setup()
        {
            _converter = new NullToBooleanConverter();
        }

        [TestMethod]
        public void Convert_ReturnsTrue_WhenValueIsNotNull()
        {
            // Arrange
            object value = new object();
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            var result = _converter.Convert(value, typeof(bool), null, culture);

            // Assert
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void Convert_ReturnsFalse_WhenValueIsNull()
        {
            // Arrange
            object value = null;
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            var result = _converter.Convert(value, typeof(bool), null, culture);

            // Assert
            Assert.IsInstanceOfType(result, typeof(bool));
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            // Arrange
            object value = true;
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            _converter.ConvertBack(value, typeof(object), null, culture);
        }
    }
}
