using SecureWire;
using System.Net.Sockets;
using System.Text;

public class ClientHandler : TcpClient
{
    private TcpClient _tcpClient;

    public ClientHandler()
    {
        _tcpClient = new TcpClient();
    }
    public new void Connect(string hostname, int port)
    {
        try
        {
            _tcpClient.Connect(hostname, port);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Verbinden: {ex.Message}");
        }
    }

    public void StartReceiving(Action<Package<string>, string> messageReceivedCallback)
    {
        try
        {
            NetworkStream stream = _tcpClient.GetStream();

            Thread receiveThread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        byte[] buffer = new byte[256];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
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

                                MessageHandler(messageReceivedCallback, receivedPackage);
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
                catch (Exception ex)
                {
                    Console.WriteLine($"Fehler beim Lesen oder Verbindungsabbruch: {ex.Message}");
                }
                finally
                {
                    _tcpClient.Close();
                }
            });

            receiveThread.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Akzeptieren der Verbindung: {ex.Message}");
        }
    }
    public void SendMessage(string message, Flags flag)
    {
        try
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


            NetworkStream stream = _tcpClient.GetStream();
            stream.Write(buffer, 0, buffer.Length);

            // Keine Notwendigkeit, die Verbindung sofort zu schließen
        }
        catch (Exception)
        {
            // Fehler beim Senden
        }
    }
    private void MessageHandler(Action<Package<string>, string> messageReceivedCallback, Package<string> receivedPackage)
    {
        if(_tcpClient.Client.RemoteEndPoint == null)
        {
            throw new Exception("Kein RemoteEndPoint vorhanden.");
        }

        switch(receivedPackage.FLAG)
        {
            case Flags.MESSAGE:
                string sender = ((System.Net.IPEndPoint)_tcpClient.Client.RemoteEndPoint).Address.ToString();
                messageReceivedCallback?.Invoke(receivedPackage, sender);
                break;
            default:
                Console.WriteLine($"Ungültiger Flag-Wert: {receivedPackage.FLAG}");
                break;
        }
    }
}
