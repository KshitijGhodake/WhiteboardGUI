using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
    }
}
