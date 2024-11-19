using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhiteboardGUI.Models;

namespace WhiteboardGUI.Services
{
    public class ReceivedDataService
    {

        public List<IShape> _synchronizedShapes = new();
        public event Action<IShape, Boolean> ShapeReceived;
        public event Action<IShape> ShapeDeleted;
        public event Action<IShape> ShapeSendToBack;
        public event Action<IShape> ShapeSendBackward;
        public event Action<IShape> ShapeModified;
        public event Action<IShape> ShapeUnlocked;
        public event Action<IShape> ShapeLocked;

        public event Action ShapesClear;
        private int _Id;

        public ReceivedDataService(int Id)
        {
            _Id = Id;
        }
        public int DataReceived(string receivedData)
        {
            if (receivedData == null)
            {
                return -1;
            }
            int index = receivedData.IndexOf("END");
            int senderId = int.Parse(receivedData.Substring(2, index - 2));

            if (senderId == _Id)
            {
                return -1;
            }
            receivedData = receivedData.Substring(index + "END".Length);
            if (receivedData.StartsWith("DELETE:"))
            {
                string data = receivedData.Substring(7);
                var shape = SerializationService.DeserializeShape(data);

                if (shape != null)
                {
                    var shapeId = shape.ShapeId;
                    var shapeUserId = shape.UserID;

                    var currentShape = _synchronizedShapes.Where(s => s.ShapeId == shapeId && s.UserID == shapeUserId).FirstOrDefault();
                    if (currentShape != null)
                    {
                        ShapeDeleted?.Invoke(currentShape);

                    }
                }
            }

            else if (receivedData.StartsWith("CLEAR:"))
            {
                ShapesClear?.Invoke();
            }

            else if (receivedData.StartsWith("INDEX-BACK:"))
            {
                string data = receivedData.Substring(11);
                var shape = SerializationService.DeserializeShape(data);

                if (shape != null)
                {
                    var shapeId = shape.ShapeId;
                    var shapeUserId = shape.UserID;

                    var currentShape = _synchronizedShapes.Where(s => s.ShapeId == shapeId && s.UserID == shapeUserId).FirstOrDefault();
                    if (currentShape != null)
                    {
                        ShapeSendToBack?.Invoke(currentShape);
                    }
                }
            }

            else if (receivedData.StartsWith("INDEX-BACKWARD:"))
            {
                string data = receivedData.Substring(15);
                var shape = SerializationService.DeserializeShape(data);

                if (shape != null)
                {
                    var shapeId = shape.ShapeId;
                    var shapeUserId = shape.UserID;

                    var currentShape = _synchronizedShapes.Where(s => s.ShapeId == shapeId && s.UserID == shapeUserId).FirstOrDefault();
                    if (currentShape != null)
                    {
                        ShapeSendBackward?.Invoke(currentShape);
                    }
                }
            }

            else if (receivedData.StartsWith("MODIFY:"))
            {
                string data = receivedData.Substring(7);
                var shape = SerializationService.DeserializeShape(data);
                Debug.WriteLine($"Received shape: {shape}");
                if (shape != null)
                {
                    var shapeId = shape.ShapeId;
                    var shapeUserId = shape.UserID;

                    var currentShape = _synchronizedShapes.Where(s => s.ShapeId == shapeId && s.UserID == shapeUserId).FirstOrDefault();
                    if (currentShape != null)
                    {
                        //ShapeDeleted?.Invoke(currentShape);
                        ShapeModified?.Invoke(shape);

                    }
                }
            }

            else if (receivedData.StartsWith("CREATE:"))
            {
                string data = receivedData.Substring(7);
                var shape = SerializationService.DeserializeShape(data);
                if (shape != null)
                {
                    ShapeReceived?.Invoke(shape, true);
                }
            }

            else if (receivedData.StartsWith("DOWNLOAD:"))
            {
                string data = receivedData.Substring(9);
                var shape = SerializationService.DeserializeShape(data);
                if (shape != null)
                {
                    ShapeReceived?.Invoke(shape, false);
                }

            }

            else if (receivedData.StartsWith("UNLOCK:"))
            {
                string data = receivedData.Substring(7);
                var shape = SerializationService.DeserializeShape(data);
                if (shape != null)
                {
                    // Update the shape's lock status
                    var existingShape = _synchronizedShapes.FirstOrDefault(s => s.ShapeId == shape.ShapeId);
                    if (existingShape != null)
                    {
                        existingShape.IsLocked = false;
                        existingShape.LockedByUserID = -1;
                        ShapeUnlocked?.Invoke(existingShape);
                    }
                }
            }

            else if (receivedData.StartsWith("LOCK:"))
            {
                string data = receivedData.Substring(5);
                var shape = SerializationService.DeserializeShape(data);
                if (shape != null)
                {
                    // Update the shape's lock status
                    var existingShape = _synchronizedShapes.FirstOrDefault(s => s.ShapeId == shape.ShapeId);

                    if (existingShape != null)
                    {
                        if (_Id == 1)
                        {
                            if (existingShape.IsLocked)
                            {
                                receivedData = receivedData = "UNLOCK:" + receivedData.Substring("LOCK:".Length);
                            }
                            else
                            {
                                existingShape.IsLocked = true;
                                existingShape.LockedByUserID = senderId;
                                ShapeLocked?.Invoke(existingShape);
                            }
                        }
                        else
                        {
                            existingShape.IsLocked = true;
                            existingShape.LockedByUserID = shape.LockedByUserID;
                            ShapeLocked?.Invoke(existingShape);
                        }
                    }
                }
            }

            return 0;

        }
    }
}
