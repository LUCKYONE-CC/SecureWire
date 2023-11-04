using System.Net.Sockets;

namespace SecureWire.Models
{
    public class Client
    {
        public int Id { get; set; }
        public TcpClient? TcpClient { get; set; }
        public string? PrivateKey { get; set; }
        public string? PublicKey { get; set; }
        public string? AESKey { get; set; }
        public bool SecureConnection { get; set; } = false;
    }
}
