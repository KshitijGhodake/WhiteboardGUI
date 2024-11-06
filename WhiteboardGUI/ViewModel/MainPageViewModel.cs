using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using WhiteboardGUI.Models;
using WhiteboardGUI.Services;
using System.Windows.Media;
using System.Windows;
using System.Diagnostics;
using System.Windows.Xps;
using WhiteboardGUI.ViewModel;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace WhiteboardGUI.ViewModel
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        // Fields
        private readonly NetworkingService _networkingService;
        private readonly UndoRedoService _undoRedoService = new();
        private IShape _selectedShape;
        private ShapeType _currentTool = ShapeType.Pencil;
        private Point _startPoint;
        private bool _isSelecting;
        private bool _isDragging;
        public ObservableCollection<IShape> _shapes;
        
        //for textbox
        private string _textInput;
        private bool _isTextBoxActive;
        private TextShape _currentTextShape;
        private TextboxModel _currentTextboxModel;

        // bouding box
        private bool isBoundingBoxActive;


        public string TextInput
        {
            get => _textInput;
            set { _textInput = value; OnPropertyChanged(nameof(TextInput));
                Debug.WriteLine(_textInput); }
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

        private double txx;
        private double tyy;

        public double TextBoxFontSize { get; set; } = 16;

        // Visibility property that directly converts IsTextBoxActive to a Visibility value
        public Visibility TextBoxVisibility => IsTextBoxActive ? Visibility.Visible : Visibility.Collapsed;
        // Properties
        public ObservableCollection<IShape> Shapes
        {
            get => _shapes;
            set { _shapes = value; OnPropertyChanged(nameof(Shapes)); }
        }

        public IShape SelectedShape
        {
            get => _selectedShape;
            set { _selectedShape = value; OnPropertyChanged(nameof(SelectedShape)); }
        }

        public ShapeType CurrentTool
        {
            get => _currentTool;
            set 
            {
                //without textbox
                _currentTool = value; OnPropertyChanged(nameof(CurrentTool));
            }
        }

        public bool IsHost { get; set; }
        public bool IsClient { get; set; }

        public Brush SelectedColor { get; set; } = Brushes.Black;
        public double SelectedThickness { get; set; } = 2.0;

        

        // Commands
        public ICommand StartHostCommand { get; }
        public ICommand StartClientCommand { get; }
        public ICommand StopHostCommand { get; }
        public ICommand StopClientCommand { get; }
        public ICommand SelectToolCommand { get; }
        public ICommand DrawShapeCommand { get; }
        public ICommand SelectShapeCommand { get; }
        public ICommand DeleteShapeCommand { get; }
        public ICommand CanvasMouseDownCommand { get; }
        public ICommand CanvasMouseMoveCommand { get; }
        public ICommand CanvasMouseUpCommand { get; }
        //public ICommand FinalizeTextBoxCommand { get; }
        // Commands for finalizing or canceling the TextBox input
        public ICommand FinalizeTextBoxCommand { get; }
        public ICommand CancelTextBoxCommand { get; }

        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }


        // Events
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<IShape> ShapeReceived;
        public event Action<IShape> ShapeDeleted;

        // Constructor
        public MainPageViewModel()
        {
            Shapes = new ObservableCollection<IShape>();
            _networkingService = new NetworkingService();

            // Subscribe to networking events
            _networkingService.ShapeReceived += OnShapeReceived;
            _networkingService.ShapeDeleted += OnShapeDeleted;
            _networkingService.ShapeModified += OnShapeModified;

            // Initialize commands
            Debug.WriteLine("ViewModel init start");
            StartHostCommand = new RelayCommand(async () => await TriggerHostCheckbox(), () => { return true; });
            StartClientCommand = new RelayCommand(async () => await TriggerClientCheckBox(5000), () => { return true; });
            SelectToolCommand = new RelayCommand<ShapeType>(SelectTool);
            //DrawShapeCommand = new RelayCommand<(IShape, string)>(DrawShape);
            DrawShapeCommand = new RelayCommand<object>(parameter =>
            {
                if (parameter is Tuple<IShape, string> args)
                {
                    DrawShape(args.Item1, args.Item2);
                }
            });

            SelectShapeCommand = new RelayCommand<IShape>(SelectShape);
            DeleteShapeCommand = new RelayCommand(DeleteSelectedShape, () => SelectedShape != null);
            CanvasMouseDownCommand = new RelayCommand<MouseButtonEventArgs>(OnCanvasMouseDown);
            CanvasMouseMoveCommand = new RelayCommand<MouseEventArgs>(OnCanvasMouseMove);
            CanvasMouseUpCommand = new RelayCommand<MouseButtonEventArgs>(OnCanvasMouseUp);
            // Initialize commands
            FinalizeTextBoxCommand = new RelayCommand(FinalizeTextBox);
            CancelTextBoxCommand = new RelayCommand(CancelTextBox);
            UndoCommand = new RelayCommand(CallUndo);
            RedoCommand = new RelayCommand(CallRedo);
        }

        private void CallUndo()
        {
            if (_undoRedoService.UndoList.Count > 0)
            {
                var prevShape = _undoRedoService.UndoList[_undoRedoService.UndoList.Count - 1].Item2;
                var currentShape = _undoRedoService.UndoList[_undoRedoService.UndoList.Count - 1].Item1;
                _undoRedoService.Undo();
                if (prevShape == null)
                {
                    DeleteShape(currentShape);
                }
            }

        }

        private void CallRedo()
        {
            if (_undoRedoService.RedoList.Count > 0)
            {
                var prevShape = _undoRedoService.RedoList[_undoRedoService.RedoList.Count - 1].Item2;
                var currentShape = _undoRedoService.RedoList[_undoRedoService.RedoList.Count - 1].Item1;
                _undoRedoService.Redo();
                if (currentShape == null)
                {
                    DrawShape(prevShape, "REDO");
                }
            }

        }

        // Methods
        private async System.Threading.Tasks.Task TriggerHostCheckbox()
        {
            if (IsHost==true){
            Debug.WriteLine("ViewModel host start");
            await _networkingService.StartHost();
        }
            else
            {
                _networkingService.StopHost();
            }
        }

        private async System.Threading.Tasks.Task TriggerClientCheckBox(int port)
        {
            //IsClient = true;
            Debug.WriteLine("IsClient:", IsClient.ToString());
            if (IsClient == false)
            {
                _networkingService.StopClient();
            }
            else
            {
                IsClient = true;
                Debug.WriteLine("ViewModel client start");
                await _networkingService.StartClient(port);
            }
        }
        private void StopHost()
        {
            IsHost = false;
            _networkingService.StopHost();
        }

     

        private void SelectTool(ShapeType tool)
        {
            CurrentTool = tool;
            //for textbox
            //TextInput = string.Empty;
        }

        private void DrawShape(IShape shape, string command)
        {
            if (shape == null) return;
            if (command != "CREATE")
            {
                Shapes.Add(shape);
            }

            shape.LastModifierID = _networkingService._clientID;
            string serializedShape = SerializationService.SerializeShape(shape);

            _networkingService.BroadcastShapeData(serializedShape, -1);

            if (command == "REDO") { return; }
            _undoRedoService.UpdateLastModified(shape, null);
            
        }

        

        private void SelectShape(IShape shape)
        {
            SelectedShape = shape;
        }

        private void DeleteShape(IShape shape)
        {
            Shapes.Remove(shape);
            _networkingService._synchronizedShapes.Remove(shape);
            string serializedShape = SerializationService.SerializeShape(shape);
            string deletionMessage = $"DELETE:{serializedShape}";
            _networkingService.BroadcastShapeData(deletionMessage, -1);
        }
        private void DeleteSelectedShape()
        {
            if (SelectedShape != null)
            {
                Shapes.Remove(SelectedShape);
                _networkingService._synchronizedShapes.Remove(SelectedShape);
                string serializedShape = SerializationService.SerializeShape(SelectedShape);
                string deletionMessage = $"DELETE:{serializedShape}";
                _networkingService.BroadcastShapeData(deletionMessage, -1);
                SelectedShape = null;
            }
        }

        private void OnCanvasMouseDown(MouseButtonEventArgs e)
        {
            // Pass the canvas as the element
            var canvas = e.Source as FrameworkElement;
            if (canvas != null)
            {
                _startPoint = e.GetPosition(canvas);
                if (CurrentTool == ShapeType.Select)
                {
                    // Implement selection logic
                    _isSelecting = true;
                }
                if (CurrentTool == ShapeType.Text)
                {
                    // If there's an active textbox, finalize it
                    //if (_currentTextboxModel != null && !string.IsNullOrEmpty(TextInput) )
                    if(IsTextBoxActive==true)
                    {
                        FinalizeTextBox();
                    }
                    // Get the position of the click
                    var position = e.GetPosition((IInputElement)e.Source);
                    var textboxModel = new TextboxModel
                    {
                        X = position.X,
                        Y = position.Y,
                        Width = 150,
                        Height = 30,
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
                        _networkingService._synchronizedShapes.Add(newShape);
                        SelectedShape = newShape;
                    }
                }
            }
        }

        private void OnCanvasMouseMove(MouseEventArgs e)
        {
            //without textbox
            if (e.LeftButton == MouseButtonState.Pressed && SelectedShape != null)
            {
                var canvas = e.Source as FrameworkElement;
                if (canvas != null)
                {
                    Point currentPoint = e.GetPosition(canvas);
                    UpdateShape(SelectedShape, currentPoint);
                }
            }
        }

        private void OnCanvasMouseUp(MouseButtonEventArgs e)
        {
            //without textbox
            if (SelectedShape != null)
            {
                // Finalize shape drawing
                DrawShape(SelectedShape, "CREATE");
                SelectedShape = null;
            }
            _isSelecting = false;
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
            shape.UserID = _networkingService._clientID;
            shape.ShapeId = Guid.NewGuid();
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

        private void OnShapeReceived(IShape shape)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Shapes.Add(shape);
                _networkingService._synchronizedShapes.Add(shape);
            });
        }

        private void OnShapeModified( IShape shape)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Shapes.Add(shape);
                _networkingService._synchronizedShapes.Add(shape);
                _undoRedoService.RemoveLastModified(_networkingService, shape);
            });
        }



        private void OnShapeDeleted(IShape shape)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Shapes.Remove(shape);
                _networkingService._synchronizedShapes.Remove(shape);
            });
        }
        public void CancelTextBox()
        {
            TextInput = string.Empty;
            IsTextBoxActive = false;
            OnPropertyChanged(nameof(TextBoxVisibility));
        }
        public void FinalizeTextBox()
        {
            if ((_currentTextboxModel != null && !string.IsNullOrEmpty(_currentTextboxModel.Text)))
            {
                var textShape = new TextShape
                {
                    X = _currentTextboxModel.X,
                    Y = _currentTextboxModel.Y,
                    Text = _currentTextboxModel.Text,
                    Color = SelectedColor.ToString(),
                    FontSize = TextBoxFontSize
                };
                textShape.ShapeId = Guid.NewGuid();
                textShape.UserID = _networkingService._clientID;
                textShape.LastModifierID = _networkingService._clientID;
                Shapes.Add(textShape);
                _undoRedoService.UpdateLastModified(textShape, null);
                _networkingService._synchronizedShapes.Add(textShape);
                string serializedShape = SerializationService.SerializeShape(textShape);
                _networkingService.BroadcastShapeData(serializedShape, -1);

                // Reset input and hide TextBox
                TextInput = string.Empty;
                IsTextBoxActive = false;
                Shapes.Remove(_currentTextboxModel);
                _currentTextboxModel = null;
                OnPropertyChanged(nameof(TextBoxVisibility));
            }
        }
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
