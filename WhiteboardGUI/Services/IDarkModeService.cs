using System.Collections.ObjectModel;
using WhiteboardGUI.Models;

namespace WhiteboardGUI.Services
{
    public interface IDarkModeService
    {
        void ToggleDarkMode(bool isDarkMode, ObservableCollection<IShape> shapes);
    }
}
