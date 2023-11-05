using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using SecureWire.Cryptography;
using SecureWire.Models;

namespace SecureWire
{
    public class ServerHandler : TcpListener
    {
        private TcpListener _tcpListener;
        private List<Client> _connectedClients = new List<Client>();
        private bool _allowMultipleConnections;

        public ServerHandler(IPAddress iPAddress, int port, bool allowMultipleConnections) : base(iPAddress, port)
        {
            _tcpListener = this;
            _allowMultipleConnections = allowMultipleConnections;
        }

        public async Task StartReceiving(Action<Package<string>, string> messageReceivedCallback)
        {
            try
            {
                while (true)
                {
                    if (!_allowMultipleConnections && _connectedClients.Count > 0)
                    {
                        continue;
                    }

                    TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync();
                    var (privateKey, publicKey) = RSA.GenerateKeys();
                    var client = new Client { TcpClient = tcpClient, PrivateKey = privateKey, PublicKey = publicKey };
                    _connectedClients.Add(client);

                    string clientAddress = ((System.Net.IPEndPoint)client.TcpClient.Client.RemoteEndPoint).Address.ToString();

                    SecureWire(client);

                    Console.WriteLine($"Client {clientAddress} hat sich erfolgreich verbunden.");

                    NetworkStream stream = client.TcpClient.GetStream();

                    Task receiveTask = ReceiveMessagesAsync(stream, messageReceivedCallback, client.TcpClient);
                }
            }
            catch (Exception)
            {

            }
        }

        public async Task ReceiveMessagesAsync(NetworkStream stream, Action<Package<string>, string> messageReceivedCallback, TcpClient tcpClient)
        {
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Package<string> receivedPackage = JsonConvert.DeserializeObject<Package<string>>(message);

                    MessageHandler(messageReceivedCallback, receivedPackage, tcpClient);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Empfangen der Nachricht: {ex.Message}");
            }
        }

        public void SendMessageToClients(string message)
        {
            if (_connectedClients.Count == 0)
            {
                throw new Exception("Es sind keine Clients verbunden.");
            }
            if (!_allowMultipleConnections)
            {
                var client = _connectedClients.First();
                if (client.TcpClient == null)
                {
                    throw new Exception("Es ist kein Client verbunden.");
                }
                SendMessageToClient(client, message, Flags.MESSAGE);
                return;
            }

            foreach (var client in _connectedClients)
            {
                try
                {
                    if (client.TcpClient == null)
                        throw new Exception($"The client with the ID: {client.Id} has no TcpClient.");
                    SendMessageToClient(client, message, Flags.MESSAGE);
                }
                catch (Exception)
                {

                }
            }
        }

        public void SendMessageToClient(Client client, string message, Flags flag)
        {
            try
            {
                if (client.TcpClient == null || !client.TcpClient.Connected)
                {
                    throw new Exception("Client nicht verbunden.");
                }

                NetworkStream stream = client.TcpClient.GetStream();

                if (client.SecureConnection)
                {
                    message = AES.Encrypt(client.AESKey, message);
                }

                Package<string> package = new Package<string>
                {
                    FLAG = flag,
                    Value = message
                };

                byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(package));
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Senden der Nachricht an Client: {ex.Message}");
            }
        }

        private void MessageHandler(Action<Package<string>, string>? messageReceivedCallback, Package<string> receivedPackage, TcpClient tcpClient)
        {
            var client = _connectedClients.FirstOrDefault(c => c.TcpClient == tcpClient);
            if (client == null)
            {
                throw new Exception("Client nicht gefunden.");
            }
            if (client.TcpClient == null)
            {
                throw new Exception("Kein TcpClient vorhanden.");
            }
            if (client.TcpClient.Client.RemoteEndPoint == null)
            {
                throw new Exception("Kein RemoteEndPoint vorhanden.");
            }

            switch (receivedPackage.FLAG)
            {
                case Flags.PUBKEYFROMSERVER:
                    if (client.PublicKey == null)
                        throw new Exception("Client hat keinen PublicKey.");
                    SendMessageToClient(client, client.PublicKey, Flags.PUBKEYFROMSERVER);
                    break;
                case Flags.AESFORSERVER:
                    if (receivedPackage.Value == null)
                        throw new Exception("Client hat keinen AESKey gesendet.");
                    string encryptedAESKey = receivedPackage.Value;
                    string decryptedAESKey = RSA.DecryptWithPrivateKey(encryptedAESKey, client.PrivateKey);
                    client.AESKey = decryptedAESKey;
                    SendMessageToClient(client, AES.Encrypt(client.AESKey, client.PrivateKey), Flags.CONFIRMRECEPTION);
                    break;
                case Flags.CONFIRMRECEPTION:
                    break;
                case Flags.SUCCESSFULKEYEXCHANGE:
                    client.SecureConnection = true;
                    break;

                case Flags.MESSAGE:
                    string sender = ((System.Net.IPEndPoint)client.TcpClient.Client.RemoteEndPoint).Address.ToString();
                    messageReceivedCallback?.Invoke(receivedPackage, sender);
                    break;
                case Flags.CLOSECONNECTION:
                    client.TcpClient.Close();
                    break;
                default:
                    Console.WriteLine($"Ungültiger Flag-Wert: {receivedPackage.FLAG}");
                    break;
            }
        }
        private void SecureWire(Client client)
        {
            (string publicKey, string privateKey) = RSA.GenerateKeys();
            client.PublicKey = publicKey;
            client.PrivateKey = privateKey;
            MessageHandler(null, new Package<string>() { FLAG = Flags.PUBKEYFROMSERVER }, client.TcpClient);
        }
    }
}