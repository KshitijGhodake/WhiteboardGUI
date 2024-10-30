using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace WhiteboardGUI.Models
{
    public class CircleShape : IShape
    {
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double RadiusX { get; set; }
        public double RadiusY { get; set; }
        public CircleShape()
        {
            ShapeType = "Circle";
        }
    }
}
