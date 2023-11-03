﻿using SecureWire;
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
