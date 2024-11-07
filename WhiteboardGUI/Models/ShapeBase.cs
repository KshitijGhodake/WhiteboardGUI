using System;
using System.ComponentModel;
using System.Windows;

namespace WhiteboardGUI.Models
{
    public abstract class ShapeBase : IShape
    {
        private Guid _shapeId;
        private string _color = "#000000"; // Default color
        private double _strokeThickness;
        private double _userID;
        private bool _isSelected;

        public ShapeBase()
        {
            ShapeId = Guid.NewGuid();
        }

        public Guid ShapeId
        {
            get => _shapeId;
            set { _shapeId = value; OnPropertyChanged(nameof(ShapeId)); }
        }

        public abstract string ShapeType { get; }

        public string Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(nameof(Color)); }
        }

        public double StrokeThickness
        {
            get => _strokeThickness;
            set { _strokeThickness = value; OnPropertyChanged(nameof(StrokeThickness)); }
        }

        public double UserID
        {
            get => _userID;
            set { _userID = value; OnPropertyChanged(nameof(UserID)); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public abstract Rect GetBounds();

        // Changed access modifier to public
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
