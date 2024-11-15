using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiteboardGUI.Services;
using WhiteboardGUI.Models;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace WhiteboardGUI.Tests.Services
{
    [TestClass]
    public class SerializationServiceTests
    {
        #region Helper Methods

        private CircleShape GetSampleCircle()
        {
            return new CircleShape
            {
                ShapeType = "Circle",
                Radius = 50,
                Center = new Point { X = 100, Y = 100 },
                Color = "Red"
            };
        }

        private LineShape GetSampleLine()
        {
            return new LineShape
            {
                ShapeType = "Line",
                StartPoint = new Point { X = 0, Y = 0 },
                EndPoint = new Point { X = 200, Y = 200 },
                Color = "Blue",
                Thickness = 2
            };
        }

        private ScribbleShape GetSampleScribble()
        {
            return new ScribbleShape
            {
                ShapeType = "Scribble",
                Points = new List<Point>
                {
                    new Point { X = 10, Y = 10 },
                    new Point { X = 20, Y = 20 },
                    new Point { X = 30, Y = 30 }
                },
                Color = "Green"
            };
        }

        private TextShape GetSampleTextShape()
        {
            return new TextShape
            {
                ShapeType = "TextShape",
                Text = "Hello World",
                Position = new Point { X = 50, Y = 50 },
                FontSize = 12,
                Color = "Black"
            };
        }

        private SnapShot GetSampleSnapShot()
        {
            return new SnapShot
            {
                Shapes = new ObservableCollection<IShape>
                {
                    GetSampleCircle(),
                    GetSampleLine(),
                    GetSampleScribble(),
                    GetSampleTextShape()
                },
                Timestamp = DateTime.Now
            };
        }

        #endregion

        #region SerializeShape Tests

        [TestMethod]
        public void SerializeShape_CircleShape_ReturnsValidJson()
        {
            // Arrange
            IShape circle = GetSampleCircle();

            // Act
            string json = SerializationService.SerializeShape(circle);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(json));
            Assert.IsTrue(json.Contains("\"ShapeType\":\"Circle\""));
            Assert.IsTrue(json.Contains("\"Radius\":50"));
        }

        [TestMethod]
        public void SerializeShape_LineShape_ReturnsValidJson()
        {
            // Arrange
            IShape line = GetSampleLine();

            // Act
            string json = SerializationService.SerializeShape(line);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(json));
            Assert.IsTrue(json.Contains("\"ShapeType\":\"Line\""));
            Assert.IsTrue(json.Contains("\"Thickness\":2"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SerializeShape_NullShape_ThrowsArgumentNullException()
        {
            // Arrange
            IShape shape = null;

            // Act
            SerializationService.SerializeShape(shape);

            // Assert is handled by ExpectedException
        }

        #endregion

        #region DeserializeShape Tests

        [TestMethod]
        public void DeserializeShape_ValidCircleJson_ReturnsCircleShape()
        {
            // Arrange
            IShape expected = GetSampleCircle();
            string json = SerializationService.SerializeShape(expected);

            // Act
            IShape actual = SerializationService.DeserializeShape(json);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(CircleShape));
            CircleShape actualCircle = actual as CircleShape;
            Assert.AreEqual(expected.Radius, actualCircle.Radius);
            Assert.AreEqual(expected.Center.X, actualCircle.Center.X);
            Assert.AreEqual(expected.Center.Y, actualCircle.Center.Y);
            Assert.AreEqual(expected.Color, actualCircle.Color);
        }

        [TestMethod]
        public void DeserializeShape_ValidLineJson_ReturnsLineShape()
        {
            // Arrange
            IShape expected = GetSampleLine();
            string json = SerializationService.SerializeShape(expected);

            // Act
            IShape actual = SerializationService.DeserializeShape(json);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(LineShape));
            LineShape actualLine = actual as LineShape;
            Assert.AreEqual(expected.Thickness, actualLine.Thickness);
            Assert.AreEqual(expected.StartPoint.X, actualLine.StartPoint.X);
            Assert.AreEqual(expected.StartPoint.Y, actualLine.StartPoint.Y);
            Assert.AreEqual(expected.EndPoint.X, actualLine.EndPoint.X);
            Assert.AreEqual(expected.EndPoint.Y, actualLine.EndPoint.Y);
            Assert.AreEqual(expected.Color, actualLine.Color);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void DeserializeShape_UnsupportedShapeType_ThrowsNotSupportedException()
        {
            // Arrange
            var unsupportedShape = new
            {
                ShapeType = "UnsupportedShape",
                SomeProperty = "Value"
            };
            string json = JsonConvert.SerializeObject(unsupportedShape);

            // Act
            SerializationService.DeserializeShape(json);

            // Assert is handled by ExpectedException
        }

        [TestMethod]
        [ExpectedException(typeof(JsonException))]
        public void DeserializeShape_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            string invalidJson = "{ invalid json }";

            // Act
            SerializationService.DeserializeShape(invalidJson);

            // Assert is handled by ExpectedException
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeserializeShape_NullJson_ThrowsArgumentNullException()
        {
            // Arrange
            string json = null;

            // Act
            SerializationService.DeserializeShape(json);

            // Assert is handled by ExpectedException
        }

        #endregion

        #region SerializeSnapShot Tests

        [TestMethod]
        public void SerializeSnapShot_ValidSnapShot_ReturnsValidJson()
        {
            // Arrange
            SnapShot snapShot = GetSampleSnapShot();

            // Act
            string json = SerializationService.SerializeSnapShot(snapShot);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(json));
            Assert.IsTrue(json.Contains("\"Timestamp\""));
            Assert.IsTrue(json.Contains("\"ShapeType\":\"Circle\""));
            Assert.IsTrue(json.Contains("\"ShapeType\":\"Line\""));
            Assert.IsTrue(json.Contains("\"ShapeType\":\"Scribble\""));
            Assert.IsTrue(json.Contains("\"ShapeType\":\"TextShape\""));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SerializeSnapShot_NullSnapShot_ThrowsArgumentNullException()
        {
            // Arrange
            SnapShot snapShot = null;

            // Act
            SerializationService.SerializeSnapShot(snapShot);

            // Assert is handled by ExpectedException
        }

        #endregion

        #region DeserializeSnapShot Tests

        [TestMethod]
        public void DeserializeSnapShot_ValidJson_ReturnsSnapShot()
        {
            // Arrange
            SnapShot expected = GetSampleSnapShot();
            string json = SerializationService.SerializeSnapShot(expected);

            // Act
            SnapShot actual = SerializationService.DeserializeSnapShot(json);

            // Assert
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Timestamp.ToString(), actual.Timestamp.ToString());
            Assert.AreEqual(expected.Shapes.Count, actual.Shapes.Count);
            for (int i = 0; i < expected.Shapes.Count; i++)
            {
                Assert.AreEqual(expected.Shapes[i].ShapeType, actual.Shapes[i].ShapeType);
                // Further property comparisons can be added based on shape type
            }
        }

        [TestMethod]
        [ExpectedException(typeof(JsonException))]
        public void DeserializeSnapShot_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            string invalidJson = "{ invalid json }";

            // Act
            SerializationService.DeserializeSnapShot(invalidJson);

            // Assert is handled by ExpectedException
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeserializeSnapShot_NullJson_ThrowsArgumentNullException()
        {
            // Arrange
            string json = null;

            // Act
            SerializationService.DeserializeSnapShot(json);

            // Assert is handled by ExpectedException
        }

        #endregion

        #region SerializeShapes Tests

        [TestMethod]
        public void SerializeShapes_ValidCollection_ReturnsValidJson()
        {
            // Arrange
            ObservableCollection<IShape> shapes = new ObservableCollection<IShape>
            {
                GetSampleCircle(),
                GetSampleLine(),
                GetSampleScribble(),
                GetSampleTextShape()
            };

            // Act
            string json = SerializationService.SerializeShapes(shapes);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(json));
            Assert.IsTrue(json.Contains("\"ShapeType\":\"Circle\""));
            Assert.IsTrue(json.Contains("\"ShapeType\":\"Line\""));
            Assert.IsTrue(json.Contains("\"ShapeType\":\"Scribble\""));
            Assert.IsTrue(json.Contains("\"ShapeType\":\"TextShape\""));
        }

        [TestMethod]
        public void SerializeShapes_EmptyCollection_ReturnsEmptyJsonArray()
        {
            // Arrange
            ObservableCollection<IShape> shapes = new ObservableCollection<IShape>();

            // Act
            string json = SerializationService.SerializeShapes(shapes);

            // Assert
            Assert.AreEqual("[]", json);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SerializeShapes_NullCollection_ThrowsArgumentNullException()
        {
            // Arrange
            ObservableCollection<IShape> shapes = null;

            // Act
            SerializationService.SerializeShapes(shapes);

            // Assert is handled by ExpectedException
        }

        #endregion

        #region DeserializeShapes Tests

        [TestMethod]
        public void DeserializeShapes_ValidJson_ReturnsShapesCollection()
        {
            // Arrange
            ObservableCollection<IShape> expectedShapes = new ObservableCollection<IShape>
            {
                GetSampleCircle(),
                GetSampleLine(),
                GetSampleScribble(),
                GetSampleTextShape()
            };
            string json = SerializationService.SerializeShapes(expectedShapes);

            // Act
            ObservableCollection<IShape> actualShapes = SerializationService.DeserializeShapes(json);

            // Assert
            Assert.IsNotNull(actualShapes);
            Assert.AreEqual(expectedShapes.Count, actualShapes.Count);
            for (int i = 0; i < expectedShapes.Count; i++)
            {
                Assert.AreEqual(expectedShapes[i].ShapeType, actualShapes[i].ShapeType);
                // Further property comparisons can be added based on shape type
            }
        }

        [TestMethod]
        public void DeserializeShapes_EmptyJsonArray_ReturnsEmptyCollection()
        {
            // Arrange
            string json = "[]";

            // Act
            ObservableCollection<IShape> shapes = SerializationService.DeserializeShapes(json);

            // Assert
            Assert.IsNotNull(shapes);
            Assert.AreEqual(0, shapes.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void DeserializeShapes_UnsupportedShapeType_ThrowsNotSupportedException()
        {
            // Arrange
            var shapesList = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "ShapeType", "UnsupportedShape" },
                    { "SomeProperty", "Value" }
                }
            };
            string json = JsonConvert.SerializeObject(shapesList);

            // Act
            SerializationService.DeserializeShapes(json);

            // Assert is handled by ExpectedException
        }

        [TestMethod]
        [ExpectedException(typeof(JsonException))]
        public void DeserializeShapes_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            string invalidJson = "{ invalid json }";

            // Act
            SerializationService.DeserializeShapes(invalidJson);

            // Assert is handled by ExpectedException
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeserializeShapes_NullJson_ThrowsArgumentNullException()
        {
            // Arrange
            string json = null;

            // Act
            SerializationService.DeserializeShapes(json);

            // Assert is handled by ExpectedException
        }
    }
}
