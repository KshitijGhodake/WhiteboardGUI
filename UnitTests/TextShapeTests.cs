using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Windows;
using WhiteboardGUI.Models;

namespace WhiteboardGUI.Tests
{
    [TestClass]
    public class TextShapeTests
    {
        [TestMethod]
        public void TextShape_Clone_ShouldCreateEqualShapeWithIsSelectedFalse()
        {
            // Arrange
            var originalShape = new TextShape
            {
                ShapeId = Guid.NewGuid(),
                UserID = 1.0,
                Color = "Green",
                StrokeThickness = 1.5,
                LastModifierID = 2.0,
                IsSelected = true, // Original shape is selected
                Text = "Hello, World!",
                X = 100,
                Y = 150,
                FontSize = 14,
                ZIndex = 3
            };

            // Act
            var clonedShape = originalShape.Clone() as TextShape;

            // Assert
            Assert.IsNotNull(clonedShape, "Cloned shape should not be null.");
            Assert.AreEqual(originalShape.ShapeId, clonedShape.ShapeId, "ShapeId should be equal.");
            Assert.AreEqual(originalShape.UserID, clonedShape.UserID, "UserID should be equal.");
            Assert.AreEqual(originalShape.Color, clonedShape.Color, "Color should be equal.");
            Assert.AreEqual(originalShape.StrokeThickness, clonedShape.StrokeThickness, "StrokeThickness should be equal.");
            Assert.AreEqual(originalShape.LastModifierID, clonedShape.LastModifierID, "LastModifierID should be equal.");
            Assert.AreEqual(originalShape.Text, clonedShape.Text, "Text should be equal.");
            Assert.AreEqual(originalShape.X, clonedShape.X, "X should be equal.");
            Assert.AreEqual(originalShape.Y, clonedShape.Y, "Y should be equal.");
            Assert.AreEqual(originalShape.FontSize, clonedShape.FontSize, "FontSize should be equal.");
            Assert.AreEqual(originalShape.ZIndex, clonedShape.ZIndex, "ZIndex should be equal.");
            Assert.IsFalse(clonedShape.IsSelected, "Cloned shape's IsSelected should be false.");
        }

        [TestMethod]
        public void TextShape_Clone_ShouldNotAffectOriginalWhenClonedPropertiesAreModified()
        {
            // Arrange
            var originalShape = new TextShape
            {
                ShapeId = Guid.NewGuid(),
                UserID = 1.0,
                Color = "Blue",
                StrokeThickness = 2.0,
                LastModifierID = 2.0,
                IsSelected = true,
                Text = "Original Text",
                X = 50,
                Y = 75,
                FontSize = 12,
                ZIndex = 1
            };

            // Act
            var clonedShape = originalShape.Clone() as TextShape;
            clonedShape.Text = "Modified Text";
            clonedShape.X = 100;
            clonedShape.Y = 150;
            clonedShape.FontSize = 16;
            clonedShape.IsSelected = true; // Even if set to true, original should remain unaffected

            // Assert
            // Original shape should remain unchanged
            Assert.AreEqual("Original Text", originalShape.Text, "Original shape's Text should remain unchanged.");
            Assert.AreEqual(50, originalShape.X, "Original shape's X should remain unchanged.");
            Assert.AreEqual(75, originalShape.Y, "Original shape's Y should remain unchanged.");
            Assert.AreEqual(12, originalShape.FontSize, "Original shape's FontSize should remain unchanged.");
            Assert.IsTrue(originalShape.IsSelected, "Original shape's IsSelected should remain true.");
        }


        [TestMethod]
        public void TextShape_Clone_ShouldBeIndependentOfOriginal()
        {
            // Arrange
            var originalShape = new TextShape
            {
                ShapeId = Guid.NewGuid(),
                UserID = 1.0,
                Color = "Black",
                StrokeThickness = 1.0,
                LastModifierID = 2.0,
                IsSelected = true,
                Text = "Original Text",
                X = 200,
                Y = 300,
                FontSize = 16,
                ZIndex = 4
            };

            // Act
            var clonedShape = originalShape.Clone() as TextShape;
            clonedShape.Text = "Cloned Text";
            clonedShape.X = 250;
            clonedShape.Y = 350;
            clonedShape.FontSize = 18;
            clonedShape.IsSelected = true; // Should not affect original

            // Assert
            // Original shape should remain unchanged
            Assert.AreEqual("Original Text", originalShape.Text, "Original shape's Text should remain unchanged.");
            Assert.AreEqual(200, originalShape.X, "Original shape's X should remain unchanged.");
            Assert.AreEqual(300, originalShape.Y, "Original shape's Y should remain unchanged.");
            Assert.AreEqual(16, originalShape.FontSize, "Original shape's FontSize should remain unchanged.");
            Assert.IsTrue(originalShape.IsSelected, "Original shape's IsSelected should remain true.");

            // Cloned shape should reflect the changes
            Assert.AreEqual("Cloned Text", clonedShape.Text, "Cloned shape's Text should be updated.");
            Assert.AreEqual(250, clonedShape.X, "Cloned shape's X should be updated.");
            Assert.AreEqual(350, clonedShape.Y, "Cloned shape's Y should be updated.");
            Assert.AreEqual(18, clonedShape.FontSize, "Cloned shape's FontSize should be updated.");
            Assert.IsTrue(clonedShape.IsSelected, "Cloned shape's IsSelected should be true.");
        }

    }
}
