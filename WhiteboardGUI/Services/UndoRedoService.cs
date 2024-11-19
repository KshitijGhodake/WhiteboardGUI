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
    /// <summary>
    /// Service for managing undo and redo operations for shapes on the whiteboard.
    /// Maintains a history of shape modifications to allow reversing or reapplying changes.
    /// </summary>
    public class UndoRedoService
    {
        /// <summary>
        /// List of actions that can be undone.
        /// Each entry is a tuple containing the current state of the shape and its previous state.
        /// </summary>
        public List<(IShape, IShape?)> UndoList = new();

        /// <summary>
        /// List of actions that can be redone.
        /// Each entry is a tuple containing the state of the shape before the undo and its state after the undo.
        /// </summary>
        public List<(IShape, IShape?)> RedoList = new();

        /// <summary>
        /// Removes all modifications of a specific shape from the undo and redo lists.
        /// Ensures that any existing history for the shape is cleared.
        /// </summary>
        /// <param name="_networkingService">The networking service associated with the shape.</param>
        /// <param name="shape">The shape whose modifications are to be removed.</param>
        public void RemoveLastModified(NetworkingService _networkingService, IShape shape)
        {
            UndoList.RemoveAll(item =>
                item.Item1 != null &&
                item.Item1.ShapeId == shape.ShapeId &&
                item.Item1.UserID == shape.UserID);

            RedoList.RemoveAll(item =>
                item.Item1 != null &&
                item.Item1.ShapeId == shape.ShapeId &&
                item.Item1.UserID == shape.UserID);
        }

        /// <summary>
        /// Adds a new modification to the undo list, representing the current and previous states of a shape.
        /// Limits the undo list to a maximum of 5 entries.
        /// </summary>
        /// <param name="currentShape">The current state of the shape.</param>
        /// <param name="previousShape">The previous state of the shape.</param>
        public void UpdateLastDrawing(IShape currentShape, IShape previousShape)
        {
            UndoList.Add((currentShape, previousShape));
            if (UndoList.Count > 5)
            {
                // Removes the oldest modification if the undo list exceeds 5 entries.
                UndoList.RemoveAt(0);
            }
        }

        /// <summary>
        /// Undoes the last modification, moving it to the redo list.
        /// Limits the redo list to a maximum of 5 entries.
        /// </summary>
        public void Undo()
        {
            if (UndoList.Count > 0)
            {
                RedoList.Add((UndoList[UndoList.Count - 1].Item2, UndoList[UndoList.Count - 1].Item1));
                if (RedoList.Count > 5)
                {
                    // Removes the oldest redo action if the redo list exceeds 5 entries.
                    RedoList.RemoveAt(0);
                }
                UndoList.RemoveAt(UndoList.Count - 1);
            }
        }

        /// <summary>
        /// Redoes the last undone modification, moving it back to the undo list.
        /// </summary>
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
