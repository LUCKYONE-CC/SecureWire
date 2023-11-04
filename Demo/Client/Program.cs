using System.Net.Sockets;
using SecureWire.Models;

namespace SecureWireClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ClientHandler clientHandler = new ClientHandler();
                clientHandler.Connect("127.0.0.1", 12345);

                clientHandler.StartReceiving((message, sender) =>
                {
                    Console.WriteLine($"Nachricht von {sender}: {message.Value}");
                });

                while (true)
                {
                    string userInput = Console.ReadLine();
                    clientHandler.SendMessage(userInput, Flags.MESSAGE);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
