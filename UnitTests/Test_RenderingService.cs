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
        private NetworkingService _realNetworkingService;
        private Mock<UndoRedoService> _mockUndoRedoService;
        private ObservableCollection<IShape> _shapes;
        private RenderingService _renderingService;
        private IShape _realShape;

        [TestInitialize]
        public void Setup()
        {
            _realNetworkingService = new NetworkingService();
            _mockUndoRedoService = new Mock<UndoRedoService>();
            _shapes = new ObservableCollection<IShape>();

            _renderingService = new RenderingService(_realNetworkingService, _mockUndoRedoService.Object, _shapes);

            _realShape = new CircleShape
            {
                ShapeId = Guid.NewGuid(),
                UserID = 0.0,
                IsSelected = false,
                LastModifierID = 0.0
            };

            _realNetworkingService._synchronizedShapes = new List<IShape>();
        }

        [TestMethod]
        public void RenderShape_CreateCommand_AddsShape()
        {
            // Arrange
            string command = "CREATE";

            // Act
            _renderingService.RenderShape(_realShape, command);

            // Assert
            bool shapeExists = _realNetworkingService._synchronizedShapes.Any(s => s.ShapeId == _realShape.ShapeId);
            Assert.IsTrue(shapeExists, "Shape with the same ShapeId should be added to _synchronizedShapes.");
            Assert.IsFalse(_realShape.IsSelected, "Shape.IsSelected should be false after creation.");
        }

        [TestMethod]
        public void RenderShape_IndexCommand_UpdatesAllSynchronizedShapes()
        {
            // Arrange
            string command = "INDEX_UPDATE";

            _shapes.Add(_realShape);
            _realNetworkingService._synchronizedShapes.Add(_realShape);

            // Act
            _renderingService.RenderShape(_realShape, command);

            // Assert
            Assert.AreEqual(_shapes.Count, _realNetworkingService._synchronizedShapes.Count);
            bool shapeExists = _realNetworkingService._synchronizedShapes.Any(s => s.ShapeId == _realShape.ShapeId);
            Assert.IsTrue(shapeExists, "SynchronizedShapes should contain a shape with the same ShapeId.");
        }

        [TestMethod]
        public void RenderShape_DownloadCommand_AddsShape()
        {
            // Arrange
            string command = "DOWNLOAD";

            // Act
            _renderingService.RenderShape(_realShape, command);

            // Assert
            bool shapeExists = _realNetworkingService._synchronizedShapes.Any(s => s.ShapeId == _realShape.ShapeId);
            Assert.IsTrue(shapeExists, "Shape with the same ShapeId should be added to _synchronizedShapes.");
            Assert.IsFalse(_realShape.IsSelected, "Shape.IsSelected should be false after download.");
        }

        [TestMethod]
        public void RenderShape_ModifyCommand_UpdatesShape()
        {
            // Arrange
            string command = "MODIFY";

            var existingShape = new CircleShape
            {
                ShapeId = _realShape.ShapeId,
                UserID = _realShape.UserID
            };
            _realNetworkingService._synchronizedShapes.Add(existingShape);

            // Act
            _renderingService.RenderShape(_realShape, command);

            // Assert
            bool shapeExists = _realNetworkingService._synchronizedShapes.Any(s => s.ShapeId == _realShape.ShapeId);
            Assert.IsTrue(shapeExists, "SynchronizedShapes should contain the updated shape with the same ShapeId.");
        }

        [TestMethod]
        public void RenderShape_ClearCommand_ClearsAll()
        {
            // Arrange
            string command = "CLEAR";

            _shapes.Add(_realShape);
            _realNetworkingService._synchronizedShapes.Add(_realShape);

            // Act
            _renderingService.RenderShape(_realShape, command);

            // Assert
            Assert.AreEqual(0, _shapes.Count, "Shapes collection should be cleared.");
            Assert.AreEqual(0, _realNetworkingService._synchronizedShapes.Count, "_synchronizedShapes should be cleared.");
        }

        [TestMethod]
        public void RenderShape_DeleteCommand_RemovesShape()
        {
            // Arrange
            string command = "DELETE";

            _shapes.Add(_realShape);
            _realNetworkingService._synchronizedShapes.Add(_realShape);

            // Act
            _renderingService.RenderShape(_realShape, command);

            // Assert
            bool shapeExists = _realNetworkingService._synchronizedShapes.Any(s => s.ShapeId == _realShape.ShapeId);
            Assert.IsFalse(shapeExists, "Shape with the same ShapeId should be removed from _synchronizedShapes.");
        }

        [TestMethod]
        public void RenderShape_UndoCommand_DeletesPreviousShape()
        {
            // Arrange
            string command = "UNDO";

            var prevShape = new CircleShape { ShapeId = _realShape.ShapeId };
            _mockUndoRedoService.Object.UndoList.Add((prevShape, null));
            _shapes.Add(prevShape);
            _realNetworkingService._synchronizedShapes.Add(prevShape);

            // Act
            _renderingService.RenderShape(_realShape, command);

            // Assert
            bool shapeExists = _realNetworkingService._synchronizedShapes.Any(s => s.ShapeId == prevShape.ShapeId);
            Assert.IsFalse(shapeExists, "Previous shape should be removed from _synchronizedShapes.");
        }

        [TestMethod]
        public void RenderShape_UndoCommand_ModifiesCurrentShape()
        {
            // Arrange
            string command = "UNDO";

            var prevShape = new CircleShape { ShapeId = _realShape.ShapeId };
            var currentShape = new CircleShape { ShapeId = _realShape.ShapeId };
            _mockUndoRedoService.Object.UndoList.Add((prevShape, currentShape));
            _shapes.Add(prevShape);
            _realNetworkingService._synchronizedShapes.Add(prevShape);

            // Act
            _renderingService.RenderShape(_realShape, command);

            // Assert
            bool shapeExists = _realNetworkingService._synchronizedShapes.Any(s => s.ShapeId == currentShape.ShapeId);
            Assert.IsTrue(shapeExists, "Current shape should be updated in _synchronizedShapes.");
        }

        [TestMethod]
        public void RenderShape_RedoCommand_DeletesPreviousShape()
        {
            // Arrange
            string command = "REDO";

            var prevShape = new CircleShape { ShapeId = _realShape.ShapeId };
            _mockUndoRedoService.Object.RedoList.Add((prevShape, null));
            _shapes.Add(prevShape);
            _realNetworkingService._synchronizedShapes.Add(prevShape);

            // Act
            _renderingService.RenderShape(_realShape, command);

            // Assert
            bool shapeExists = _realNetworkingService._synchronizedShapes.Any(s => s.ShapeId == prevShape.ShapeId);
            Assert.IsFalse(shapeExists, "Previous shape should be removed from _synchronizedShapes.");
        }

        [TestMethod]
        public void RenderShape_RedoCommand_CreatesNewShape()
        {
            // Arrange
            string command = "REDO";

            var newShape = new CircleShape { ShapeId = Guid.NewGuid() };
            _mockUndoRedoService.Object.RedoList.Add((null, newShape));

            // Act
            _renderingService.RenderShape(_realShape, command);

            // Assert
            bool shapeExists = _realNetworkingService._synchronizedShapes.Any(s => s.ShapeId == newShape.ShapeId);
            Assert.IsTrue(shapeExists, "New shape should be added to _synchronizedShapes.");
        }

        [TestMethod]
        public void RenderShape_RedoCommand_ModifiesCurrentShape()
        {
            // Arrange
            string command = "REDO";

            var prevShape = new CircleShape { ShapeId = _realShape.ShapeId };
            var currentShape = new CircleShape { ShapeId = _realShape.ShapeId };
            _mockUndoRedoService.Object.RedoList.Add((prevShape, currentShape));
            _shapes.Add(prevShape);
            _realNetworkingService._synchronizedShapes.Add(prevShape);

            // Act
            _renderingService.RenderShape(_realShape, command);

            // Assert
            bool shapeExists = _realNetworkingService._synchronizedShapes.Any(s => s.ShapeId == currentShape.ShapeId);
            Assert.IsTrue(shapeExists, "Current shape should be updated in _synchronizedShapes.");
        }
    }
}
