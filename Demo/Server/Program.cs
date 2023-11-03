using System.Net;
using SecureWire;

namespace Server
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            ServerHandler serverHandler = null;

            try
            {
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
                int port = 12345;

                serverHandler = new ServerHandler(ipAddress, port, false);
                serverHandler.Start();

                Console.WriteLine("Server gestartet. Warte auf Verbindung...");

                Task receivingTask = serverHandler.StartReceiving((message, sender) =>
                {
                    Console.WriteLine($"Client {sender}: {message}");
                });

                while (true)
                {
                    string input = Console.ReadLine();
                    serverHandler.SendMessageToClients(input);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                serverHandler?.Stop();
            }
        }
    }
}
