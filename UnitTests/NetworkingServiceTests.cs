// NetworkingServiceTests.cs
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using WhiteboardGUI.Models;
using WhiteboardGUI.Services;

namespace UnitTests
{
    [TestClass]
    public class NetworkingServiceTests
    {
        private NetworkingService _networkingServiceHost;
        private NetworkingService _networkingServiceClient;
        private List<IShape> _hostSynchronizedShapes;
        private List<IShape> _clientSynchronizedShapes;

        // Event flags
        private bool _shapeReceivedEventFired;
        private bool _shapeDeletedEventFired;
        private bool _shapeModifiedEventFired;
        private bool _shapesClearEventFired;
        private IShape _receivedShape;

        // Event flags for client
        private bool _clientShapeReceivedEventFired;
        private bool _clientShapeDeletedEventFired;
        private bool _clientShapeModifiedEventFired;
        private bool _clientShapesClearEventFired;
        private IShape _clientReceivedShape;

        // Event flags for host
        private bool _hostShapeReceivedEventFired;
        private bool _hostShapeDeletedEventFired;
        private bool _hostShapeModifiedEventFired;
        private bool _hostShapesClearEventFired;
        private IShape _hostReceivedShape;

        private const int Port = 5000;

        [TestInitialize]
        public void Setup()
        {
            _hostSynchronizedShapes = new List<IShape>();
            _clientSynchronizedShapes = new List<IShape>();

            _networkingServiceHost = new NetworkingService();
            _networkingServiceHost._synchronizedShapes = _hostSynchronizedShapes;

            _networkingServiceClient = new NetworkingService();
            _networkingServiceClient._synchronizedShapes = _clientSynchronizedShapes;

            // Reset client event flags
            _clientShapeReceivedEventFired = false;
            _clientShapeDeletedEventFired = false;
            _clientShapeModifiedEventFired = false;
            _clientShapesClearEventFired = false;
            _clientReceivedShape = null;

            _hostShapeReceivedEventFired = false;
            _hostShapeDeletedEventFired = false;
            _hostShapeModifiedEventFired = false;
            _hostShapesClearEventFired = false;
            //_hostShapeSendToBackEventFired = false;
            //_hostShapeSendBackwardEventFired = false;
            _hostReceivedShape = null;

            // Subscribe to client events
            _networkingServiceClient.ShapeReceived += (shape, flag) =>
            {
                _clientShapeReceivedEventFired = true;
                _clientReceivedShape = shape;
                
                if (flag) // Indicates a CREATE message
                {
                    _clientSynchronizedShapes.Add(shape);
                }
            };
            _networkingServiceHost.ShapeReceived += (shape, flag) =>
            {
                
                _hostShapeReceivedEventFired = true;
                _hostReceivedShape = shape;
                if (flag) // Indicates a CREATE message
                {
                    _hostSynchronizedShapes.Add(shape);
                }
            };
            _networkingServiceClient.ShapeDeleted += (shape) =>
            {
                _clientShapeDeletedEventFired = true;
                
            };
            _networkingServiceHost.ShapeDeleted += (shape) =>
            {
               
                _hostShapeDeletedEventFired = true;
            };
            _networkingServiceClient.ShapeModified += (shape) =>
            {
                _clientShapeModifiedEventFired = true;
                
            };
            _networkingServiceHost.ShapeModified += (shape) =>
            {
                
                _hostShapeModifiedEventFired = true;
            };
            _networkingServiceClient.ShapesClear += () =>
            {
                _clientShapesClearEventFired = true;
             
            };
            _networkingServiceHost.ShapesClear += () =>
            {
                
                _hostShapesClearEventFired = true;
            };
        }


        /// <summary>
        /// Helper method to access private fields via reflection.
        /// </summary>

        [TestMethod]
        public async Task StartHost_ShouldStartTcpListener()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();

            // Wait to ensure the listener is started
            await Task.Delay(500);

            // Access the private _listener field using reflection
            TcpListener listener = _networkingServiceHost._listener;

            Assert.IsNotNull(listener, "Listener should not be null after starting the host.");
            Assert.IsTrue(listener.Server.IsBound, "Listener should be bound to the server.");

            // Cleanup
            _networkingServiceHost.StopHost();
            await Task.Delay(500); // Allow time for the host to stop
        }

        [TestMethod]
        public async Task StartClient_ShouldConnectToHost()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500); // Ensure host is up

            // Start the client
            await _networkingServiceClient.StartClient(Port);
            await Task.Delay(500); // Ensure client is connected

            // Access the private _client field using reflection
            TcpClient client = _networkingServiceClient._client;

            Assert.IsNotNull(client, "Client should not be null after connecting.");
            Assert.IsTrue(client.Connected, "Client should be connected to the host.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500); // Allow time for cleanup
        }


        [TestMethod]
        public async Task RunningClient_ShouldHandleCreateMessage()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500); // Ensure host is up

            // Start the client
            await _networkingServiceClient.StartClient(Port);
            await Task.Delay(500); // Ensure client is connected

            // Create a shape on the host
            var shape = new CircleShape
            {
                ShapeId = Guid.NewGuid(),
                UserID = 1.0,
                Color = "Red",
                StrokeThickness = 2.0,
                CenterX = 50,
                CenterY = 50,
                RadiusX = 20,
                RadiusY = 20
            };
            _hostSynchronizedShapes.Add(shape);

            // Serialize the CREATE message
            string serializedShape = SerializationService.SerializeShape(shape);
            string createMessage = $"CREATE:{serializedShape}";

            // Broadcast the CREATE message from host to client
            await _networkingServiceHost.BroadcastShapeData(createMessage, 0);
            await Task.Delay(500); // Wait for the message to be processed

            // Assert that the ShapeReceived event was fired on the client
            Assert.IsTrue(_clientShapeReceivedEventFired, "ShapeReceived event should have been fired for CREATE message.");
            Assert.IsNotNull(_clientReceivedShape, "Received shape should not be null for CREATE message.");
            Assert.AreEqual(shape.ShapeId, _clientReceivedShape.ShapeId, "Shape IDs should match for CREATE message.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500); // Allow time for cleanup
        }

        [TestMethod]
        public async Task RunningClient_ShouldHandleDeleteMessage()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500); // Ensure host is up

            // Start the client
            await _networkingServiceClient.StartClient(Port);
            await Task.Delay(500); // Ensure client is connected

            // Add a shape to the host's synchronized shapes
            var shape = new LineShape
            {
                ShapeId = Guid.NewGuid(),
                UserID = 1.0,
                StartX = 10,
                StartY = 10,
                EndX = 100,
                EndY = 100,
                Color = "Blue",
                StrokeThickness = 1.0
            };
            _hostSynchronizedShapes.Add(shape);

            // Add the same shape to the client's synchronized shapes
            _clientSynchronizedShapes.Add(shape);

            // Serialize the DELETE message
            string serializedShape = SerializationService.SerializeShape(shape);
            string deleteMessage = $"DELETE:{serializedShape}";

            // Broadcast the DELETE message from host to client
            await _networkingServiceHost.BroadcastShapeData(deleteMessage, 0);
            await Task.Delay(500); // Wait for the message to be processed

            // Assert that the ShapeDeleted event was fired on the client
            Assert.IsTrue(_clientShapeDeletedEventFired, "ShapeDeleted event should have been fired for DELETE message.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500); // Allow time for cleanup
        }


        [TestMethod]
        public async Task RunningClient_ShouldHandleModifyMessage()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500); // Ensure host is up

            // Start the client
            await _networkingServiceClient.StartClient(Port);
            await Task.Delay(500); // Ensure client is connected

            // Add a shape to the host's synchronized shapes
            var shape = new LineShape
            {
                ShapeId = Guid.NewGuid(),
                UserID = 1.0,
                StartX = 10,
                StartY = 10,
                EndX = 100,
                EndY = 100,
                Color = "Blue",
                StrokeThickness = 1.0
            };
            _hostSynchronizedShapes.Add(shape);

            // Add the same shape to the client's synchronized shapes
            _clientSynchronizedShapes.Add(shape);

            // Modify the shape
            shape.Color = "Red";
            string serializedShape = SerializationService.SerializeShape(shape);
            string modifyMessage = $"MODIFY:{serializedShape}";

            // Broadcast the MODIFY message from host to client
            await _networkingServiceHost.BroadcastShapeData(modifyMessage, 0);
            await Task.Delay(500); // Wait for the message to be processed

            // Assert that the ShapeModified event was fired on the client
            Assert.IsTrue(_clientShapeModifiedEventFired, "ShapeModified event should have been fired for MODIFY message.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500); // Allow time for cleanup
        }

        [TestMethod]
        public async Task RunningClient_ShouldHandleClearMessage()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500); // Ensure host is up

            // Start the client
            await _networkingServiceClient.StartClient(Port);
            await Task.Delay(500); // Ensure client is connected

            // Serialize the CLEAR message
            string clearMessage = "CLEAR:";

            // Broadcast the CLEAR message from host to client
            await _networkingServiceHost.BroadcastShapeData(clearMessage, 0);
            await Task.Delay(500); // Wait for the message to be processed

            // Assert that the ShapesClear event was fired on the client
            Assert.IsTrue(_clientShapesClearEventFired, "ShapesClear event should have been fired for CLEAR message.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500); // Allow time for cleanup
        }


        [TestMethod]
        public async Task RunningClient_ShouldHandleIndexBackMessage()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500); // Ensure host is up

            // Start the client
            await _networkingServiceClient.StartClient(Port);
            await Task.Delay(500); // Ensure client is connected

            // Add a shape to the host's synchronized shapes
            var shape = new LineShape
            {
                ShapeId = Guid.NewGuid(),
                UserID = 1.0,
                StartX = 20,
                StartY = 20,
                EndX = 200,
                EndY = 200,
                Color = "Green",
                StrokeThickness = 2.0
            };
            _hostSynchronizedShapes.Add(shape);

            // Add the same shape to the client's synchronized shapes
            _clientSynchronizedShapes.Add(shape);

            // Subscribe to the client's ShapeSendToBack event
            bool shapeSendToBackEventFired = false;
            _networkingServiceClient.ShapeSendToBack += (receivedShape) =>
            {
                shapeSendToBackEventFired = true;
            };

            // Serialize the INDEX-BACK message
            string serializedShape = SerializationService.SerializeShape(shape);
            string indexBackMessage = $"INDEX-BACK:{serializedShape}";

            // Broadcast the INDEX-BACK message from host to client
            await _networkingServiceHost.BroadcastShapeData(indexBackMessage, 0);
            await Task.Delay(500); // Wait for the message to be processed

            // Assert that the ShapeSendToBack event was fired on the client
            Assert.IsTrue(shapeSendToBackEventFired, "ShapeSendToBack event should have been fired for INDEX-BACK message.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500); // Allow time for cleanup
        }

        [TestMethod]
        public async Task RunningClient_ShouldHandleIndexBackwardMessage()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500); // Ensure host is up

            // Start the client
            await _networkingServiceClient.StartClient(Port);
            await Task.Delay(500); // Ensure client is connected

            // Add a shape to the host's synchronized shapes
            var shape = new LineShape
            {
                ShapeId = Guid.NewGuid(),
                UserID = 1.0,
                StartX = 30,
                StartY = 30,
                EndX = 300,
                EndY = 300,
                Color = "Yellow",
                StrokeThickness = 3.0
            };
            _hostSynchronizedShapes.Add(shape);

            // Add the same shape to the client's synchronized shapes
            _clientSynchronizedShapes.Add(shape);

            // Subscribe to the client's ShapeSendBackward event
            bool shapeSendBackwardEventFired = false;
            _networkingServiceClient.ShapeSendBackward += (receivedShape) =>
            {
                shapeSendBackwardEventFired = true;
            };

            // Serialize the INDEX-BACKWARD message
            string serializedShape = SerializationService.SerializeShape(shape);
            string indexBackwardMessage = $"INDEX-BACKWARD:{serializedShape}";

            // Broadcast the INDEX-BACKWARD message from host to client
            await _networkingServiceHost.BroadcastShapeData(indexBackwardMessage, 0);
            await Task.Delay(500); // Wait for the message to be processed

            // Assert that the ShapeSendBackward event was fired on the client
            Assert.IsTrue(shapeSendBackwardEventFired, "ShapeSendBackward event should have been fired for INDEX-BACKWARD message.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500); // Allow time for cleanup
        }

        [TestMethod]
        public async Task ListenClients_ShouldHandleCreateMessage()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500); // Ensure host is up

            // Start the client
            await _networkingServiceClient.StartClient(5000);
            await Task.Delay(500); // Ensure client is connected

            // Create a shape on the client
            var shape = new CircleShape
            {
                ShapeId = Guid.NewGuid(),
                UserID = 1.0,
                Color = "Red",
                StrokeThickness = 2.0,
                CenterX = 50,
                CenterY = 50,
                RadiusX = 20,
                RadiusY = 20
            };

            // Serialize the CREATE message
            string serializedShape = SerializationService.SerializeShape(shape);
            string createMessage = $"CREATE:{serializedShape}";

            // Send the CREATE message from client to server
            NetworkStream clientStream = _networkingServiceClient._client.GetStream();
            StreamWriter clientWriter = new(clientStream) { AutoFlush = true };
            await clientWriter.WriteLineAsync(createMessage);
            await Task.Delay(500); // Wait for the message to be processed

            // Assert that the ShapeReceived event was fired on the host
            Assert.IsTrue(_hostShapeReceivedEventFired, "ShapeReceived event should have been fired on the host for CREATE message.");
            Assert.IsNotNull(_hostReceivedShape, "Received shape should not be null on the host for CREATE message.");
            Assert.AreEqual(shape.ShapeId, _hostReceivedShape.ShapeId, "Shape IDs should match for CREATE message.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500); // Allow time for cleanup
        }



        [TestMethod]
        public async Task ListenClients_ShouldHandleDeleteMessage()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500);

            // Start the client
            await _networkingServiceClient.StartClient(5000);
            await Task.Delay(500);

            // Create a shape on the client
            var shape = new LineShape
            {
                ShapeId = Guid.NewGuid(),
                UserID = 1.0,
                StartX = 10,
                StartY = 10,
                EndX = 100,
                EndY = 100,
                Color = "Blue",
                StrokeThickness = 1.0
            };

            // Serialize the CREATE message
            string serializedShape = SerializationService.SerializeShape(shape);
            string createMessage = $"CREATE:{serializedShape}";

            // Send the CREATE message from client to server
            NetworkStream clientStream = _networkingServiceClient._client.GetStream();
            StreamWriter clientWriter = new(clientStream) { AutoFlush = true };
            await clientWriter.WriteLineAsync(createMessage);
            await Task.Delay(500); // Wait for the message to be processed

            // Reset the event flag
            _hostShapeReceivedEventFired = false;

            // Send the DELETE message from client to server
            string deleteMessage = $"DELETE:{serializedShape}";
            await clientWriter.WriteLineAsync(deleteMessage);
            await Task.Delay(500); // Wait for the message to be processed

            // Assert that the ShapeDeleted event was fired on the host
            Assert.IsTrue(_hostShapeDeletedEventFired, "ShapeDeleted event should have been fired on the host for DELETE message.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500);
        }


        [TestMethod]
        public async Task ListenClients_ShouldHandleModifyMessage()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500);

            // Start the client
            await _networkingServiceClient.StartClient(5000);
            await Task.Delay(500);

            // Create a shape on the client
            var shape = new LineShape
            {
                ShapeId = Guid.NewGuid(),
                UserID = 1.0,
                StartX = 10,
                StartY = 10,
                EndX = 100,
                EndY = 100,
                Color = "Blue",
                StrokeThickness = 1.0
            };

            // Send a CREATE message to add the shape to the server
            string serializedShape = SerializationService.SerializeShape(shape);
            string createMessage = $"CREATE:{serializedShape}";
            NetworkStream clientStream = _networkingServiceClient._client.GetStream();
            StreamWriter clientWriter = new(clientStream) { AutoFlush = true };
            await clientWriter.WriteLineAsync(createMessage);
            await Task.Delay(500);

            // Modify the shape
            shape.Color = "Red";
            serializedShape = SerializationService.SerializeShape(shape);
            string modifyMessage = $"MODIFY:{serializedShape}";

            // Send the MODIFY message from client to server
            await clientWriter.WriteLineAsync(modifyMessage);
            await Task.Delay(500);

            // Assert that the ShapeModified event was fired on the host
            Assert.IsTrue(_hostShapeModifiedEventFired, "ShapeModified event should have been fired on the host for MODIFY message.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500);
        }


        [TestMethod]
        public async Task ListenClients_ShouldHandleClearMessage()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500);

            // Start the client
            await _networkingServiceClient.StartClient(5000);
            await Task.Delay(500);

            // Send the CLEAR message from client to server
            string clearMessage = "CLEAR:";
            NetworkStream clientStream = _networkingServiceClient._client.GetStream();
            StreamWriter clientWriter = new(clientStream) { AutoFlush = true };
            await clientWriter.WriteLineAsync(clearMessage);
            await Task.Delay(500);

            // Assert that the ShapesClear event was fired on the host
            Assert.IsTrue(_hostShapesClearEventFired, "ShapesClear event should have been fired on the host for CLEAR message.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500);
        }


        [TestMethod]
        public async Task ListenClients_ShouldHandleIndexBackMessage()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500); // Ensure host is up

            // Start the client
            await _networkingServiceClient.StartClient(5000);
            await Task.Delay(500); // Ensure client is connected

            // Create a shape on the client
            var shape = new LineShape
            {
                ShapeId = Guid.NewGuid(),
                UserID = 1.0,
                StartX = 20,
                StartY = 20,
                EndX = 200,
                EndY = 200,
                Color = "Green",
                StrokeThickness = 2.0
            };

            // Send a CREATE message to add the shape to the server
            string serializedShape = SerializationService.SerializeShape(shape);
            string createMessage = $"CREATE:{serializedShape}";
            NetworkStream clientStream = _networkingServiceClient.Client.GetStream();
            StreamWriter clientWriter = new(clientStream) { AutoFlush = true };
            await clientWriter.WriteLineAsync(createMessage);
            await Task.Delay(500);

            // Subscribe to the host's ShapeSendToBack event
            bool shapeSendToBackEventFired = false;
            _networkingServiceHost.ShapeSendToBack += (receivedShape) =>
            {
                shapeSendToBackEventFired = true;
            };

            // Send the INDEX-BACK message from client to server
            string indexBackMessage = $"INDEX-BACK:{serializedShape}";
            await clientWriter.WriteLineAsync(indexBackMessage);
            await Task.Delay(500);

            // Assert that the ShapeSendToBack event was fired on the host
            Assert.IsTrue(shapeSendToBackEventFired, "ShapeSendToBack event should have been fired on the host for INDEX-BACK message.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500);
        }

        [TestMethod]
        public async Task ListenClients_ShouldHandleIndexBackwardMessage()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500);

            // Start the client
            await _networkingServiceClient.StartClient(5000);
            await Task.Delay(500);

            // Create a shape on the client
            var shape = new LineShape
            {
                ShapeId = Guid.NewGuid(),
                UserID = 1.0,
                StartX = 30,
                StartY = 30,
                EndX = 300,
                EndY = 300,
                Color = "Yellow",
                StrokeThickness = 3.0
            };

            // Send a CREATE message to add the shape to the server
            string serializedShape = SerializationService.SerializeShape(shape);
            string createMessage = $"CREATE:{serializedShape}";
            NetworkStream clientStream = _networkingServiceClient.Client.GetStream();
            StreamWriter clientWriter = new(clientStream) { AutoFlush = true };
            await clientWriter.WriteLineAsync(createMessage);
            await Task.Delay(500);

            // Subscribe to the host's ShapeSendBackward event
            bool shapeSendBackwardEventFired = false;
            _networkingServiceHost.ShapeSendBackward += (receivedShape) =>
            {
                shapeSendBackwardEventFired = true;
            };

            // Send the INDEX-BACKWARD message from client to server
            string indexBackwardMessage = $"INDEX-BACKWARD:{serializedShape}";
            await clientWriter.WriteLineAsync(indexBackwardMessage);
            await Task.Delay(500);

            // Assert that the ShapeSendBackward event was fired on the host
            Assert.IsTrue(shapeSendBackwardEventFired, "ShapeSendBackward event should have been fired on the host for INDEX-BACKWARD message.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500);
        }




        [TestMethod]
        public async Task ListenClients_ShouldHandleDownloadMessage()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500);

            // Start the client
            await _networkingServiceClient.StartClient(5000);
            await Task.Delay(500);

            // Create a shape on the client
            var shape = new TextShape
            {
                ShapeId = Guid.NewGuid(),
                UserID = 1.0,
                Color = "Black",
                StrokeThickness = 0.5,
                Text = "Hello World",
                FontSize = 12,
                X = 30,
                Y = 30
            };

            // Serialize the DOWNLOAD message
            string serializedShape = SerializationService.SerializeShape(shape);
            string downloadMessage = $"DOWNLOAD:{serializedShape}";

            // Send the DOWNLOAD message from client to server
            NetworkStream clientStream = _networkingServiceClient._client.GetStream();
            StreamWriter clientWriter = new(clientStream) { AutoFlush = true };
            await clientWriter.WriteLineAsync(downloadMessage);
            await Task.Delay(500);

            // Assert that the ShapeReceived event was fired on the host
            Assert.IsTrue(_hostShapeReceivedEventFired, "ShapeReceived event should have been fired on the host for DOWNLOAD message.");
            Assert.IsNotNull(_hostReceivedShape, "Received shape should not be null for DOWNLOAD message.");
            Assert.AreEqual(shape.ShapeId, _hostReceivedShape.ShapeId, "Shape IDs should match for DOWNLOAD message.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500);
        }


        [TestMethod]
        public async Task ListenClients_ShouldHandleInvalidMessage()
        {
            // Arrange
            var clientTask = _networkingServiceClient.StartClient(Port);
            await Task.Delay(500);

            string invalidMessage = "INVALID_MESSAGE";

            // Act
            await _networkingServiceHost.BroadcastShapeData(invalidMessage, 1.0);
            await Task.Delay(500);

            // Assert
            Assert.IsFalse(_hostShapeReceivedEventFired, "ShapeReceived event should not have been fired for invalid message.");
            Assert.IsFalse(_hostShapeDeletedEventFired, "ShapeDeleted event should not have been fired for invalid message.");
            Assert.IsFalse(_hostShapeModifiedEventFired, "ShapeModified event should not have been fired for invalid message.");
            Assert.IsFalse(_hostShapesClearEventFired, "ShapesClear event should not have been fired for invalid message.");
        }

        [TestMethod]
        public async Task ListenClients_ShouldHandleMalformedShapeData()
        {
            // Arrange
            var clientTask = _networkingServiceClient.StartClient(Port);
            await Task.Delay(500);

            string malformedShapeData = "{\"ShapeType\":\"Circle\",\"CenterX\":50,\"CenterY\":50";
            string createMessage = $"CREATE:{malformedShapeData}";

            // Act
            await _networkingServiceHost.BroadcastShapeData(createMessage, 1.0);
            await Task.Delay(500);

            // Assert
            Assert.IsFalse(_hostShapeReceivedEventFired, "ShapeReceived event should not have been fired for malformed shape data.");
        }


        [TestMethod]
        public async Task RunningClient_ShouldHandleDownloadMessage()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500); // Ensure host is up

            // Start the client
            await _networkingServiceClient.StartClient(Port);
            await Task.Delay(500); // Ensure client is connected

            // Create a shape on the host
            var shape = new TextShape
            {
                ShapeId = Guid.NewGuid(),
                UserID = 1.0,
                Color = "Black",
                StrokeThickness = 0.5,
                Text = "Hello World",
                FontSize = 12,
                X = 30,
                Y = 30
            };
            _hostSynchronizedShapes.Add(shape);

            // Serialize the DOWNLOAD message
            string serializedShape = SerializationService.SerializeShape(shape);
            string downloadMessage = $"DOWNLOAD:{serializedShape}";

            // Broadcast the DOWNLOAD message from host to client
            await _networkingServiceHost.BroadcastShapeData(downloadMessage, 0);
            await Task.Delay(500); // Wait for the message to be processed

            // Assert that the ShapeReceived event was fired on the client with 'false' flag
            Assert.IsTrue(_clientShapeReceivedEventFired, "ShapeReceived event should have been fired for DOWNLOAD message.");
            Assert.IsNotNull(_clientReceivedShape, "Received shape should not be null for DOWNLOAD message.");
            Assert.AreEqual(shape.ShapeId, _clientReceivedShape.ShapeId, "Shape IDs should match for DOWNLOAD message.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500); // Allow time for cleanup
        }

        [TestMethod]
        public async Task RunningClient_ShouldHandleInvalidMessage()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500); // Ensure host is up

            // Start the client
            await _networkingServiceClient.StartClient(Port);
            await Task.Delay(500); // Ensure client is connected

            // Send an invalid message that doesn't match any known prefixes
            string invalidMessage = "INVALID_MESSAGE";

            // Broadcast the invalid message from host to client
            await _networkingServiceHost.BroadcastShapeData(invalidMessage, 0);
            await Task.Delay(500); // Wait for the message to be processed

            // Assert that no unintended events were fired on the client
            Assert.IsFalse(_clientShapeReceivedEventFired, "ShapeReceived event should not have been fired for invalid message.");
            Assert.IsFalse(_clientShapeDeletedEventFired, "ShapeDeleted event should not have been fired for invalid message.");
            Assert.IsFalse(_clientShapeModifiedEventFired, "ShapeModified event should not have been fired for invalid message.");
            Assert.IsFalse(_clientShapesClearEventFired, "ShapesClear event should not have been fired for invalid message.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500); // Allow time for cleanup
        }

        [TestMethod]
        public async Task RunningClient_ShouldHandleMalformedShapeData()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500); // Ensure host is up

            // Start the client
            await _networkingServiceClient.StartClient(Port);
            await Task.Delay(500); // Ensure client is connected

            // Send a CREATE message with malformed JSON data
            string malformedShapeData = "{\"ShapeType\":\"Circle\",\"CenterX\":50,\"CenterY\":50"; // Missing closing brace
            string createMessage = $"CREATE:{malformedShapeData}";

            // Broadcast the CREATE message with malformed data
            await _networkingServiceHost.BroadcastShapeData(createMessage, 0);
            await Task.Delay(500); // Wait for the message to be processed

            // Assert that the ShapeReceived event was not fired on the client due to deserialization failure
            Assert.IsFalse(_clientShapeReceivedEventFired, "ShapeReceived event should not have been fired for malformed shape data.");

            // Cleanup
            _networkingServiceClient.StopClient();
            _networkingServiceHost.StopHost();
            await Task.Delay(500); // Allow time for cleanup
        }

        [TestMethod]
        public async Task StopHost_ShouldStopTcpListener()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500); // Ensure host is up

            // Stop the host
            _networkingServiceHost.StopHost();
            await Task.Delay(500); // Allow time for the host to stop

            // Access the private _listener field using reflection
            TcpListener listener = null;
            try
            {
                listener = _networkingServiceHost._listener;
            }
            catch (ArgumentException)
            {
                // Field not found or already null
            }

            Assert.IsNull(listener, "Listener should be null after stopping the host.");
        }

        [TestMethod]
        public async Task StopClient_ShouldCloseTcpClient()
        {
            // Start the host
            var hostTask = _networkingServiceHost.StartHost();
            await Task.Delay(500); // Ensure host is up

            // Start the client
            await _networkingServiceClient.StartClient(Port);
            await Task.Delay(500); // Ensure client is connected

            // Stop the client
            _networkingServiceClient.StopClient();
            await Task.Delay(500); // Allow time for the client to stop

            // Access the private _client field using reflection
            TcpClient client = null;
            try
            {
                client = _networkingServiceClient._client;
            }
            catch (ArgumentException)
            {
                // Field not found or already null
            }

            Assert.IsNull(client, "Client should be null after stopping.");

            // Cleanup
            _networkingServiceHost.StopHost();
            await Task.Delay(500); // Allow time for cleanup
        }

        [TestCleanup]
        public void Cleanup()
        {
            _networkingServiceClient?.StopClient();
            _networkingServiceHost?.StopHost();
        }
    }
}
