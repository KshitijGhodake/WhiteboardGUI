using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using WhiteboardGUI.Models;

namespace WhiteboardGUI.Services
{
    public static class SerializationService
    {
        public static string SerializeShape(IShape shape)
        {
            return JsonConvert.SerializeObject(shape);
        }

        public static string SerializeSnapShot(SnapShot snapShot)
        {
            return JsonConvert.SerializeObject(snapShot);
        }

        public static SnapShot DeserializeSnapShot(String data)
        {

            return JsonConvert.DeserializeObject<SnapShot>(data, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
        }

        public static IShape DeserializeShape(string data)
        {
            var shapeDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            string shapeType = shapeDict["ShapeType"].ToString();
            Debug.WriteLine(shapeType);

            return shapeType switch
            {
                "Circle" => JsonConvert.DeserializeObject<CircleShape>(data),
                "Line" => JsonConvert.DeserializeObject<LineShape>(data),
                "Scribble" => JsonConvert.DeserializeObject<ScribbleShape>(data),
                "TextShape" => JsonConvert.DeserializeObject<TextShape>(data),
                _ => throw new NotSupportedException("Shape type not supported"),
            };
        }


        // Serialize an ObservableCollection of IShape
        public static string SerializeShapes(ObservableCollection<IShape> shapes)
        {
            return JsonConvert.SerializeObject(shapes);
        }

        // Deserialize an ObservableCollection of IShape
        public static ObservableCollection<IShape> DeserializeShapes(string data)
        {
            var shapeList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(data);
            var shapes = new ObservableCollection<IShape>();

            foreach (var shapeDict in shapeList)
            {
                string shapeType = shapeDict["ShapeType"].ToString();
                Debug.WriteLine($"Deserializing shape of type: {shapeType}");

                IShape shape = shapeType switch
                {
                    "Circle" => JsonConvert.DeserializeObject<CircleShape>(JsonConvert.SerializeObject(shapeDict)),
                    "Line" => JsonConvert.DeserializeObject<LineShape>(JsonConvert.SerializeObject(shapeDict)),
                    "Scribble" => JsonConvert.DeserializeObject<ScribbleShape>(JsonConvert.SerializeObject(shapeDict)),
                    "TextShape" => JsonConvert.DeserializeObject<TextShape>(JsonConvert.SerializeObject(shapeDict)),
                    _ => throw new NotSupportedException($"Shape type '{shapeType}' not supported"),
                };

                shapes.Add(shape);
            }

            return shapes;
        }
    }
}
