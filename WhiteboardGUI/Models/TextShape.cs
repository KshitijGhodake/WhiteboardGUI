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
            set { _text = value; OnPropertyChanged(nameof(Text)); }
        }

        public double X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(nameof(X)); }
        }

        public double Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(nameof(Y)); }
        }

        public double FontSize
        {
            get => _fontSize;
            set { _fontSize = value; OnPropertyChanged(nameof(FontSize)); }
        }

        // Property for binding in XAML
        public Brush Foreground => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
    }
}
