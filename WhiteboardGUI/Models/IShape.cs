using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

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
        int ZIndex { get; set; }

        bool IsSelected { get; set; }

        bool IsLocked { get; set; }
        double LockedByUserID { get; set; }

        string BoundingBoxColor { get; set; }
        Rect GetBounds();

        IShape Clone();
    }
}