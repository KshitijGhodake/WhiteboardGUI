using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using WhiteboardGUI.Models;

namespace WhiteboardGUI.Services
{
    public class UndoRedoService
    {
        //public List<IShape> _synchronizedShapes;
        public List<(IShape, IShape?)> UndoList = new();
        public List<(IShape, IShape?)> RedoList = new();
        public void RemoveLastModified(NetworkingService _networkingService, IShape shape)
        {
            //_synchronizedShapes = _networkingService._synchronizedShapes;
            UndoList.RemoveAll(item =>
            item.Item1 != null &&
            item.Item1.ShapeId == shape.ShapeId &&
            item.Item1.UserID == shape.UserID);

            

            RedoList.RemoveAll(item =>
            item.Item1 != null &&
            item.Item1.ShapeId == shape.ShapeId &&
            item.Item1.UserID == shape.UserID);

        }

        public void UpdateLastDrawing(IShape currentShape, IShape previousShape)
        {
            UndoList.Add((currentShape, previousShape));
            if (UndoList.Count > 5)
            {
                // removing first element if queue has more than 5 elements
                // you are welcome, sarthak.
                UndoList.RemoveAt(0);
            }
        }

        public void Undo()
        {
            if (UndoList.Count > 0)
            {
                RedoList.Add((UndoList[UndoList.Count - 1].Item2, UndoList[UndoList.Count - 1].Item1));
                if (RedoList.Count > 5)
                {
                    // removing first element if queue has more than 5 elements
                    // you are welcome, sarthak.
                    RedoList.RemoveAt(0);
                }
                UndoList.RemoveAt(UndoList.Count - 1);
            }
        }
        public void Redo()
        {
            if (RedoList.Count > 0)
            {
                UndoList.Add((RedoList[RedoList.Count - 1].Item2, RedoList[RedoList.Count - 1].Item1));
                RedoList.RemoveAt(RedoList.Count - 1);
            }
        }
    }
}