using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhiteboardGUI.Models;

namespace WhiteboardGUI.Services
{
    public class RenderingService
    {
        NetworkingService _networkingService;
        UndoRedoService _undoRedoService;
        ObservableCollection<IShape> Shapes;
        public RenderingService(NetworkingService networkingService, UndoRedoService undoRedoService,ObservableCollection<IShape> shapes)
        {
            _networkingService = networkingService;
            _undoRedoService = undoRedoService;
            Shapes = shapes;
        }
        private IShape UpdateSynchronizedShapes(IShape shape)
        {
            var prevShape = _networkingService._synchronizedShapes.Where(s => s.ShapeId == shape.ShapeId && s.UserID == shape.UserID).FirstOrDefault();
            _networkingService._synchronizedShapes.Remove(prevShape);
            _networkingService._synchronizedShapes.Add(shape);
            return prevShape;
        }
        internal void RenderShape(IShape currentShape, string command)
        {

            if (command == "CREATE")
            {
                var newShape = currentShape.Clone();
                _networkingService._synchronizedShapes.Add(newShape);
                newShape.IsSelected = false;
                _undoRedoService.UpdateLastDrawing(newShape, null);

            }else if (command.StartsWith("INDEX"))
            {
                var newShape = currentShape.Clone();
                UpdateAllSynchronizedShapes();
                newShape.IsSelected = false;
            }
            else if (command == "DOWNLOAD")
            {
                var newShape = currentShape.Clone();
                _networkingService._synchronizedShapes.Add(newShape);
                newShape.IsSelected = false;

            }
            else if (command == "MODIFY")
            {
                var newShape = currentShape.Clone();
                var prevShape = UpdateSynchronizedShapes(newShape);
                newShape.IsSelected = false;
                _undoRedoService.UpdateLastDrawing(newShape, prevShape);

            }
            else if (command == "CLEAR")
            {
                Shapes.Clear();
                _undoRedoService.UndoList.Clear();
                _undoRedoService.RedoList.Clear();
                _networkingService._synchronizedShapes.Clear();
                string clearMessage = $"{command}:";
                Debug.WriteLine(clearMessage);
                _networkingService.BroadcastShapeData(clearMessage, -1);
                return;
            }
            else if (command == "UNDO")
            {
                var prevShape = _undoRedoService.UndoList[_undoRedoService.UndoList.Count - 1].Item1;
                var _currentShape = _undoRedoService.UndoList[_undoRedoService.UndoList.Count - 1].Item2;
                if (_currentShape == null)
                {
                    currentShape = prevShape;
                    foreach (var s in Shapes)
                    {
                        if (s.ShapeId == currentShape.ShapeId)
                        {
                            Shapes.Remove(s);
                            break;
                        }
                    }
                    _networkingService._synchronizedShapes.Remove(currentShape);
                    prevShape.LastModifierID = _networkingService._clientID;
                    command = "DELETE";
                }
                else if (prevShape == null)
                {
                    currentShape = _currentShape;
                    var newShape = currentShape.Clone();
                    Shapes.Add(newShape);
                    _networkingService._synchronizedShapes.Add(newShape);
                    command = "CREATE";
                }
                else
                {
                    currentShape = _currentShape;
                    var newShape = currentShape.Clone();
                    UpdateSynchronizedShapes(newShape);
                    foreach (var s in Shapes)
                    {
                        if (s.ShapeId == prevShape.ShapeId)
                        {
                            Shapes.Remove(s);
                            break;
                        }
                    }
                    Shapes.Add(currentShape);
                    _currentShape.LastModifierID = _networkingService._clientID;
                    command = "MODIFY";

                }
                _undoRedoService.Undo();
            }
            else if (command == "REDO")
            {
                var prevShape = _undoRedoService.RedoList[_undoRedoService.RedoList.Count - 1].Item1;
                var _currentShape = _undoRedoService.RedoList[_undoRedoService.RedoList.Count - 1].Item2;
                if (_currentShape == null)
                {
                    currentShape = prevShape;
                    foreach (var s in Shapes)
                    {
                        if (s.ShapeId == prevShape.ShapeId)
                        {
                            Shapes.Remove(s);
                            break;
                        }
                    }
                    _networkingService._synchronizedShapes.Remove(currentShape);
                    prevShape.LastModifierID = _networkingService._clientID;
                    command = "DELETE";
                }
                else if (prevShape == null)
                {
                    currentShape = _currentShape;
                    var newShape = currentShape.Clone();
                    Shapes.Add(newShape);
                    _networkingService._synchronizedShapes.Add(newShape);
                    command = "CREATE";
                }
                else
                {
                    currentShape = _currentShape;
                    var newShape = currentShape.Clone();
                    UpdateSynchronizedShapes(newShape);
                    foreach (var s in Shapes)
                    {
                        if (s.ShapeId == currentShape.ShapeId)
                        {
                            Shapes.Remove(s);
                            break;
                        }
                    }
                    Shapes.Add(currentShape);
                    _currentShape.LastModifierID = _networkingService._clientID;
                    command = "MODIFY";

                }
                _undoRedoService.Redo();
            }
            else if (command == "DELETE")
            {
                Shapes.Remove(currentShape);
                _networkingService._synchronizedShapes.Remove(currentShape);
                _undoRedoService.UpdateLastDrawing(null, currentShape);
            }

            currentShape.LastModifierID = _networkingService._clientID;
            string serializedShape = SerializationService.SerializeShape(currentShape);
            string serializedMessage = $"{command}:{serializedShape}";
            Debug.WriteLine(serializedMessage);
            _networkingService.BroadcastShapeData(serializedMessage, -1);
        }

        private void UpdateAllSynchronizedShapes()
        {
            _networkingService._synchronizedShapes.Clear();
            foreach(IShape shape in Shapes)
            {
                _networkingService._synchronizedShapes.Add(shape);
            }
        }
    }
}