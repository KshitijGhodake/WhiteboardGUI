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
using Networking.Communication;
using Networking;
namespace WhiteboardGUI.Services
{
    public class NetworkingService : INotificationHandler
    {
        public double _clientID;
        private bool _isHost;
        public ICommunicator? _communicator;
        public ReceivedDataService _receivedDataService;
        public List<IShape> _synchronizedShapes;

        public NetworkingService(ReceivedDataService dataTransferService)
        {
            _receivedDataService = dataTransferService;
            _synchronizedShapes = dataTransferService._synchronizedShapes;
        }

        public void StartHost()
        {
            StartHostServer();
        }

        private string _moduleIdentifier = "WhiteBoard";
        private void StartHostServer()
        {
            try
            {
                _communicator = CommunicationFactory.GetCommunicator(false);
                _communicator.Subscribe(_moduleIdentifier, this, false);
                _communicator.Start();
                _isHost = true;

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Host error: {ex.Message}");
            }
        }

        public void StartClient()
        {
            StartClientServer();
        }

        private void StartClientServer()
        {
            try
            {
                _communicator = CommunicationFactory.GetCommunicator(true);
                _communicator.Subscribe(_moduleIdentifier, this, false);
                _communicator.Start();
                _isHost = false;

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Host error: {ex.Message}");
            }
        }

        public void OnDataReceived(string serializedData)
        {
            if (_isHost)
            {
                int id = _receivedDataService.DataReceived(serializedData);
                if (id == -1) return;
                BroadcastShapeData(serializedData);

            }
            else
            {
                int id = _receivedDataService.DataReceived(serializedData);
            }
        }

        /// I have to ensure that when a new client joins he receives all the shapes. All Client IDs?

        public void BroadcastShapeData(string serializedData)
        {
            _communicator.Send(serializedData, _moduleIdentifier, null);
        }

        public void StopHost()
        {
            _communicator?.Stop();
        }
        public void StopClient()
        {
            _communicator?.Stop();
        }
    }
}
