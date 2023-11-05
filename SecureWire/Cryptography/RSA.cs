using System.Security.Cryptography;
using System.Text;

namespace SecureWire.Cryptography
{
    public class RSA
    {
        public static (string publicKey, string privateKey) GenerateKeys()
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                string publicKey = rsa.ToXmlString(false);
                string privateKey = rsa.ToXmlString(true);
                return (publicKey, privateKey);
            }
        }

        public static string EncryptWithPublicKey(string plainText, string publicKey)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKey);
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = rsa.Encrypt(plainBytes, false);
                return Convert.ToBase64String(encryptedBytes);
            }
        }

        public static string DecryptWithPrivateKey(string cipherText, string privateKey)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKey);
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] decryptedBytes = rsa.Decrypt(cipherBytes, false);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }
        public static byte[] RSAEncrypt(string publicKey, byte[] data)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKey);
                return rsa.Encrypt(data, false);
            }
        }

        public static byte[] RSADecrypt(string privateKey, byte[] data)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKey);
                return rsa.Decrypt(data, false);
            }
        }

    }
}
