using Newtonsoft.Json;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WhiteboardGUI.Models;
using System.ComponentModel;
using System.Collections.Concurrent;

namespace WhiteboardGUI.ViewModel
{
    public class MainPageViewModel :INotifyPropertyChanged
    {
        private TcpListener listener;
        private TcpClient client;
        private ConcurrentDictionary<double, TcpClient> clients = new();
        public List<IShape> synchronizedShapes = new List<IShape>(); // Keeps track of all shapes on the whiteboard
        private double clientID;
        public event Action<IShape> ShapeReceived; // Event for shape received


        public MainPageViewModel() { }



        public async Task StartHost()
        {
            StartServer();
        }

        private async Task StartServer()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, 5000);
                listener.Start();
                Debug.WriteLine("Host started, waiting for clients...");
                double _currentUserID = 1;

                while (true)
                {
                    TcpClient newClient = await listener.AcceptTcpClientAsync();
                    clients.TryAdd(_currentUserID, newClient);
                    Debug.WriteLine($"Client connected! Assigned ID: {_currentUserID}");

                    // Send the client ID to the newly connected client
                    NetworkStream stream = newClient.GetStream();
                    StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
                    await writer.WriteLineAsync($"ID:{_currentUserID}");

                    _currentUserID++;
                    _ = Task.Run(() => ListenClients(newClient, 5000, _currentUserID - 1));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Host error: {ex.Message}");
            }
        }

        private async Task ListenClients(TcpClient client, int port, double senderUserID)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    // Send the current whiteboard state (all shapes) to the new client
                    foreach (var shape in synchronizedShapes)
                    {
                        string serializedShape = SerializeShape(shape);
                        await writer.WriteLineAsync(serializedShape);
                    }

                    while (true)
                    {
                        var receivedData = await reader.ReadLineAsync();
                        if (receivedData == null) continue;

                        Debug.WriteLine($"Received data: {receivedData}");
                        BroadcastShapeData(receivedData, senderUserID);
                        var shape = DeserializeShape(receivedData);
                        if (shape != null)
                        {
                            // Use Dispatcher to call DrawReceivedShape on the UI thread
                            Debug.Write("Shape Received");
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

        public void HostCheckBox_Unchecked()
        {
            listener?.Stop();
            clients.Clear();
        }
        public async Task StartClient(int port)
        {
            client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            Console.WriteLine("Connected to host");

            clients.TryAdd(0, client);
            _ = Task.Run(() => RunningClient(client)); // Start listening to the host
        }

        public async void BroadcastShapeData(string shapeData, double senderUserID)
        {
            byte[] dataToSend = System.Text.Encoding.UTF8.GetBytes(shapeData + "\n");

            foreach (var kvp in clients)
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

        private async Task RunningClient(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    // Read the initial client ID message from the server
                    string initialMessage = await reader.ReadLineAsync();
                    if (initialMessage != null && initialMessage.StartsWith("ID:"))
                    {
                        clientID = double.Parse(initialMessage.Substring(3)); // Extract and store client ID
                        Debug.WriteLine($"Received Client ID: {clientID}");
                    }

                    // Listen for further shape data from the server
                    while (true)
                    {
                        var receivedData = await reader.ReadLineAsync();

                        if (receivedData == null) continue; // Continue if no data is received

                        Debug.WriteLine($"Received data: {receivedData}");

                        var shape = DeserializeShape(receivedData);
                        if (shape != null)
                        {
                            // Use Dispatcher to call DrawReceivedShape on the UI thread
                            ShapeReceived?.Invoke(shape);
                        }
                    }
                }
            }
            catch (IOException ioEx)
            {
                Debug.WriteLine($"IO error while communicating with client: {ioEx.Message}");
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
                    foreach (var kvp in clients)
                    {
                        if (kvp.Value == client)
                        {
                            clients.TryRemove(kvp);
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

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        internal void ClientCheckBox_Unchecked()
        {
            client?.Close();
        }

        private string SerializeShape(IShape shape)
        {
            // Serialize the shape object to JSON format
            //return Newtonsoft.Json.JsonConvert.SerializeObject(shape);
            return JsonConvert.SerializeObject(shape);
        }
        private IShape DeserializeShape(string data)
        {
            // Deserialize the shape based on its type
            var shapeDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            string shapeType = shapeDict["ShapeType"].ToString();

            switch (shapeType)
            {
                case "Circle":
                    return JsonConvert.DeserializeObject<CircleShape>(data);
                case "Line":
                    return JsonConvert.DeserializeObject<LineShape>(data);
                case "Scribble":
                    return JsonConvert.DeserializeObject<ScribbleShape>(data);
                default:
                    throw new NotSupportedException("Shape type not supported");
            }
        }

        internal void SerilaizeAndBroadcastShapeData(IShape shapeToSend)
        {
            string serializedShape = SerializeShape(shapeToSend);
            Console.WriteLine("Broadcasting shape data: " + serializedShape);
            BroadcastShapeData(serializedShape, -1);
        }


    }

}
