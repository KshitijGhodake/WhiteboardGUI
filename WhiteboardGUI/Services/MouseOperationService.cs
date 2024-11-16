using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using WhiteboardGUI.Models;
using System.Diagnostics;
using System.Windows.Shapes;
using WhiteboardGUI.ViewModel;
using System.ComponentModel;


namespace WhiteboardGUI.Services
{
    public class MouseOperationService
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void HandleCanvasLeftMouseDown(MouseButtonEventArgs e, MainPageViewModel viewModel)
        {
            if (viewModel.IsTextBoxActive)
            {
                viewModel.FinalizeTextBox();
            }

            var canvas = e.Source as FrameworkElement;
            if (canvas != null)
            {
                viewModel.StartPoint = e.GetPosition(canvas);

                if (viewModel.CurrentTool == ShapeType.Select)
                {
                    viewModel.IsSelecting = true;
                    foreach (var shape in viewModel.Shapes.Reverse())
                    {
                        if (viewModel.IsPointOverShape(shape, viewModel.StartPoint))
                        {
                            viewModel.SelectedShape = shape;
                            Debug.WriteLine(shape.IsSelected);
                            viewModel.LastMousePosition = viewModel.StartPoint;
                            viewModel.IsSelecting = true;
                            break;
                        }
                        else
                        {
                            viewModel.IsSelecting = false;
                            viewModel.SelectedShape = null;
                        }
                    }
                }
                else if (viewModel.CurrentTool == ShapeType.Text)
                {
                    var position = e.GetPosition((IInputElement)e.Source);
                    var textboxModel = new TextboxModel
                    {
                        X = position.X,
                        Y = position.Y,
                        Width = 150,
                        Height = 30,
                    };

                    viewModel.CurrentTextboxModel = textboxModel;
                    viewModel.TextInput = string.Empty;
                    viewModel.IsTextBoxActive = true;
                    viewModel.Shapes.Add(textboxModel);
                    OnPropertyChanged(nameof(viewModel.TextBoxVisibility));
                }
                else
                {
                    // Start drawing a new shape
                    IShape newShape = viewModel.CreateShape(viewModel.StartPoint);
                    if (newShape != null)
                    {
                        viewModel.Shapes.Add(newShape);
                        viewModel.SelectedShape = newShape;
                    }
                }
            }
        }

        protected void OnPropertyChanged(string propertyName) =>
           PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

