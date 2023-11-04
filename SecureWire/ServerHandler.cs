using System.Net;
using System.Net.Sockets;
using System.Text;
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

                    SecureWire(client);

                    string clientAddress = ((System.Net.IPEndPoint)client.TcpClient.Client.RemoteEndPoint).Address.ToString();

                    Console.WriteLine($"Client {clientAddress} hat sich erfolgreich verbunden.");

                    NetworkStream stream = client.TcpClient.GetStream();

                    Task receiveTask = ReceiveMessagesAsync(stream, messageReceivedCallback, client.TcpClient);
                }
            }
            catch (Exception)
            {
                // Fehler beim Akzeptieren der Verbindung
            }
        }

        private async Task ReceiveMessagesAsync(NetworkStream stream, Action<Package<string>, string> messageReceivedCallback, TcpClient client)
        {
            try
            {
                byte[] buffer = new byte[256];
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break; // Verbindung wurde geschlossen

                    if (bytesRead >= 1)
                    {
                        byte flagByte = buffer[0];
                        if (Enum.IsDefined(typeof(Flags), (int)flagByte))
                        {
                            Package<string> receivedPackage = new Package<string>
                            {
                                FLAG = (Flags)flagByte,
                                Value = Encoding.ASCII.GetString(buffer, 1, bytesRead - 1)
                            };

                            string sender = ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                            messageReceivedCallback?.Invoke(receivedPackage, sender);
                        }
                        else
                        {
                            Console.WriteLine($"Ungültiger Enum-Wert: {flagByte}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Nicht genügend Daten zum Lesen vorhanden.");
                    }
                }
            }
            catch (Exception)
            {
                // Fehler beim Lesen oder Verbindungsabbruch
            }
            finally
            {
                stream.Close();
                var clientToRemove = _connectedClients.FirstOrDefault(c => c.TcpClient == client);
                if(clientToRemove != null)
                    _connectedClients.Remove(clientToRemove);
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
                var tcpClient = _connectedClients.First().TcpClient;
                if(tcpClient == null)
                {
                    throw new Exception("Es ist kein Client verbunden.");
                }
                SendMessageToClient(tcpClient, message, Flags.MESSAGE);
                return;
            }

            foreach (var client in _connectedClients)
            {
                try
                {
                    if(client.TcpClient == null)
                        throw new Exception($"The client with the ID: {client.Id} has no TcpClient.");
                    SendMessageToClient(client.TcpClient, message, Flags.MESSAGE);
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

        private void SecureWire(Client client)
        {

        }
    }
}
