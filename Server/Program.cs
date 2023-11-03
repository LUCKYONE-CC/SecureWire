using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SecureWire;

namespace Server
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            TcpListener server = null;
            ServerHandler serverHandler = new ServerHandler();

            try
            {
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
                int port = 12345;

                server = new TcpListener(ipAddress, port);
                server.Start();

                Console.WriteLine("Server gestartet. Warte auf Verbindung...");

                serverHandler.Initialize(server);

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
                server?.Stop();
            }
        }
    }
}
