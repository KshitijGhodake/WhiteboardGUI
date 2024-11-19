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
using System.Collections.Specialized;
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
        private readonly MoveShapeZIndexing _moveShapeZIndexing;
        private string _defaultColor;
        public string DefaultColor
        {
            get => _defaultColor;
            set
            {
                if (_defaultColor != value)
                {
                    _defaultColor = value;
                    OnPropertyChanged(nameof(DefaultColor));
                }
            }
        }
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

        //private bool _isDarkMode;
        //public bool IsDarkMode
        //{
        //    get => _isDarkMode;
        //    set
        //    {
        //        if (_isDarkMode != value)
        //        {
        //            _isDarkMode = value;
        //            OnPropertyChanged(nameof(IsDarkMode));
        //        }
        //    }
        //}


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

        private Color _currentColor = Colors.Black;
        public Color CurrentColor
        {
            get => _currentColor;
            set
            {
                if (_currentColor != value)
                {
                    _currentColor = value;
                    OnPropertyChanged(nameof(CurrentColor));
                }
            }
        }

        private void Shapes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _moveShapeZIndexing.UpdateZIndices();
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
        public ICommand CanvasLeftMouseDownCommand { get; }
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
        public ICommand SendBackwardCommand { get; }
        public ICommand SendToBackCommand { get; }
        public ICommand EditTextCommand { get; }


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
            _moveShapeZIndexing = new MoveShapeZIndexing(Shapes);

            DownloadItems = new ObservableCollection<string>();
            InitializeDownloadItems();
            _snapShotService.OnSnapShotUploaded += RefreshDownloadItems;

            // Subscribe to networking events
            _networkingService.ShapeReceived += OnShapeReceived;
            _networkingService.ShapeDeleted += OnShapeDeleted;
            _networkingService.ShapeModified += OnShapeModified;
            _networkingService.ShapesClear += OnShapeClear;
            _networkingService.ShapeSendBackward += _moveShapeZIndexing.MoveShapeBackward;
            _networkingService.ShapeSendToBack += _moveShapeZIndexing.MoveShapeBack;

            Shapes.CollectionChanged += Shapes_CollectionChanged;

            // Initialize DarkModeService
            //_darkModeService = new DarkModeService();

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
            CanvasLeftMouseDownCommand = new RelayCommand<MouseButtonEventArgs>(OnCanvasLeftMouseDown);
            CanvasMouseMoveCommand = new RelayCommand<MouseEventArgs>(OnCanvasMouseMove);
            CanvasMouseUpCommand = new RelayCommand<MouseButtonEventArgs>(OnCanvasMouseUp);
            // Initialize commands
            FinalizeTextBoxCommand = new RelayCommand(FinalizeTextBox);
            CancelTextBoxCommand = new RelayCommand(CancelTextBox);
            UndoCommand = new RelayCommand(CallUndo);
            RedoCommand = new RelayCommand(CallRedo);
            SelectColorCommand = new RelayCommand<string>(SelectColor);

            // Right-Click Menu Commands
            SendBackwardCommand = new RelayCommand<ShapeBase>(SendBackward);
            SendToBackCommand = new RelayCommand<ShapeBase>(SendToBack);
            EditTextCommand = new RelayCommand<ShapeBase>(EditText);

            SubmitCommand = new RelayCommand(async () => await SubmitFileName());
            OpenDownloadPopupCommand = new RelayCommand(OpenDownloadPopup);
            DownloadItemCommand = new RelayCommand(DownloadSelectedItem, () => CanDownload);

            OpenPopupCommand = new RelayCommand(OpenPopup);
            ClearShapesCommand = new RelayCommand(ClearShapes);

            //Initialize Dark Mode to Light
             IsDarkMode = false;
            DefaultColor = "Black";
            Red = 0;
            Green = 0;
            Blue = 0;
            UpdateSelectedColor();

        }

        //// Toggle Dark Mode
        //private void ToggleDarkMode(bool isDarkMode)
        //{
        //    _darkModeService.ToggleDarkMode(isDarkMode, Shapes);
        //    UpdateBackground(isDarkMode);
        //}

        // Update Background Colors
        private void UpdateBackground(bool isDarkMode)
        {
            if (isDarkMode)
            {
                PageBackground = new SolidColorBrush(Color.FromRgb(30, 30, 30)); // Dark gray
                CanvasBackground = new SolidColorBrush(Color.FromRgb(50, 50, 50)); // Slightly lighter
            }
            else
            {
                PageBackground = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // Light gray
                CanvasBackground = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // White
            }
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

        private async void InitializeDownloadItems()
        {
            DownloadItems.Clear();
            ObservableCollection<string> newSnaps = await _snapShotService.getSnaps("a",true);
            foreach (var snap in newSnaps)
            {
                DownloadItems.Add(snap);
            }

            OnPropertyChanged(nameof(DownloadItems));
        }
        private async void RefreshDownloadItems()
        {
            DownloadItems.Clear();
            ObservableCollection<string> newSnaps = await _snapShotService.getSnaps("a",false);
            foreach (var snap in newSnaps)
            {
                DownloadItems.Add(snap);
            }

            OnPropertyChanged(nameof(DownloadItems));
        }

        //Z-Index
        private void SendBackward(IShape shape)
        {
            _moveShapeZIndexing.MoveShapeBackward(shape);
            _renderingService.RenderShape(shape, "INDEX-BACKWARD");
        }

        private void SendToBack(IShape shape)
        {
            _moveShapeZIndexing.MoveShapeBack(shape);
            _renderingService.RenderShape(shape, "INDEX-BACK");
        }

        private void UpdateSelectedColor()
        {
            SelectedColor = Color.FromRgb(Red, Green, Blue);
        }

        private void SelectColor(string colorName)
        {
            var color = (Color)ColorConverter.ConvertFromString(colorName);
            SelectedColor = color;
            if (IsDarkMode && colorName == "Black")
            {
                CurrentColor = Colors.White;
            }
            else
            {
                CurrentColor = color;
            }
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
                await _snapShotService.UploadSnapShot(SnapShotFileName, Shapes, false);
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

        private void EditText(IShape shape)
        {
            if (shape is TextShape textShape)
            {
                if (CurrentTool == ShapeType.Select)
                {

                    // Found a TextShape under the click
                    _currentTextShape = textShape;
                    IsTextBoxActive = true;
                    //Shapes.Remove(_currentTextShape);

                    _currentTextShape.Color = "#FFFFFF";
                    _currentTextShape.OnPropertyChanged(null);
                    //OnPropertyChanged(nameof(textShape.Color));
                    // Create a TextboxModel over the existing TextShape
                    var textboxModel = new TextboxModel
                    {
                        X = textShape.X,
                        Y = textShape.Y,
                        Width = textShape.Width,
                        Height = textShape.Height,
                        Text = textShape.Text,
                    };
                    _currentTextboxModel = textboxModel;
                    Shapes.Add(textboxModel);
                    OnPropertyChanged(nameof(TextBoxVisibility));
                }
            }

        }
        private void OnCanvasLeftMouseDown(MouseButtonEventArgs e)
        {
            // Pass the canvas as the element
            if (IsTextBoxActive == true)
            {
                FinalizeTextBox();
            }
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
                //Shapes.Add(shape);

                var existingShape = Shapes.FirstOrDefault(s => s.ShapeId == shape.ShapeId && s.UserID == shape.UserID); 
                
                switch (shape.ShapeType)
                {
                    case "Circle":
                        if (existingShape is CircleShape existingCircle && shape is CircleShape modifiedCircle)
                        {
                            existingCircle.LastModifierID = modifiedCircle.LastModifierID;
                                    existingCircle.Color = modifiedCircle.Color;
                                    existingCircle.StrokeThickness = modifiedCircle.StrokeThickness;

                                    // Update Circle-specific properties
                                    existingCircle.CenterX = modifiedCircle.CenterX;
                                    existingCircle.CenterY = modifiedCircle.CenterY;
                                    existingCircle.RadiusX = modifiedCircle.RadiusX;
                                    existingCircle.RadiusY = modifiedCircle.RadiusY;
                                    existingCircle.ZIndex = modifiedCircle.ZIndex;
                                }
                                break;
                    case "Line":
                        if (existingShape is LineShape existingLine && shape is LineShape modifiedLine)
                        {
                            // Update common properties
                            existingLine.LastModifierID = modifiedLine.LastModifierID;
                            existingLine.Color = modifiedLine.Color;
                            existingLine.StrokeThickness = modifiedLine.StrokeThickness;

                            // Update Line-specific properties
                            existingLine.StartX = modifiedLine.StartX;
                            existingLine.StartY = modifiedLine.StartY;
                            existingLine.EndX = modifiedLine.EndX;
                            existingLine.EndY = modifiedLine.EndY;
                            existingLine.ZIndex = modifiedLine.ZIndex;
                        }
                        break;
                    case "Scribble":
                        if (existingShape is ScribbleShape existingScribble && shape is ScribbleShape modifiedScribble)
                        {
                            // Update common properties
                            existingScribble.LastModifierID = modifiedScribble.LastModifierID;
                            existingScribble.Color = modifiedScribble.Color;
                            existingScribble.StrokeThickness = modifiedScribble.StrokeThickness;

                            // Update Scribble-specific properties
                            existingScribble.Points = new List<Point>(modifiedScribble.Points);
                            existingScribble.ZIndex = modifiedScribble.ZIndex;
                        }
                        break;
                    case "TextShape":
                        if (existingShape is TextShape existingText && shape is TextShape modifiedText)
                        {
                            // Update common properties
                            existingText.LastModifierID = modifiedText.LastModifierID;
                            existingText.Color = modifiedText.Color;
                            existingText.StrokeThickness = modifiedText.StrokeThickness;

                            // Update TextShape-specific properties
                            existingText.Text = modifiedText.Text;
                            existingText.X = modifiedText.X;
                            existingText.Y = modifiedText.Y;
                            existingText.FontSize = modifiedText.FontSize;
                            existingText.ZIndex = modifiedText.ZIndex;
                        }
                        break;
                }
                        
                var newShape = shape.Clone();
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
            _currentTextShape = null;
            OnPropertyChanged(nameof(TextBoxVisibility));
        }
        public void FinalizeTextBox()
        {

            if ((_currentTextboxModel != null))
            {
                if (!string.IsNullOrEmpty(_currentTextboxModel.Text))
                {
                    if (_currentTextShape != null)
                    {
                        // We are editing an existing TextShape
                        _currentTextShape.Text = _currentTextboxModel.Text;
                        _currentTextShape.Color = "black";
                        _currentTextShape.LastModifierID = _networkingService._clientID;
                        _currentTextShape.X = _currentTextboxModel.X;
                        _currentTextShape.Y = _currentTextboxModel.Y;


                        // Notify property changes
                        //_currentTextShape.OnPropertyChanged(nameof(TextShape.Text));
                        //_currentTextShape.OnPropertyChanged(nameof(TextShape.FontSize));
                        //_currentTextShape.OnPropertyChanged(nameof(TextShape.Color));
                        _currentTextShape.OnPropertyChanged(null);
                        _renderingService.RenderShape(_currentTextShape, "MODIFY");
                        //Shapes.Add(_currentTextShape );
                    }
                    else
                    {
                        // Create a new TextShape
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
                   
                }
                TextInput = string.Empty;
                IsTextBoxActive = false;
                Shapes.Remove(_currentTextboxModel);
                _currentTextboxModel = null;
                _currentTextShape = null;
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

        // Dark Mode Property
        private bool _isDarkMode;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnPropertyChanged(nameof(IsDarkMode));
                    //ToggleDarkMode(_isDarkMode);
                    UpdateBackground(IsDarkMode);
                    if (value == true)
                    {
                        DefaultColor = "White";
                        if (_selectedColor == Colors.Black)
                        {
                            CurrentColor = Colors.White;
                        }
                    }
                    else
                    {
                        DefaultColor = "Black";
                        if (_selectedColor == Colors.Black)
                        {
                            CurrentColor = Colors.Black;
                        }
                    }

                   
                }
            }
        }

        // Background Properties
        private Brush _pageBackground = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // Light
        public Brush PageBackground
        {
            get => _pageBackground;
            set
            {
                if (_pageBackground != value)
                {
                    _pageBackground = value;
                    OnPropertyChanged(nameof(PageBackground));
                }
            }
        }

        private Brush _canvasBackground = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // Light
        public Brush CanvasBackground
        {
            get => _canvasBackground;
            set
            {
                if (_canvasBackground != value)
                {
                    _canvasBackground = value;
                    OnPropertyChanged(nameof(CanvasBackground));
                }
            }
        }

        //private readonly IDarkModeService _darkModeService;

    }
}