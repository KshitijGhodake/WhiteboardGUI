// UnitTests/Test_UndoRedoService.cs

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using WhiteboardGUI.Models;
using WhiteboardGUI.Services;

namespace UnitTests
{
    [TestClass]
    public class Test_UndoRedoService
    {
        private UndoRedoService _undoRedoService;
        private Mock<IShape> _shape1;
        private Mock<IShape> _shape2;
        private Mock<NetworkingService> _networkingServiceMock;

        [TestInitialize]
        public void Setup()
        {
            _undoRedoService = new UndoRedoService();
            _networkingServiceMock = new Mock<NetworkingService>();

            // Setup mock shapes with correct Guid for ShapeId and double for UserID
            _shape1 = new Mock<IShape>();
            _shape2 = new Mock<IShape>();

            // Mock properties for IShape using correct types
            _shape1.Setup(s => s.ShapeId).Returns(Guid.NewGuid());
            _shape1.Setup(s => s.UserID).Returns(1001.0);

            _shape2.Setup(s => s.ShapeId).Returns(Guid.NewGuid());
            _shape2.Setup(s => s.UserID).Returns(1002.0);
        }

        [TestMethod]
        public void UpdateLastDrawing_ShouldAddToUndoList()
        {
            // Act
            _undoRedoService.UpdateLastDrawing(_shape1.Object, _shape2.Object);

            // Assert
            Assert.AreEqual(1, _undoRedoService.UndoList.Count);
            Assert.AreEqual(_shape1.Object, _undoRedoService.UndoList[0].Item1);
            Assert.AreEqual(_shape2.Object, _undoRedoService.UndoList[0].Item2);
        }

        [TestMethod]
        public void UpdateLastDrawing_ShouldLimitUndoListSize()
        {
            // Act
            for (int i = 0; i < 6; i++)
            {
                var shapeMock = new Mock<IShape>();
                shapeMock.Setup(s => s.ShapeId).Returns(Guid.NewGuid());
                shapeMock.Setup(s => s.UserID).Returns(1000.0 + i);
                _undoRedoService.UpdateLastDrawing(shapeMock.Object, null);
            }

            // Assert
            Assert.AreEqual(5, _undoRedoService.UndoList.Count);
        }

        [TestMethod]
        public void Undo_ShouldMoveLastItemToRedoList()
        {
            // Arrange
            _undoRedoService.UpdateLastDrawing(_shape1.Object, _shape2.Object);

            // Act
            _undoRedoService.Undo();

            // Assert
            Assert.AreEqual(0, _undoRedoService.UndoList.Count);
            Assert.AreEqual(1, _undoRedoService.RedoList.Count);
            Assert.AreEqual(_shape2.Object, _undoRedoService.RedoList[0].Item1);
            Assert.AreEqual(_shape1.Object, _undoRedoService.RedoList[0].Item2);
        }

        [TestMethod]
        public void Undo_ShouldNotFailWhenUndoListIsEmpty()
        {
            // Act
            _undoRedoService.Undo();

            // Assert
            Assert.AreEqual(0, _undoRedoService.UndoList.Count);
            Assert.AreEqual(0, _undoRedoService.RedoList.Count);
        }

        [TestMethod]
        public void Redo_ShouldMoveLastItemToUndoList()
        {
            // Arrange
            _undoRedoService.UpdateLastDrawing(_shape1.Object, _shape2.Object);
            _undoRedoService.Undo();

            // Act
            _undoRedoService.Redo();

            // Assert
            Assert.AreEqual(1, _undoRedoService.UndoList.Count);
            Assert.AreEqual(0, _undoRedoService.RedoList.Count);
            Assert.AreEqual(_shape1.Object, _undoRedoService.UndoList[0].Item1);
            Assert.AreEqual(_shape2.Object, _undoRedoService.UndoList[0].Item2);
        }

        [TestMethod]
        public void Redo_ShouldNotFailWhenRedoListIsEmpty()
        {
            // Act
            _undoRedoService.Redo();

            // Assert
            Assert.AreEqual(0, _undoRedoService.UndoList.Count);
            Assert.AreEqual(0, _undoRedoService.RedoList.Count);
        }

        [TestMethod]
        public void RemoveLastModified_ShouldRemoveShapeFromUndoAndRedoLists()
        {
            // Arrange
            _undoRedoService.UpdateLastDrawing(_shape1.Object, _shape2.Object);
            _undoRedoService.UpdateLastDrawing(_shape2.Object, _shape1.Object);

            // Act
            _undoRedoService.RemoveLastModified(_networkingServiceMock.Object, _shape1.Object);

            // Assert
            Assert.IsFalse(_undoRedoService.UndoList.Exists(item => item.Item1.ShapeId == _shape1.Object.ShapeId));
            Assert.IsFalse(_undoRedoService.RedoList.Exists(item => item.Item1.ShapeId == _shape1.Object.ShapeId));
        }

        [TestMethod]
        public void RemoveLastModified_ShouldHandleEmptyLists()
        {
            // Act
            _undoRedoService.RemoveLastModified(_networkingServiceMock.Object, _shape1.Object);

            // Assert
            Assert.AreEqual(0, _undoRedoService.UndoList.Count);
            Assert.AreEqual(0, _undoRedoService.RedoList.Count);
        }

        [TestMethod]
        public void RemoveLastModified_ShouldNotRemoveUnmatchedShape()
        {
            // Arrange
            _undoRedoService.UpdateLastDrawing(_shape1.Object, _shape2.Object);

            var differentShapeMock = new Mock<IShape>();
            differentShapeMock.Setup(s => s.ShapeId).Returns(Guid.NewGuid());
            differentShapeMock.Setup(s => s.UserID).Returns(1003.0);

            // Act
            _undoRedoService.RemoveLastModified(_networkingServiceMock.Object, differentShapeMock.Object);

            // Assert
            Assert.AreEqual(1, _undoRedoService.UndoList.Count);
        }
    }
}
