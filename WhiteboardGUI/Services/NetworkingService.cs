using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using WhiteboardGUI.Models;

namespace WhiteboardGUI.Services
{
    public class NetworkingService
    {
        private TcpListener _listener;
        private TcpClient _client;
        private ConcurrentDictionary<double, TcpClient> _clients = new();
        private double _clientID;
        private ObservableCollection<IShape> _synchronizedShapes;

        public event Action<IShape> ShapeReceived; // Event for shape received
        public event Action<IShape> ShapeDeleted;

        public NetworkingService(ObservableCollection<IShape> synchronizedShapes)
        {
            _synchronizedShapes = synchronizedShapes;
        }

        public async Task StartHost()
        {
            await StartServer();
        }

        private async Task StartServer()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, 5000);
                _listener.Start();
                Debug.WriteLine("Host started, waiting for clients...");
                double currentUserID = 1;

                while (true)
                {
                    TcpClient newClient = await _listener.AcceptTcpClientAsync();
                    _clients.TryAdd(currentUserID, newClient);
                    Debug.WriteLine($"Client connected! Assigned ID: {currentUserID}");

                    // Send the client ID to the newly connected client
                    NetworkStream stream = newClient.GetStream();
                    StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
                    await writer.WriteLineAsync($"ID:{currentUserID}");

                    // Start listening to this client's messages
                    _ = Task.Run(() => ListenClient(newClient, currentUserID));

                    // Send all existing shapes to the new client
                    foreach (var shape in _synchronizedShapes)
                    {
                        string serializedShape = SerializationService.SerializeShape(shape);
                        await writer.WriteLineAsync(serializedShape);
                    }

                    currentUserID++;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Host error: {ex.Message}");
            }
        }

        private async Task ListenClient(TcpClient client, double senderUserID)
        {
            try
            {
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream);

                while (true)
                {
                    var receivedData = await reader.ReadLineAsync();
                    if (receivedData == null) continue;

                    Debug.WriteLine($"Received data from client {senderUserID}: {receivedData}");
                    await ProcessReceivedData(receivedData, senderUserID);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ListenClient: {ex.Message}");
                // Remove client from the dictionary
                _clients.TryRemove(senderUserID, out _);
                client.Close();
            }
        }

        private async Task ProcessReceivedData(string receivedData, double senderUserID)
        {
            if (receivedData.StartsWith("DELETE:"))
            {
                string data = receivedData.Substring(7);
                var shape = SerializationService.DeserializeShape(data);

                if (shape != null)
                {
                    var shapeId = shape.ShapeId;

                    var currentShape = _synchronizedShapes.FirstOrDefault(s => s.ShapeId == shapeId);
                    if (currentShape != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _synchronizedShapes.Remove(currentShape);
                            ShapeDeleted?.Invoke(currentShape);
                        });
                        // Broadcast the deletion to other clients
                        await BroadcastShapeData(receivedData, senderUserID);
                    }
                }
            }
            else if (receivedData.StartsWith("UPDATE:"))
            {
                string data = receivedData.Substring(7);
                var shape = SerializationService.DeserializeShape(data);

                if (shape != null)
                {
                    var shapeId = shape.ShapeId;

                    var currentShape = _synchronizedShapes.FirstOrDefault(s => s.ShapeId == shapeId);
                    if (currentShape != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            UpdateShapeProperties(currentShape, shape);
                        });
                    }
                    else
                    {
                        // Shape not found, add it
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _synchronizedShapes.Add(shape);
                            ShapeReceived?.Invoke(shape);
                        });
                    }
                    // Broadcast the update to other clients
                    await BroadcastShapeData(receivedData, senderUserID);
                }
            }
            else
            {
                // Assuming this is a new shape
                var shape = SerializationService.DeserializeShape(receivedData);
                if (shape != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _synchronizedShapes.Add(shape);
                        ShapeReceived?.Invoke(shape);
                    });
                    // Broadcast the new shape to other clients
                    await BroadcastShapeData(receivedData, senderUserID);
                }
            }
        }

        private void UpdateShapeProperties(IShape existingShape, IShape newShape)
        {
            existingShape.Color = newShape.Color;
            existingShape.StrokeThickness = newShape.StrokeThickness;

            switch (existingShape)
            {
                case LineShape existingLine when newShape is LineShape newLine:
                    existingLine.StartX = newLine.StartX;
                    existingLine.StartY = newLine.StartY;
                    existingLine.EndX = newLine.EndX;
                    existingLine.EndY = newLine.EndY;
                    break;
                case CircleShape existingCircle when newShape is CircleShape newCircle:
                    existingCircle.CenterX = newCircle.CenterX;
                    existingCircle.CenterY = newCircle.CenterY;
                    existingCircle.RadiusX = newCircle.RadiusX;
                    existingCircle.RadiusY = newCircle.RadiusY;
                    break;
                case ScribbleShape existingScribble when newShape is ScribbleShape newScribble:
                    existingScribble.Points = new List<Point>(newScribble.Points);
                    break;
                case TextShape existingText when newShape is TextShape newText:
                    existingText.Text = newText.Text;
                    existingText.X = newText.X;
                    existingText.Y = newText.Y;
                    existingText.FontSize = newText.FontSize;
                    break;
                case TextboxModel existingTextbox when newShape is TextboxModel newTextbox:
                    existingTextbox.Text = newTextbox.Text;
                    existingTextbox.X = newTextbox.X;
                    existingTextbox.Y = newTextbox.Y;
                    existingTextbox.Width = newTextbox.Width;
                    existingTextbox.Height = newTextbox.Height;
                    existingTextbox.FontSize = newTextbox.FontSize;
                    break;
                    // Add cases for other shape types as needed
            }

            // Notify that the shape has been updated
            if (existingShape is ShapeBase shapeBase)
            {
                shapeBase.OnPropertyChanged(null); // Notify all properties have changed
            }
        }

        public void StopHost()
        {
            _listener?.Stop();
            foreach (var client in _clients.Values)
            {
                client.Close();
            }
            _clients.Clear();
            Debug.WriteLine("Host stopped.");
        }

        public async Task StartClient(int port)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(IPAddress.Loopback, port);
                Debug.WriteLine("Connected to host");

                // Start listening to messages from the server
                _ = Task.Run(() => ListenToServer(_client));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error connecting to server: {ex.Message}");
            }
        }

        private async Task ListenToServer(TcpClient client)
        {
            try
            {
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream);
                using StreamWriter writer = new(stream) { AutoFlush = true };

                // Read the initial client ID message from the server
                string initialMessage = await reader.ReadLineAsync();
                if (initialMessage != null && initialMessage.StartsWith("ID:"))
                {
                    _clientID = double.Parse(initialMessage.Substring(3)); // Extract and store client ID
                    Debug.WriteLine($"Received Client ID: {_clientID}");
                }

                // Receive initial shapes from the server
                while (true)
                {
                    var receivedData = await reader.ReadLineAsync();
                    if (receivedData == null) continue;

                    Debug.WriteLine($"Received data from server: {receivedData}");
                    await ProcessReceivedData(receivedData, -1); // -1 indicates the server
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Client communication error: {ex.Message}");
            }
        }

        public void StopClient()
        {
            _client?.Close();
            Debug.WriteLine("Client disconnected.");
        }

        public async Task BroadcastShapeData(string shapeData, double senderUserID)
        {
            byte[] dataToSend = System.Text.Encoding.UTF8.GetBytes(shapeData + "\n");

            // Broadcast to clients (server mode)
            if (_listener != null)
            {
                foreach (var kvp in _clients)
                {
                    var userId = kvp.Key;
                    var client = kvp.Value;
                    if (userId != senderUserID)
                    {
                        try
                        {
                            NetworkStream stream = client.GetStream();
                            await stream.WriteAsync(dataToSend, 0, dataToSend.Length);
                            await stream.FlushAsync();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error sending data to client {userId}: {ex.Message}");
                        }
                    }
                }
            }

            // Send to server (client mode)
            if (_client != null && senderUserID == _clientID)
            {
                try
                {
                    NetworkStream stream = _client.GetStream();
                    await stream.WriteAsync(dataToSend, 0, dataToSend.Length);
                    await stream.FlushAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error sending data to server: {ex.Message}");
                }
            }
        }
    }
}
