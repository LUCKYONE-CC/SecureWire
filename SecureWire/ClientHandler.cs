using Newtonsoft.Json;
using SecureWire.Cryptography;
using SecureWire.Models;
using System.Net.Sockets;
using System.Text;

public class ClientHandler : TcpClient
{
    private Client client;

    public ClientHandler()
    {
        client = new Client();
        client.TcpClient = new TcpClient();
    }
    public new void Connect(string hostname, int port)
    {
        try
        {
                client.TcpClient.Connect(hostname, port);
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
            NetworkStream stream = client.TcpClient.GetStream();

            while (true)
            {
                byte[] buffer = new byte[4096];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Package<string> receivedPackage = JsonConvert.DeserializeObject<Package<string>>(message);

                if (client.SecureConnection == true)
                {
                    receivedPackage.Value = AES.Decrypt(client.AESKey, receivedPackage.Value);
                }

                MessageHandler(messageReceivedCallback, receivedPackage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Empfangen der Nachricht: {ex.Message}");
        }
    }
    public void SendMessage(string message, Flags flag)
    {
        try
        {
            if (client.TcpClient == null || !client.TcpClient.Connected)
            {
                throw new Exception("Client nicht verbunden.");
            }

            NetworkStream stream = client.TcpClient.GetStream();

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
            Console.WriteLine($"Fehler beim Senden der Nachricht an Server: {ex.Message}");
        }
    }
    private void MessageHandler(Action<Package<string>, string> messageReceivedCallback, Package<string> receivedPackage)
    {
        if(client.TcpClient.Client.RemoteEndPoint == null)
        {
            throw new Exception("Kein RemoteEndPoint vorhanden.");
        }

        switch(receivedPackage.FLAG)
        {
            case Flags.MESSAGE:
                string sender = ((System.Net.IPEndPoint)client.TcpClient.Client.RemoteEndPoint).Address.ToString();
                messageReceivedCallback?.Invoke(receivedPackage, sender);
                break;
            case Flags.PUBKEYFROMSERVER:
                client.AESKey = AES.GenerateRandomString(32);
                if (receivedPackage.Value == null)
                    throw new Exception("Kein Public-Key vom Server erhalten.");
                string encryptedAESKey = RSA.EncryptWithPublicKey(client.AESKey, receivedPackage.Value);
                client.PublicKey = receivedPackage.Value;
                SendMessage(encryptedAESKey, Flags.AESFORSERVER);
                break;
            case Flags.CONFIRMRECEPTION:
                bool validationSuccessStatus = ValidateSuccessfulKeyExchange(receivedPackage.Value, client.PublicKey);
                if(validationSuccessStatus == true)
                {
                    client.SecureConnection = true;
                    SendMessage("sdsd", Flags.SUCCESSFULKEYEXCHANGE);
                }
                else
                {
                    SendMessage("sdsd", Flags.CLOSECONNECTION);
                    client.TcpClient.Close();
                    throw new Exception("Fehler bei der Validierung des Schlüsselaustausches.");
                }
                break;
            default:
                Console.WriteLine($"Ungültiger Flag-Wert: {receivedPackage.FLAG}");
                break;
        }
    }
    private bool ValidateSuccessfulKeyExchange(string encryptedPrivateKey, string publicKey)
    {
        //string decryptedPrivateKey = AES.Decrypt(client.AESKey, encryptedPrivateKey);

        //string validationString = "VALIDATION";
        //string encryptedValidationString = RSA.EncryptWithPublicKey(validationString, publicKey);
        //string decryptedValidationString = RSA.DecryptWithPrivateKey(encryptedValidationString, decryptedPrivateKey);

        //if(validationString != decryptedValidationString)
        //{
        //    return false;
        //}

        return true;
    }
}