using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteboardGUI.Models;
using System.Diagnostics;
using WhiteboardGUI.ViewModel;
using System.Threading.Tasks.Dataflow;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;


namespace WhiteboardGUI
{
    public partial class MainPage : Page
    {
        private enum Tool { Pencil, Line, Circle, Text, Select }
        private Tool currentTool = Tool.Pencil;
        private Point startPoint;
        private Line currentLine;
        private Ellipse currentEllipse;
        private Polyline currentPolyline;
        private TextBlock currentTextBlock;
        private TextBox currentTextBox;
        private List<UIElement> shapes = new List<UIElement>();
        //private List<IShape> synchronizedShapes = new List<IShape>(); // Keeps track of all shapes on the whiteboard
        private Dictionary<UIElement, IShape?> uiElementToShapeId = new Dictionary<UIElement, IShape?>();
        private Brush selectedColor = Brushes.Black;
        //private TcpClient client;
        private List<UIElement> selectedShapes = new List<UIElement>();
        private Rectangle selectionRectangle;
        private bool isSelecting;
        private Rectangle boundingBox;
        private bool isDragging;
        private Point clickPosition; // Declare this variable at the class level
        //private TcpListener listener;
        //private ConcurrentDictionary<double, TcpClient> clients = new();
        private bool isServerRunning = false;
        //private double clientID;

        int cuserID = 0;
        public MainPage()
        {
            InitializeComponent();
            drawingCanvas.MouseDown += Canvas_MouseDown;
            drawingCanvas.MouseMove += Canvas_MouseMove;
            drawingCanvas.MouseUp += Canvas_MouseUp;

            try
            {
                // Create the ViewModel and set as data context.
                MainPageViewModel viewModel = new();
                DataContext = viewModel;
                viewModel.ShapeReceived += OnShapeReceived; // Subscribe to the event
                viewModel.ShapeDeleted += OnShapeDeleted;
            }
            catch (Exception exception)
            {
                _ = MessageBox.Show(exception.Message);
                Application.Current.Shutdown();
            }
        }

        private void OnShapeDeleted(IShape shape)
        {
            Dispatcher.Invoke(() => RemoveShape(shape));
        }

        private void RemoveShape(IShape shape)
        {
            foreach (var kvp in uiElementToShapeId) {

                if (uiElementToShapeId[kvp.Key].ShapeId == shape.ShapeId)
                {
                    drawingCanvas.Children.Remove(kvp.Key);
                    uiElementToShapeId.Remove(kvp.Key);
                }
            }
        }
            
        private void OnShapeReceived(IShape shape)
        {
            Debug.Write("Drawing Shape");
            Dispatcher.Invoke(() => DrawReceivedShape(shape)); // Call the method to draw the shape
        }

        private void Text_Click(object sender, RoutedEventArgs e) => currentTool = Tool.Text;
        private void Pencil_Click(object sender, RoutedEventArgs e) => currentTool = Tool.Pencil;
        private void Line_Click(object sender, RoutedEventArgs e) => currentTool = Tool.Line;
        private void Circle_Click(object sender, RoutedEventArgs e) => currentTool = Tool.Circle;
        private void Select_Click(object sender, RoutedEventArgs e) => currentTool = Tool.Select;

        private void ColorPicker_SelectionChanged(object sender, RoutedEventArgs e)
        {
            string selectedColorName = (colorPicker.SelectedItem as ComboBoxItem)?.Content.ToString();
            selectedColor = selectedColorName switch
            {
                "Red" => Brushes.Red,
                "Blue" => Brushes.Blue,
                "Green" => Brushes.Green,
                _ => Brushes.Black
            };
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(drawingCanvas);


            if (currentTool == Tool.Select)
            {
                BeginSelection(startPoint);
                isSelecting = true;
            }
            else
            {
                switch (currentTool)
                {
                    case Tool.Text:
                        CreateTextBox();
                        break;
                    case Tool.Pencil:
                        StartDrawingPolyline();
                        break;
                    case Tool.Line:
                        StartDrawingLine();
                        break;
                    case Tool.Circle:
                        StartDrawingEllipse();
                        break;
                }
            }
        }



        private void TextInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Create a TextBlock with the content of the TextBox
                if (currentTextBox != null)
                {
                    currentTextBlock = new TextBlock
                    {
                        Text = currentTextBox.Text,
                        Foreground = selectedColor,
                        FontSize = 14,
                    };

                    // Position the TextBlock at the same location as the TextBox
                    // using the Margin of the TextBox for accurate placement.
                    Canvas.SetLeft(currentTextBlock, Canvas.GetLeft(currentTextBox));
                    Canvas.SetTop(currentTextBlock, Canvas.GetTop(currentTextBox));

                    // Add the TextBlock to the Canvas and store it in the shapes list
                    drawingCanvas.Children.Add(currentTextBlock);
                    uiElementToShapeId.TryAdd(currentTextBlock, null);
                    shapes.Add(currentTextBlock);
                        

                    // Remove the TextBox after confirming input
                    drawingCanvas.Children.Remove(currentTextBox);
                    currentTextBox = null;
                    FinalizeDrawing();

                }
            }
            else if (e.Key == Key.Escape)
            {
                // Remove the TextBox if Escape is pressed
                if (currentTextBox != null)
                {
                    drawingCanvas.Children.Remove(currentTextBox);
                    currentTextBox = null;
                }
            }
        }



        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point endPoint = e.GetPosition(drawingCanvas);

            if (isSelecting)
            {
                UpdateSelectionRectangle(endPoint);
            }
            //else if (isDragging && boundingBox != null)
            //{
            //    // Move the bounding box and its contents
            //    double offsetX = endPoint.X - startPoint.X;
            //    double offsetY = endPoint.Y - startPoint.Y;
            //    Canvas.SetLeft(boundingBox, Canvas.GetLeft(boundingBox) + offsetX);
            //    Canvas.SetTop(boundingBox, Canvas.GetTop(boundingBox) + offsetY);

            //    foreach (var shape in selectedShapes)
            //    {
            //        Canvas.SetLeft(shape, Canvas.GetLeft(shape) + offsetX);
            //        Canvas.SetTop(shape, Canvas.GetTop(shape) + offsetY);
            //    }

            //    startPoint = endPoint;
            //}
            else
            {
                switch (currentTool)
                {
                    case Tool.Pencil:
                        currentPolyline?.Points.Add(endPoint);
                        break;
                    case Tool.Line:
                        if (currentLine != null)
                        {
                            currentLine.X2 = endPoint.X;
                            currentLine.Y2 = endPoint.Y;
                        }
                        break;
                    case Tool.Circle:
                        UpdateEllipse(endPoint);
                        break;
                }
            }
        }

        

        private void CreateTextBox()
        {
            currentTextBox = new TextBox
            {
                Width = 100,
                Height = 30
            };

            // Add the TextBox to the Canvas
            drawingCanvas.Children.Add(currentTextBox);

            // Set the absolute position using Canvas.SetLeft and Canvas.SetTop
            Canvas.SetLeft(currentTextBox, startPoint.X);
            Canvas.SetTop(currentTextBox, startPoint.Y);

            // Set focus and handle the KeyDown event
            currentTextBox.Focus();
            currentTextBox.KeyDown += TextInput_KeyDown;
        }


        private void StartDrawingPolyline()
        {
            currentPolyline = new Polyline
            {
                Stroke = selectedColor,
                StrokeThickness = 2,
            };
            currentPolyline.Points.Add(startPoint);
            drawingCanvas.Children.Add(currentPolyline);
            uiElementToShapeId.TryAdd(currentPolyline, null);
            shapes.Add(currentPolyline);
        }

        private void StartDrawingLine()
        {
            currentLine = new Line
            {
                Stroke = selectedColor,
                StrokeThickness = 2,
                X1 = startPoint.X,
                Y1 = startPoint.Y
            };
            drawingCanvas.Children.Add(currentLine);
            uiElementToShapeId.TryAdd(currentLine, null);
            shapes.Add(currentLine);    
        }

        private void StartDrawingEllipse()
        {
            currentEllipse = new Ellipse
            {
                Stroke = selectedColor,
                StrokeThickness = 2
            };
            Canvas.SetLeft(currentEllipse, startPoint.X);
            Canvas.SetTop(currentEllipse, startPoint.Y);
            drawingCanvas.Children.Add(currentEllipse);
           
            uiElementToShapeId.TryAdd(currentEllipse, null);
            shapes.Add(currentEllipse) ;
        }

        private void UpdateEllipse(Point endPoint)
        {
            if (currentEllipse != null)
            {
                double radiusX = Math.Abs(endPoint.X - startPoint.X);
                double radiusY = Math.Abs(endPoint.Y - startPoint.Y);
                currentEllipse.Width = 2 * radiusX;
                currentEllipse.Height = 2 * radiusY;
                Canvas.SetLeft(currentEllipse, Math.Min(startPoint.X, endPoint.X));
                Canvas.SetTop(currentEllipse, Math.Min(startPoint.Y, endPoint.Y));
            }
        }

        private void FinalizeDrawing()
        {
            currentLine = null;
            currentEllipse = null;
            currentPolyline = null;

            var lastShape = shapes.LastOrDefault();
            shapes.Remove(lastShape);
            if (lastShape is UIElement)
            {

                IShape shapeToSend = ConvertToShapeObject(lastShape);
                shapeToSend.ShapeId = Guid.NewGuid();
                uiElementToShapeId[lastShape] = shapeToSend;
                AddSynchronizedShape(shapeToSend);
                //string serializedShape = SerializeShape(shapeToSend);
                //Console.WriteLine("Broadcasting shape data: " + serializedShape);

                MainPageViewModel? viewModel = DataContext as MainPageViewModel;
                _ = Task.Run(() => viewModel.SerilaizeAndBroadcastShapeData(shapeToSend));
                // Broadcast to all clients

            }
        }

        private void AddSynchronizedShape(IShape shapeToSend)
        {
            MainPageViewModel? viewModel = DataContext as MainPageViewModel;
            _ = Task.Run(() => viewModel.synchronizedShapes.Add(shapeToSend));
        }

        private void RemoveSynchronizedShape(IShape shapeToSend)
        {
            MainPageViewModel? viewModel = DataContext as MainPageViewModel;
            _ = Task.Run(() => viewModel.synchronizedShapes.Remove(shapeToSend));
        }



        private void BeginSelection(Point start)
        {
            selectionRectangle = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromArgb(70, 0, 120, 215))
            };
            Canvas.SetLeft(selectionRectangle, start.X);
            Canvas.SetTop(selectionRectangle, start.Y);
            drawingCanvas.Children.Add(selectionRectangle);
        }

        private void UpdateSelectionRectangle(Point endPoint)
        {
            if (selectionRectangle != null)
            {
                double left = Math.Min(startPoint.X, endPoint.X);
                double top = Math.Min(startPoint.Y, endPoint.Y);
                double width = Math.Abs(endPoint.X - startPoint.X);
                double height = Math.Abs(endPoint.Y - startPoint.Y);
                Canvas.SetLeft(selectionRectangle, left);
                Canvas.SetTop(selectionRectangle, top);
                selectionRectangle.Width = width;
                selectionRectangle.Height = height;
            }
        }

        //private void EndSelection()
        //{
        //    Rect selectionBounds = new Rect(Canvas.GetLeft(selectionRectangle), Canvas.GetTop(selectionRectangle), selectionRectangle.Width, selectionRectangle.Height);
        //    selectedShapes.Clear();

        //    foreach (var shape in shapes)
        //    {
        //        if (shape is Shape s && s != selectionRectangle)
        //        {
        //            Rect shapeBounds = new Rect(Canvas.GetLeft(s), Canvas.GetTop(s), s.Width, s.Height);
        //            if (selectionBounds.IntersectsWith(shapeBounds))
        //            {
        //                selectedShapes.Add(s);
        //                // No color change for selected shapes
        //            }
        //        }
        //    }

        //    drawingCanvas.Children.Remove(selectionRectangle);
        //    selectionRectangle = null;

        //    if (selectedShapes.Any())
        //    {
        //        CreateBoundingBox();
        //    }
        //}
        private void EndSelection()
        {
            Rect selectionBounds = new(Canvas.GetLeft(selectionRectangle), Canvas.GetTop(selectionRectangle), selectionRectangle.Width, selectionRectangle.Height);
            selectedShapes.Clear();

            foreach (var kvp in uiElementToShapeId)
            {
                if (kvp.Key is UIElement uiElement)
                {
                    Rect shapeBounds = VisualTreeHelper.GetDescendantBounds(uiElement);
                    GeneralTransform transform = uiElement.TransformToVisual(drawingCanvas);
                    Rect transformedBounds = transform.TransformBounds(shapeBounds);

                    if (selectionBounds.IntersectsWith(transformedBounds))
                    {
                        selectedShapes.Add(uiElement);
                    }
                }
            }

            drawingCanvas.Children.Remove(selectionRectangle);
            selectionRectangle = null;

            if (selectedShapes.Any())
            {
                CreateBoundingBox();
            }
        }
        private void CreateBoundingBox()
        {
            if (boundingBox != null)
            {
                drawingCanvas.Children.Remove(boundingBox);
            }

            // Calculate bounds based on selected shapes
            double minX = selectedShapes.Min(s => Canvas.GetLeft(s));
            double minY = selectedShapes.Min(s => Canvas.GetTop(s));
            double maxX = selectedShapes.Max(s => Canvas.GetLeft(s) + (s is Shape shape ? shape.Width : 0));
            double maxY = selectedShapes.Max(s => Canvas.GetTop(s) + (s is Shape shape ? shape.Height : 0));

            boundingBox = new Rectangle
            {
                Stroke = Brushes.Gray,
                StrokeThickness = 1,
                Fill = Brushes.Transparent // No fill color
            };

            Canvas.SetLeft(boundingBox, minX);
            Canvas.SetTop(boundingBox, minY);
            boundingBox.Width = maxX - minX;
            boundingBox.Height = maxY - minY;

            drawingCanvas.Children.Add(boundingBox);

            // Modify the event handlers to capture and release the mouse
            boundingBox.MouseLeftButtonDown += BoundingBox_MouseLeftButtonDown;
            boundingBox.MouseMove += BoundingBox_MouseMove;
            boundingBox.MouseLeftButtonUp += BoundingBox_MouseLeftButtonUp;
        }

        private void BoundingBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            startPoint = e.GetPosition(drawingCanvas);
            boundingBox.CaptureMouse(); // Capture the mouse
            e.Handled = true; // Prevent event bubbling
        }

        private void BoundingBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point currentPoint = e.GetPosition(drawingCanvas);
                double offsetX = currentPoint.X - startPoint.X;
                double offsetY = currentPoint.Y - startPoint.Y;

                // Move the bounding box
                Canvas.SetLeft(boundingBox, Canvas.GetLeft(boundingBox) + offsetX);
                Canvas.SetTop(boundingBox, Canvas.GetTop(boundingBox) + offsetY);

                // Move the selected shapes
                foreach (var shape in selectedShapes)
                {
                    Canvas.SetLeft(shape, Canvas.GetLeft(shape) + offsetX);
                    Canvas.SetTop(shape, Canvas.GetTop(shape) + offsetY);
                }

                startPoint = currentPoint;
                e.Handled = true; // Prevent event bubbling
            }
        }

        private void BoundingBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                boundingBox.ReleaseMouseCapture(); // Release the mouse
                e.Handled = true; // Prevent event bubbling

                // After dragging ends, broadcast the movement
                if (selectedShapes.Count == 1)
                {
                    var shape = selectedShapes[0];
                    var shapeId = uiElementToShapeId[shape].ShapeId;
                    IShape shapeToSend = ConvertToShapeObject(shape);
                    shapeToSend.ShapeId = shapeId;
                    BroadcastShapeModify(shapeToSend);
                }
                else if (selectedShapes.Count > 1)
                {
                    foreach (var shape in selectedShapes)
                    {
                        var shapeId = uiElementToShapeId[shape].ShapeId;
                        IShape shapeToSend = ConvertToShapeObject(shape);
                        shapeToSend.ShapeId = shapeId;
                        BroadcastShapeModify(shapeToSend);
                    }
                }
            }
        }
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isSelecting)
            {
                EndSelection();
                isSelecting = false;
            }
            else if (isDragging)
            {
                isDragging = false;
                if (boundingBox != null)
                {
                    boundingBox.ReleaseMouseCapture(); // Ensure mouse capture is released
                }

                // Broadcast movement if necessary
                if (selectedShapes.Count == 1)
                {
                    var shape = selectedShapes[0];
                    IShape shapeToSend = ConvertToShapeObject(shape);
                    BroadcastShapeModify(shapeToSend);
                }
                else if (selectedShapes.Count > 1)
                {
                    foreach (var shape in selectedShapes)
                    {
                        IShape shapeToSend = ConvertToShapeObject(shape);
                        //BroadcastShapeMovement(shapeToSend);
                    }
                }
            }
            else
            {
                FinalizeDrawing();
            }
        }
        //private void CreateBoundingBox()
        //{
        //    if (boundingBox != null)
        //    {
        //        drawingCanvas.Children.Remove(boundingBox);
        //    }

        //    // Calculate bounds based on selected shapes
        //    double minX = selectedShapes.Min(s => Canvas.GetLeft(s));
        //    double minY = selectedShapes.Min(s => Canvas.GetTop(s));
        //    double maxX = selectedShapes.Max(s => Canvas.GetLeft(s) + (s is Shape shape ? shape.Width : 0));
        //    double maxY = selectedShapes.Max(s => Canvas.GetTop(s) + (s is Shape shape ? shape.Height : 0));

        //    boundingBox = new Rectangle
        //    {
        //        Stroke = Brushes.Gray,
        //        StrokeThickness = 1,
        //        Fill = Brushes.Transparent // No fill color
        //    };

        //    Canvas.SetLeft(boundingBox, minX);
        //    Canvas.SetTop(boundingBox, minY);
        //    boundingBox.Width = maxX - minX;
        //    boundingBox.Height = maxY - minY;

        //    drawingCanvas.Children.Add(boundingBox);

        //    boundingBox.MouseLeftButtonDown += (s, e) => isDragging = true;
        //    boundingBox.MouseMove += (s, e) =>
        //    {
        //        if (isDragging)
        //        {
        //            Point pos = e.GetPosition(drawingCanvas);
        //            double offsetX = pos.X - (boundingBox.Width / 2);
        //            double offsetY = pos.Y - (boundingBox.Height / 2);
        //            Canvas.SetLeft(boundingBox, offsetX);
        //            Canvas.SetTop(boundingBox, offsetY);
        //        }
        //    };
        //    boundingBox.MouseLeftButtonUp += (s, e) => isDragging = false;
        //}




        private void DeleteSelectedShapes()
        {
            foreach (var shape in selectedShapes.ToList())
            {
                drawingCanvas.Children.Remove(shape);
                IShape shapeToDelete = uiElementToShapeId[shape];
                BroadcastShapeDeletion(shapeToDelete);
                uiElementToShapeId.Remove(shape);
                RemoveSynchronizedShape(shapeToDelete);
            }
            selectedShapes.Clear();
            drawingCanvas.Children.Remove(boundingBox);
            boundingBox = null;
        }

        
        private void BroadcastShapeDeletion(IShape shapeToDelete)
        {
            MainPageViewModel? viewModel = DataContext as MainPageViewModel;
            string serializedShape = SerializeShape(shapeToDelete);
            string deletionMessage = $"DELETE:{serializedShape}";
            _ = Task.Run(() => viewModel.BroadcastShapeData(deletionMessage, -1));
        }

        private void BroadcastShapeModify(IShape shapeToDelete)
        {
            MainPageViewModel? viewModel = DataContext as MainPageViewModel;
            string serializedShape = SerializeShape(shapeToDelete);
            string modifyMessage = $"MODIFY:{serializedShape}";
            _ = Task.Run(() => viewModel.BroadcastShapeData(modifyMessage, -1));
        }


        private string SerializeShape(IShape shape)
        {
            // Serialize the shape object to JSON format
            //return Newtonsoft.Json.JsonConvert.SerializeObject(shape);
            return JsonConvert.SerializeObject(shape);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedShapes();
        }



        private IShape ConvertToShapeObject(UIElement element)
        {
            switch (element)
            {
                case Polyline polyline:
                    var scribbleShape = new ScribbleShape
                    {
                        Color = selectedColor.ToString(),
                        StrokeThickness = polyline.StrokeThickness,
                        Points = polyline.Points.Select(p => new System.Drawing.Point((int)p.X, (int)p.Y)).ToList(),
                    };
                    return scribbleShape;

                case Line line:
                    return new LineShape
                    {
                        StartX = line.X1,
                        StartY = line.Y1,
                        EndX = line.X2,
                        EndY = line.Y2,
                        Color = selectedColor.ToString(),
                        StrokeThickness = line.StrokeThickness
                    };

                case Ellipse ellipse:
                    return new CircleShape
                    {
                        CenterX = Canvas.GetLeft(ellipse),
                        CenterY = Canvas.GetTop(ellipse),
                        RadiusX = ellipse.Width / 2,
                        RadiusY = ellipse.Height / 2,
                        Color = selectedColor.ToString(),
                        StrokeThickness = ellipse.StrokeThickness
                    };

                case TextBlock textBlock:
                    return new TextShape
                    {
                        Text = textBlock.Text,
                        X = Canvas.GetLeft(textBlock),
                        Y = Canvas.GetTop(textBlock),
                        Color = textBlock.Foreground.ToString(),
                        FontSize = textBlock.FontSize,
                    };
                default:
                    throw new NotSupportedException("Shape type not supported");
            }
        }

        private void HostCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (isServerRunning)
            {
                // If a server is already running, show an error message
                MessageBox.Show("A server is already running. You cannot start another server.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Uncheck the checkbox to indicate the action was not successful
                ((CheckBox)sender).IsChecked = false;
                return;
            }

            isServerRunning = true; // Set the flag indicating the server is starting

            MainPageViewModel? viewModel = DataContext as MainPageViewModel;
            _ = Task.Run(() => viewModel.StartHost());
            Debug.WriteLine("Host started");
        }

        private void ClientCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            int port = 5000; // Ensure this matches the Host port
            MainPageViewModel? viewModel = DataContext as MainPageViewModel;
            _ = Task.Run(() => viewModel.StartClient(port));
            Debug.WriteLine("Client started");
        }

        private void HostCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            isServerRunning = false; // Reset the flag when the server is stopped
            MainPageViewModel? viewModel = DataContext as MainPageViewModel;
            _ = Task.Run(() => viewModel.HostCheckBox_Unchecked());
            //listener?.Stop();
            //clients.Clear();
            StatusTextBlock.Text = "Host stopped";
        }

        private void ClientCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            MainPageViewModel? viewModel = DataContext as MainPageViewModel;
            _ = Task.Run(() => viewModel.ClientCheckBox_Unchecked());
            StatusTextBlock.Text = "Disconnected from Host";
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Start the Host when the window loads
            MainPageViewModel? viewModel = DataContext as MainPageViewModel;
            _ = viewModel.StartHost();
        }

      
        private void DrawReceivedShape(IShape shape)
        {
            AddSynchronizedShape(shape);
            
            switch (shape)
            {
                case CircleShape circle:
                    Ellipse ellipse = new Ellipse
                    {
                        Width = 2 * circle.RadiusX,
                        Height = 2 * circle.RadiusY,
                        Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(circle.Color)),
                        StrokeThickness = circle.StrokeThickness
                    };
                    Canvas.SetLeft(ellipse, circle.CenterX);
                    Canvas.SetTop(ellipse, circle.CenterY);
                    drawingCanvas.Children.Add(ellipse);
                    uiElementToShapeId.TryAdd(ellipse, shape);
                    break;

                case LineShape line:
                    Line lineShape = new Line
                    {
                        X1 = line.StartX,
                        Y1 = line.StartY,
                        X2 = line.EndX,
                        Y2 = line.EndY,
                        Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(line.Color)),
                        StrokeThickness = line.StrokeThickness
                    };
                    drawingCanvas.Children.Add(lineShape);
                    uiElementToShapeId.TryAdd(lineShape, shape);
                    break;

                case ScribbleShape scribble:
                    Polyline polyline = new Polyline
                    {
                        Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(scribble.Color)),
                        StrokeThickness = scribble.StrokeThickness
                    };
                    foreach (var point in scribble.Points)
                    {
                        polyline.Points.Add(new System.Windows.Point(point.X, point.Y));
                    }

                    drawingCanvas.Children.Add(polyline);
                    uiElementToShapeId.TryAdd(polyline, shape);
                    break;

                case TextShape textShape:
                    TextBlock textBlock = new TextBlock
                    {
                        Text = textShape.Text,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(textShape.Color)),
                        FontSize = textShape.FontSize
                    };
                    Canvas.SetLeft(textBlock, textShape.X);
                    Canvas.SetTop(textBlock, textShape.Y);
                    drawingCanvas.Children.Add(textBlock);
                    uiElementToShapeId.TryAdd(textBlock, shape);
                    break;
            }
        }

        // Event handler for Clear Button Click
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            drawingCanvas.Children.Clear();
        }
    }
}
