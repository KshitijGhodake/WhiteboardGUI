using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SECloud.Services;
using SECloud.Models;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using WhiteboardGUI.Models;
using WhiteboardGUI.Services;
using Microsoft.Extensions.Logging;
using SECloud.Interfaces;

namespace WhiteboardGUI.Tests
{
    [TestClass]
    public class SnapShotServiceTests
    {
        private Mock<ICloud> _mockCloudService;
        private Mock<NetworkingService> _mockNetworkingService;
        private Mock<RenderingService> _mockRenderingService;
        private Mock<UndoRedoService> _mockUndoRedoService;
        private SnapShotService _snapShotService;
        private ObservableCollection<IShape> _shapes;

        [TestInitialize]
        public void Setup()
        {

            // Initialize shapes collection
            _shapes = new ObservableCollection<IShape>();

            // Mock dependencies
            _mockNetworkingService = new Mock<NetworkingService>();
            _mockUndoRedoService = new Mock<UndoRedoService>();
            _mockCloudService = new Mock<ICloud>();

            // Create a real RenderingService
            _mockRenderingService = new Mock<RenderingService>(_mockNetworkingService.Object, _mockUndoRedoService.Object, _shapes);

            // Initialize SnapShotService with mocked ICloud
            _snapShotService = new SnapShotService(
                _mockNetworkingService.Object,
                _mockRenderingService.Object,
                _shapes,
                _mockUndoRedoService.Object
            );

            // Use reflection to replace the private cloudService field with the mock
            var cloudServiceField = typeof(SnapShotService).GetField("cloudService", BindingFlags.NonPublic | BindingFlags.Instance);
            cloudServiceField.SetValue(_snapShotService, _mockCloudService.Object);
        }

        [TestMethod]
        public async Task UploadSnapShot_ShouldUploadSuccessfully()
        {
            // Arrange: Mock the UploadAsync method
            _mockCloudService.Setup(cs => cs.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
                             .ReturnsAsync(new ServiceResponse<string> { Data = "Success", Success = true });

            var snapShotFileName = "test_snapshot";
            var shapes = new ObservableCollection<IShape>
            {
                new LineShape { StartX = 10, StartY = 20, EndX = 30, EndY = 40 }
            };

            // Act
            await _snapShotService.UploadSnapShot(snapShotFileName, shapes, true);

            // Assert: Verify the snapshot was added to the Snaps dictionary
            var snapsField = typeof(SnapShotService).GetField("SnapshotFilename", BindingFlags.NonPublic | BindingFlags.Instance);
            var SnapshotFilename = (Dictionary<string, string>)snapsField.GetValue(_snapShotService);
            Assert.IsTrue(SnapshotFilename.ContainsKey(snapShotFileName));

            // Verify UploadAsync was called
            _mockCloudService.Verify(cs => cs.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task UploadSnapShot_ShouldFailWhenLimitExceeded()
        {
            // Arrange: Mock UploadAsync to simulate immediate response
            _mockCloudService.Setup(cs => cs.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
                             .ReturnsAsync(new ServiceResponse<string> { Data = "Success", Success = true });

            // Ensure Dispatcher is initialized
            if (System.Windows.Application.Current == null)
            {
                new System.Windows.Application();
            }

            // Clear the OnSnapShotUploaded event to prevent any hanging invocation
            var onSnapShotUploadedField = typeof(SnapShotService).GetField("OnSnapShotUploaded", BindingFlags.NonPublic | BindingFlags.Instance);
            onSnapShotUploadedField.SetValue(_snapShotService, null);

            // Add enough entries to exceed the limit
            var snapsField = typeof(SnapShotService).GetField("Snaps", BindingFlags.NonPublic | BindingFlags.Instance);
            var snapshotFilenameField = typeof(SnapShotService).GetField("SnapshotFilename", BindingFlags.NonPublic | BindingFlags.Instance);
            var snaps = new Dictionary<string, ObservableCollection<IShape>>();
            var snapshotFilename = new Dictionary<string, string>();

            for (int i = 0; i < 10; i++)
            {
                snaps.Add($"0_snapshot{i}_414134141{i}", new ObservableCollection<IShape>());
                snapshotFilename.Add($"snapshot{i}", $"0_snapshot{i}_414134141{i}");
            }
            snapsField.SetValue(_snapShotService, snaps);
            snapshotFilenameField.SetValue(_snapShotService, snapshotFilename);

            var snapShotFileName = "test_snapshot";

            // Act
            await _snapShotService.UploadSnapShot(snapShotFileName, _shapes,true);

            // Assert: Verify that the number of snaps does not exceed the limit
            var updatedSnaps = (Dictionary<string, string>)snapshotFilenameField.GetValue(_snapShotService);
            Assert.IsTrue(updatedSnaps.Count <= 5);
            Assert.IsTrue(updatedSnaps.ContainsKey(snapShotFileName));
        }

        [TestMethod]
        public void DownloadSnapShot_ShouldClearShapesAndAddNewOnes()
        {
            // Arrange
            var selectedDownloadItem = "snapshot1";
            var snapShotShapes = new ObservableCollection<IShape>
        {
            new LineShape { StartX = 10, StartY = 20, EndX = 30, EndY = 40 }
        };

            // Access and set private Snaps dictionary
            var snapsField = typeof(SnapShotService).GetField("Snaps", BindingFlags.NonPublic | BindingFlags.Instance);
            var snapshotFilenameField = typeof(SnapShotService).GetField("SnapshotFilename", BindingFlags.NonPublic | BindingFlags.Instance);

            var snaps = new Dictionary<string, ObservableCollection<IShape>> { { "snapshot1", snapShotShapes } };
            var snapshotFilename = new Dictionary<string, string> { { "snapshot1", "snapshot1" } };

            snapsField.SetValue(_snapShotService, snaps);
            snapshotFilenameField.SetValue(_snapShotService, snapshotFilename);

            // Mock RenderShape calls
            _mockRenderingService.Setup(rs => rs.RenderShape(It.IsAny<IShape>(), "DOWNLOAD"));
            _mockRenderingService.Setup(rs => rs.RenderShape(null, "CLEAR"));

            // Act
            _snapShotService.DownloadSnapShot(selectedDownloadItem);

            // Assert
            Assert.AreEqual(snapShotShapes.Count, _shapes.Count, "Shapes should match the downloaded snapshot.");
            Assert.AreEqual(1, _shapes.Count);
            _mockRenderingService.Verify(rs => rs.RenderShape(It.IsAny<IShape>(), "DOWNLOAD"), Times.Exactly(snapShotShapes.Count));
            _mockRenderingService.Verify(rs => rs.RenderShape(null, "CLEAR"), Times.Once);
            Assert.AreEqual(0, _mockUndoRedoService.Object.RedoList.Count, "Redo list should be cleared.");
            Assert.AreEqual(0, _mockUndoRedoService.Object.UndoList.Count, "Undo list should be cleared.");
        }

        [TestMethod]
        public void IsValidFilename_ShouldReturnCorrectValidation()
        {
            // Arrange
            var snapshotFilenameField = typeof(SnapShotService).GetField("SnapshotFilename", BindingFlags.NonPublic | BindingFlags.Instance);
            var snapshotFilename = new Dictionary<string, string>
        {
            { "valid_filename", "snapshot_valid" }
        };
            snapshotFilenameField.SetValue(_snapShotService, snapshotFilename);

            // Act & Assert
            Assert.IsFalse(_snapShotService.IsValidFilename("valid_filename"), "Filename already exists, should return false.");
            Assert.IsTrue(_snapShotService.IsValidFilename("new_filename"), "Filename does not exist, should return true.");
        }

        [TestMethod]
        public void GetSnapShot_ShouldReturnCorrectSnapshot()
        {
            // Arrange
            var snapsField = typeof(SnapShotService).GetField("Snaps", BindingFlags.NonPublic | BindingFlags.Instance);
            var snapshotFilenameField = typeof(SnapShotService).GetField("SnapshotFilename", BindingFlags.NonPublic | BindingFlags.Instance);

            var snapShotShapes = new ObservableCollection<IShape>
        {
            new LineShape { StartX = 10, StartY = 20, EndX = 30, EndY = 40 }
        };
            var snaps = new Dictionary<string, ObservableCollection<IShape>> { { "snapshot_key", snapShotShapes } };
            var snapshotFilename = new Dictionary<string, string> { { "snapshot_name", "snapshot_key" } };

            snapsField.SetValue(_snapShotService, snaps);
            snapshotFilenameField.SetValue(_snapShotService, snapshotFilename);

            // Use reflection to invoke the private getSnapShot method
            var getSnapShotMethod = typeof(SnapShotService).GetMethod("getSnapShot", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = (ObservableCollection<IShape>)getSnapShotMethod.Invoke(_snapShotService, new object[] { "snapshot_name" });

            // Assert
            Assert.IsNotNull(result, "getSnapShot should return a valid snapshot.");
            Assert.AreEqual(snapShotShapes.Count, result.Count, "Snapshot should match the expected shapes.");
        }

        [TestMethod]
        public void AddShapes_ShouldAddShapesToShapesCollection()
        {
            // Arrange
            var snapShotShapes = new ObservableCollection<IShape>
        {
            new LineShape { StartX = 10, StartY = 20, EndX = 30, EndY = 40 },
            new LineShape { StartX = 50, StartY = 60, EndX = 70, EndY = 80 }
        };

            // Use reflection to invoke the private addShapes method
            var addShapesMethod = typeof(SnapShotService).GetMethod("addShapes", BindingFlags.NonPublic | BindingFlags.Instance);

            // Mock RenderShape calls
            _mockRenderingService.Setup(rs => rs.RenderShape(It.IsAny<IShape>(), "DOWNLOAD"));

            // Act
            addShapesMethod.Invoke(_snapShotService, new object[] { snapShotShapes });

            // Assert
            Assert.AreEqual(snapShotShapes.Count, _shapes.Count, "Shapes collection should match the snapshot shapes.");
            _mockRenderingService.Verify(rs => rs.RenderShape(It.IsAny<IShape>(), "DOWNLOAD"), Times.Exactly(snapShotShapes.Count));
        }

        [TestMethod]
        public void CheckLimit_ShouldDeleteExcessSnapshots()
        {
            // Arrange: Set up Snaps with a large number of snapshots
            var snapsField = typeof(SnapShotService).GetField("Snaps", BindingFlags.NonPublic | BindingFlags.Instance);
            var snapshotFileNameField = typeof(SnapShotService).GetField("SnapshotFilename", BindingFlags.NonPublic | BindingFlags.Instance);

            var snaps = new Dictionary<string, ObservableCollection<IShape>>();
            var snapshotFileName = new Dictionary<string, string>();

            for (int i = 0; i < 10; i++)
            {
                snaps.Add($"snapshot_{i}_1234567890", new ObservableCollection<IShape>());
                snapshotFileName.Add(i.ToString(), $"snapshot_{i}_1234567890");
            }

            snapsField.SetValue(_snapShotService, snaps);
            snapshotFileNameField.SetValue(_snapShotService, snapshotFileName);

            // Act: Call CheckLimit
            _snapShotService.GetType()
                            .GetMethod("CheckLimit", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(_snapShotService, null);

            // Assert: Verify that there are only 5 snapshots left
            var remainingSnaps = ((Dictionary<string, ObservableCollection<IShape>>)snapsField.GetValue(_snapShotService)).Count;
            Assert.IsTrue(remainingSnaps <= 5);
        }

        [TestMethod]
        public async Task GetSnaps_ShouldReturnValidSnapshotFileNames()
        {
            // Arrange: Create and serialize SnapShot objects
            var snapshot1 = new SnapShot
            {
                userID = "user_1",
                fileName = "snapshot1",
                Shapes = new ObservableCollection<IShape>
        {
            new LineShape { StartX = 10, StartY = 20, EndX = 30, EndY = 40 }
        }
            };

            var snapshot2 = new SnapShot
            {
                userID = "user_2",
                fileName = "snapshot2",
                Shapes = new ObservableCollection<IShape>
        {
            new LineShape { StartX = 50, StartY = 60, EndX = 70, EndY = 80 }
        }
            };

            var jsonMatches = new List<JsonSearchMatch>
    {
        new JsonSearchMatch
        {
            FileName = "snapshot1.json",
            Content = JsonDocument.Parse(SerializationService.SerializeSnapShot(snapshot1)).RootElement
        },
        new JsonSearchMatch
        {
            FileName = "snapshot2.json",
            Content = JsonDocument.Parse(SerializationService.SerializeSnapShot(snapshot2)).RootElement
        }
    };

            _mockCloudService.Setup(cs => cs.SearchJsonFilesAsync(It.IsAny<string>(), It.IsAny<string>()))
                             .ReturnsAsync(new ServiceResponse<JsonSearchResponse>
                             {
                                 Data = new JsonSearchResponse { Matches = jsonMatches },
                                 Success = true
                             });

            // Act
            var result = await _snapShotService.getSnaps("", true);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("snapshot1"));
            Assert.IsTrue(result.Contains("snapshot2"));
        }

    }

}
