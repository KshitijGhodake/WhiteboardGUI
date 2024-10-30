using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhiteboardGUI.Models
{
    public abstract class IShape
    {
        public string ShapeType { get; set; }
        public string Color { get; set; } 
        public double StrokeThickness { get; set; }
        public Guid ShapeId { get; set; } = Guid.NewGuid();
        public double UserID { get; set; }
    }
}
