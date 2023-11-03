using System;
using System.Net.Sockets;
using SecureWire;

namespace SecureWireClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                TcpClient client = new TcpClient();
                client.Connect("127.0.0.1", 12345);

                ClientHandler clientHandler = new ClientHandler();
                clientHandler.Initialize(client);

                clientHandler.StartReceiving((message, sender) =>
                {
                    Console.WriteLine($"Nachricht von {sender}: {message}");
                });

                while (true)
                {
                    string userInput = Console.ReadLine();
                    clientHandler.SendMessage(userInput);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
