using Newtonsoft.Json;
using SecureWire.Cryptography;
using SecureWire.Exceptions;
using SecureWire.Models;
using System.Net.Sockets;
using System.Text;

namespace SecureWire
{
    public static class ServerClientShared
    {
        public static void SendPackage(Client client, string message, Flags flag)
        {
            try
            {
                if (client.TcpClient == null || !client.TcpClient.Connected)
                {
                    throw new ClientNotConnectedException();
                }

                NetworkStream stream = client.TcpClient.GetStream();

                if (client.SecureConnection)
                {
                    message = AES.Encrypt(client.AESKey, message);
                }

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
                throw new Exception($"Error while attempting to send a message to the client with the ÍD: {client.Id}", innerException: ex);
            }
        }
    }
}
