// Test_RenderingService.cs
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WhiteboardGUI.Models;
using WhiteboardGUI.Services;

namespace UnitTests
{
    [TestClass]
    public class Test_RenderingService
    {
        private NetworkingService _realNetworkingService; // Use real instance
        private Mock<UndoRedoService> _mockUndoRedoService;
        private ObservableCollection<IShape> _shapes;
        private RenderingService _renderingService;
        private Mock<IShape> _mockShape;

        [TestInitialize]
        public void Setup()
        {
            // Initialize the real NetworkingService
            _realNetworkingService = new NetworkingService();

            // Initialize the mock UndoRedoService
            _mockUndoRedoService = new Mock<UndoRedoService>();

            // Initialize the Shapes collection
            _shapes = new ObservableCollection<IShape>();

            // Initialize the RenderingService with real NetworkingService and mocked UndoRedoService
            _renderingService = new RenderingService(_realNetworkingService, _mockUndoRedoService.Object, _shapes);

            // Initialize the mock IShape with correct property types
            _mockShape = new Mock<IShape>();
            _mockShape.Setup(s => s.Clone()).Returns(_mockShape.Object);
            _mockShape.SetupProperty<Guid>(s => s.ShapeId, Guid.NewGuid());
            _mockShape.SetupProperty<double>(s => s.UserID, 0.0); // UserID is double
            _mockShape.SetupProperty<bool>(s => s.IsSelected, false);
            _mockShape.SetupProperty<double>(s => s.LastModifierID, 0.0); // LastModifierID is double

            // Ensure that _synchronizedShapes is initialized
            _realNetworkingService._synchronizedShapes = new List<IShape>();
        }

        [TestMethod]
        public void RenderShape_CreateCommand_AddsShape()
        {
            // Arrange
            string command = "CREATE";

            // Act
            _renderingService.RenderShape(_mockShape.Object, command);

            // Assert
            // Verify that the shape is added to both Shapes and _synchronizedShapes
            Assert.IsTrue(_shapes.Contains(_mockShape.Object), "Shape should be added to Shapes collection.");
            Assert.IsTrue(_realNetworkingService._synchronizedShapes.Contains(_mockShape.Object), "Shape should be added to _synchronizedShapes.");

            // Verify that UndoRedoService.UpdateLastDrawing was called with (newShape, null)
            _mockUndoRedoService.Verify(ur => ur.UpdateLastDrawing(_mockShape.Object, null), Times.Once, "UndoRedoService.UpdateLastDrawing should be called once with (newShape, null).");

            // Since we cannot mock NetworkingService's BroadcastShapeData, we cannot verify its call.
            // However, we can check the state after the method execution.
            Assert.IsFalse(_mockShape.Object.IsSelected, "Shape.IsSelected should be false after creation.");
        }

        [TestMethod]
        public void RenderShape_IndexCommand_UpdatesAllSynchronizedShapes()
        {
            // Arrange
            string command = "INDEX_UPDATE";

            // Add initial shapes
            var initialShape = new Mock<IShape>().Object;
            _shapes.Add(initialShape);
            _realNetworkingService._synchronizedShapes.Add(initialShape);

            // Act
            _renderingService.RenderShape(_mockShape.Object, command);

            // Assert
            // Verify that _synchronizedShapes now contains all shapes from Shapes
            Assert.AreEqual(_shapes.Count, _realNetworkingService._synchronizedShapes.Count, "SynchronizedShapes should match Shapes count.");
            foreach (var shape in _shapes)
            {
                Assert.IsTrue(_realNetworkingService._synchronizedShapes.Contains(shape), $"SynchronizedShapes should contain shape with ID: {shape.ShapeId}");
            }

            // Verify that _synchronizedShapes includes the new shape if applicable
            // Depending on the implementation, INDEX might not add the new shape
            // Adjust the assertion based on actual behavior
            // Here, assuming it adds the new shape
            Assert.IsTrue(_shapes.Contains(_mockShape.Object), "Shapes should contain the new shape.");
            Assert.IsFalse(_mockShape.Object.IsSelected, "Shape.IsSelected should be false after index update.");
        }

        [TestMethod]
        public void RenderShape_DownloadCommand_AddsShape()
        {
            // Arrange
            string command = "DOWNLOAD";

            // Act
            _renderingService.RenderShape(_mockShape.Object, command);

            // Assert
            // Verify that the shape is added to both Shapes and _synchronizedShapes
            Assert.IsTrue(_shapes.Contains(_mockShape.Object), "Shape should be added to Shapes collection.");
            Assert.IsTrue(_realNetworkingService._synchronizedShapes.Contains(_mockShape.Object), "Shape should be added to _synchronizedShapes.");
            Assert.IsFalse(_mockShape.Object.IsSelected, "Shape.IsSelected should be false after download.");
        }

        [TestMethod]
        public void RenderShape_ModifyCommand_UpdatesShape()
        {
            // Arrange
            string command = "MODIFY";

            // Setup synchronized shapes with a shape that matches the new shape's ID and UserID
            var existingShapeMock = new Mock<IShape>();
            existingShapeMock.Setup(s => s.ShapeId).Returns(_mockShape.Object.ShapeId);
            existingShapeMock.Setup(s => s.UserID).Returns(_mockShape.Object.UserID);
            var existingShape = existingShapeMock.Object;
            _realNetworkingService._synchronizedShapes.Add(existingShape);

            // Act
            _renderingService.RenderShape(_mockShape.Object, command);

            // Assert
            // Verify that the previous shape was removed and the new shape was added
            Assert.IsFalse(_realNetworkingService._synchronizedShapes.Contains(existingShape), "Existing shape should be removed from _synchronizedShapes.");
            Assert.IsTrue(_realNetworkingService._synchronizedShapes.Contains(_mockShape.Object), "New shape should be added to _synchronizedShapes.");

            // Verify that UndoRedoService.UpdateLastDrawing was called with (newShape, existingShape)
            _mockUndoRedoService.Verify(ur => ur.UpdateLastDrawing(_mockShape.Object, existingShape), Times.Once, "UndoRedoService.UpdateLastDrawing should be called once with (newShape, existingShape).");

            Assert.IsFalse(_mockShape.Object.IsSelected, "Shape.IsSelected should be false after modification.");
        }

        [TestMethod]
        public void RenderShape_ClearCommand_ClearsAll()
        {
            // Arrange
            string command = "CLEAR";

            // Add shapes and undo/redo lists
            _shapes.Add(_mockShape.Object);
            _realNetworkingService._synchronizedShapes.Add(_mockShape.Object);
            _mockUndoRedoService.Object.UndoList.Add((null, null));
            _mockUndoRedoService.Object.RedoList.Add((null, null));

            // Act
            _renderingService.RenderShape(_mockShape.Object, command);

            // Assert
            Assert.AreEqual(0, _shapes.Count, "Shapes collection should be cleared.");
            Assert.AreEqual(0, _realNetworkingService._synchronizedShapes.Count, "_synchronizedShapes should be cleared.");
            Assert.AreEqual(0, _mockUndoRedoService.Object.UndoList.Count, "UndoList should be cleared.");
            Assert.AreEqual(0, _mockUndoRedoService.Object.RedoList.Count, "RedoList should be cleared.");

            // Since BroadcastShapeData cannot be mocked, verify that no shapes remain
            Assert.AreEqual(0, _realNetworkingService._synchronizedShapes.Count, "_synchronizedShapes should have no shapes after clear.");
        }

        [TestMethod]
        public void RenderShape_UndoCommand_RevertsLastAction_Delete()
        {
            // Arrange
            string command = "UNDO";

            var prevShape = new Mock<IShape>().Object;
            var currentShape = _mockShape.Object;

            // Add to UndoList: (prevShape, null) signifies deletion
            _mockUndoRedoService.Object.UndoList.Add((prevShape, null));

            // Add currentShape to Shapes and _synchronizedShapes
            _shapes.Add(currentShape);
            _realNetworkingService._synchronizedShapes.Add(currentShape);

            // Act
            _renderingService.RenderShape(currentShape, command);

            // Assert
            // Shape should be removed from Shapes and _synchronizedShapes
            Assert.IsFalse(_shapes.Contains(currentShape), "Shape should be removed from Shapes after undoing delete.");
            Assert.IsFalse(_realNetworkingService._synchronizedShapes.Contains(currentShape), "Shape should be removed from _synchronizedShapes after undoing delete.");

            // Verify that UndoRedoService.Undo was called
            _mockUndoRedoService.Verify(ur => ur.Undo(), Times.Once, "Undo method should be called once.");

            // Verify that UpdateLastDrawing was called with (null, currentShape)
            _mockUndoRedoService.Verify(ur => ur.UpdateLastDrawing(null, currentShape), Times.Once, "UndoRedoService.UpdateLastDrawing should be called with (null, currentShape).");
        }

        [TestMethod]
        public void RenderShape_UndoCommand_RevertsLastAction_Create()
        {
            // Arrange
            string command = "UNDO";

            IShape prevShape = null;
            var currentShape = _mockShape.Object;

            // Add to UndoList: (null, currentShape) signifies creation
            _mockUndoRedoService.Object.UndoList.Add((prevShape, currentShape));

            // Act
            _renderingService.RenderShape(currentShape, command);

            // Assert
            // Shape should be present in Shapes and _synchronizedShapes
            Assert.IsTrue(_shapes.Contains(currentShape), "Shape should be present in Shapes after undoing create.");
            Assert.IsTrue(_realNetworkingService._synchronizedShapes.Contains(currentShape), "Shape should be present in _synchronizedShapes after undoing create.");

            // Verify that UndoRedoService.Undo was called
            _mockUndoRedoService.Verify(ur => ur.Undo(), Times.Once, "Undo method should be called once.");

            // Verify that UpdateLastDrawing was called with (currentShape, null)
            _mockUndoRedoService.Verify(ur => ur.UpdateLastDrawing(currentShape, prevShape), Times.Once, "UndoRedoService.UpdateLastDrawing should be called with (currentShape, null).");
        }

        [TestMethod]
        public void RenderShape_UndoCommand_RevertsLastAction_Modify()
        {
            // Arrange
            string command = "UNDO";

            var prevShape = new Mock<IShape>().Object;
            var currentShape = _mockShape.Object;

            // Add to UndoList: (prevShape, currentShape) signifies modification
            _mockUndoRedoService.Object.UndoList.Add((prevShape, currentShape));

            // Add currentShape to Shapes and _synchronizedShapes
            _shapes.Add(currentShape);
            _realNetworkingService._synchronizedShapes.Add(currentShape);

            // Act
            _renderingService.RenderShape(currentShape, command);

            // Assert
            // Shape should still be present in Shapes and _synchronizedShapes after undoing modify
            Assert.IsTrue(_shapes.Contains(currentShape), "Shape should remain in Shapes after undoing modify.");
            Assert.IsTrue(_realNetworkingService._synchronizedShapes.Contains(currentShape), "Shape should remain in _synchronizedShapes after undoing modify.");

            // Verify that UndoRedoService.Undo was called
            _mockUndoRedoService.Verify(ur => ur.Undo(), Times.Once, "Undo method should be called once.");

            // Verify that UpdateLastDrawing was called with (currentShape, prevShape)
            _mockUndoRedoService.Verify(ur => ur.UpdateLastDrawing(currentShape, prevShape), Times.Once, "UndoRedoService.UpdateLastDrawing should be called with (currentShape, prevShape).");
        }

        [TestMethod]
        public void RenderShape_RedoCommand_Redes()
        {
            // Arrange
            string command = "REDO";

            var prevShape = new Mock<IShape>().Object;
            var currentShape = _mockShape.Object;

            // Add to RedoList: (prevShape, currentShape) signifies a redo action
            _mockUndoRedoService.Object.RedoList.Add((prevShape, currentShape));

            // Add currentShape to Shapes and _synchronizedShapes
            _shapes.Add(currentShape);
            _realNetworkingService._synchronizedShapes.Add(currentShape);

            // Act
            _renderingService.RenderShape(currentShape, command);

            // Assert
            // Verify that UndoRedoService.Redo was called
            _mockUndoRedoService.Verify(ur => ur.Redo(), Times.Once, "Redo method should be called once.");

            // Since BroadcastShapeData cannot be mocked, verify the state
            Assert.IsTrue(_shapes.Contains(currentShape), "Shape should remain in Shapes after redo.");
            Assert.IsTrue(_realNetworkingService._synchronizedShapes.Contains(currentShape), "Shape should remain in _synchronizedShapes after redo.");
        }

        [TestMethod]
        public void RenderShape_DeleteCommand_RemovesShape()
        {
            // Arrange
            string command = "DELETE";

            // Add shape to Shapes and _synchronizedShapes
            _shapes.Add(_mockShape.Object);
            _realNetworkingService._synchronizedShapes.Add(_mockShape.Object);

            // Act
            _renderingService.RenderShape(_mockShape.Object, command);

            // Assert
            // Shape should be removed from Shapes and _synchronizedShapes
            Assert.IsFalse(_shapes.Contains(_mockShape.Object), "Shape should be removed from Shapes after deletion.");
            Assert.IsFalse(_realNetworkingService._synchronizedShapes.Contains(_mockShape.Object), "Shape should be removed from _synchronizedShapes after deletion.");

            // Verify that UndoRedoService.UpdateLastDrawing was called with (null, currentShape)
            _mockUndoRedoService.Verify(ur => ur.UpdateLastDrawing(null, _mockShape.Object), Times.Once, "UndoRedoService.UpdateLastDrawing should be called with (null, currentShape).");
        }

        [TestMethod]
        public void UpdateSynchronizedShapes_PrivateMethod_UpdatesCorrectly()
        {
            // Arrange
            var shapeToUpdate = _mockShape.Object;
            var existingShapeMock = new Mock<IShape>();
            existingShapeMock.Setup(s => s.ShapeId).Returns(shapeToUpdate.ShapeId);
            existingShapeMock.Setup(s => s.UserID).Returns(shapeToUpdate.UserID);
            var existingShape = existingShapeMock.Object;
            _realNetworkingService._synchronizedShapes.Add(existingShape);

            // Act
            // Invoke the private method UpdateSynchronizedShapes using reflection
            var method = typeof(RenderingService).GetMethod("UpdateSynchronizedShapes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = method.Invoke(_renderingService, new object[] { shapeToUpdate }) as IShape;

            // Assert
            Assert.AreEqual(existingShape, result, "UpdateSynchronizedShapes should return the previous shape.");
            Assert.IsTrue(_realNetworkingService._synchronizedShapes.Contains(shapeToUpdate), "SynchronizedShapes should contain the updated shape.");
            Assert.IsFalse(_realNetworkingService._synchronizedShapes.Contains(existingShape), "SynchronizedShapes should no longer contain the previous shape.");
        }

        [TestMethod]
        public void UpdateAllSynchronizedShapes_PrivateMethod_UpdatesAllShapes()
        {
            // Arrange
            var shape1 = new Mock<IShape>().Object;
            var shape2 = new Mock<IShape>().Object;

            _shapes.Add(shape1);
            _shapes.Add(shape2);
            _realNetworkingService._synchronizedShapes.Add(shape1); // Initially contains shape1

            // Act
            // Invoke the private method UpdateAllSynchronizedShapes using reflection
            var method = typeof(RenderingService).GetMethod("UpdateAllSynchronizedShapes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(_renderingService, null);

            // Assert
            var synchronizedShapes = _realNetworkingService._synchronizedShapes;
            Assert.AreEqual(_shapes.Count, synchronizedShapes.Count, "SynchronizedShapes should match Shapes count after update.");
            foreach (var shape in _shapes)
            {
                Assert.IsTrue(synchronizedShapes.Contains(shape), $"SynchronizedShapes should contain shape with ID: {shape.ShapeId}");
            }
        }

        [TestMethod]
        public void Constructor_InitializesCorrectly()
        {
            // Act & Assert
            // Since we cannot access private fields directly, use reflection to verify
            var networkingServiceField = typeof(RenderingService).GetField("_networkingService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var undoRedoServiceField = typeof(RenderingService).GetField("_undoRedoService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var shapesField = typeof(RenderingService).GetField("Shapes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(networkingServiceField.GetValue(_renderingService), "_networkingService should be initialized.");
            Assert.AreEqual(_mockUndoRedoService.Object, networkingServiceField.GetValue(_renderingService), "_networkingService should match the injected instance.");
            Assert.AreEqual(_mockUndoRedoService.Object, undoRedoServiceField.GetValue(_renderingService), "_undoRedoService should match the injected mock.");
            Assert.AreEqual(_shapes, shapesField.GetValue(_renderingService), "Shapes collection should match the injected collection.");
        }

        /// <summary>
        /// Helper method to access private fields via reflection.
        /// </summary>
        private T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (T)field.GetValue(obj);
        }
    }
}
