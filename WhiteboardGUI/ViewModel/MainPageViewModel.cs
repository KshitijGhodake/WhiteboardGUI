using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WhiteboardGUI.Models;
using WhiteboardGUI.Services;

namespace WhiteboardGUI.ViewModel
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        // Fields
        private readonly NetworkingService _networkingService;
        private IShape _selectedShape;
        private ShapeType _currentTool = ShapeType.Pencil;
        private Point _startPoint;
        private Point _lastMousePosition;

        // For textbox
        private string _textInput;
        private bool _isTextBoxActive;
        private TextboxModel _currentTextboxModel;

        // Properties for color selection
        private byte _red = 0;
        public byte Red
        {
            get => _red;
            set { _red = value; OnPropertyChanged(nameof(Red)); UpdateSelectedColor(); }
        }

        private byte _green = 0;
        public byte Green
        {
            get => _green;
            set { _green = value; OnPropertyChanged(nameof(Green)); UpdateSelectedColor(); }
        }

        private byte _blue = 0;
        public byte Blue
        {
            get => _blue;
            set { _blue = value; OnPropertyChanged(nameof(Blue)); UpdateSelectedColor(); }
        }

        private double _selectedThickness = 2.0;
        public double SelectedThickness
        {
            get => _selectedThickness;
            set
            {
                _selectedThickness = value;
                OnPropertyChanged(nameof(SelectedThickness));
                if (SelectedShape != null)
                {
                    UpdateSelectedShapeProperties();
                }
            }
        }

        private Color _selectedColor = Colors.Black;
        public Color SelectedColor
        {
            get => _selectedColor;
            set
            {
                _selectedColor = value;
                OnPropertyChanged(nameof(SelectedColor));
                if (SelectedShape != null)
                {
                    UpdateSelectedShapeProperties();
                }
            }
        }

        public string TextInput
        {
            get => _textInput;
            set
            {
                _textInput = value;
                OnPropertyChanged(nameof(TextInput));
                if (_currentTextboxModel != null)
                {
                    _currentTextboxModel.Text = _textInput;
                }
            }
        }

        public bool IsTextBoxActive
        {
            get => _isTextBoxActive;
            set
            {
                _isTextBoxActive = value;
                OnPropertyChanged(nameof(IsTextBoxActive));
                OnPropertyChanged(nameof(TextBoxVisibility));
            }
        }

        public Rect TextBoxBounds { get; set; }

        public double TextBoxFontSize { get; set; } = 16;

        // Visibility property that directly converts IsTextBoxActive to a Visibility value
        public Visibility TextBoxVisibility => IsTextBoxActive ? Visibility.Visible : Visibility.Collapsed;

        // Properties
        public ObservableCollection<IShape> Shapes { get; set; }

        public IShape SelectedShape
        {
            get => _selectedShape;
            set
            {
                if (_selectedShape != value)
                {
                    if (_selectedShape != null)
                    {
                        _selectedShape.IsSelected = false;
                    }

                    _selectedShape = value;

                    if (_selectedShape != null)
                    {
                        _selectedShape.IsSelected = true;
                    }

                    OnPropertyChanged(nameof(SelectedShape));
                    OnPropertyChanged(nameof(IsShapeSelected));
                    UpdateColorAndThicknessFromSelectedShape();
                }
            }
        }

        public bool IsShapeSelected => SelectedShape != null;

        public ShapeType CurrentTool
        {
            get => _currentTool;
            set { _currentTool = value; OnPropertyChanged(nameof(CurrentTool)); }
        }

        private bool _isHost;
        public bool IsHost
        {
            get => _isHost;
            set { _isHost = value; OnPropertyChanged(nameof(IsHost)); }
        }

        private bool _isClient;
        public bool IsClient
        {
            get => _isClient;
            set { _isClient = value; OnPropertyChanged(nameof(IsClient)); }
        }

        // Commands
        public ICommand StartHostCommand { get; }
        public ICommand StartClientCommand { get; }
        public ICommand SelectToolCommand { get; }
        public ICommand DrawShapeCommand { get; }
        public ICommand SelectShapeCommand { get; }
        public ICommand DeleteShapeCommand { get; }
        public ICommand CanvasMouseDownCommand { get; }
        public ICommand CanvasMouseMoveCommand { get; }
        public ICommand CanvasMouseUpCommand { get; }
        public ICommand FinalizeTextBoxCommand { get; }
        public ICommand CancelTextBoxCommand { get; }

        // Events
        public event PropertyChangedEventHandler PropertyChanged;

        // Constructor
        public MainPageViewModel()
        {
            Shapes = new ObservableCollection<IShape>();
            _networkingService = new NetworkingService(Shapes);

            // Subscribe to networking events
            _networkingService.ShapeReceived += OnShapeReceived;
            _networkingService.ShapeDeleted += OnShapeDeleted;

            // Initialize commands
            StartHostCommand = new RelayCommand(async () => await TriggerHostCheckbox(), () => true);
            StartClientCommand = new RelayCommand(async () => await TriggerClientCheckBox(5000), () => true);
            SelectToolCommand = new RelayCommand<ShapeType>(SelectTool);
            DrawShapeCommand = new RelayCommand<IShape>(DrawShape);
            SelectShapeCommand = new RelayCommand<IShape>(SelectShape);
            DeleteShapeCommand = new RelayCommand(DeleteSelectedShape, () => SelectedShape != null);
            CanvasMouseDownCommand = new RelayCommand<MouseButtonEventArgs>(OnCanvasMouseDown);
            CanvasMouseMoveCommand = new RelayCommand<MouseEventArgs>(OnCanvasMouseMove);
            CanvasMouseUpCommand = new RelayCommand<MouseButtonEventArgs>(OnCanvasMouseUp);
            FinalizeTextBoxCommand = new RelayCommand(FinalizeTextBox);
            CancelTextBoxCommand = new RelayCommand(CancelTextBox);

            // Initialize color
            Red = 0;
            Green = 0;
            Blue = 0;
            UpdateSelectedColor();
        }

        // Methods
        private void UpdateSelectedColor()
        {
            SelectedColor = Color.FromRgb(Red, Green, Blue);
        }

        private void UpdateSelectedShapeProperties()
        {
            if (SelectedShape == null) return;

            SelectedShape.Color = SelectedColor.ToString();
            SelectedShape.StrokeThickness = SelectedThickness;

            // Notify property changes for the shape
            if (SelectedShape is ShapeBase shapeBase)
            {
                shapeBase.OnPropertyChanged(nameof(SelectedShape.Color));
                shapeBase.OnPropertyChanged(nameof(SelectedShape.StrokeThickness));
            }

            // Broadcast updated shape
            string serializedShape = SerializationService.SerializeShape(SelectedShape);
            string updateMessage = $"UPDATE:{serializedShape}";
            _networkingService.BroadcastShapeData(updateMessage, -1);
        }


        private void UpdateColorAndThicknessFromSelectedShape()
        {
            if (SelectedShape == null) return;

            // Parse color from SelectedShape.Color
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(SelectedShape.Color);
                Red = color.R;
                Green = color.G;
                Blue = color.B;
            }
            catch
            {
                // Ignore parsing errors
            }

            SelectedThickness = SelectedShape.StrokeThickness;
        }

        private async System.Threading.Tasks.Task TriggerHostCheckbox()
        {
            if (IsHost)
            {
                await _networkingService.StartHost();
            }
            else
            {
                _networkingService.StopHost();
            }
        }

        private async System.Threading.Tasks.Task TriggerClientCheckBox(int port)
        {
            if (!IsClient)
            {
                _networkingService.StopClient();
            }
            else
            {
                IsClient = true;
                await _networkingService.StartClient(port);
            }
        }

        private void SelectTool(ShapeType tool)
        {
            CurrentTool = tool;
            SelectedShape = null;
        }

        private void DrawShape(IShape shape)
        {
            if (shape == null) return;

            string serializedShape = SerializationService.SerializeShape(shape);
            _networkingService.BroadcastShapeData(serializedShape, -1);
        }

        private void SelectShape(IShape shape)
        {
            // Deselect all shapes
            foreach (var s in Shapes)
            {
                s.IsSelected = false;
            }

            // Select the new shape
            SelectedShape = shape;
            if (SelectedShape != null)
            {
                SelectedShape.IsSelected = true;
            }
        }

        private void DeleteSelectedShape()
        {
            if (SelectedShape != null)
            {
                Shapes.Remove(SelectedShape);
                string serializedShape = SerializationService.SerializeShape(SelectedShape);
                string deletionMessage = $"DELETE:{serializedShape}";
                _networkingService.BroadcastShapeData(deletionMessage, -1);
                SelectedShape = null;
            }
        }

        private void OnCanvasMouseDown(MouseButtonEventArgs e)
        {
            var canvas = e.Source as FrameworkElement;
            if (canvas != null)
            {
                Point position = e.GetPosition(canvas);
                _startPoint = position;

                if (CurrentTool == ShapeType.Select)
                {
                    // Hit test to find the shape under the mouse
                    IShape hitShape = HitTestShape(position);
                    if (hitShape != null)
                    {
                        SelectShape(hitShape);
                        _lastMousePosition = position;
                    }
                    else
                    {
                        SelectedShape = null;
                    }
                }
                else if (CurrentTool == ShapeType.Text)
                {
                    var textboxModel = new TextboxModel
                    {
                        X = position.X,
                        Y = position.Y,
                        Width = 150,
                        Height = 30,
                        FontSize = TextBoxFontSize,
                        Color = SelectedColor.ToString()
                    };

                    _currentTextboxModel = textboxModel;
                    TextInput = string.Empty;
                    IsTextBoxActive = true;
                    Shapes.Add(textboxModel);
                    OnPropertyChanged(nameof(TextBoxVisibility));
                }
                else
                {
                    // Start drawing a new shape
                    IShape newShape = CreateShape(_startPoint);
                    if (newShape != null)
                    {
                        Shapes.Add(newShape);
                        SelectedShape = newShape;
                    }
                }
            }
        }

        private void OnCanvasMouseMove(MouseEventArgs e)
        {
            var canvas = e.Source as FrameworkElement;
            if (canvas != null)
            {
                Point currentPoint = e.GetPosition(canvas);

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    if (CurrentTool == ShapeType.Select && SelectedShape != null)
                    {
                        // Move the selected shape
                        MoveShape(SelectedShape, currentPoint);
                        _lastMousePosition = currentPoint;
                    }
                    else if (SelectedShape != null)
                    {
                        UpdateShape(SelectedShape, currentPoint);
                    }
                }
            }
        }

        private void OnCanvasMouseUp(MouseButtonEventArgs e)
        {
            if (SelectedShape != null && CurrentTool != ShapeType.Select)
            {
                // Finalize shape drawing
                DrawShape(SelectedShape);
                SelectedShape = null;
            }
            else if (SelectedShape != null && CurrentTool == ShapeType.Select)
            {
                // Broadcast updated position of the shape
                string serializedShape = SerializationService.SerializeShape(SelectedShape);
                string updateMessage = $"UPDATE:{serializedShape}";
                _networkingService.BroadcastShapeData(updateMessage, -1);
            }
        }

        private IShape CreateShape(Point startPoint)
        {
            IShape shape = null;
            switch (CurrentTool)
            {
                case ShapeType.Pencil:
                    var scribbleShape = new ScribbleShape
                    {
                        Color = SelectedColor.ToString(),
                        StrokeThickness = SelectedThickness,
                        Points = new System.Collections.Generic.List<Point> { startPoint }
                    };
                    shape = scribbleShape;
                    break;
                case ShapeType.Line:
                    var lineShape = new LineShape
                    {
                        StartX = startPoint.X,
                        StartY = startPoint.Y,
                        EndX = startPoint.X,
                        EndY = startPoint.Y,
                        Color = SelectedColor.ToString(),
                        StrokeThickness = SelectedThickness
                    };
                    shape = lineShape;
                    break;
                case ShapeType.Circle:
                    var circleShape = new CircleShape
                    {
                        CenterX = startPoint.X,
                        CenterY = startPoint.Y,
                        RadiusX = 0,
                        RadiusY = 0,
                        Color = SelectedColor.ToString(),
                        StrokeThickness = SelectedThickness
                    };
                    shape = circleShape;
                    break;
            }
            return shape;
        }

        private void UpdateShape(IShape shape, Point currentPoint)
        {
            switch (shape)
            {
                case ScribbleShape scribble:
                    scribble.AddPoint(currentPoint);
                    break;
                case LineShape line:
                    line.EndX = currentPoint.X;
                    line.EndY = currentPoint.Y;
                    break;
                case CircleShape circle:
                    circle.RadiusX = Math.Abs(currentPoint.X - circle.CenterX);
                    circle.RadiusY = Math.Abs(currentPoint.Y - circle.CenterY);
                    break;
            }
        }

        private void MoveShape(IShape shape, Point currentPoint)
        {
            Vector delta = currentPoint - _lastMousePosition;

            switch (shape)
            {
                case LineShape line:
                    line.StartX += delta.X;
                    line.StartY += delta.Y;
                    line.EndX += delta.X;
                    line.EndY += delta.Y;
                    break;
                case CircleShape circle:
                    circle.CenterX += delta.X;
                    circle.CenterY += delta.Y;
                    break;
                case ScribbleShape scribble:
                    for (int i = 0; i < scribble.Points.Count; i++)
                    {
                        scribble.Points[i] = new Point(scribble.Points[i].X + delta.X, scribble.Points[i].Y + delta.Y);
                    }
                    break;
                case TextShape text:
                    text.X += delta.X;
                    text.Y += delta.Y;
                    break;
                case TextboxModel textbox:
                    textbox.X += delta.X;
                    textbox.Y += delta.Y;
                    break;
            }

            // Notify property changes
            if (shape is ShapeBase shapeBase)
            {
                shapeBase.OnPropertyChanged(null); // Notify all properties have changed
            }
        }

        private IShape HitTestShape(Point position)
        {
            // Reverse order to hit test top-most shape first
            foreach (var shape in Shapes.Reverse())
            {
                if (IsPointOverShape(shape, position))
                {
                    return shape;
                }
            }
            return null;
        }

        private bool IsPointOverShape(IShape shape, Point point)
        {
            // Simple bounding box hit testing
            Rect bounds = shape.GetBounds();
            return bounds.Contains(point);
        }

        private void OnShapeReceived(IShape shape)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Shapes.Add(shape);
            });
        }

        private void OnShapeDeleted(IShape shape)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var existingShape = Shapes.FirstOrDefault(s => s.ShapeId == shape.ShapeId);
                if (existingShape != null)
                {
                    Shapes.Remove(existingShape);
                }
            });
        }

        public void CancelTextBox()
        {
            TextInput = string.Empty;
            IsTextBoxActive = false;
            if (_currentTextboxModel != null)
            {
                Shapes.Remove(_currentTextboxModel);
                _currentTextboxModel = null;
            }
            OnPropertyChanged(nameof(TextBoxVisibility));
        }

        public void FinalizeTextBox()
        {
            if (_currentTextboxModel != null && !string.IsNullOrEmpty(_currentTextboxModel.Text))
            {
                var textShape = new TextShape
                {
                    X = _currentTextboxModel.X,
                    Y = _currentTextboxModel.Y,
                    Text = _currentTextboxModel.Text,
                    Color = SelectedColor.ToString(),
                    FontSize = TextBoxFontSize
                };
                Shapes.Add(textShape);
                string serializedShape = SerializationService.SerializeShape(textShape);
                _networkingService.BroadcastShapeData(serializedShape, -1);

                // Reset input and hide TextBox
                TextInput = string.Empty;
                IsTextBoxActive = false;
                Shapes.Remove(_currentTextboxModel);
                _currentTextboxModel = null;
                OnPropertyChanged(nameof(TextBoxVisibility));
            }
            else
            {
                // If text is empty, remove the textbox
                CancelTextBox();
            }
        }

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
