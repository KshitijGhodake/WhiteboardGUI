using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiteboardGUI.Converters;

namespace WhiteboardGUI.Tests.Converters
{
    [TestClass]
    public class Test_HeightToFontSizeConverter
    {
        private HeightToFontSizeConverter _converter;

        [TestInitialize]
        public void Setup()
        {
            _converter = new HeightToFontSizeConverter();
        }

        [TestMethod]
        public void Convert_ReturnsHalfHeight_WhenHeightIsValid()
        {
            // Arrange
            double height = 20.0;
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            var result = _converter.Convert(height, typeof(double), null, culture);

            // Assert
            Assert.IsInstanceOfType(result, typeof(double));
            Assert.AreEqual(10.0, result);
        }

        [TestMethod]
        public void Convert_ReturnsDefaultFontSize_WhenValueIsNotDouble()
        {
            // Arrange
            string invalidHeight = "InvalidHeight";
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            var result = _converter.Convert(invalidHeight, typeof(double), null, culture);

            // Assert
            Assert.IsInstanceOfType(result, typeof(int));
            Assert.AreEqual(12, result);
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            // Arrange
            double fontSize = 10.0;
            CultureInfo culture = CultureInfo.InvariantCulture;

            // Act
            _converter.ConvertBack(fontSize, typeof(double), null, culture);
        }
    }
}
