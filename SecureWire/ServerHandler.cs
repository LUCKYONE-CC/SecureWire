using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SecureWire
{
    public class ServerHandler : TcpListener
    {
        private TcpListener _tcpListener;
        private List<TcpClient> _connectedClients = new List<TcpClient>();
        private bool _allowMultipleConnections;

        public ServerHandler(IPAddress iPAddress, int port, bool allowMultipleConnections) : base(iPAddress, port)
        {
            _tcpListener = this;
            _allowMultipleConnections = allowMultipleConnections;
        }

        public async Task StartReceiving(Action<string, string> messageReceivedCallback)
        {
            try
            {
                while (true)
                {
                    if (!_allowMultipleConnections && _connectedClients.Count > 0)
                    {
                        // Wenn _allowMultipleConnections false ist und bereits eine Verbindung besteht,
                        // wird keine neue Verbindung akzeptiert.
                        continue;
                    }

                    TcpClient client = await _tcpListener.AcceptTcpClientAsync();
                    _connectedClients.Add(client);

                    string clientAddress = ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                    Console.WriteLine($"Client {clientAddress} hat sich erfolgreich verbunden.");

                    NetworkStream stream = client.GetStream();

                    Task receiveTask = ReceiveMessagesAsync(stream, messageReceivedCallback, client);
                }
            }
            catch (Exception)
            {

            }
        }


        private async Task ReceiveMessagesAsync(NetworkStream stream, Action<string, string> messageReceivedCallback, TcpClient client)
        {
            try
            {
                byte[] buffer = new byte[256];
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break; // Verbindung wurde geschlossen

                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    string sender = ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    messageReceivedCallback?.Invoke(message, sender);
                }
            }
            catch (Exception)
            {
                // Fehler beim Lesen oder Verbindungsabbruch
            }
            finally
            {
                stream.Close();
                _connectedClients.Remove(client);
            }
        }

        public void SendMessageToClients(string message)
        {
            if(_connectedClients.Count == 0)
            {
                throw new Exception("Es sind keine Clients verbunden.");
            }
            if (!_allowMultipleConnections)
            {
                SendMessageToClient(_connectedClients.First(), message);
                return;
            }

            byte[] buffer = Encoding.ASCII.GetBytes(message);
            foreach (var client in _connectedClients)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(buffer, 0, buffer.Length);
                }
                catch (Exception)
                {
                    // Fehler beim Senden
                }
            }
        }

        public void SendMessageToClient(TcpClient client, string message)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            try
            {
                NetworkStream stream = client.GetStream();
                stream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception)
            {
                // Fehler beim Senden
            }
        }
    }
}
