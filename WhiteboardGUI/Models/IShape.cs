using System;
using System.ComponentModel;

namespace WhiteboardGUI.Models
{
    public interface IShape : INotifyPropertyChanged
    {
        Guid ShapeId { get; set; }
        string ShapeType { get; }
        string Color { get; set; }
        double StrokeThickness { get; set; }
        double UserID { get; set; }
        double LastModifierID { get; set; }
    }
}
