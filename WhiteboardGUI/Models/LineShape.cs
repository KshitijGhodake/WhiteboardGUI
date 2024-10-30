using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhiteboardGUI.Models
{
    public class LineShape : IShape
    {
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }

        public LineShape()
        {
            ShapeType = "Line";
        }
    }
}
