using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

 namespace WhiteboardGUI.Models
    {
        public class TextboxModel : ShapeBase, INotifyPropertyChanged
        {
            public override string ShapeType => "TextBoxModel";
            private string _text;
            private double _width;
            private double _height;
            private double _x;
            private double _y;
            private Visibility _visibility;
            private Brush _background;
            private Brush _borderBrush;
        private double _fontSize;

        public double Width
            {
                get => _width;
                set { _width = value; OnPropertyChanged(nameof(Width)); }
            }

            

            public double Height
            {
                get => _height;
                set { _height = value; OnPropertyChanged(nameof(Height)); }
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

            public Visibility Visibility
            {
                get => _visibility;
                set { _visibility = value; OnPropertyChanged(nameof(Visibility)); }
            }

            public Brush Background
            {
                get => _background;
                set { _background = value; OnPropertyChanged(nameof(Background)); }
            }

          

            public Brush BorderBrush
            {
                get => _borderBrush;
                set { _borderBrush = value; OnPropertyChanged(nameof(BorderBrush)); }
            }

        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(nameof(Text)); }
        }

        public double FontSize
        {
            get => _fontSize;
            set { _fontSize = value; OnPropertyChanged(nameof(FontSize)); }
        }

        // Other properties like X, Y, Width, Height, etc.

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        // Default values (can be adjusted as needed)


    }
    }

