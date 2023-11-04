using System.Security.Cryptography;
using System.Text;

namespace SecureWire.Cryptography
{
    public class RSA
    {
        private RSAParameters _privateKey;
        private RSAParameters _publicKey;

        public RSA()
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                _privateKey = rsa.ExportParameters(true);
                _publicKey = rsa.ExportParameters(false);
            }
        }

        public string Encrypt(string plainText, RSAParameters key)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(key);
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = rsa.Encrypt(plainBytes, false);
                return Convert.ToBase64String(encryptedBytes);
            }
        }

        public string EncryptWithPublicKey(string plainText)
        {
            return Encrypt(plainText, _publicKey);
        }

        public string Decrypt(string cipherText, RSAParameters key)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(key);
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] decryptedBytes = rsa.Decrypt(cipherBytes, false);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }

        public string DecryptWithPrivateKey(string cipherText)
        {
            return Decrypt(cipherText, _privateKey);
        }
    }
}
