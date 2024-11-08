using System;
using System.Windows;
using System.Windows.Media;

namespace WhiteboardGUI.Models
{
    public class TextShape : ShapeBase
    {
        public override string ShapeType => "TextShape";

        private string _text;
        private double _x;
        private double _y;
        private double _fontSize;


        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(nameof(Text)); OnPropertyChanged(nameof(Width)); }
        }

        public double X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(nameof(X)); OnPropertyChanged(nameof(Left)); }
        }

        public double Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(nameof(Y)); OnPropertyChanged(nameof(Top)); }
        }

        public double FontSize
        {
            get => _fontSize;
            set { _fontSize = value; OnPropertyChanged(nameof(FontSize)); OnPropertyChanged(nameof(Height)); OnPropertyChanged(nameof(Width)); }
        }

        // Properties for binding in XAML
        public double Left => X;
        public double Top => Y;

        public Brush Foreground => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));

        // Estimate Width and Height based on text length and font size
        public double Width => EstimateTextWidth();
        public double Height => EstimateTextHeight();

        // Implement IsSelected property
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        // Implement GetBounds method
        public override Rect GetBounds()
        {
            return new Rect(Left, Top, Width, Height);
        }

        // Methods to estimate text dimensions
        private double EstimateTextWidth()
        {
            return Text.Length * FontSize * 0.5; // Adjust the multiplier based on average character width
        }

        private double EstimateTextHeight()
        {
            return FontSize * 1.2; // Adjust based on line height
        }

        public override IShape Clone()
        {
            return new TextShape
            {
                ShapeId = this.ShapeId, // Assign a new unique ID
                UserID = this.UserID,
                Color = this.Color,
                StrokeThickness = this.StrokeThickness,
                LastModifierID = this.LastModifierID,
                IsSelected = false, // New shape should not be selected by default
                Text = this.Text,
                X = this.X,
                Y = this.Y,
                FontSize = this.FontSize
                // If ShapeBase has additional properties, copy them here
            };
        }

    }
}