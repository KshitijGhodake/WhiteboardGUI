using System;
using System.ComponentModel;
using System.Windows;

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

        bool IsSelected { get; set; }
        Rect GetBounds();

        IShape Clone();
    }
}