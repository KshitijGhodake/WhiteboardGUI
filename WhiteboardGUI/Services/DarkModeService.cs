using System;
using System.Collections.ObjectModel;
using WhiteboardGUI.Models;
using System.Windows.Media;
using System.Diagnostics;

namespace WhiteboardGUI.Services
{
    public class DarkModeService : IDarkModeService
    {
        public void ToggleDarkMode(bool isDarkMode, ObservableCollection<IShape> shapes)
        {
            foreach (var shape in shapes)
            {
                if (shape is ShapeBase shapeBase)
                {
                    Debug.WriteLine($"color of the shape is {shapeBase.Color}");
                    // Swap black to white and white to black
                    if (shapeBase.Color == "#FFFFFFFF")
                    {
                        if (isDarkMode)
                        {
                            shapeBase.Color = "#FFFFFFFF";
                        }
                        else
                        {
                            shapeBase.Color = "#FF000000";
                        }
          
                    }
                    else if (shapeBase.Color == "#FF000000")
                    {
                        if (isDarkMode)
                        {
                            shapeBase.Color = "#FFFFFFFF";
                        }
                        else
                        {
                            shapeBase.Color = "#FF000000";
                        }
                    }
                    // Optionally, handle other colors or invert colors
                    //else
                    //{
                    //    // Example: Invert the color
                    //    Color originalColor = (Color)ColorConverter.ConvertFromString(shapeBase.Color);
                    //    Color invertedColor = Color.FromRgb((byte)(255 - originalColor.R), (byte)(255 - originalColor.G), (byte)(255 - originalColor.B));
                    //    shapeBase.Color = isDarkMode ? invertedColor.ToString() : originalColor.ToString();
                    //}

                    // Notify property changed if necessary
                    shapeBase.OnPropertyChanged(shapeBase.Color);
                }
            }
        }
    }
}
