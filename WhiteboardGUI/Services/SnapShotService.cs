using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using WhiteboardGUI.Models;

namespace WhiteboardGUI.Services
{
    public class SnapShotService
    {
        private String CloudSave;
        private readonly NetworkingService _networkingService;
        private readonly RenderingService _renderingService;
        private readonly UndoRedoService _undoRedoService;
        private ObservableCollection<IShape> Shapes;
        private Dictionary<string,string> Snaps = new();
        public event Action OnSnapShotUploaded;
        //Max Snap
        int limit = 5;

        public SnapShotService(NetworkingService networkingService, RenderingService renderingService, ObservableCollection<IShape> shapes, UndoRedoService undoRedoService)
        {
            _networkingService = networkingService;
            _renderingService = renderingService;
            _undoRedoService = undoRedoService;
            Shapes = shapes;
        }

        public async Task UploadSnapShot(string snapShotFileName, ObservableCollection<IShape> shapes)
        {
            ObservableCollection<string> tempSnapList;
            await Task.Run(async () =>
            {
                // Validate the filename or trigger a save operation
                snapShotFileName = parseSnapShotName(snapShotFileName);
                Debug.WriteLine($"Uploading snapshot '{snapShotFileName}' with {shapes.Count} shapes.");
                //Thread.Sleep(5000);
                sendToCloud(snapShotFileName, shapes);

                MessageBox.Show($"Filename '{snapShotFileName}' has been set.", "Filename Set", MessageBoxButton.OK);
                // Perform the upload operation here (e.g., using HttpClient for HTTP requests)
            });
            System.Windows.Application.Current.Dispatcher.Invoke(() => OnSnapShotUploaded?.Invoke());
            Debug.WriteLine("Upload completed.");
            


            // Close the popup after submission
            

            
        }

        private ObservableCollection<string> sendToCloud(string snapShotFileName, ObservableCollection<IShape> shapes)
        {
            CheckLimit();
            CloudSave = SerializationService.SerializeShapes(shapes);
            Snaps.Add(snapShotFileName, CloudSave);
            Debug.WriteLine(CloudSave);
            return new ObservableCollection<string>(Snaps.Keys);
        }

        private void CheckLimit()
        { 
            while (Snaps.Count >= limit)
            {
                String lastSnapName = findLastSnap();
                deleteSnap(lastSnapName);
            }
        }

        private void deleteSnap(string lastSnapName)
        {
            Snaps.Remove(lastSnapName);
        }

        private string findLastSnap()
        {
            return Snaps.Keys.OrderBy(key =>
            {
                // Split the key and parse the epoch time
                string[] parts = key.Split('_');
                if (parts.Length < 3)
                    return long.MaxValue; // Invalid format, place at the end

                if (long.TryParse(parts[2], out long epochTime))
                    return epochTime;

                return long.MaxValue; // If parsing fails, place at the end
            }).FirstOrDefault();
        }

        private string parseSnapShotName(string snapShotFileName)
        {
            Debug.WriteLine("Current Name:" + snapShotFileName);
            if (string.IsNullOrWhiteSpace(snapShotFileName))
            {
                DateTime currentDateTime = DateTime.Now;
                snapShotFileName = currentDateTime.ToString("yyyyMMdd:HHmmss");
            }
            DateTimeOffset currentDateTimeEpoch = DateTimeOffset.UtcNow;
            long epochTime = currentDateTimeEpoch.ToUnixTimeSeconds();
            snapShotFileName = _networkingService._clientID + "_" + snapShotFileName + "_" + epochTime.ToString();

            return snapShotFileName;
        }

        public ObservableCollection<string> getSnaps(string v)
        {
            return new ObservableCollection<string>(Snaps.Keys);
        }

        internal void DownloadSnapShot(string selectedDownloadItem)
        {
            ObservableCollection<IShape> snapShot = getSnapShot(selectedDownloadItem);
            _renderingService.RenderShape(null, "CLEAR");
            addShapes(snapShot);
            _undoRedoService.RedoList.Clear();
            _undoRedoService.UndoList.Clear();
        }

        private void addShapes(ObservableCollection<IShape> snapShot)
        {
            foreach (IShape shape in snapShot)
            {
                Shapes.Add(shape);
                _renderingService.RenderShape(shape, "DOWNLOAD");
                Debug.WriteLine($"Added Shape {shape.GetType}");
            }
        }

        private ObservableCollection<IShape> getSnapShot(string selectedDownloadItem)
        {

            ObservableCollection<IShape> deserialized = SerializationService.DeserializeShapes(Snaps[selectedDownloadItem]);
            return deserialized;
        }
    }
}
