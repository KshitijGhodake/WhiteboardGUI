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
    /// <summary>
    /// The SnapShotService class manages the creation, uploading, downloading, and management of snapshots
    /// containing shapes in the Whiteboard application.
    /// </summary>
    public class SnapShotService
    {
        /// <summary>
        /// Path for saving snapshots in the cloud.
        /// </summary>
        private String CloudSave;

        /// <summary>
        /// Networking service used to manage client-related network operations.
        /// </summary>
        private readonly NetworkingService _networkingService;

        /// <summary>
        /// Rendering service used to render shapes on the whiteboard.
        /// </summary>
        private readonly RenderingService _renderingService;

        /// <summary>
        /// Service for handling undo and redo operations.
        /// </summary>
        private readonly UndoRedoService _undoRedoService;

        /// <summary>
        /// Observable collection of shapes currently on the whiteboard.
        /// </summary>
        private ObservableCollection<IShape> Shapes;

        /// <summary>
        /// Dictionary containing snapshots and their associated shapes.
        /// </summary>
        private Dictionary<string, ObservableCollection<IShape>> Snaps = new();

        /// <summary>
        /// List of snapshot download items available to the user.
        /// </summary>
        private List<SnapShotDownloadItem> SnapShotDownloadItems = new();

        /// <summary>
        /// Event triggered when a snapshot is uploaded successfully.
        /// </summary>
        public event Action OnSnapShotUploaded;

        /// <summary>
        /// Cloud service for interacting with the backend snapshot storage.
        /// </summary>
        private ICloud cloudService;

        /// <summary>
        /// Maximum number of snapshots allowed to be stored.
        /// </summary>
        int limit = 5;

        /// <summary>
        /// Initializes a new instance of the SnapShotService class.
        /// </summary>
        /// <param name="networkingService">Networking service for managing client ID.</param>
        /// <param name="renderingService">Rendering service for managing whiteboard shapes.</param>
        /// <param name="shapes">Observable collection of shapes on the whiteboard.</param>
        /// <param name="undoRedoService">Undo/redo service for managing user operations.</param>
        public SnapShotService(NetworkingService networkingService, RenderingService renderingService, ObservableCollection<IShape> shapes, UndoRedoService undoRedoService)
        {
            _networkingService = networkingService;
            _renderingService = renderingService;
            _undoRedoService = undoRedoService;
            Shapes = shapes;
            initializeCloudService();
        }

        /// <summary>
        /// Initializes the cloud service for managing snapshot uploads and downloads.
        /// </summary>
        private void initializeCloudService()
        {
            // Dependency injection setup for logging and HTTP client
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

            // Configuration for the cloud service
            var baseUrl = "https://secloudapp-2024.azurewebsites.net/api";
            var team = "whiteboard";
            var sasToken = "sp=racwdli&st=2024-11-14T21:02:09Z&se=2024-11-30T05:02:09Z&spr=https&sv=2022-11-02&sr=c&sig=tSw6pO8%2FgqiG2MgU%2FoepmRkFuuJrTerVy%2BDn91Y0WH8%3D";

            cloudService = new CloudService(baseUrl, team, sasToken, httpClient, logger);
        }

        /// <summary>
        /// Uploads a snapshot containing the given shapes to the cloud.
        /// </summary>
        /// <param name="snapShotFileName">Name of the snapshot file.</param>
        /// <param name="shapes">Collection of shapes to be included in the snapshot.</param>
        /// <param name="isTest">Indicates if this is a test operation.</param>
        public async Task UploadSnapShot(string snapShotFileName, ObservableCollection<IShape> shapes, bool isTest)
        {
            await Task.Run(async () =>
            {
                // Create and parse snapshot details
                var SnapShot = new SnapShot();
                snapShotFileName = parseSnapShotName(snapShotFileName, SnapShot);
                Debug.WriteLine($"Uploading snapshot '{snapShotFileName}' with {shapes.Count} shapes.");

                // Upload snapshot to cloud
                sendToCloud(snapShotFileName, SnapShot, shapes);

                // Show confirmation message if not in test mode
                if (!isTest)
                    MessageBox.Show($"Filename '{snapShotFileName}' has been set.", "Filename Set", MessageBoxButton.OK);
            });

            // Trigger upload completed event
            if (!isTest)
                System.Windows.Application.Current.Dispatcher.Invoke(() => OnSnapShotUploaded?.Invoke());
            Debug.WriteLine("Upload completed.");
        }

        /// <summary>
        /// Sends the serialized snapshot to the cloud storage.
        /// </summary>
        /// <param name="snapShotFileName">Snapshot file name.</param>
        /// <param name="snapShot">Snapshot object containing shapes and metadata.</param>
        /// <param name="shapes">Shapes to be serialized and uploaded.</param>
        private async void sendToCloud(string snapShotFileName, SnapShot snapShot, ObservableCollection<IShape> shapes)
        {
            CheckLimit(); // Ensure storage limit is respected
            snapShot.userID = _networkingService._clientID.ToString();
            snapShot.Shapes = new ObservableCollection<IShape>(shapes);
            String SnapShotSerialized = SerializationService.SerializeSnapShot(snapShot);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(SnapShotSerialized));
            var response = await cloudService.UploadAsync(snapShotFileName + ".json", stream, "application/json");
            Debug.WriteLine("RESPONSE:" + response.ToString());

            // Add the snapshot to the local dictionary
            Snaps.Add(snapShotFileName, snapShot.Shapes);
        }

        /// <summary>
        /// Checks the current number of snapshots and removes the oldest one if the limit is exceeded.
        /// </summary>
        private void CheckLimit()
        {
            while (Snaps.Count >= limit)
            {
                SnapShotDownloadItem lastSnapName = findLastSnap();
                deleteSnap(lastSnapName);
            }
        }

        /// <summary>
        /// Deletes a snapshot from both cloud and local storage.
        /// </summary>
        /// <param name="lastSnap">Snapshot to delete.</param>
        private void deleteSnap(SnapShotDownloadItem lastSnap)
        {
            SnapShotDownloadItems.RemoveAll(item => item.FileName == lastSnap.FileName && item.Time == lastSnap.Time);
            var SnapName = $"{_networkingService._clientID}_{lastSnap.FileName}_{((DateTimeOffset)lastSnap.Time.ToUniversalTime()).ToUnixTimeSeconds()}";

            cloudService.DeleteAsync(SnapName + ".json");
            Snaps.Remove(SnapName);
        }

        /// <summary>
        /// Finds the oldest snapshot in the list based on its timestamp.
        /// </summary>
        /// <returns>The oldest snapshot download item.</returns>
        private SnapShotDownloadItem findLastSnap()
        {
            return SnapShotDownloadItems
                .OrderBy(item => item.Time)
                .FirstOrDefault();
        }

        /// <summary>
        /// Parses and prepares the snapshot name, adding it to the local list.
        /// </summary>
        /// <param name="snapShotFileName">Name of the snapshot file.</param>
        /// <param name="snapShot">Snapshot object to populate metadata.</param>
        /// <returns>The parsed snapshot file name.</returns>
        private string parseSnapShotName(string snapShotFileName, SnapShot snapShot)
        {
            if (string.IsNullOrWhiteSpace(snapShotFileName))
            {
                DateTime currentDateTime = DateTime.Now;
                snapShotFileName = currentDateTime.ToString("yyyyMMdd-HHmmss");
            }
            DateTimeOffset currentDateTimeEpoch = DateTimeOffset.UtcNow;
            snapShot.dateTime = currentDateTimeEpoch.LocalDateTime;
            long epochTime = currentDateTimeEpoch.ToUnixTimeSeconds();
            var newSnapShotFileName = $"{_networkingService._clientID}_{snapShotFileName}_{epochTime}";
            snapShot.fileName = snapShotFileName;

            // Add to the list of downloadable snapshots
            SnapShotDownloadItems.Add(new SnapShotDownloadItem(snapShotFileName, snapShot.dateTime));
            return newSnapShotFileName;
        }

        /// <summary>
        /// Retrieves the list of snapshots from the cloud and populates the local list.
        /// </summary>
        /// <param name="v">Not used in the current implementation.</param>
        /// <param name="isInit">Indicates whether this is an initialization request.</param>
        /// <returns>A list of <see cref="SnapShotDownloadItem"/> objects.</returns>
        public async Task<List<SnapShotDownloadItem>> getSnaps(string v, bool isInit)
        {
            if (isInit)
            {
                // Search for JSON files in the cloud matching the user ID
                var response = await cloudService.SearchJsonFilesAsync("userID", _networkingService._clientID.ToString());
                if (response != null && response.Data != null && response.Data.Matches != null)
                {
                    // Map each match to a dictionary of snapshots and shapes
                    Snaps = response.Data.Matches
                        .ToDictionary(
                            match => match.FileName.Substring(0, match.FileName.Length - 5),
                            match => SerializationService.DeserializeSnapShot(match.Content.ToString()).Shapes
                        );

                    // Populate the local list of SnapShotDownloadItems
                    PopulateSnapShotDownloadItems(response);

                    // Extract the file names from the response
                    var fileNames = response.Data.Matches
                        .Select(match => match.FileName.Substring(0, match.FileName.Length - 5))
                        .ToList();

                    return SnapShotDownloadItems;
                }

                // Return an empty list if the response or data is null
                return new List<SnapShotDownloadItem>();
            }

            // Return the current list of SnapShotDownloadItems
            return SnapShotDownloadItems;
        }

        /// <summary>
        /// Downloads a snapshot and renders its shapes on the whiteboard.
        /// </summary>
        /// <param name="selectedDownloadItem">The snapshot item to be downloaded.</param>
        internal void DownloadSnapShot(SnapShotDownloadItem selectedDownloadItem)
        {
            // Retrieve the snapshot from local storage
            ObservableCollection<IShape> snapShot = getSnapShot(selectedDownloadItem);

            // Clear the whiteboard before adding new shapes
            _renderingService.RenderShape(null, "CLEAR");

            // Add shapes to the whiteboard
            addShapes(snapShot);

            // Clear undo and redo lists
            _undoRedoService.RedoList.Clear();
            _undoRedoService.UndoList.Clear();
        }

        /// <summary>
        /// Adds a collection of shapes to the whiteboard and renders them.
        /// </summary>
        /// <param name="snapShot">The collection of shapes to be added.</param>
        private void addShapes(ObservableCollection<IShape> snapShot)
        {
            foreach (IShape shape in snapShot)
            {
                Shapes.Add(shape);
                _renderingService.RenderShape(shape, "DOWNLOAD");
                Debug.WriteLine($"Added Shape {shape.GetType()}");
            }
        }

        /// <summary>
        /// Validates whether a filename is unique in the current list of snapshots.
        /// </summary>
        /// <param name="filename">The filename to validate.</param>
        /// <returns><c>true</c> if the filename is valid; otherwise, <c>false</c>.</returns>
        public bool IsValidFilename(string filename)
        {
            return !SnapShotDownloadItems.Any(item => item.FileName == filename);
        }

        /// <summary>
        /// Retrieves the shapes associated with a specific snapshot.
        /// </summary>
        /// <param name="selectedDownloadItem">The snapshot item to retrieve.</param>
        /// <returns>An <see cref="ObservableCollection{IShape}"/> containing the shapes.</returns>
        private ObservableCollection<IShape> getSnapShot(SnapShotDownloadItem selectedDownloadItem)
        {
            var SnapName = $"{_networkingService._clientID}_{selectedDownloadItem.FileName}_{((DateTimeOffset)selectedDownloadItem.Time.ToUniversalTime()).ToUnixTimeSeconds()}";
            return Snaps[SnapName];
        }

        /// <summary>
        /// Populates the local list of SnapShotDownloadItems from the cloud response.
        /// </summary>
        /// <param name="response">The cloud service response containing snapshot metadata.</param>
        private void PopulateSnapShotDownloadItems(ServiceResponse<JsonSearchResponse> response)
        {
            SnapShotDownloadItems = response.Data.Matches
                .Select(match =>
                {
                    // Deserialize snapshot details
                    var fileName = SerializationService.DeserializeSnapShot(match.Content.ToString()).fileName;
                    var time = SerializationService.DeserializeSnapShot(match.Content.ToString()).dateTime;

                    // Create a new SnapShotDownloadItem
                    return new SnapShotDownloadItem(fileName, time);
                })
                .ToList();
        }

    }
}