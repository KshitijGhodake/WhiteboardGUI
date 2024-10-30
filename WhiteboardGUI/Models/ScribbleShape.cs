using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhiteboardGUI.Models
{
    public class ScribbleShape : IShape
    {
        public List<Point> Points { get; set; } = new List<Point>();

        public ScribbleShape()
        {
            ShapeType = "Scribble";
        }
    }
}
