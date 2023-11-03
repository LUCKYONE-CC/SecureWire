using System.Net;
using System.Net.Sockets;
using System.Text;

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
                SendMessageToClient(_connectedClients.First(), message, Flags.MESSAGE);
                return;
            }

            foreach (var client in _connectedClients)
            {
                try
                {
                    SendMessageToClient(client, message, Flags.MESSAGE);
                }
                catch (Exception)
                {
                    // Fehler beim Senden
                }
            }
        }

        public void SendMessageToClient(TcpClient client, string message, Flags flag)
        {
            Package<string> package = new Package<string>
            {
                FLAG = flag,
                Value = message
            };

            // Wandeln Sie das Package in ein Byte-Array um, um es zu senden
            byte[] flagBytes = new byte[] { (byte)package.FLAG };
            byte[] valueBytes = Encoding.ASCII.GetBytes(package.Value);

            byte[] buffer = new byte[flagBytes.Length + valueBytes.Length];
            Array.Copy(flagBytes, buffer, flagBytes.Length);
            Array.Copy(valueBytes, 0, buffer, flagBytes.Length, valueBytes.Length);

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

        private void SendPublicKey(TcpClient tcpClient, string publicKey)
        {

        }
    }
}
