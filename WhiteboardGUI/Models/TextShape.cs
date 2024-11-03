using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhiteboardGUI.Models
{
    public class TextShape : IShape
    {
        public string Text { get; set; }
        public double X { get; set; }      // X-coordinate position
        public double Y { get; set; }      // Y-coordinate position
        public string Color { get; set; }  // Text color
        public double FontSize { get; set; }
        public TextShape()
        {
            ShapeType = "TextShape";
        }
    }
}
