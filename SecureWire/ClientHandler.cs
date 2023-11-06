﻿using Newtonsoft.Json;
using SecureWire.Cryptography;
using SecureWire.Exceptions;
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
            if (client.TcpClient == null)
                throw new ClientNotConnectedException();
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
            Task.Run(() =>
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

                    Package<string>? receivedPackage = JsonConvert.DeserializeObject<Package<string>>(message);

                    if(receivedPackage.FLAG == null)
                    {
                        throw new PackageFlagIsNullException();
                    }

                    if (client.SecureConnection == true)
                    {
                        receivedPackage.Value = AES.Decrypt(client.AESKey, receivedPackage.Value);
                    }

                    MessageHandler(messageReceivedCallback, receivedPackage);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Empfangen der Nachricht: {ex.Message}");
        }
    }
    public void SendMessageToServer(string message)
    {
        SendPackageToServer(message, Flags.MESSAGE);
    }
    private void SendPackageToServer(string message, Flags flag)
    {
        try
        {
            if (client.TcpClient == null || !client.TcpClient.Connected)
            {
                throw new ClientNotConnectedException();
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
        if (client.TcpClient == null)
            throw new ClientNotConnectedException();

        switch (receivedPackage.FLAG)
        {
            case Flags.MESSAGE:
                if (client.TcpClient.Client.RemoteEndPoint == null)
                {
                    throw new Exception("Kein RemoteEndPoint vorhanden.");
                }
                string sender = ((System.Net.IPEndPoint)client.TcpClient.Client.RemoteEndPoint).Address.ToString();
                messageReceivedCallback?.Invoke(receivedPackage, sender);
                break;
            case Flags.PUBKEYFROMSERVER:
                client.AESKey = AES.GenerateRandomString(32);
                if (receivedPackage.Value == null)
                    throw new Exception("Kein Public-Key vom Server erhalten.");
                string encryptedAESKey = RSA.EncryptWithPublicKey(client.AESKey, receivedPackage.Value);
                client.PublicKey = receivedPackage.Value;
                SendPackageToServer(encryptedAESKey, Flags.AESFORSERVER);
                break;
            case Flags.CONFIRMRECEPTION:
                bool validationSuccessStatus = ValidateSuccessfulKeyExchange(receivedPackage.Value, client.PublicKey);
                if(validationSuccessStatus == true)
                {
                    client.SecureConnection = true;
                    SendPackageToServer("SUCCESSFULKEYEXCHANGE", Flags.SUCCESSFULKEYEXCHANGE);
                }
                else
                {
                    SendPackageToServer("CLOSECONNECTION", Flags.CLOSECONNECTION);
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