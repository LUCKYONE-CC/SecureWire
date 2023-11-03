using System;
using System.Net.Sockets;
using System.Text;

public class ClientHandler
{
    private TcpClient _tcpClient;

    public void Initialize(TcpClient tcpClient)
    {
        _tcpClient = tcpClient;
    }

    public void StartReceiving(Action<string, string> messageReceivedCallback)
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

                        string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        string sender = ((System.Net.IPEndPoint)_tcpClient.Client.RemoteEndPoint).Address.ToString();
                        messageReceivedCallback?.Invoke(message, sender);
                    }
                }
                catch (Exception)
                {
                    // Fehler beim Lesen oder Verbindungsabbruch
                }
                finally
                {
                    _tcpClient.Close();
                }
            });

            receiveThread.Start();
        }
        catch (Exception)
        {
            // Fehler beim Akzeptieren der Verbindung
        }
    }

    public void SendMessage(string message)
    {
        try
        {
            NetworkStream stream = _tcpClient.GetStream();
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            stream.Write(buffer, 0, buffer.Length);

            // Keine Notwendigkeit, die Verbindung sofort zu schließen
        }
        catch (Exception)
        {
            // Fehler beim Senden
        }
    }
}
