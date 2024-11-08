using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using WhiteboardGUI.Models;

namespace WhiteboardGUI.Services
{
    public class NetworkingService
    {
        private TcpListener _listener;
        private TcpClient _client;
        private ConcurrentDictionary<double, TcpClient> _clients = new();
        public double _clientID;
        public List<IShape> _synchronizedShapes = new();

        public event Action<IShape> ShapeReceived; // Event for shape received
        public event Action<IShape> ShapeDeleted;
        public event Action<IShape> ShapeModified;
        //public NetworkingService(List<IShape> synchronizedShapes)
        //{
        //    _synchronizedShapes = synchronizedShapes;
        //}

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

                    currentUserID++;
                    _ = Task.Run(() => ListenClients(newClient, currentUserID - 1));
                    //Send all existing shapes to new clients
                    foreach (var shape in _synchronizedShapes)
                    {

                        string serializedShape = SerializationService.SerializeShape(shape);
                        await BroadcastShapeData(serializedShape, -1);

                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Host error: {ex.Message}");
            }
        }

        private async Task ListenClients(TcpClient client, double senderUserID)
        {
            try
            {
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream);
                using StreamWriter writer = new(stream) { AutoFlush = true };

                // Send the current whiteboard state (all shapes) to the new client
                foreach (var shape in _synchronizedShapes)
                {

                    string serializedShape = SerializationService.SerializeShape(shape);
                    string serializedMessage = $"CREATE:{serializedShape}";
                    await writer.WriteLineAsync(serializedMessage);
                }

                while (true)
                {
                    var receivedData = await reader.ReadLineAsync();
                    if (receivedData == null) continue;

                    Debug.WriteLine($"Received data: {receivedData}");
                    await BroadcastShapeData(receivedData, senderUserID);

                    if (receivedData.StartsWith("DELETE:"))
                    {
                        string data = receivedData.Substring(7);
                        var shape = SerializationService.DeserializeShape(data);

                        if (shape != null)
                        {
                            var shapeId = shape.ShapeId;
                            var shapeUserId = shape.UserID;

                            var currentShape = _synchronizedShapes.Where(s => s.ShapeId == shapeId && s.UserID == shapeUserId).FirstOrDefault();
                            if (currentShape != null)
                            {
                                ShapeDeleted?.Invoke(currentShape);

                            }
                        }
                    }
                    else if (receivedData.StartsWith("MODIFY:"))
                    {
                        string data = receivedData.Substring(7);
                        var shape = SerializationService.DeserializeShape(data);

                        if (shape != null)
                        {
                            var shapeId = shape.ShapeId;
                            var shapeUserId = shape.UserID;

                            var currentShape = _synchronizedShapes.Where(s => s.ShapeId == shapeId && s.UserID == shapeUserId).FirstOrDefault();
                            if (currentShape != null)
                            {
                                ShapeDeleted?.Invoke(currentShape);
                                ShapeModified?.Invoke(shape);

                            }

                        }
                    }
                    else if (receivedData.StartsWith("CREATE:"))
                    {
                        string data = receivedData.Substring(7);
                        var shape = SerializationService.DeserializeShape(data);
                        if (shape != null)
                        {
                            ShapeReceived?.Invoke(shape);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ListenClients: {ex}");
            }
        }

        public void StopHost()
        {
            _listener?.Stop();
            _clients.Clear();
        }

        public async Task StartClient(int port)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(IPAddress.Loopback, port);
            Console.WriteLine("Connected to host");

            _clients.TryAdd(0, _client);
            _ = Task.Run(() => RunningClient(_client));
        }

        private async Task RunningClient(TcpClient client)
        {
            try
            {
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream);

                // Read the initial client ID message from the server
                string initialMessage = await reader.ReadLineAsync();
                if (initialMessage != null && initialMessage.StartsWith("ID:"))
                {
                    _clientID = double.Parse(initialMessage.Substring(3)); // Extract and store client ID
                    Debug.WriteLine($"Received Client ID: {_clientID}");
                }

                // Listen for further shape data from the server
                while (true)
                {
                    var receivedData = await reader.ReadLineAsync();

                    if (receivedData == null) continue;

                    Debug.WriteLine($"Received data: {receivedData}");
                    if (receivedData.StartsWith("DELETE:"))
                    {
                        string data = receivedData.Substring(7);
                        var shape = SerializationService.DeserializeShape(data);

                        if (shape != null)
                        {
                            var shapeId = shape.ShapeId;
                            var shapeUserId = shape.UserID;

                            var currentShape = _synchronizedShapes.Where(s => s.ShapeId == shapeId && s.UserID == shapeUserId).FirstOrDefault();
                            if (currentShape != null)
                            {
                                ShapeDeleted?.Invoke(currentShape);

                            }
                        }
                    }
                    else if (receivedData.StartsWith("MODIFY:"))
                    {
                        string data = receivedData.Substring(7);
                        var shape = SerializationService.DeserializeShape(data);

                        if (shape != null)
                        {
                            var shapeId = shape.ShapeId;
                            var shapeUserId = shape.UserID;

                            var currentShape = _synchronizedShapes.Where(s => s.ShapeId == shapeId && s.UserID == shapeUserId).FirstOrDefault();
                            if (currentShape != null)
                            {
                                ShapeDeleted?.Invoke(currentShape);
                                ShapeModified?.Invoke(shape);

                            }

                        }
                    }
                    else if (receivedData.StartsWith("CREATE:"))
                    {
                        string data = receivedData.Substring(7);
                        var shape = SerializationService.DeserializeShape(data);
                        if (shape != null)
                        {
                            ShapeReceived?.Invoke(shape);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Client communication error: {ex.Message}");
            }
            finally
            {
                if (client != null)
                {
                    // Remove client from the dictionary safely
                    foreach (var kvp in _clients)
                    {
                        if (kvp.Value == client)
                        {
                            _clients.TryRemove(kvp);
                            break;
                        }
                    }
                    client.Close();
                    Debug.WriteLine("Client disconnected.");
                }
                else
                {
                    Debug.WriteLine("Client was null, no action taken.");
                }
            }
        }

        public void StopClient()
        {
            _client?.Close();
        }

        public async Task BroadcastShapeData(string shapeData, double senderUserID)
        {
            byte[] dataToSend = System.Text.Encoding.UTF8.GetBytes(shapeData + "\n");

            foreach (var kvp in _clients)
            {
                var userId = kvp.Key;
                var client = kvp.Value;
                if (kvp.Key != senderUserID)
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
    }
}