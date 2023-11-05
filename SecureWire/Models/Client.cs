using System.Net.Sockets;

namespace SecureWire.Models
{
    public class Client
    {
        public int Id { get; set; }
        public TcpClient? TcpClient { get; set; }
        public string PrivateKey { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string AESKey { get; set; } = string.Empty;
        public bool SecureConnection { get; set; } = false;
    }
}
