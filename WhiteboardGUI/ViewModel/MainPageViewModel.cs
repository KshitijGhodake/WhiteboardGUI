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
using System.Linq;
using Microsoft.Windows.Input;
using WhiteboardGUI.Adorners;

namespace WhiteboardGUI.ViewModel
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        // Fields
        private readonly NetworkingService _networkingService;
        private readonly UndoRedoService _undoRedoService = new();
        public readonly RenderingService _renderingService;
        private readonly SnapShotService _snapShotService;
        private IShape _selectedShape;
        private ShapeType _currentTool = ShapeType.Pencil;
        private Point _startPoint;
        private Point _lastMousePosition;
        private bool _isSelecting;
        private bool _isDragging;
        private ObservableCollection<IShape> _shapes;

        //for textbox
        private string _textInput;
        private bool _isTextBoxActive;
        private TextShape _currentTextShape;
        private TextboxModel _currentTextboxModel;

        // bouding box
        private bool isBoundingBoxActive;

        private IShape _hoveredShape;
        private bool _isShapeHovered;
        public HoverAdorner CurrentHoverAdorner { get; set; }

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

        private bool _isUploading;
        public bool IsUploading
        {
            get => _isUploading;
            set
            {
                _isUploading = value;
                OnPropertyChanged(nameof(IsUploading));
            }
        }

        private bool _isDownloading;
        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                _isDownloading = value;
                OnPropertyChanged(nameof(IsDownloading));
            }
        }
        private double _selectedThickness = 2.0;
        public double SelectedThickness
        {
            get => _selectedThickness;
            set
            {
                if (_selectedThickness != value)
                {
                    _selectedThickness = value;
                    OnPropertyChanged(nameof(SelectedThickness));
                    if (SelectedShape is ShapeBase shapeBase)
                    {
                        SelectedShape.StrokeThickness = _selectedThickness;
                        shapeBase.OnPropertyChanged(nameof(SelectedShape.StrokeThickness));
                        _renderingService.RenderShape(SelectedShape, "MODIFY");

                    }
                }
            }
        }
   

        private Color _selectedColor = Colors.Black;
        public Color SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (_selectedColor != value)
                {
                    _selectedColor = value;
                    OnPropertyChanged(nameof(SelectedColor));
                    if (SelectedShape is ShapeBase shapeBase)
                    {
                        SelectedShape.Color = _selectedColor.ToString();
                        shapeBase.OnPropertyChanged(nameof(SelectedShape.Color));
                        _renderingService.RenderShape(SelectedShape, "MODIFY");
                    }
                }
            }
        }
       
      
        public string TextInput
        {
            get => _textInput;
            set
            {
                _textInput = value; OnPropertyChanged(nameof(TextInput));
                Debug.WriteLine(_textInput);
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

        private bool _isPopupOpen;
        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set
            {
                if (_isPopupOpen != value)
                {
                    _isPopupOpen = value;
                    OnPropertyChanged(nameof(IsPopupOpen));
                }
            }
        }

        public Rect TextBoxBounds { get; set; }

        public double TextBoxFontSize { get; set; } = 16;

        // Visibility property that directly converts IsTextBoxActive to a Visibility value
        public Visibility TextBoxVisibility => IsTextBoxActive ? Visibility.Visible : Visibility.Collapsed;
        // Properties
        public ObservableCollection<IShape> Shapes
        {
            get => _shapes;
            set { _shapes = value; OnPropertyChanged(nameof(Shapes)); }
        }
        private string _snapShotFileName;
        public string SnapShotFileName
        {
            get => _snapShotFileName;
            set
            {
                if (_snapShotFileName != value)
                {
                    _snapShotFileName = value;
                    OnPropertyChanged(nameof(SnapShotFileName));
                }
            }
        }

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
                    //UpdateColorAndThicknessFromSelectedShape();
                }
            }
        }


        public bool IsShapeSelected => SelectedShape != null;

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


        public ObservableCollection<string> DownloadItems { get; set; }

        private string _selectedDownloadItem;
        public string SelectedDownloadItem
        {
            get => _selectedDownloadItem;
            set
            {
                _selectedDownloadItem = value;
                OnPropertyChanged(SelectedDownloadItem);
                OnPropertyChanged(nameof(CanDownload)); // Notify change for CanDownload
            }
        }
        public bool CanDownload => !string.IsNullOrEmpty(SelectedDownloadItem);
        public bool IsDownloadPopupOpen { get; set; }


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

        public ICommand SelectColorCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand OpenPopupCommand { get; }
        public ICommand ClearShapesCommand { get; }
        public ICommand OpenDownloadPopupCommand { get; }
        public ICommand DownloadItemCommand { get; }


        // Events
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<IShape> ShapeReceived;
        public event Action<IShape> ShapeDeleted;

        // Constructor
        public MainPageViewModel()
        {
            Shapes = new ObservableCollection<IShape>();
            _networkingService = new NetworkingService();
            _renderingService = new RenderingService(_networkingService, _undoRedoService, Shapes);
            _snapShotService = new SnapShotService(_networkingService,_renderingService, Shapes, _undoRedoService);

            DownloadItems = new ObservableCollection<string>(_snapShotService.getSnaps("a"));
            _snapShotService.OnSnapShotUploaded += RefreshDownloadItems;

            // Subscribe to networking events
            _networkingService.ShapeReceived += OnShapeReceived;
            _networkingService.ShapeDeleted += OnShapeDeleted;
            _networkingService.ShapeModified += OnShapeModified;
            _networkingService.ShapesClear += OnShapeClear;

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
                    _renderingService.RenderShape(args.Item1, args.Item2);
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
            SelectColorCommand = new RelayCommand<string>(SelectColor);

            SubmitCommand = new RelayCommand(async () => await SubmitFileName());
            OpenDownloadPopupCommand = new RelayCommand(OpenDownloadPopup);
            DownloadItemCommand = new RelayCommand(DownloadSelectedItem, () => CanDownload);

            OpenPopupCommand = new RelayCommand(OpenPopup);
            ClearShapesCommand = new RelayCommand(ClearShapes);
            Red = 0;
            Green = 0;
            Blue = 0;
            UpdateSelectedColor();

        }

        // Function to open the download popup
        private void OpenDownloadPopup()
        {
            IsDownloadPopupOpen = true;
            OnPropertyChanged(nameof(IsDownloadPopupOpen));
        }

        private void DownloadSelectedItem()
        {
            if (!string.IsNullOrEmpty(SelectedDownloadItem))
            {
                IsDownloading = false;
                try
                {
                    Debug.WriteLine($"Downloading item: {SelectedDownloadItem}");
                    _snapShotService.DownloadSnapShot(SelectedDownloadItem);
                    Debug.WriteLine("Download Complete");
                }
                catch(Exception ex)
                {
                    Debug.WriteLine($"Download failed: {ex.Message}");
                }
                finally
                {
                    // Re-enable UI elements
                    IsDownloading = false;
                }
            }
            
            // Close the popup after download
            IsDownloadPopupOpen = false;
            OnPropertyChanged(nameof(IsDownloadPopupOpen));
        }

        private void RefreshDownloadItems()
        {
            DownloadItems.Clear();
            var newSnaps = _snapShotService.getSnaps("a");
            foreach (var snap in newSnaps)
            {
                DownloadItems.Add(snap);
            }

            OnPropertyChanged(nameof(DownloadItems));
        }



        private void UpdateSelectedColor()
        {
            SelectedColor = Color.FromRgb(Red, Green, Blue);
        }

        private void SelectColor(string colorName)
        {
                var color = (Color)ColorConverter.ConvertFromString(colorName);
                SelectedColor = color;
        }

        private void CallUndo()
        {
            if (_undoRedoService.UndoList.Count > 0)
            {
                _renderingService.RenderShape(null, "UNDO");
            }

        }


        private void CallRedo()
        {
            if (_undoRedoService.RedoList.Count > 0)
            {
                _renderingService.RenderShape(null, "REDO");
            }
        }
        private void OpenPopup()
        {
            SnapShotFileName = "";
            IsPopupOpen = true;

        }

        private async Task SubmitFileName()
        {
            IsUploading = true;

            try
            {
                // Call the asynchronous upload method
                await _snapShotService.UploadSnapShot(SnapShotFileName, Shapes);
                IsPopupOpen = false;
                Debug.WriteLine("Snapshot uploaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Upload failed: {ex.Message}");
            }
            finally
            {
                // Re-enable UI elements
                IsUploading = false;
            }
        }
        private void ClearShapes()
        {
            _renderingService.RenderShape(null, "CLEAR");
        }

        // Methods
        private async System.Threading.Tasks.Task TriggerHostCheckbox()
        {
            if (IsHost == true)
            {
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
       

        



        private void SelectShape(IShape shape)
        {
            
        }

        private void DeleteShape(IShape shape)
        {
            _renderingService.RenderShape(shape, "DELETE");
        }
        private void DeleteSelectedShape()
        {
            if (SelectedShape != null)
            {
                _renderingService.RenderShape(SelectedShape, "DELETE");
                SelectedShape = null;
            }
        }
        private bool IsPointOverShape(IShape shape, Point point)
        {
            // Simple bounding box hit testing
            Rect bounds = shape.GetBounds();
            return bounds.Contains(point);
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
                    foreach (var shape in Shapes.Reverse())
                    {
                        if (IsPointOverShape(shape, _startPoint))
                        {
                            SelectedShape = shape;
                            Debug.WriteLine(shape.IsSelected);
                            _lastMousePosition = _startPoint;
                            _isSelecting = true;
                            break;
                        }
                        else
                        {
                            _isSelecting = false;
                            SelectedShape = null;
                            
                        }
                    }
                }
                else if (CurrentTool == ShapeType.Text)
                {
                    // If there's an active textbox, finalize it
                    //if (_currentTextboxModel != null && !string.IsNullOrEmpty(TextInput) )
                    if (IsTextBoxActive == true)
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
                        SelectedShape = newShape;
                    }
                }
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
            }

            // Notify property changes
            if (shape is ShapeBase shapeBase)
            {
                shapeBase.OnPropertyChanged(null); // Notify all properties have changed
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
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        if (CurrentTool == ShapeType.Select && SelectedShape != null)
                        {
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
        }

        private void OnCanvasMouseUp(MouseButtonEventArgs e)
        {
            //without textbox
            if (SelectedShape != null && !_isSelecting)
            {
                // Finalize shape drawing
                _renderingService.RenderShape(SelectedShape, "CREATE");
                SelectedShape = null;
                
            }
            else if (IsShapeSelected)
            {
                _renderingService.RenderShape(SelectedShape, "MODIFY");
                Debug.WriteLine(SelectedShape.IsSelected);
                //SelectedShape = null;
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

        private void OnShapeReceived(IShape shape, bool addToUndo)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                shape.IsSelected = false;
                Shapes.Add(shape);
                var newShape = shape.Clone();
                _networkingService._synchronizedShapes.Add(newShape);
                if(addToUndo){
                    _undoRedoService.RemoveLastModified(_networkingService, shape);
                }
            });
        }

        private void OnShapeModified(IShape shape)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                shape.IsSelected = false;
                Shapes.Add(shape);
                var newShape = shape.Clone();
                _networkingService._synchronizedShapes.Add(newShape);
                _undoRedoService.RemoveLastModified(_networkingService, shape);
            });
        }



        private void OnShapeDeleted(IShape shape)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var s in Shapes)
                {
                    if (s.ShapeId == shape.ShapeId)
                    {
                        Shapes.Remove(s);
                        break;
                    }
                }
                _networkingService._synchronizedShapes.Remove(shape);
            });
        }

        private void OnShapeClear()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Shapes.Clear();
                _undoRedoService.RedoList.Clear();
                _undoRedoService.UndoList.Clear();
                _networkingService._synchronizedShapes.Clear();
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
            if ((_currentTextboxModel != null ))
            {
                if (!string.IsNullOrEmpty(_currentTextboxModel.Text))
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
                    _renderingService.RenderShape(textShape, "CREATE");
                }
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

        public IShape HoveredShape
        {
            get => _hoveredShape;
            set
            {
                if (_hoveredShape != value)
                {
                    _hoveredShape = value;
                    OnPropertyChanged(nameof(HoveredShape));
                    OnPropertyChanged(nameof(HoveredShapeDetails));
                }
            }
        }

        public string HoveredShapeDetails
        {
            get
            {
                if (HoveredShape == null) return string.Empty;
                string colorHex = HoveredShape.Color.ToString();
                // Customize the details based on the shape type
                string details =
                                 $"Created By: {HoveredShape.UserID}\n" +
                                 $"Last Modified By: {HoveredShape.LastModifierID}\n";
                return details;
            }
        }

        public bool IsShapeHovered
        {
            get => _isShapeHovered;
            set
            {
                if (_isShapeHovered != value)
                {
                    _isShapeHovered = value;
                    OnPropertyChanged(nameof(IsShapeHovered));
                }
            }
        }
    }
}