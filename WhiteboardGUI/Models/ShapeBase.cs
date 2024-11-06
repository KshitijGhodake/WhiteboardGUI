using System;
using System.ComponentModel;

namespace WhiteboardGUI.Models
{
    public abstract class ShapeBase : IShape
    {
        private Guid _shapeId;
        private string _color;
        private double _strokeThickness;
        private double _userID;
        private double _lastModifierID;

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
        public double LastModifierID
        {
            get => _lastModifierID;
            set { _lastModifierID = value; OnPropertyChanged(nameof(LastModifierID)); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
