using SECloud.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WhiteboardGUI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using SECloud.Services;
using SECloud.Models;
using System.Net.Http;
using SECloud.Interfaces;
using System.Windows.Data;

namespace WhiteboardGUI.Services
{
    public class SnapShotService
    {
        private String CloudSave;
        private readonly NetworkingService _networkingService;
        private readonly RenderingService _renderingService;
        private readonly UndoRedoService _undoRedoService;
        private ObservableCollection<IShape> Shapes;
        private Dictionary<string,ObservableCollection<IShape>> Snaps = new();
        //private Dictionary<string, string> SnapshotFilename = new();
        private List<SnapShotDownloadItem> SnapShotDownloadItems = new();
        public event Action OnSnapShotUploaded;
        private ICloud cloudService;
        //Max Snap
        int limit = 5;

        public SnapShotService(NetworkingService networkingService, RenderingService renderingService, ObservableCollection<IShape> shapes, UndoRedoService undoRedoService)
        {
            _networkingService = networkingService;
            _renderingService = renderingService;
            _undoRedoService = undoRedoService;
            Shapes = shapes;
            initializeCloudService();
        }

        private void initializeCloudService()
        {
            var serviceProvider = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            })
            .AddHttpClient()
            .BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<CloudService>>();
            var httpClient = new HttpClient(); // Simplified for testing

            // Configuration values - replace with your actual values
            var baseUrl = "https://secloudapp-2024.azurewebsites.net/api";
            var team = "whiteboard";
            var sasToken = "sp=racwdli&st=2024-11-14T21:02:09Z&se=2024-11-30T05:02:09Z&spr=https&sv=2022-11-02&sr=c&sig=tSw6pO8%2FgqiG2MgU%2FoepmRkFuuJrTerVy%2BDn91Y0WH8%3D";

            // Create CloudService instance
            cloudService = new CloudService(
                baseUrl,
                team,
                sasToken,
                httpClient,
                logger);
        }
        public async Task UploadSnapShot(string snapShotFileName, ObservableCollection<IShape> shapes, bool isTest)
        {
            await Task.Run(async () =>
            {
                // Validate the filename or trigger a save operation
                var SnapShot = new SnapShot();
                snapShotFileName = parseSnapShotName(snapShotFileName, SnapShot);
                Debug.WriteLine($"Uploading snapshot '{snapShotFileName}' with {shapes.Count} shapes.");
                //Thread.Sleep(5000);
                sendToCloud(snapShotFileName,SnapShot, shapes);
                if(!isTest) MessageBox.Show($"Filename '{snapShotFileName}' has been set.", "Filename Set", MessageBoxButton.OK);
                // Perform the upload operation here (e.g., using HttpClient for HTTP requests)
            });
            if(!isTest) System.Windows.Application.Current.Dispatcher.Invoke(() => OnSnapShotUploaded?.Invoke());
            Debug.WriteLine("Upload completed.");
            


            // Close the popup after submission
            

            
        }

        private async void sendToCloud(string snapShotFileName, SnapShot snapShot, ObservableCollection<IShape> shapes)
        {
            CheckLimit();
            snapShot.userID = _networkingService._clientID.ToString();
            snapShot.Shapes = new ObservableCollection<IShape>(shapes); 
            String SnapShotSerialized = SerializationService.SerializeSnapShot(snapShot);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(SnapShotSerialized));
            var response = await cloudService.UploadAsync(snapShotFileName+".json", stream,"application/json");
            Debug.WriteLine("RESPONSE:"+response.ToString());

            Snaps.Add(snapShotFileName, snapShot.Shapes);
            Debug.WriteLine(CloudSave);
        }

        private void CheckLimit()
        { 
            while (Snaps.Count >= limit)
            {
                SnapShotDownloadItem lastSnapName = findLastSnap();
                deleteSnap(lastSnapName);
            }
        }

        private void deleteSnap(SnapShotDownloadItem lastSnap)
        {
            SnapShotDownloadItems.RemoveAll(item=> item.FileName==lastSnap.FileName && item.Time==lastSnap.Time);
            var SnapName = $"{_networkingService._clientID}_{lastSnap.FileName}_{((DateTimeOffset)lastSnap.Time.ToUniversalTime()).ToUnixTimeSeconds().ToString()}";

            cloudService.DeleteAsync(SnapName+".json");
            Snaps.Remove(SnapName);
        }

        private SnapShotDownloadItem findLastSnap()
        {
            return SnapShotDownloadItems
                .OrderBy(item => item.Time) // Order items by Time in ascending order
                .FirstOrDefault();
        }

        private string parseSnapShotName(string snapShotFileName, SnapShot snapShot)
        {
            Debug.WriteLine("Current Name:" + snapShotFileName);
            if (string.IsNullOrWhiteSpace(snapShotFileName))
            {
                DateTime currentDateTime = DateTime.Now;
                snapShotFileName = currentDateTime.ToString("yyyyMMdd-HHmmss");
            }
            DateTimeOffset currentDateTimeEpoch = DateTimeOffset.UtcNow;
            snapShot.dateTime = currentDateTimeEpoch.LocalDateTime;
            long epochTime = currentDateTimeEpoch.ToUnixTimeSeconds();
            var newSnapShotFileName = _networkingService._clientID + "_" + snapShotFileName + "_" + epochTime.ToString();
            snapShot.fileName = snapShotFileName;
            SnapShotDownloadItems.Add(new SnapShotDownloadItem(snapShotFileName, snapShot.dateTime));
            snapShotFileName = newSnapShotFileName;
            return snapShotFileName;
        }

        public async Task<List<SnapShotDownloadItem>> getSnaps(string v, bool isInit)
        {
            if(isInit){
            var response = await cloudService.SearchJsonFilesAsync("userID", _networkingService._clientID.ToString());
                if (response != null && response.Data != null && response.Data.Matches != null)
                {
                    Snaps = response.Data.Matches
                        .ToDictionary(
                            match => match.FileName.Substring(0,match.FileName.Length-5),
                            match => SerializationService.DeserializeSnapShot(match.Content.ToString()).Shapes
                        );

                    PopulateSnapShotDownloadItems(response);

                    // Extract the FileName from each JsonSearchMatch and convert it to ObservableCollection
                    var fileNames = response.Data.Matches
                        .Select(match => match.FileName.Substring(0, match.FileName.Length - 5))
                        .ToList();


                    return SnapShotDownloadItems;
                }
                // Return an empty ObservableCollection if the response or data is null
                return new List<SnapShotDownloadItem>();

            }

            return SnapShotDownloadItems;
        }

        internal void DownloadSnapShot(SnapShotDownloadItem selectedDownloadItem)
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

        public bool IsValidFilename(String filename)
        {
            return !SnapShotDownloadItems.Any(item=> item.FileName==filename);
        }

        private ObservableCollection<IShape> getSnapShot(SnapShotDownloadItem selectedDownloadItem)
        {
            var SnapName = $"{_networkingService._clientID}_{selectedDownloadItem.FileName}_{((DateTimeOffset)selectedDownloadItem.Time.ToUniversalTime()).ToUnixTimeSeconds().ToString()}";
            return Snaps[SnapName];
        }

        private void PopulateSnapShotDownloadItems(ServiceResponse<JsonSearchResponse> response)
        {
            SnapShotDownloadItems = response.Data.Matches
                .Select(match =>
                {
                    var fileName = SerializationService.DeserializeSnapShot(match.Content.ToString()).fileName;
                    var time = SerializationService.DeserializeSnapShot(match.Content.ToString()).dateTime;
                    return new SnapShotDownloadItem(fileName, time);
                })
                .ToList();
        }
    }
}